using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HarmonyLib;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using Sandbox.Graphics;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Marigold
{
    internal sealed partial class SchematicOverlay : IDisposable
    {
        private const string PatchId = "Marigold.Schematic.0.2";
        private const string Texture = "Textures\\GUI\\Blank.dds";
        private static SchematicOverlay active;
        private Harmony harmony;
        private MyCubeGrid grid;
        private GridObservation observation;
        private int observationRevision=-1;
        private Point3[] points = new Point3[0];
        private readonly DamagePulse damagePulse=new DamagePulse();
        private double[] pulseStarts=new double[0];
        private int[] shadedMarkers=new int[0];
        private readonly List<FillBand> fillBands=new List<FillBand>();
        private ChangeKind[] lineKinds = new ChangeKind[0];
        private bool[] drawLine=new bool[0];
        private int renderedLines,renderedFills,unmergedFills;
        private readonly FillRaster fillRaster=new FillRaster();
        private Point3[] rotated = new Point3[0];
        private bool dirty, faulted;
        private int cooldown, reconcile;
        private readonly SchematicControls controls = new SchematicControls();
        private IMyUtilities commandUtilities;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double lastTime;
        private string caption = "";
        private int classificationRevision=-1;
        private Task<BuildResult> pending;
        private int generation;
        private BlockSample[] lastObservation;
        private int lastObservationGeneration=-1;
        private sealed class Reference
        {
            public BlockSample[] Blocks;
            public string ProfileId;
            public AnchorFrame Anchor;
        }
        private ProfileStore profileStore;
        private string settingsPath;
        private readonly Dictionary<MyCubeGrid,Reference> references=new Dictionary<MyCubeGrid,Reference>();
        private readonly Dictionary<MyCubeGrid,bool> stationByGrid=new Dictionary<MyCubeGrid,bool>();
        private string comparisonReport="No reference captured.";
        private void ClearReferences()
        {
            CancelRecovery();CancelBlueprintPreview();blueprintLoad=null;blueprint=null;converted=null;blueprintAnchors.Clear();blueprintGrid=-1;
            manualDetached.Clear();ResetAssociation();
            foreach(var pair in references) pair.Key.OnGridSplit-=OnSplit;
            references.Clear();stationByGrid.Clear();stationBaselines.Clear(); generation++; comparisonReport="No reference captured.";
        }
        private void OnSplit(MyCubeGrid original,MyCubeGrid detached)
        {
            CancelRecovery();
            CancelBlueprintPreview();
            Reference reference;
            var controlled=Sandbox.Game.World.MySession.Static?.ControlledEntity?.Entity as IMyShipController;
            if(references.TryGetValue(original,out reference) && controlled!=null && controlled.CubeGrid==detached)
            {
                // SE splits preserve block-local coordinates and the grid world basis.
                Detach(original);references[detached]=reference;detached.OnGridSplit+=OnSplit;
                bool station;if(stationByGrid.TryGetValue(original,out station))stationByGrid[detached]=station;
            }
            generation++; dirty=true; cooldown=0;
        }
        private static BlockSample[] Sample(MyCubeGrid source)
        {
            var result=new List<BlockSample>();
            foreach(var block in source.GetBlocks())result.Add(GridObservation.Read(block));
            return result.ToArray();
        }
        private void RefreshReference(string message)
        { generation++;dirty=true;cooldown=0;comparisonReport=message; }
        private void Detach(MyCubeGrid target)
        { if(target==grid){CancelRecovery();CancelBlueprintPreview();} if(references.Remove(target)) target.OnGridSplit-=OnSplit; }
        private void BindProfile(ShipProfile profile,AnchorFrame anchor)
        {
            health=new ShipHealth();healthPreview=false;
            CancelRecovery();
            stationBaselines.Clear();
            var closed=new List<MyCubeGrid>();
            foreach(var pair in references)if(pair.Key.Closed || pair.Key.MarkedForClose)closed.Add(pair.Key);
            foreach(var old in closed)Detach(old);
            CancelBlueprintPreview();
            if(!references.ContainsKey(grid))grid.OnGridSplit+=OnSplit;
            controls.Station=profile.Station;stationByGrid[grid]=profile.Station;LoadShipSettings(profile.Id);
            references[grid]=new Reference { ProfileId=profile.Id,Anchor=anchor,Blocks=ProfileAlignment.Transform(profile.Blocks,anchor,false) };
            RefreshReference(profile.Blocks==null?"Profile anchored; use /mg capture.":"Comparison pending.");
        }
        private AnchorFrame SeatedAnchor()
        {
            var cockpit=Sandbox.Game.World.MySession.Static?.ControlledEntity?.Entity as MyCockpit;
            if(cockpit==null || cockpit.CubeGrid!=grid)throw new InvalidOperationException("Sit in the intended anchor cockpit first.");
            Matrix m;cockpit.Orientation.GetMatrix(out m);
            return new AnchorFrame { Position=new Point3(cockpit.Position.X,cockpit.Position.Y,cockpit.Position.Z),
                Right=new Point3(m.Right.X,m.Right.Y,m.Right.Z),Up=new Point3(m.Up.X,m.Up.Y,m.Up.Z),Backward=new Point3(m.Backward.X,m.Backward.Y,m.Backward.Z),
                Definition=cockpit.BlockDefinition.Id.ToString(),Name=cockpit.CustomName.ToString() };
        }
        private string ReferenceCommand(string action)
        {
            if(action=="profile force")return RecoveryCommand("force");
            if(action.StartsWith("recovery ",StringComparison.Ordinal))return RecoveryCommand(action.Substring(9));
            if(DisplayFilter.Contains("capture clear anchor",action) || action=="profile delete")CancelRecovery();
            if(action.StartsWith("language ",StringComparison.Ordinal))
            {
                string previous=controls.Language;controls.Language=action.Substring(9);
                try{DisplaySettings.Save(settingsPath,controls);}catch{controls.Language=previous;throw;}
                return "Interface language saved. Diagnostic messages remain in English.";
            }
            if(action.StartsWith("auto ",StringComparison.Ordinal))return AutomationCommand(action.Substring(5));
            if(action.StartsWith("station ",StringComparison.Ordinal))
            {
                if(action!="station status")
                {
                    if(grid==null)return "Control the grid whose station mode you want to change.";
                    bool enabled=action=="station on";Reference binding;
                    if(references.TryGetValue(grid,out binding))
                    {
                        var p=profileStore.Find(binding.ProfileId);
                        profileStore.Put(new ShipProfile {Id=p.Id,Name=p.Name,GridSize=p.GridSize,Anchor=p.Anchor,Blocks=p.Blocks,Station=enabled},true);
                    }
                    controls.Station=enabled;stationByGrid[grid]=enabled;
                }
                return "Station mode "+(controls.Station?"on":"off")+". "+stationStatus;
            }
            if(action.StartsWith("settings ",StringComparison.Ordinal))
            {
                if(action=="settings save")DisplaySettings.Save(settingsPath,controls);
                else if(action=="settings load")DisplaySettings.Load(settingsPath,controls);
                else DisplaySettings.Reset(controls);
                bool station;controls.Station=grid!=null && stationByGrid.TryGetValue(grid,out station) && station;
                ResetAssociation();
                return "Display settings "+(action=="settings save"?"saved. They will load on the next game launch.":action=="settings load"?"loaded.":"reset. Use /mg settings save to keep these defaults.");
            }
            if(action.StartsWith("blueprint ",StringComparison.Ordinal))return BlueprintCommand(action.Substring(10));
            if(action=="profiles")
            {
                var lines=new List<string>();
                if(profileStore.Warning!=null)lines.Add(profileStore.Warning);
                foreach(var p in profileStore.Profiles)lines.Add(p.Number+" | "+p.Name+" | "+(p.Blocks==null?"anchor only":p.Blocks.Length+" baseline blocks"));
                return lines.Count==0?"No saved profiles. Sit in a cockpit and use /mg anchor.":string.Join("\n",lines);
            }
            if(action=="recover") { profileStore.Recover();ClearReferences();RefreshReference("Select a restored profile.");return "Backup restored. Use /mg profile select ID from the matching cockpit."; }
            if(action=="profile delete")
            {
                var deleted=profileStore.Find(controls.ProfileArgument);
                profileStore.Delete(deleted.Id);stationBaselines.Clear();
                var detach=new List<MyCubeGrid>();
                foreach(var pair in references)if(pair.Value.ProfileId==deleted.Id)detach.Add(pair.Key);
                foreach(var target in detach)Detach(target);
                RefreshReference("Profile deleted.");return "Deleted profile "+deleted.Name+". Previous database retained in backup.";
            }
            if(grid==null || grid.Closed || grid.MarkedForClose) return "Control a ship first.";
            Reference selected;references.TryGetValue(grid,out selected);
            if(action=="report") return (recoveryPreview!=null?"UNSAVED recovery preview. ":previewBlocks==null?"":"UNSAVED blueprint preview. ")+(selected==null?"No profile selected. Use /mg profiles or /mg anchor. ":"Profile "+profileStore.Find(selected.ProfileId).Number+". ")+
                (!controls.Visible ? "Hidden; comparison paused. " : dirty || pending!=null ? "Refresh pending. " : "")+comparisonReport+" "+stationStatus;
            if(action=="profile suggest")
            {
                var anchor=SeatedAnchor();var current=ProfileAlignment.Transform(Sample(grid),anchor,true);
                var suggestions=new List<Tuple<double,string>>();
                foreach(var p in profileStore.Profiles)
                {
                    if(p.Blocks==null || p.Anchor.Definition!=anchor.Definition || Math.Abs(p.GridSize-grid.GridSize)>.001)continue;
                    double score=ProfileSuggestions.Score(p.Blocks,current);
                    suggestions.Add(Tuple.Create(score,p.Number+" | "+p.Name+" | "+score.ToString("P0")+" layout overlap"));
                }
                suggestions.Sort((a,b)=>b.Item1.CompareTo(a.Item1));
                var lines=new List<string>();foreach(var suggestion in suggestions)lines.Add(suggestion.Item2);
                return lines.Count==0?"No compatible saved baselines. Sit in the original anchor cockpit or its equivalent.":string.Join("\n",lines)+"\nSuggestions are not identity checks. Choose with /mg profile select ID; identical ships can tie.";
            }
            if(action=="profile detach") { manualDetached.Add(grid);matchStability.Reset();associationStatus="Manually detached for this session. /mg auto retry to allow matching again.";Detach(grid);RefreshReference("No profile selected.");return "Detached this ship from its profile; saved data is unchanged."; }
            if(action=="profile select")
            {
                var profile=profileStore.Find(controls.ProfileArgument);var anchor=SeatedAnchor();
                if(profile.Anchor.Definition!=anchor.Definition || Math.Abs(profile.GridSize-grid.GridSize)>.001)
                    return "Anchor cockpit definition or grid size does not match this profile.";
                foreach(var pair in references)if(pair.Value.ProfileId==profile.Id && pair.Key!=grid && !pair.Key.Closed && !pair.Key.MarkedForClose)
                    return "That profile is attached to another ship this session. Detach it there before reusing it.";
                BindProfile(profile,anchor);
                return "Selected "+profile.Name+" using this cockpit as its saved anchor counterpart. Verify alignment before capturing again.";
            }
            if(action=="anchor")
            {
                var anchor=SeatedAnchor();
                var profile=new ShipProfile { Id=selected==null?Guid.NewGuid().ToString("N"):selected.ProfileId,Name=grid.DisplayName??"Unnamed ship",GridSize=grid.GridSize,Anchor=anchor,
                    Station=controls.Station,Blocks=selected==null?null:ProfileAlignment.Transform(selected.Blocks,anchor,true) };
                if(selected!=null)ArchiveBaseline(profileStore.Find(selected.ProfileId));profileStore.Put(profile);BindProfile(profile,anchor);
                return "Saved cockpit anchor for "+profile.Name+" ("+profile.Number.ToString()+"). "+(profile.Blocks==null?"Use /mg capture to save the baseline.":"Existing baseline preserved.");
            }
            if(selected==null && action=="capture")
            {
                var anchor=SeatedAnchor();if(previewBlocks!=null)return "Accept or cancel the blueprint preview first.";var p=new ShipProfile {Id=Guid.NewGuid().ToString("N"),Name=grid.DisplayName??"Ship",GridSize=grid.GridSize,Anchor=anchor,Blocks=ProfileAlignment.Transform(SnapshotComparison.HealthyBaseline(Sample(grid)),anchor,true),Station=controls.Station};
                profileStore.Put(p);BindProfile(p,anchor);return "Captured profile "+p.Number+" with this cockpit as its anchor and full-health baseline.";
            }
            if(selected==null)return "Use /mg capture to create a profile, or /mg profile select ID to reconnect an existing one.";
            var saved=profileStore.Find(selected.ProfileId);
            if(action=="clear" || action=="capture")
            {
                if(previewBlocks!=null)return "Accept or cancel the blueprint preview first.";
                var blocks=action=="clear"?null:ProfileAlignment.Transform(SnapshotComparison.HealthyBaseline(Sample(grid)),selected.Anchor,true);
                var updated=new ShipProfile { Id=saved.Id,Name=grid.DisplayName??saved.Name,GridSize=saved.GridSize,Anchor=selected.Anchor,Blocks=blocks };
                ArchiveBaseline(saved);profileStore.Put(updated);BindProfile(updated,selected.Anchor);
                return action=="clear"?"Baseline cleared. Profile "+saved.Number+" ("+saved.Name+") and its cockpit anchor are still saved; /mg profiles lists it.":"Saved "+blocks.Length+" baseline blocks to profile "+saved.Number.ToString()+". This reference survives leaving the world.";
            }
            return SchematicControls.Help;
        }
        private Color LineColor(ChangeKind kind, byte missingAlpha,byte weight=100)
        {
            uint rgb=controls.Appearance.Color((int)kind);
            int alpha=kind==ChangeKind.Missing?missingAlpha:kind==ChangeKind.Damaged?90:kind==ChangeKind.Hull?110:kind==ChangeKind.Unknown?220:240;
            return new Color((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb,(byte)Math.Min(255,alpha*controls.Appearance.Outline*weight/100));
        }

        private ShipHealth health=new ShipHealth();
        private bool healthPreview;
        private byte[] lineWeights=new byte[0];
        private double buildMs, snapshotMs, drawTotalMs, drawMaxMs, cellSize;
        private int drawSamples;
        private string Statistics()
        {
            return (dirty || pending!=null ? "Rebuild pending; displayed model: " : "") + "Requested detail " + controls.DetailName + "; " + points.Length/2 + " geometry lines / "+renderedLines+" rendered / "+renderedFills+" fills (from "+unmergedFills+"); cell " + cellSize +
                "; snapshot " + snapshotMs.ToString("F2") + " ms; worker " + buildMs.ToString("F2") +
                " ms; HUD CPU mean/max " + (drawSamples==0 ? "no samples" :
                (drawTotalMs/drawSamples).ToString("F2") + "/" + drawMaxMs.ToString("F2") + " ms (" + drawSamples + " draws)") +
                ". CPU submission only; excludes GPU and other game work.";
        }
        private Point3 cockpitRight = new Point3(1,0,0), cockpitUp = new Point3(0,1,0), cockpitBackward = new Point3(0,0,1);
        private sealed class BuildResult
        {
            public int Generation, Count, Cells, Page=1;
            public Point3[] Points;
            public double Milliseconds, CellSize;
            public Exception Error;
            public ChangeKind[] Kinds;
            public int[] ShadedMarkers;
            public ShipHealth Health;
            public bool Preview;
            public byte[] Weights;
            public string Report;
            public StationGrid[] Station;
            public BlockSample[] Snapshot,Reference;
            public readonly List<BlockChange> Regions=new List<BlockChange>();
        }

        private static BuildResult Build(BlockSample[] snapshot, BlockSample[] reference, int version, int detail, string mode, string category, string stateFilter, int page, bool preview, StationGrid[] station,string styles,bool outlineOnly)
        {
            var timer=Stopwatch.StartNew();
            var result=new BuildResult { Snapshot=snapshot,Reference=reference,Generation=version, Count=snapshot.Length, Station=station,Preview=preview };
            try
            {
                result.Health=ShipHealth.Build(reference,snapshot);
                var boxes=new List<Box3>();var faded=new List<Box3>();var highlighted=new List<Box3>();category=DisplayFilter.VisibleCategories(category,styles);
                Box3? frame=null;
                foreach(var block in snapshot)frame=frame.HasValue?SchematicGeometry.Union(frame.Value,block.Bounds):block.Bounds;
                if(reference!=null)foreach(var block in reference)frame=frame.HasValue?SchematicGeometry.Union(frame.Value,block.Bounds):block.Bounds;
                var stationReports=new List<string>();
                foreach(var connected in station)
                {
                    if(connected.Baseline==null && connected.Anchors!=null && connected.Profiles!=null)
                    {
                        var match=AutoMatcher.Find(connected.Blocks,connected.Anchors,connected.Profiles,connected.GridSize);
                        if(match.ProfileId!=null)foreach(var profile in connected.Profiles)if(profile.Id==match.ProfileId){connected.Baseline=ProfileAlignment.Transform(profile.Blocks,connected.Anchors[match.AnchorIndex],false);connected.MatchedProfile=profile.Id;}
                    }
                    if(connected.Baseline!=null)foreach(var b in connected.Baseline){var bounds=connected.Bounds(b.Bounds);frame=frame.HasValue?SchematicGeometry.Union(frame.Value,bounds):bounds;}
                }
                foreach(var connected in station)foreach(var block in connected.Blocks)
                {var bounds=connected.Bounds(block.Bounds);frame=frame.HasValue?SchematicGeometry.Union(frame.Value,bounds):bounds;}
                mode="compare";var outlineSource=snapshot;
                foreach(var block in outlineSource)if(DisplayFilter.CategoryMatches(block.Definition,category)){char style=DisplayFilter.Style(block.Definition,styles);(style=='2'?faded:style=='3'?highlighted:boxes).Add(block.Bounds);}
                var model=OutlineGeometry.Build(boxes,detail,frame);
                var geometry=new List<Point3>(model.Points);
                var kinds=new List<ChangeKind>();var shaded=new List<int>();
                var weights=new List<byte>();for(int i=0;i<model.Points.Length/2;i++){kinds.Add(ChangeKind.Hull);weights.Add(100);}
                for(int group=0;group<2;group++){var styled=OutlineGeometry.Build(group==0?faded:highlighted,detail,frame);geometry.AddRange(styled.Points);for(int i=0;i<styled.Points.Length/2;i++){kinds.Add(ChangeKind.Hull);weights.Add(group==0?(byte)25:(byte)230);}}
                if(frame.HasValue)foreach(var connected in station)
                {
                    for(int group=1;group<=3;group++)
                    {
                        var styledBoxes=StationGeometry.StyledBoxes(connected.Blocks,category,styles,(char)('0'+group));
                        var context=StationGeometry.Build(connected,category,frame.Value,styledBoxes,detail,256);geometry.AddRange(context);
                        for(int i=0;i<context.Length/2;i++){kinds.Add(ChangeKind.Hull);weights.Add(group==2?(byte)25:group==3?(byte)230:(byte)100);}
                    }
                    if(connected.Baseline!=null)
                    {
                        var differences=SnapshotComparison.Compare(connected.Baseline,connected.Blocks);stationReports.Add(connected.Name+": "+differences.Summary);
                        foreach(ChangeKind kind in new[]{ChangeKind.Damaged,ChangeKind.NonFunctional,ChangeKind.Missing,ChangeKind.Added,ChangeKind.Replaced,ChangeKind.Unknown})
                        {
                            var changed=new List<Box3>();foreach(var change in differences.Changes)if(change.Kind==kind && DisplayFilter.StateMatches(kind,stateFilter) && DisplayFilter.CategoryMatches(change.Definition,category))changed.Add(change.Bounds);
                            if(changed.Count==0)continue;var lines=StationGeometry.Build(connected,category,frame.Value,changed,detail);geometry.AddRange(lines);for(int i=0;i<lines.Length/2;i++){kinds.Add(kind);weights.Add(100);}
                        }
                    }
                    else stationReports.Add(connected.Name+": no unambiguous baseline (context only).");
                }
                result.Report="No reference captured.";
                if(reference!=null)
                {
                    var comparison=SnapshotComparison.Compare(reference,snapshot);
                    var visible=comparison.Changes.FindAll(change=>DisplayFilter.CategoryMatches(change.Definition,category) && DisplayFilter.StateMatches(change.Kind,stateFilter));
                    int pages=Math.Max(1,(visible.Count+127)/128),actualPage=Math.Min(page,pages),start=(actualPage-1)*128;
                    result.Page=actualPage;
                    int shown=Math.Min(128,Math.Max(0,visible.Count-start));
                    // Whole-ship fault layers must never depend on the 128-detail-marker page.
                    foreach(var kind in new[]{ChangeKind.Missing,ChangeKind.Damaged,ChangeKind.NonFunctional,ChangeKind.Replaced,ChangeKind.Added,ChangeKind.Unknown})
                    {
                        if(!outlineOnly && (kind==ChangeKind.Missing || kind==ChangeKind.Damaged || kind==ChangeKind.NonFunctional))continue; // Filled boxes already include complete outlines.
                        var overview=OutlineGeometry.Faults(visible,kind,frame.Value);geometry.AddRange(overview.Points);
                        for(int i=0;i<overview.Points.Length/2;i++)kinds.Add(kind);
                    }
                    if(preview)
                    {
                        var ghostBoxes=new List<Box3>();foreach(var block in reference)if(DisplayFilter.CategoryMatches(block.Definition,category))ghostBoxes.Add(block.Bounds);
                        var ghost=OutlineGeometry.Build(ghostBoxes,0,frame);
                        geometry.AddRange(ghost.Points);for(int i=0;i<ghost.Points.Length/2;i++)kinds.Add(ChangeKind.Replaced);
                    }
                    // Every primary fault gets fill coverage, independent of marker pages.
                    // Dense same-state regions merge rather than dropping later blocks.
                    if(!outlineOnly)foreach(var kind in new[]{ChangeKind.Damaged,ChangeKind.Missing,ChangeKind.NonFunctional})
                        foreach(var region in MarkerFill.Regions(visible,kind))
                        {result.Regions.Add(region);shaded.Add(geometry.Count);AddMarker(geometry,kinds,region,frame.Value);}
                    for(int i=0;i<shown;i++)
                    {
                        var change=visible[start+i];
                        if(change.Kind==ChangeKind.Damaged || change.Kind==ChangeKind.NonFunctional || change.Kind==ChangeKind.Missing)continue;
                        AddMarker(geometry,kinds,change,frame.Value);
                    }
                    result.Report=comparison.Summary+(outlineOnly?". Outline-only mode: merged fault contours, no translucent fills.":". All matching damage, missing and non-functional regions highlighted across every page (dense fills may merge).")+" Detail marker page "+actualPage+"/"+pages+"; "+shown+" shown of "+visible.Count+" matching filters. Mode "+mode+"; system "+category+"; filter "+stateFilter+".";
                }
                result.Report+="\nSchematic uses block envelopes, not model meshes. Coarse detail can merge small gaps; use High detail for inspection.";
                if(stationReports.Count>0)result.Report+="\nDocked grids: "+string.Join("; ",stationReports);
                while(weights.Count<kinds.Count)weights.Add(100);result.Weights=weights.ToArray();
                result.Points=geometry.ToArray(); result.Kinds=kinds.ToArray(); result.ShadedMarkers=shaded.ToArray();
                result.Cells=model.Cells; result.CellSize=model.CellSize;
            }
            catch(Exception e) { result.Error=e; }
            result.Milliseconds=timer.Elapsed.TotalMilliseconds;
            return result;
        }
        private static void AddMarker(List<Point3> points,List<ChangeKind> kinds,BlockChange change,Box3 frame)
        {
            double x=(frame.Min.X+frame.Max.X)/2,y=(frame.Min.Y+frame.Max.Y)/2,z=(frame.Min.Z+frame.Max.Z)/2;
            double dx=frame.Max.X-frame.Min.X,dy=frame.Max.Y-frame.Min.Y,dz=frame.Max.Z-frame.Min.Z;
            double radius=Math.Max(.5,Math.Sqrt(dx*dx+dy*dy+dz*dz)/2);
            var corners=new Point3[8];
            for(int i=0;i<8;i++) corners[i]=new Point3(
                (((i&1)==0?change.Bounds.Min.X:change.Bounds.Max.X)-x)/radius,
                (((i&2)==0?change.Bounds.Min.Y:change.Bounds.Max.Y)-y)/radius,
                (((i&4)==0?change.Bounds.Min.Z:change.Bounds.Max.Z)-z)/radius);
            for(int i=0;i<8;i++) for(int axis=1;axis<=4;axis*=2) if((i&axis)==0)
            { points.Add(corners[i]); points.Add(corners[i|axis]); kinds.Add(change.Kind); }
        }

        public void Init()
        {
            var paths=DataPaths.Initialize(VRage.FileSystem.MyFileSystem.UserDataPath);
            settingsPath=System.IO.Path.Combine(paths.Root,"display.txt");
            profileStore=new ProfileStore(paths.Root);
            try { DisplaySettings.Load(settingsPath,controls); } catch(Exception e) { MyLog.Default.WriteLineAndConsole("[Marigold] Display settings: "+e.Message); }
            if(profileStore.Warning!=null)MyLog.Default.WriteLineAndConsole("[Marigold] "+profileStore.Warning);
            harmony = new Harmony(PatchId);
            harmony.Patch(AccessTools.Method(typeof(MyGuiScreenHudSpace), "Draw"),
                postfix: new HarmonyMethod(typeof(SchematicOverlay), nameof(AfterHudDraw)));
            active = this;
        }

        private static void AfterHudDraw(bool __result)
        {
            if (__result && active?.panel==null) active?.DrawSafely();
        }

        public void SetGrid(MyCubeGrid next)
        {
            if (ReferenceEquals(grid,next)) return;
            ClosePanel();
            observation?.Dispose();observation=null;observationRevision=-1;
            ClearStationObservers();
            CancelBlueprintPreview();
            ResetAssociation();CancelRecovery();stationStatus="";
            lastObservation=null;lastObservationGeneration=-1;
            damagePulse.Reset();pulseStarts=new double[0];grid=next;health=new ShipHealth();healthPreview=false;
            bool auto=controls.AutoAssociate;string language=controls.Language;
            try{DisplaySettings.Reset(controls);DisplaySettings.Load(settingsPath,controls);}catch(Exception e){MyLog.Default.WriteLine("[Marigold] Display defaults: "+e.Message);}controls.AutoAssociate=auto;controls.Language=language;
            Reference savedBinding;if(grid!=null && references.TryGetValue(grid,out savedBinding))LoadShipSettings(savedBinding.ProfileId);
            bool station;controls.Station=grid!=null && stationByGrid.TryGetValue(grid,out station) && station;
            generation++;
            points=new Point3[0]; rotated=new Point3[0]; lineKinds=new ChangeKind[0]; shadedMarkers=new int[0]; fillBands.Clear();
            comparisonReport=grid!=null && references.ContainsKey(grid)?"Comparison pending.":"No reference captured.";
            drawSamples=0; drawTotalMs=drawMaxMs=buildMs=snapshotMs=cellSize=0;
            dirty=grid!=null; cooldown=0; reconcile=0; faulted=false;
            if(grid!=null)observation=new GridObservation(grid);
        }

        private void BindCommands(IMyUtilities utilities)
        {
            if (ReferenceEquals(commandUtilities, utilities)) return;
            if (commandUtilities != null) commandUtilities.MessageEntered -= OnMessage;
            commandUtilities = utilities;
            if (commandUtilities != null) commandUtilities.MessageEntered += OnMessage;
        }

        public void BindSessionCommands(bool available)
        {
            if(!available)ClosePanel();
            if(!available && references.Count>0) ClearReferences();
            BindCommands(available ? MyAPIGateway.Utilities : null);
        }

        private void OnMessage(string text, ref bool sendToOthers)
        {
            try
            {
                string response;
                string before=controls.GeometryOptions;
                if (controls.TryHandle(text, ref sendToOthers, out response))
                {
                    if(controls.ReferenceAction=="ui") { OpenPanel();response="Marigold controls opened."; }
                    else if(controls.ReferenceAction!=null) response=ReferenceCommand(controls.ReferenceAction);
                    if(controls.StatsRequested) { response=Statistics(); MyLog.Default.WriteLine("[Marigold] " + response); }
                    if(before!=controls.GeometryOptions) { generation++; dirty=true; cooldown=0; }
                    commandUtilities?.ShowMessage("Marigold", response);
                }
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("[Marigold] Command failed: " + e); commandUtilities?.ShowMessage("Marigold", "Command failed: "+e.Message); }
        }

        public void Update(MyCubeGrid current, IMyShipController controller)
        {
            SetGrid(current);
            if(classificationRevision!=DisplayFilter.ClassificationRevision){classificationRevision=DisplayFilter.ClassificationRevision;generation++;dirty=true;}
            UpdateBlueprint();UpdateRecovery();
            UpdateHotkeys();
            if(observation!=null && (controls.Visible || controls.AutoAssociate))
            {
                var sampling=Stopwatch.StartNew();observation.Step(512);snapshotMs=sampling.Elapsed.TotalMilliseconds;
                if(observation.Revision!=observationRevision){observationRevision=observation.Revision;dirty=true;}
            }
            UpdateStationObservers();
            UpdateAssociation();
            if (grid==null || faulted) return;
            try
            {
                caption=(string.IsNullOrWhiteSpace(grid.DisplayName) ? "Unnamed ship" : grid.DisplayName)+(recoveryPreview!=null?" [recovery preview]":previewBlocks==null?"":" [blueprint preview]")+(controls.Station?" [station]":"");
                Matrix orientation;
                controller.Orientation.GetMatrix(out orientation);
                cockpitRight = new Point3(orientation.Right.X,orientation.Right.Y,orientation.Right.Z);
                cockpitUp = new Point3(orientation.Up.X,orientation.Up.Y,orientation.Up.Z);
                cockpitBackward = new Point3(orientation.Backward.X,orientation.Backward.Y,orientation.Backward.Z);
                double now=clock.Elapsed.TotalSeconds, elapsed=Math.Min(0.1, now-lastTime);
                lastTime=now;
                if(!controls.Visible) return;
                if(controls.Spin && !(panel?.RecoveryFocus??false)) controls.Yaw=(controls.Yaw+elapsed*0.25)%(Math.PI*2);
                if(pending!=null && pending.IsCompleted)
                {
                    var result=pending.Result;
                    pending=null;
                    if(result.Generation==generation)
                    {
                        if(result.Error!=null)throw result.Error;
                        if(stationBaselines.Count>128)stationBaselines.Clear();
                        foreach(var docked in result.Station)if(docked.MatchedProfile!=null)stationBaselines[docked.EntityId]=new StationGrid {Baseline=docked.Baseline,MatchedProfile=docked.MatchedProfile};
                        controls.Page=result.Page;
                        health=result.Health;healthPreview=result.Preview;
                        pulseStarts=new double[result.Kinds.Length];for(int i=0;i<pulseStarts.Length;i++)pulseStarts[i]=double.NegativeInfinity;
                        if(result.Preview)damagePulse.Reset();
                        else
                        {
                            damagePulse.Observe(result.Reference,result.Snapshot,now);
                            for(int i=0;i<result.Regions.Count;i++)
                            {double start=damagePulse.Start(result.Regions[i]);int first=result.ShadedMarkers[i]/2;for(int j=first;j<first+12;j++)pulseStarts[j]=start;}
                        }
                        points=result.Points; lineWeights=result.Weights;lineKinds=result.Kinds; shadedMarkers=result.ShadedMarkers; comparisonReport=result.Report;
                        rotated=new Point3[points.Length];
                        // Remove identical same-state edges, retaining the latest alert on shared boundaries.
                        drawLine=new bool[lineKinds.Length];renderedLines=0;
                        var unique=new Dictionary<ChangeKind,Dictionary<Point3,Dictionary<Point3,int>>>();
                        for(int i=0;i<lineKinds.Length;i++)
                        {
                            var a=points[i*2];var b=points[i*2+1];
                            if(a.X>b.X || (a.X==b.X && (a.Y>b.Y || (a.Y==b.Y && a.Z>b.Z)))){var swap=a;a=b;b=swap;}
                            Dictionary<Point3,Dictionary<Point3,int>> first;Dictionary<Point3,int> second;int prior;
                            if(!unique.TryGetValue(lineKinds[i],out first))unique[lineKinds[i]]=first=new Dictionary<Point3,Dictionary<Point3,int>>();
                            if(!first.TryGetValue(a,out second))first[a]=second=new Dictionary<Point3,int>();
                            if(second.TryGetValue(b,out prior) && lineWeights[prior]==lineWeights[i]){pulseStarts[prior]=Math.Max(pulseStarts[prior],pulseStarts[i]);continue;}
                            second[b]=i;drawLine[i]=true;renderedLines++;
                        }
                        buildMs=result.Milliseconds; cellSize=result.CellSize;
                        drawSamples=0; drawTotalMs=drawMaxMs=0;
                    }
                }
                if(++reconcile>=(references.ContainsKey(grid) || controls.Station?60:600)) { dirty=true; reconcile=0; }
                if(cooldown>0) cooldown--;
                if(dirty && cooldown==0 && pending==null && observation.Ready)
                {
                    var timer=Stopwatch.StartNew();
                    var snapshot=observation.Snapshot();
                    Reference reference;
                    references.TryGetValue(grid,out reference);
                    var baseline=recoveryPreview??previewBlocks??(reference==null?null:reference.Blocks);
                    var station=SampleStation();
                    if(!controls.Station && lastObservationGeneration==generation && ObservationCache.Same(lastObservation,snapshot))
                    { dirty=false;cooldown=30;snapshotMs=timer.Elapsed.TotalMilliseconds;return; }
                    lastObservation=snapshot;lastObservationGeneration=generation;
                    int version=generation, detail=controls.Detail,page=controls.Page;
                    string mode=controls.Mode,category=controls.Category,stateFilter=controls.StateFilter;bool preview=recoveryPreview!=null || previewBlocks!=null;
                    string styles=controls.SystemStyles;bool outlineOnly=!controls.Appearance.DrawFills && !(panel?.RecoveryFocus??false);pending=Task.Run(()=>Build(snapshot,baseline,version,detail,mode,category,stateFilter,page,preview,station,styles,outlineOnly));
                    snapshotMs=timer.Elapsed.TotalMilliseconds;
                    dirty=false; cooldown=30;
                }
            }
            catch(Exception e) { Fail(e); }
        }

        private void DrawSafely()
        {
            if(!controls.Visible || faulted || grid==null || grid.Closed || grid.MarkedForClose ||
                MyAPIGateway.Gui==null || (MyAPIGateway.Gui.IsCursorVisible && panel==null) || MyHud.MinimalHud || MyHud.CutsceneHud) return;
            try
            {
                long drawStart=Stopwatch.GetTimestamp();
                Vector2 screen=MyGuiManager.GetScreenSizeFromNormalizedSize(Vector2.One,true);
                bool recoveryFocus=panel?.RecoveryFocus??false;
                var view=recoveryFocus?panel.RecoveryView:controls;
                if(controls.SchematicVisible || recoveryFocus)
                {
                float side=panel!=null?Math.Min(screen.X*.14f,screen.Y*.18f):controls.Expanded?Math.Min(screen.X*.6f,screen.Y*.7f*(float)(controls.Appearance.Size/.32)):Math.Min(screen.X*.6f,screen.Y*(float)controls.Appearance.Size);
                float left=panel!=null?8:controls.Expanded?(screen.X-side)/2:8+(screen.X-side-24)*(float)controls.Appearance.X, top=panel!=null?(screen.Y-side)/2:8+(screen.Y-side-65)*(float)controls.Appearance.Y;
                if(recoveryFocus)
                {
                    // Use the GUI safe area so preview and controls align on wide screens.
                    var previewSize=MyGuiManager.GetScreenSizeFromNormalizedSize(new Vector2(.78f,.37f));
                    side=Math.Min(previewSize.X,previewSize.Y);
                    var previewCenter=MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(new Vector2(.5f,.365f));
                    left=previewCenter.X-side/2;top=previewCenter.Y-side/2;
                }
                var panelRect=new RectangleF(left-8,top-8,side+16,side+16);
                MyRenderProxy.DrawSprite(Texture,ref panelRect,null,new Color(5,18,25,(byte)(255*controls.Appearance.Background)),0,false,false);
                float half=side/2;
                // Projection is linear. Evaluate trigonometry three times per frame, not per endpoint.
                var axisX=view.Project(SchematicGeometry.ToBasis(new Point3(1,0,0),cockpitRight,cockpitUp,cockpitBackward));
                var axisY=view.Project(SchematicGeometry.ToBasis(new Point3(0,1,0),cockpitRight,cockpitUp,cockpitBackward));
                var axisZ=view.Project(SchematicGeometry.ToBasis(new Point3(0,0,1),cockpitRight,cockpitUp,cockpitBackward));
                for(int i=0;i<points.Length;i++)
                {var p=points[i];rotated[i]=new Point3(p.X*axisX.X+p.Y*axisY.X+p.Z*axisZ.X,p.X*axisX.Y+p.Y*axisY.Y+p.Z*axisZ.Y,p.X*axisX.Z+p.Y*axisY.Z+p.Z*axisZ.Z);}
                double pulseTime=clock.Elapsed.TotalSeconds;
                double pulseWave=DamagePulse.Wave(pulseTime);
                byte missingAlpha=(byte)(205+35*controls.Appearance.Pulse*(2*pulseWave-1));
                if(recoveryFocus || controls.Appearance.DrawFills)MarkerFill.Build(rotated,shadedMarkers,view.Zoom,side,fillBands);else fillBands.Clear();
                unmergedFills=fillBands.Count;
                fillRaster.Merge(fillBands,lineKinds,pulseStarts,pulseTime,controls.Appearance.NewDamagePulse);
                renderedFills=0;
                foreach(var band in fillBands)
                {
                    var fill=new RectangleF(left+half+(float)band.X*half,top+half+(float)band.Y*half,
                        (float)band.Width*half,(float)band.Height*half);
                    var kind=lineKinds[band.MarkerStart/2];
                    var shade=LineColor(kind,missingAlpha);
                    double flash=controls.Appearance.NewDamagePulse && DamagePulse.VisibleFor(kind) && band.MarkerStart/2<pulseStarts.Length?(pulseTime-pulseStarts[band.MarkerStart/2]>=0 && pulseTime-pulseStarts[band.MarkerStart/2]<4?pulseWave:0):0;
                    shade.A=(byte)Math.Min(255,((kind==ChangeKind.Missing?missingAlpha/4:kind==ChangeKind.Damaged?12:60)+150*flash)*controls.Appearance.Fill);
                    renderedFills++;
                    MyRenderProxy.DrawSprite(Texture,ref fill,null,shade,0,false,false);
                }
                renderedLines=0;
                for(int b=0;b<points.Length;b+=2)
                    {
                        if(!recoveryFocus && !controls.Appearance.DrawHull && lineKinds[b/2]==ChangeKind.Hull)continue;
                        if(b/2<drawLine.Length && !drawLine[b/2])continue;
                        var a=rotated[b]; var c=rotated[b+1];
                        double x0=a.X*view.Zoom,y0=-a.Y*view.Zoom,x1=c.X*view.Zoom,y1=-c.Y*view.Zoom;
                        if(!SchematicGeometry.Clip(ref x0,ref y0,ref x1,ref y1))continue;
                        var from=new Vector2(left+half+(float)x0*half,top+half+(float)y0*half);
                        var to=new Vector2(left+half+(float)x1*half,top+half+(float)y1*half);
                        var delta=to-from; float length=delta.Length(); if(length<0.25f)continue;

                        var lineColor=LineColor(lineKinds[b/2],missingAlpha,lineWeights[b/2]);
                        double flash=controls.Appearance.NewDamagePulse && DamagePulse.VisibleFor(lineKinds[b/2]) && b/2<pulseStarts.Length?(pulseTime-pulseStarts[b/2]>=0 && pulseTime-pulseStarts[b/2]<4?pulseWave:0):0;
                        lineColor.A=(byte)Math.Min(255,lineColor.A+105*flash*controls.Appearance.Outline);
                        float thickness=1+(float)flash;
                        var line=new RectangleF((from.X+to.X-length)/2,(from.Y+to.Y-thickness)/2,length,thickness);
                        renderedLines++;
                        MyRenderProxy.DrawSprite(Texture,ref line,null,lineColor,(float)Math.Atan2(delta.Y,delta.X),false,false);
                    }
                if(!recoveryFocus)MyGuiManager.DrawString("White",caption+" | "+(controls.Category=="all"?"All systems":controls.Category=="none"?"No systems":controls.Category.Replace("weapons","Offensive").Replace(",",", "))+" | "+(controls.StateFilter=="all"?"All states":controls.StateFilter=="broken"?"Non-functional":controls.StateFilter),new Vector2((left-8)/screen.X,(top+side+12)/screen.Y),0.55f,
                    useFullClientArea:true,maxTextWidth:controls.Expanded?.65f:.28f);
                if(controls.Expanded && panel==null)MyGuiManager.DrawString("White",controls.Mode+" | "+controls.Category.Replace("weapons","Offensive")+" | "+controls.StateFilter+" | /mg report for counts",new Vector2((left-8)/screen.X,(top+side+32)/screen.Y),.5f,useFullClientArea:true,maxTextWidth:.65f);
                }
                if(!recoveryFocus)DrawHealthBars(screen);
                double drawMs=(Stopwatch.GetTimestamp()-drawStart)*1000.0/Stopwatch.Frequency;
                if(drawSamples>=600) { drawSamples=0; drawTotalMs=drawMaxMs=0; }
                drawSamples++; drawTotalMs+=drawMs; drawMaxMs=Math.Max(drawMaxMs,drawMs);
            }
            catch(Exception e) { Fail(e); }
        }
        private void Fail(Exception e)
        {
            faulted=true;
            MyLog.Default.WriteLineAndConsole("[Marigold] Schematic disabled until grid change: "+e);
        }
        public void Dispose()
        {
            if(ReferenceEquals(active,this))active=null;
            SetGrid(null);
            ClearReferences();
            BindCommands(null);
            harmony?.Unpatch(AccessTools.Method(typeof(MyGuiScreenHudSpace),"Draw"),HarmonyPatchType.Postfix,PatchId);
            harmony=null;
        }
    }
}

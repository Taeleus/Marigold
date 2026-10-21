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
    internal sealed class SchematicOverlay : IDisposable
    {
        private const string PatchId = "Marigold.Schematic.0.2";
        private const string Texture = "Textures\\GUI\\Blank.dds";
        private static SchematicOverlay active;
        private Harmony harmony;
        private MyCubeGrid grid;
        private Point3[] points = new Point3[0];
        private int[] shadedMarkers=new int[0];
        private readonly List<FillBand> fillBands=new List<FillBand>();
        private ChangeKind[] lineKinds = new ChangeKind[0];
        private Point3[] rotated = new Point3[0];
        private bool dirty, faulted;
        private int cooldown, reconcile;
        private readonly SchematicControls controls = new SchematicControls();
        private IMyUtilities commandUtilities;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double lastTime;
        private string caption = "";
        private Task<BuildResult> pending;
        private int generation;
        private sealed class Reference
        {
            public BlockSample[] Blocks;
            public string ProfileId;
            public AnchorFrame Anchor;
        }
        private ProfileStore profileStore;
        private readonly Dictionary<MyCubeGrid,Reference> references=new Dictionary<MyCubeGrid,Reference>();
        private string comparisonReport="No reference captured.";
        private void ClearReferences()
        {
            foreach(var pair in references) pair.Key.OnGridSplit-=OnSplit;
            references.Clear(); generation++; comparisonReport="No reference captured.";
        }
        private void OnSplit(MyCubeGrid original,MyCubeGrid detached)
        {
            generation++; dirty=true; cooldown=0;
        }
        private static BlockSample[] Sample(MyCubeGrid source)
        {
            var result=new List<BlockSample>();
            foreach(var block in source.GetBlocks())
                result.Add(new BlockSample {
                    Position=new Point3(block.Position.X,block.Position.Y,block.Position.Z),
                    Bounds=new Box3(new Point3(block.Min.X-.5,block.Min.Y-.5,block.Min.Z-.5),
                        new Point3(block.Max.X+.5,block.Max.Y+.5,block.Max.Z+.5)),
                    Definition=block.BlockDefinition.Id.ToString(),Forward=(int)block.Orientation.Forward,Up=(int)block.Orientation.Up,
                    HasFunctionalState=block.FatBlock is Sandbox.ModAPI.IMyFunctionalBlock,
                    IsFunctional=block.FatBlock==null || block.FatBlock.IsFunctional,
                    Integrity=block.Integrity,BuildIntegrity=block.BuildIntegrity,MaxIntegrity=block.MaxIntegrity
                });
            return result.ToArray();
        }
        private void RefreshReference(string message)
        { generation++;dirty=true;cooldown=0;comparisonReport=message; }
        private void Detach(MyCubeGrid target)
        { if(references.Remove(target)) target.OnGridSplit-=OnSplit; }
        private void BindProfile(ShipProfile profile,AnchorFrame anchor)
        {
            var closed=new List<MyCubeGrid>();
            foreach(var pair in references)if(pair.Key.Closed || pair.Key.MarkedForClose)closed.Add(pair.Key);
            foreach(var old in closed)Detach(old);
            if(!references.ContainsKey(grid))grid.OnGridSplit+=OnSplit;
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
            if(action=="profiles")
            {
                var lines=new List<string>();
                if(profileStore.Warning!=null)lines.Add(profileStore.Warning);
                foreach(var p in profileStore.Profiles)lines.Add(p.Id.Substring(0,8)+" | "+p.Name+" | "+(p.Blocks==null?"anchor only":p.Blocks.Length+" baseline blocks"));
                return lines.Count==0?"No saved profiles. Sit in a cockpit and use /mg anchor.":string.Join("\n",lines);
            }
            if(action=="recover") { profileStore.Recover();ClearReferences();RefreshReference("Select a restored profile.");return "Backup restored. Use /mg profile select ID from the matching cockpit."; }
            if(action=="profile delete")
            {
                var deleted=profileStore.Find(controls.ProfileArgument);
                profileStore.Delete(deleted.Id);
                var detach=new List<MyCubeGrid>();
                foreach(var pair in references)if(pair.Value.ProfileId==deleted.Id)detach.Add(pair.Key);
                foreach(var target in detach)Detach(target);
                RefreshReference("Profile deleted.");return "Deleted profile "+deleted.Name+". Previous database retained in backup.";
            }
            if(grid==null || grid.Closed || grid.MarkedForClose) return "Control a ship first.";
            Reference selected;references.TryGetValue(grid,out selected);
            if(action=="report") return (selected==null?"No profile selected. Use /mg profiles or /mg anchor. ":"Profile "+selected.ProfileId.Substring(0,8)+". ")+
                (!controls.Visible ? "Hidden; comparison paused. " : dirty || pending!=null ? "Refresh pending. " : "")+comparisonReport;
            if(action=="profile detach") { Detach(grid);RefreshReference("No profile selected.");return "Detached this ship from its profile; saved data is unchanged."; }
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
                    Blocks=selected==null?null:ProfileAlignment.Transform(selected.Blocks,anchor,true) };
                profileStore.Put(profile);BindProfile(profile,anchor);
                return "Saved cockpit anchor for "+profile.Name+" ("+profile.Id.Substring(0,8)+"). "+(profile.Blocks==null?"Use /mg capture to save the baseline.":"Existing baseline preserved.");
            }
            if(selected==null)return "Use /mg anchor to create a profile, or /mg profile select ID to reconnect an existing one.";
            var saved=profileStore.Find(selected.ProfileId);
            if(action=="clear" || action=="capture")
            {
                var blocks=action=="clear"?null:ProfileAlignment.Transform(Sample(grid),selected.Anchor,true);
                var updated=new ShipProfile { Id=saved.Id,Name=grid.DisplayName??saved.Name,GridSize=saved.GridSize,Anchor=selected.Anchor,Blocks=blocks };
                profileStore.Put(updated);BindProfile(updated,selected.Anchor);
                return action=="clear"?"Saved baseline cleared; profile and cockpit anchor retained.":"Saved "+blocks.Length+" baseline blocks to profile "+saved.Id.Substring(0,8)+". This reference survives leaving the world.";
            }
            return SchematicControls.Help;
        }
        private static Color LineColor(ChangeKind kind, byte missingAlpha)
        {
            switch(kind)
            {
                case ChangeKind.Missing:return new Color(255,55,55,missingAlpha);
                case ChangeKind.Damaged:return new Color(220,205,65,150);
                case ChangeKind.NonFunctional:return new Color(255,130,20,240);
                case ChangeKind.Replaced:return new Color(220,100,255,240);
                case ChangeKind.Added:return new Color(90,255,120,240);
                case ChangeKind.Unknown:return new Color(190,190,190,220);
                default:return new Color(80,220,220,110);
            }
        }
        private double buildMs, snapshotMs, drawTotalMs, drawMaxMs, cellSize;
        private int drawSamples;
        private string Statistics()
        {
            return (dirty || pending!=null ? "Rebuild pending; displayed model: " : "") + "Requested detail " + controls.DetailName + "; " + points.Length/2 + " lines; cell " + cellSize +
                "; snapshot " + snapshotMs.ToString("F2") + " ms; worker " + buildMs.ToString("F2") +
                " ms; HUD CPU mean/max " + (drawSamples==0 ? "no samples" :
                (drawTotalMs/drawSamples).ToString("F2") + "/" + drawMaxMs.ToString("F2") + " ms (" + drawSamples + " draws)") +
                ". CPU submission only; excludes GPU and other game work.";
        }
        private Point3 cockpitRight = new Point3(1,0,0), cockpitUp = new Point3(0,1,0), cockpitBackward = new Point3(0,0,1);
        private sealed class BuildResult
        {
            public int Generation, Count, Cells;
            public Point3[] Points;
            public double Milliseconds, CellSize;
            public Exception Error;
            public ChangeKind[] Kinds;
            public int[] ShadedMarkers;
            public string Report;
        }

        private static BuildResult Build(BlockSample[] snapshot, BlockSample[] reference, int version, int detail)
        {
            var timer=Stopwatch.StartNew();
            var result=new BuildResult { Generation=version, Count=snapshot.Length };
            try
            {
                var boxes=new List<Box3>();
                Box3? frame=null;
                foreach(var block in snapshot) { boxes.Add(block.Bounds); frame=frame.HasValue?SchematicGeometry.Union(frame.Value,block.Bounds):block.Bounds; }
                if(reference!=null) foreach(var block in reference) frame=frame.HasValue?SchematicGeometry.Union(frame.Value,block.Bounds):block.Bounds;
                var model=OutlineGeometry.Build(boxes,detail,frame);
                var geometry=new List<Point3>(model.Points);
                var kinds=new List<ChangeKind>();
                var shaded=new List<int>();
                for(int i=0;i<model.Points.Length/2;i++) kinds.Add(ChangeKind.Hull);
                result.Report="No reference captured.";
                if(reference!=null)
                {
                    var comparison=SnapshotComparison.Compare(reference,snapshot);
                    // Exact boxes preserve small fault locations even if the hull is coarsened.
                    int shown=Math.Min(128,comparison.Changes.Count);
                    for(int i=0;i<shown;i++)
                    {
                        if(comparison.Changes[i].Kind==ChangeKind.Damaged || comparison.Changes[i].Kind==ChangeKind.NonFunctional || comparison.Changes[i].Kind==ChangeKind.Missing) shaded.Add(geometry.Count);
                        AddMarker(geometry,kinds,comparison.Changes[i],frame.Value);
                    }
                    result.Report=comparison.Summary + ". " + shown + "/" + comparison.Changes.Count + " markers shown." +
                        " Red means missing from this ship relative to the reference, including detached blocks. Gray means unreadable block data.";
                }
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
            profileStore=new ProfileStore(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Marigold"));
            if(profileStore.Warning!=null)MyLog.Default.WriteLineAndConsole("[Marigold] "+profileStore.Warning);
            harmony = new Harmony(PatchId);
            harmony.Patch(AccessTools.Method(typeof(MyGuiScreenHudSpace), "Draw"),
                postfix: new HarmonyMethod(typeof(SchematicOverlay), nameof(AfterHudDraw)));
            active = this;
        }

        private static void AfterHudDraw(bool __result)
        {
            if (__result) active?.DrawSafely();
        }

        public void SetGrid(MyCubeGrid next)
        {
            if (ReferenceEquals(grid,next)) return;
            if (grid != null) { grid.OnBlockAdded -= Changed; grid.OnBlockRemoved -= Changed; }
            grid=next;
            generation++;
            points=new Point3[0]; rotated=new Point3[0]; lineKinds=new ChangeKind[0]; shadedMarkers=new int[0]; fillBands.Clear();
            comparisonReport=grid!=null && references.ContainsKey(grid)?"Comparison pending.":"No reference captured.";
            drawSamples=0; drawTotalMs=drawMaxMs=buildMs=snapshotMs=cellSize=0;
            dirty=grid!=null; cooldown=0; reconcile=0; faulted=false;
            if (grid != null) { grid.OnBlockAdded += Changed; grid.OnBlockRemoved += Changed; }
        }
        private void Changed(MySlimBlock block) { dirty=true; }

        private void BindCommands(IMyUtilities utilities)
        {
            if (ReferenceEquals(commandUtilities, utilities)) return;
            if (commandUtilities != null) commandUtilities.MessageEntered -= OnMessage;
            commandUtilities = utilities;
            if (commandUtilities != null) commandUtilities.MessageEntered += OnMessage;
        }

        public void BindSessionCommands(bool available)
        {
            if(!available && references.Count>0) ClearReferences();
            BindCommands(available ? MyAPIGateway.Utilities : null);
        }

        private void OnMessage(string text, ref bool sendToOthers)
        {
            try
            {
                string response;
                int before=controls.Detail;
                if (controls.TryHandle(text, ref sendToOthers, out response))
                {
                    if(controls.ReferenceAction!=null) response=ReferenceCommand(controls.ReferenceAction);
                    if(controls.StatsRequested) { response=Statistics(); MyLog.Default.WriteLine("[Marigold] " + response); }
                    if(before!=controls.Detail) { generation++; dirty=true; cooldown=0; }
                    commandUtilities?.ShowMessage("Marigold", response);
                }
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("[Marigold] Command failed: " + e); commandUtilities?.ShowMessage("Marigold", "Command failed: "+e.Message); }
        }

        public void Update(MyCubeGrid current, IMyShipController controller)
        {
            SetGrid(current);
            if (grid==null || faulted) return;
            try
            {
                caption=string.IsNullOrWhiteSpace(grid.DisplayName) ? "Unnamed ship" : grid.DisplayName;
                Matrix orientation;
                controller.Orientation.GetMatrix(out orientation);
                cockpitRight = new Point3(orientation.Right.X,orientation.Right.Y,orientation.Right.Z);
                cockpitUp = new Point3(orientation.Up.X,orientation.Up.Y,orientation.Up.Z);
                cockpitBackward = new Point3(orientation.Backward.X,orientation.Backward.Y,orientation.Backward.Z);
                double now=clock.Elapsed.TotalSeconds, elapsed=Math.Min(0.1, now-lastTime);
                lastTime=now;
                if(!controls.Visible) return;
                if(controls.Spin) controls.Yaw=(controls.Yaw+elapsed*0.25)%(Math.PI*2);
                if(pending!=null && pending.IsCompleted)
                {
                    var result=pending.Result;
                    pending=null;
                    if(result.Generation==generation)
                    {
                        if(result.Error!=null)throw result.Error;
                        points=result.Points; lineKinds=result.Kinds; shadedMarkers=result.ShadedMarkers; comparisonReport=result.Report;
                        rotated=new Point3[points.Length];
                        buildMs=result.Milliseconds; cellSize=result.CellSize;
                        drawSamples=0; drawTotalMs=drawMaxMs=0;
                        MyLog.Default.WriteLine("[Marigold] Schematic worker: "+result.Count+" blocks, "+result.Cells+" cells, "+result.Milliseconds.ToString("F1")+" ms.");
                    }
                }
                if(++reconcile>=(references.ContainsKey(grid)?60:600)) { dirty=true; reconcile=0; }
                if(cooldown>0) cooldown--;
                if(dirty && cooldown==0 && pending==null)
                {
                    var timer=Stopwatch.StartNew();
                    var snapshot=Sample(grid);
                    Reference reference;
                    references.TryGetValue(grid,out reference);
                    var baseline=reference==null?null:reference.Blocks;
                    int version=generation, detail=controls.Detail;
                    pending=Task.Run(()=>Build(snapshot,baseline,version,detail));
                    snapshotMs=timer.Elapsed.TotalMilliseconds;
                    dirty=false; cooldown=30;
                    MyLog.Default.WriteLine("[Marigold] Schematic snapshot: "+snapshot.Length+" blocks, "+timer.Elapsed.TotalMilliseconds.ToString("F1")+" ms.");
                }
            }
            catch(Exception e) { Fail(e); }
        }

        private void DrawSafely()
        {
            if(!controls.Visible || faulted || grid==null || grid.Closed || grid.MarkedForClose || points.Length==0 ||
                MyAPIGateway.Gui==null || MyAPIGateway.Gui.IsCursorVisible || MyHud.MinimalHud || MyHud.CutsceneHud) return;
            try
            {
                long drawStart=Stopwatch.GetTimestamp();
                Vector2 screen=MyGuiManager.GetScreenSizeFromNormalizedSize(Vector2.One,true);
                float side=Math.Min(screen.X*0.25f,screen.Y*0.32f);
                float left=screen.X-side-24, top=screen.Y*0.12f;
                var panel=new RectangleF(left-8,top-8,side+16,side+16);
                MyRenderProxy.DrawSprite(Texture,ref panel,null,new Color(5,18,25,190),0,false,false);
                float half=side/2;
                for(int i=0;i<points.Length;i++)
                    rotated[i]=controls.Project(SchematicGeometry.ToBasis(points[i],cockpitRight,cockpitUp,cockpitBackward));
                byte missingAlpha=(byte)(205+35*Math.Sin(clock.Elapsed.TotalSeconds*Math.PI));
                MarkerFill.Build(rotated,shadedMarkers,controls.Zoom,side,fillBands);
                foreach(var band in fillBands)
                {
                    var fill=new RectangleF(left+half+(float)band.X*half,top+half+(float)band.Y*half,
                        (float)band.Width*half,(float)band.Height*half);
                    var kind=lineKinds[band.MarkerStart/2];
                    var shade=LineColor(kind,missingAlpha);
                    shade.A=kind==ChangeKind.Missing?(byte)(missingAlpha/4):kind==ChangeKind.Damaged?(byte)25:(byte)60;
                    MyRenderProxy.DrawSprite(Texture,ref fill,null,shade,0,false,false);
                }
                for(int b=0;b<points.Length;b+=2)
                    {
                        var a=rotated[b]; var c=rotated[b+1];
                        double x0=a.X*controls.Zoom,y0=-a.Y*controls.Zoom,x1=c.X*controls.Zoom,y1=-c.Y*controls.Zoom;
                        if(!SchematicGeometry.Clip(ref x0,ref y0,ref x1,ref y1))continue;
                        var from=new Vector2(left+half+(float)x0*half,top+half+(float)y0*half);
                        var to=new Vector2(left+half+(float)x1*half,top+half+(float)y1*half);
                        var delta=to-from; float length=delta.Length(); if(length<0.25f)continue;
                        var line=new RectangleF((from.X+to.X-length)/2,(from.Y+to.Y)/2-0.5f,length,1);
                        MyRenderProxy.DrawSprite(Texture,ref line,null,LineColor(lineKinds[b/2],missingAlpha),(float)Math.Atan2(delta.Y,delta.X),false,false);
                    }
                MyGuiManager.DrawString("White",caption,new Vector2((left-8)/screen.X,(top+side+12)/screen.Y),0.55f,
                    useFullClientArea:true,maxTextWidth:0.28f);
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

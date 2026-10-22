using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using VRage.Game;
using VRageMath;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private Task<BlueprintDocument> blueprintLoad;
        private BlueprintDocument blueprint;
        private int blueprintGrid=-1,conversionIndex;
        private List<BlockSample> converted;
        private readonly List<AnchorFrame> blueprintAnchors=new List<AnchorFrame>();
        private BlockSample[] previewBlocks;
        private string previewProfile;
        private ShipProfile previewCandidate;
        private int skippedDefinitions,blueprintRevision;
        private string blueprintStatus="Choose a local or cloud blueprint, then Load.";
        private void BlueprintNotice(string message){blueprintStatus=message;blueprintRevision++;if(panel!=null)panel.NotifyBlueprint(message);else commandUtilities?.ShowMessage("Marigold",message);}
        private void CancelBlueprintPreview()
        {
            previewBlocks=null;previewProfile=null;previewCandidate=null;
        }
        private string BlueprintCommand(string verb)
        {
            if(verb=="cancel") { CancelBlueprintPreview();RefreshReference("Blueprint preview canceled; saved baseline unchanged.");return comparisonReport; }
            if(verb=="load")
            {
                if(blueprintLoad!=null)return "A blueprint is already loading.";
                string path=controls.ProfileArgument;
                CancelBlueprintPreview();blueprint=null;blueprintGrid=-1;blueprintAnchors.Clear();
                converted=null;blueprintLoad=path.StartsWith("cloud:",StringComparison.OrdinalIgnoreCase)?CloudBlueprints.Load(path.Substring(6)):Task.Run(()=>BlueprintDocument.Load(path));RefreshReference("Blueprint loading; saved baseline unchanged.");
                return "Reading blueprint. Its file is read-only; no blocks or scripts will be spawned.";
            }
            if(blueprint==null)return "First use /mg blueprint load \"full path to bp.sbc\".";
            if(verb=="grids")
            {
                var lines=new List<string>();
                for(int i=0;i<blueprint.Grids.Count;i++)lines.Add((i+1)+": "+blueprint.Grids[i].Name+" ("+blueprint.Grids[i].Blocks.Count+" blocks)");
                return string.Join("\n",lines)+"\nSelect with /mg blueprint grid N.";
            }
            if(verb=="grid")
            {
                int number;if(!int.TryParse(controls.ProfileArgument,out number) || number<1 || number>blueprint.Grids.Count)return "Choose a grid number from /mg blueprint grids.";
                CancelBlueprintPreview();blueprintGrid=number-1;conversionIndex=0;skippedDefinitions=0;converted=new List<BlockSample>();blueprintAnchors.Clear();
                RefreshReference("Resolving blueprint definitions.");return "Resolving the selected grid against installed block definitions.";
            }
            if(blueprintGrid<0)return "Choose a grid with /mg blueprint grid N.";
            if(converted!=null && conversionIndex<blueprint.Grids[blueprintGrid].Blocks.Count)return "Blueprint grid is still being prepared.";
            if(converted==null)return "Select the blueprint grid again to prepare it.";
            if(verb=="anchors")
            {
                var lines=new List<string>();for(int i=0;i<blueprintAnchors.Count;i++)lines.Add((i+1)+": "+blueprintAnchors[i].Name+" ["+blueprintAnchors[i].Definition+"]");
                return lines.Count==0?"No cockpit found in this grid.":string.Join("\n",lines)+"\nChoose the matching cockpit with /mg blueprint preview N.";
            }
            Reference selected;
            if(grid==null)return "Sit in the ship cockpit you want to align with the blueprint.";
            references.TryGetValue(grid,out selected);
            if(verb=="preview")
            {
                int number;if(!int.TryParse(controls.ProfileArgument,out number) || number<1 || number>blueprintAnchors.Count)return "Choose a cockpit number from /mg blueprint anchors.";
                var anchor=blueprintAnchors[number-1];var target=selected==null?SeatedAnchor():selected.Anchor;
                if(anchor.Definition!=target.Definition || Math.Abs(blueprint.Grids[blueprintGrid].GridSize-grid.GridSize)>.001)return "Blueprint cockpit definition or grid size does not match this profile anchor.";
                var canonical=ProfileAlignment.Transform(converted.ToArray(),anchor,true);
                // Validate before displaying; accept is the only operation that changes the saved baseline.
                var candidate=new ShipProfile { Id=selected==null?Guid.NewGuid().ToString("N"):selected.ProfileId,Name=grid.DisplayName??"Ship",GridSize=grid.GridSize,Anchor=target,Blocks=canonical,Station=controls.Station };
                ProfileAlignment.Validate(candidate);
                previewBlocks=ProfileAlignment.Transform(canonical,target,false);previewProfile=candidate.Id;previewCandidate=candidate;
                RefreshReference("UNSAVED blueprint preview. Inspect views and /mg report; use /mg blueprint accept or cancel. Skipped unavailable blocks: "+skippedDefinitions+".");
                return comparisonReport;
            }
            if(verb=="accept")
            {
                if(previewBlocks==null || previewCandidate==null)return "Choose the matching cockpit and Preview before accepting.";
                if(selected!=null && previewCandidate.Id!=selected.ProfileId)return "The selected profile changed; preview again.";
                var accepted=previewCandidate;profileStore.Put(accepted);BindProfile(accepted,accepted.Anchor);
                return "Blueprint saved as profile "+accepted.Number+". Unavailable blocks omitted: "+skippedDefinitions+".";
            }
            return "Blueprint commands: load, grids, grid, anchors, preview, accept, cancel.";
        }
        private void UpdateBlueprint()
        {
            if(blueprintLoad!=null && blueprintLoad.IsCompleted)
            {
                var task=blueprintLoad;blueprintLoad=null;
                try { blueprint=task.GetAwaiter().GetResult();BlueprintNotice("Loaded "+blueprint.Grids.Count+" grids. Choose a grid; its cockpits will appear when preparation finishes."); if(blueprint.Grids.Count==1){controls.ProfileArgument="1";BlueprintCommand("grid");} }
                catch(Exception e){BlueprintNotice("Blueprint load failed: "+e.Message);}
            }
            if(converted==null || blueprint==null || blueprintGrid<0)return;
            var source=blueprint.Grids[blueprintGrid];if(conversionIndex>=source.Blocks.Count)return;
            try
            {
                int end=Math.Min(source.Blocks.Count,conversionIndex+256);
                for(;conversionIndex<end;conversionIndex++)
                {
                    var raw=source.Blocks[conversionIndex];MyDefinitionId id;MyCubeBlockDefinition def;
                    if(!MyDefinitionId.TryParse(raw.Type,raw.Subtype,out id) || !MyDefinitionManager.Static.TryGetCubeBlockDefinition(id,out def)){skippedDefinitions++;continue;}
                    if((def.CubeSize==MyCubeSize.Large?2.5:.5)!=source.GridSize)throw new InvalidOperationException("Mixed-size/supergrid blueprints are unsupported.");
                    var orientation=new MyBlockOrientation((Base6Directions.Direction)raw.Forward,(Base6Directions.Direction)raw.Up);
                    if(!orientation.IsValid)throw new InvalidOperationException("Invalid block orientation.");
                    if(MyCubeGridDefinitions.GetCubeRotationOptions(def)==MyRotationOptionsEnum.None)orientation=MyBlockOrientation.Identity;
                    if(def.CubeDefinition!=null)orientation=MyCubeGridDefinitions.GetTopologyUniqueOrientation(def.CubeDefinition.CubeTopology,orientation);
                    var min=new Vector3I(raw.X,raw.Y,raw.Z);Vector3I max;
                    MySlimBlock.ComputeMax(def,orientation,ref min,out max);
                    var position=MySlimBlock.ComputePositionInGrid(new MatrixI(orientation),def,min);
                    converted.Add(new BlockSample { Position=new Point3(position.X,position.Y,position.Z),Bounds=new Box3(new Point3(min.X-.5,min.Y-.5,min.Z-.5),new Point3(max.X+.5,max.Y+.5,max.Z+.5)),Definition=def.Id.ToString(),Forward=(int)orientation.Forward,Up=(int)orientation.Up,Integrity=def.MaxIntegrity,BuildIntegrity=def.MaxIntegrity,MaxIntegrity=def.MaxIntegrity });
                    if(def is MyCockpitDefinition)
                    {
                        Matrix m;orientation.GetMatrix(out m);
                        blueprintAnchors.Add(new AnchorFrame { Position=new Point3(position.X,position.Y,position.Z),Right=new Point3(m.Right.X,m.Right.Y,m.Right.Z),Up=new Point3(m.Up.X,m.Up.Y,m.Up.Z),Backward=new Point3(m.Backward.X,m.Backward.Y,m.Backward.Z),Definition=def.Id.ToString(),Name=string.IsNullOrWhiteSpace(raw.Name)?def.Id.SubtypeName:raw.Name });
                    }
                }
                if(conversionIndex==source.Blocks.Count)BlueprintNotice("Ready: "+converted.Count+" blocks; "+skippedDefinitions+" unavailable blocks omitted. "+(blueprintAnchors.Count==0?"No usable cockpit remains. Enable the cockpit mod or choose another grid.":"Choose the matching cockpit and Preview. Accept creates a profile if needed."));
            }
            catch(Exception e){converted=null;previewBlocks=null;BlueprintNotice("Blueprint preparation failed: "+e.Message);}
        }
    }
}

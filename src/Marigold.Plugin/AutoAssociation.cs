using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using VRageMath;
namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed class AssociationResult
        { public int Generation;public AutoMatch Match;public BlockSample[] Sample;public AnchorFrame[] Anchors; }
        private Task<AssociationResult> associationTask;
        private int associationCooldown=120;
        private readonly MatchStability matchStability=new MatchStability();
        private readonly HashSet<MyCubeGrid> manualDetached=new HashSet<MyCubeGrid>();
        private string associationStatus="Waiting for a ship.";
        private void ResetAssociation(){associationCooldown=120;matchStability.Reset();associationStatus="Waiting for stable ship observations.";}
        private static AnchorFrame CockpitAnchor(MyCockpit cockpit)
        {
            Matrix m;cockpit.Orientation.GetMatrix(out m);
            return new AnchorFrame { Position=new Point3(cockpit.Position.X,cockpit.Position.Y,cockpit.Position.Z),Right=new Point3(m.Right.X,m.Right.Y,m.Right.Z),Up=new Point3(m.Up.X,m.Up.Y,m.Up.Z),Backward=new Point3(m.Backward.X,m.Backward.Y,m.Backward.Z),Definition=cockpit.BlockDefinition.Id.ToString(),Name=cockpit.CustomName.ToString() };
        }
        private string AutomationCommand(string action)
        {
            if(action=="status")
            {
                Reference selected;string state=associationStatus;
                if(grid!=null && references.TryGetValue(grid,out selected))state="Profile "+profileStore.Find(selected.ProfileId).Number.ToString()+" is selected. Existing selections are never automatically replaced.";
                else if(grid!=null && manualDetached.Contains(grid))state="Manually detached for this session. /mg auto retry allows matching again.";
                else if(profileStore.Warning!=null)state="Disabled while profile database recovery is pending. "+profileStore.Warning;
                else if(profileStore.Profiles.Count==0)state="No saved profiles. Use /mg capture once from the intended cockpit.";
                return "Auto reassociation: "+(controls.AutoAssociate?"on. ":"off. ")+state;
            }
            if(action=="retry") { if(grid!=null)manualDetached.Remove(grid);ResetAssociation();return "Automatic matching will retry if enabled; any selected profile is retained."; }
            bool enabled=action=="on";bool previous=controls.AutoAssociate;controls.AutoAssociate=enabled;
            try{DisplaySettings.Save(settingsPath,controls);}catch{controls.AutoAssociate=previous;throw;}
            ResetAssociation();return "Auto reassociation "+(enabled?"on":"off")+" and preference saved. Existing bindings are retained.";
        }
        private void UpdateAssociation()
        {
            // Observe completion even after disabling; no unobserved task exceptions.
            if(associationTask!=null && associationTask.IsCompleted)
            {
                var task=associationTask;associationTask=null;
                try
                {
                    var result=task.GetAwaiter().GetResult();
                    if(result.Generation==generation && controls.AutoAssociate && grid!=null && !references.ContainsKey(grid) && !manualDetached.Contains(grid))
                    {
                        associationStatus=result.Match.Message;
                        var match=result.Match;
                        string key=match.ProfileId==null?null:match.ProfileId+"/"+match.AnchorIndex;
                        if(!ObservationCache.Same(result.Sample,observation.Snapshot())) { matchStability.Reset();associationStatus="Ship changed during matching; waiting for it to settle."; }
                        else if(matchStability.Observe(key,result.Sample) && match.ProfileId!=null)
                        {
                            bool used=false;
                            foreach(var pair in references)if(pair.Value.ProfileId==match.ProfileId && pair.Key!=grid && !pair.Key.Closed && !pair.Key.MarkedForClose)used=true;
                            if(used)associationStatus="Matching profile is bound to another live ship. Select manually after detaching it.";
                            else
                            {
                                var profile=profileStore.Find(match.ProfileId);
                                BindProfile(profile,result.Anchors[match.AnchorIndex]);
                                associationStatus="Automatically selected "+profile.Name+" ("+profile.Number.ToString()+") at "+match.Score.ToString("P0")+" layout overlap.";
                                commandUtilities?.ShowMessage("Marigold",associationStatus+" /mg profile detach to undo; /mg auto off to disable.");
                            }
                        }
                    }
                }
                catch(Exception e){associationStatus="Matching failed: "+e.Message;}
            }
            if(!controls.AutoAssociate || grid==null || references.ContainsKey(grid) || manualDetached.Contains(grid) || profileStore.Warning!=null || profileStore.Profiles.Count==0)return;
            if(observation==null || !observation.Ready || associationTask!=null || --associationCooldown>0)return;
            associationCooldown=180;
            try
            {
                var anchors=new List<AnchorFrame>();
                foreach(var block in grid.GetBlocks())if(block.FatBlock is MyCockpit) { anchors.Add(CockpitAnchor((MyCockpit)block.FatBlock));if(anchors.Count>16)break; }
                if(anchors.Count==0){associationStatus="No anchor cockpit is currently visible on this grid.";matchStability.Reset();return;}
                var snapshot=observation.Snapshot();var candidates=anchors.ToArray();var profiles=new List<ShipProfile>(profileStore.Profiles);int version=generation;double size=grid.GridSize;
                associationTask=Task.Run(()=>new AssociationResult { Generation=version,Sample=snapshot,Anchors=candidates,Match=AutoMatcher.Find(snapshot,candidates,profiles,size) });
            }
            catch(Exception e){associationStatus="Waiting for ship data: "+e.Message;}
        }
    }
}

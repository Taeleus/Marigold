using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private Task<List<RecoveryCandidate>> recoveryTask;
        private CancellationTokenSource recoveryCancel;
        private List<RecoveryCandidate> recoveryCandidates;
        private ShipProfile recoveryProfile;
        private int recoveryIndex;
        private BlockSample[] recoveryPreview;
        private void CancelRecovery()
        {
            recoveryCancel?.Cancel();recoveryCancel?.Dispose();recoveryCancel=null;
            recoveryTask=null;recoveryCandidates=null;recoveryProfile=null;recoveryPreview=null;
        }
        private void RecoveryMessage(string text){if(panel!=null)panel.NotifyStatus(text);else commandUtilities?.ShowMessage("Marigold",text);}
        private string RecoveryCommand(string action)
        {
            if(grid==null)return "Control the remaining ship from a seat first.";
            if(action=="force")
            {
                var profile=profileStore.Find(controls.ProfileArgument);if(profile.Blocks==null)return "This profile has no baseline.";
                if(Math.Abs(profile.GridSize-grid.GridSize)>.001)return "Grid size does not match the profile.";
                if(observation==null || !observation.Ready)return "Wait for the ship scan to finish.";
                CancelRecovery();CancelBlueprintPreview();recoveryProfile=profile;controls.Visible=true;
                var snapshot=observation.Snapshot();recoveryCancel=new CancellationTokenSource();var token=recoveryCancel.Token;
                RefreshReference("Finding surviving-block alignment.");
                recoveryTask=Task.Run(()=>RecoveryMatcher.Find(profile,snapshot,token),token);
                recoveryTask.ContinueWith(t=>{var observed=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
                return "Finding surviving-block alignment. The saved baseline will not change.";
            }
            if(action=="cancel"){CancelRecovery();RefreshReference("Recovery canceled; previous binding retained.");return comparisonReport;}
            if(recoveryCandidates==null || recoveryCandidates.Count==0)return "First choose Force apply profile and wait for alignment.";
            if(action=="next")recoveryIndex=(recoveryIndex+1)%recoveryCandidates.Count;
            var candidate=recoveryCandidates[recoveryIndex];
            if(action.StartsWith("move "))
            {
                string axis=action.Substring(5);var p=candidate.Anchor.Position;
                candidate.Anchor.Position=new Point3(p.X+(axis=="x+"?1:axis=="x-"?-1:0),p.Y+(axis=="y+"?1:axis=="y-"?-1:0),p.Z+(axis=="z+"?1:axis=="z-"?-1:0));
                candidate.Matches=RecoveryMatcher.Score(ProfileAlignment.Transform(recoveryProfile.Blocks,candidate.Anchor,false),observation.Snapshot());
            }
            if(action=="accept")
            {
                if(profileStore.Find(recoveryProfile.Id).Blocks!=recoveryProfile.Blocks){CancelRecovery();return "Baseline changed; start recovery again.";}
                var transformed=ProfileAlignment.Transform(recoveryProfile.Blocks,candidate.Anchor,false);
                if(RecoveryMatcher.Score(transformed,observation.Snapshot())<3)return "Too little matching structure remains. Start recovery again.";
                var p=recoveryProfile;var anchor=candidate.Anchor;CancelRecovery();BindProfile(p,anchor);manualDetached.Remove(grid);
                return "Recovery applied to this grid. Baseline unchanged. Verify missing sections; use Force apply again after reconnect if normal matching cannot find the lost cockpit.";
            }
            recoveryPreview=ProfileAlignment.Transform(recoveryProfile.Blocks,candidate.Anchor,false);RefreshReference("Recovery alignment preview.");
            return "UNSAVED recovery "+(recoveryIndex+1)+"/"+recoveryCandidates.Count+": "+candidate.Summary+" Alternatives may tie; this is not identity proof. Inspect, Next alignment, then Accept recovery or Cancel.";
        }
        private void UpdateRecovery()
        {
            if(recoveryTask==null || !recoveryTask.IsCompleted)return;
            var task=recoveryTask;recoveryTask=null;
            try{recoveryCandidates=task.GetAwaiter().GetResult();recoveryIndex=0;RecoveryMessage(recoveryCandidates.Count==0?"No supported alignment with at least 3 matches. Try another saved profile; sparse/symmetric remnants may not be recoverable.":RecoveryCommand("preview"));}
            catch(OperationCanceledException){}catch(Exception e){CancelRecovery();RecoveryMessage("Recovery failed: "+e.Message);}
        }
    }
}

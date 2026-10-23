using System;
using System.IO;
using System.Collections.Generic;
using Marigold;
internal static class ReferenceLifecycleChecks
{
    private static int checks;
    private static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);checks++;}
    private static AnchorFrame Frame(string definition,double x=0)
    {return new AnchorFrame{Position=new Point3(x,0,0),Right=new Point3(1,0,0),Up=new Point3(0,1,0),Backward=new Point3(0,0,1),Definition=definition,Name="Seat"};}
    public static void Run()
    {
        string dir=Path.Combine(Directory.GetCurrentDirectory(),".cache","ReferenceLifecycle-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try
        {
            var blocks=new BlockSample[20];for(int i=0;i<blocks.Length;i++)blocks[i]=new BlockSample{Position=new Point3(i,i%3,0),Bounds=new Box3(new Point3(i-.5,i%3-.5,-.5),new Point3(i+.5,i%3+.5,.5)),Definition="Block/"+i,Forward=0,Up=4,Integrity=100,BuildIntegrity=100,MaxIntegrity=100};
            var store=new ProfileStore(dir);var p=new ShipProfile{Id=Guid.NewGuid().ToString("N"),Name="Ship",Anchor=Frame("Lost cockpit"),GridSize=2.5,Blocks=blocks,Source="blueprint",BlueprintIdentity="cloud:Blueprints/cloud/Ship/bp.sbc",ReferenceName="Original"};store.Put(p);
            var reloaded=new ProfileStore(dir).Find(p.Id);
            Check(reloaded.ReferenceId==p.ReferenceId && reloaded.CreatedUtcTicks>0 && reloaded.Source=="blueprint" && reloaded.BlueprintIdentity==p.BlueprintIdentity,"Reference metadata persists");
            var history=new ProfileStore(Path.Combine(dir,"versions"));ReferenceHistory.Archive(history,p);ReferenceHistory.Archive(history,p);
            Check(history.Profiles.Count==1 && history.Profiles[0].ReferenceId==p.ReferenceId,"Automatic archive is idempotent and retains identity");
            var named=ReferenceHistory.SaveNamed(history,p,"Original design");Check(named.ReferenceId!=p.ReferenceId && named.Source==p.Source && named.CreatedUtcTicks==p.CreatedUtcTicks,"Named revision has distinct identity and retains origin metadata");
            var changed=p.Copy();changed.ReferenceId="";changed.Source="capture";changed.Blocks=(BlockSample[])blocks.Clone();changed.Blocks[0].Definition="Refit";store.Put(changed);
            Check(history.Profiles[0].Blocks[0].Definition=="Block/0" && changed.ReferenceId!=p.ReferenceId,"Replacement does not mutate archived baseline");
            var seat=Frame("Replacement cockpit",8);seat.Right=new Point3(0,0,-1);seat.Backward=new Point3(1,0,0);
            var liveBaseline=Frame("Lost cockpit",12);liveBaseline.Up=new Point3(0,0,-1);liveBaseline.Backward=new Point3(0,1,0);
            var relative=ReferenceHistory.Relative(liveBaseline,seat);
            Check(ReferenceHistory.SameAnchor(ReferenceHistory.Compose(relative,seat),liveBaseline),"Baseline-to-seat frame round trip with different rotations");
            var world=ProfileAlignment.Transform(blocks,liveBaseline,false);var remnant=new BlockSample[10];Array.Copy(world,remnant,10);
            var saved=p.Copy();saved.Anchor=relative;saved.Blocks=ProfileAlignment.Transform(remnant,seat,true);
            var recoveries=new ProfileStore(Path.Combine(dir,"recovery"));recoveries.Put(saved);saved=new ProfileStore(Path.Combine(dir,"recovery")).Find(p.Id);
            var newSeat=Frame("Replacement cockpit",-30);newSeat.Right=new Point3(0,0,1);newSeat.Backward=new Point3(-1,0,0);
            var newRemnant=ProfileAlignment.Transform(saved.Blocks,newSeat,false);
            var matched=AutoMatcher.Find(newRemnant,new[]{newSeat},new[]{p},2.5,new[]{saved});
            Check(matched.ProfileId==p.Id && matched.Recovered,"Recovered remnant matches after reload, translation and rotation");
            var expected=ReferenceHistory.Compose(relative,newSeat);Check(ReferenceHistory.SameAnchor(matched.BindingAnchor,expected),"Reassociation restores original baseline axes rather than replacement seat axes");
            Check(SnapshotComparison.Compare(ProfileAlignment.Transform(p.Blocks,matched.BindingAnchor,false),newRemnant).Counts[(int)ChangeKind.Missing]==10,"Recovered binding retains missing half of original baseline");
            Check(AutoMatcher.Find(newRemnant,new[]{newSeat},new[]{changed},2.5,new[]{saved}).ProfileId==null,"Stale recovery signature cannot bind a changed baseline");
            var twin=p.Copy();twin.Id=Guid.NewGuid().ToString("N");var twinRecovery=saved.Copy();twinRecovery.Id=twin.Id;
            Check(AutoMatcher.Find(newRemnant,new[]{newSeat},new[]{p,twin},2.5,new[]{saved,twinRecovery}).ProfileId==null,"Identical recovered ships remain ambiguous");
            var repaired=ProfileAlignment.Transform(blocks,expected,false);Check(AutoMatcher.Find(repaired,new[]{newSeat},new[]{p},2.5,new[]{saved}).ProfileId==p.Id,"Fully repaired ship can reuse accepted recovery alignment");
            var noHistory=AutoMatcher.Find(newRemnant,new[]{newSeat},new[]{p},2.5);Check(noHistory.ProfileId==null,"No speculative recovery without explicit acceptance");
            while(history.Profiles.Count<16)ReferenceHistory.SaveNamed(history,p,"Snapshot "+history.Profiles.Count);
            bool refused=false;try{ReferenceHistory.Archive(history,changed);}catch(InvalidOperationException){refused=true;}
            Check(refused && new ProfileStore(Path.Combine(dir,"versions")).Profiles.Count==16,"Full revision library rejects archival without deleting history");
        }
        finally{Directory.Delete(dir,true);}
        Console.WriteLine("PASS: "+checks+" persistent recovery/reference lifecycle checks.");
    }
}

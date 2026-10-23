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
            var partial=new BlockSample[15];Array.Copy(repaired,partial,15);
            Check(AutoMatcher.Find(partial,new[]{newSeat},new[]{p},2.5,new[]{saved}).ProfileId==p.Id,"Partial repairs retain the accepted alignment");
            var differentSeat=newSeat;differentSeat.Definition="Another seat type";
            Check(AutoMatcher.Find(partial,new[]{differentSeat},new[]{p},2.5,new[]{saved}).ProfileId==p.Id,"Cockpit type change relies on structural evidence");
            var foreign=(BlockSample[])partial.Clone();for(int i=10;i<15;i++)foreign[i].Definition="Foreign";
            Check(AutoMatcher.Find(foreign,new[]{differentSeat},new[]{p},2.5,new[]{saved}).ProfileId==null,"Unrelated additions cannot pass as repairs");
            var tiny=new BlockSample[7];Array.Copy(repaired,tiny,7);Check(AutoMatcher.Find(tiny,new[]{differentSeat},new[]{p},2.5,new[]{saved}).ProfileId==null,"Repair matching does not weaken minimum evidence");
            foreach(string axis in new[]{"x","y","z"}){var rotated=expected;for(int turn=0;turn<4;turn++)rotated=RecoveryMatcher.Rotate(rotated,axis);Check(ReferenceHistory.SameAnchor(rotated,expected),"Four manual quarter turns restore the original frame");}
            var renamed=ReferenceHistory.Rename(history,named,"Refit plan");Check(renamed.ReferenceId==named.ReferenceId && renamed.Blocks==named.Blocks && history.Find(named.Id).Name=="Refit plan","Renaming retains reference identity and blocks");
            var replaced=ReferenceHistory.Replace(history,renamed,changed);Check(replaced.ReferenceId==changed.ReferenceId && history.Find(named.Id).Blocks[0].Definition=="Refit","Replace uses active baseline contents");
            bool oldPreserved=false;foreach(var item in history.Profiles)if(item.Id!=named.Id && item.ReferenceId==named.ReferenceId && item.Blocks[0].Definition=="Block/0")oldPreserved=true;
            Check(oldPreserved,"Replace preserves overwritten version");
            var cockpitChanged=(BlockSample[])partial.Clone();cockpitChanged[0].Definition="New cockpit";
            Check(AutoMatcher.Find(cockpitChanged,new[]{differentSeat},new[]{p},2.5,new[]{saved}).ProfileId==p.Id,"One replaced cockpit block is tolerated with enough unchanged structure");
            var styleBlocks=new[]{blocks[0],blocks[1]};styleBlocks[0].Definition="MyObjectBuilder_CubeBlock/Armor";styleBlocks[1].Definition="MyObjectBuilder_Thrust/Engine";
            var styles=DisplayFilter.DefaultStyles.ToCharArray();styles[Array.IndexOf(DisplayFilter.Categories.Split(' '),"armor")]='2';styles[Array.IndexOf(DisplayFilter.Categories.Split(' '),"thrusters")]='3';
            Check(StationGeometry.StyledBoxes(styleBlocks,"all",new string(styles),'2').Count==1 && StationGeometry.StyledBoxes(styleBlocks,"all",new string(styles),'3').Count==1,"Station styles separate faint armor and highlighted thrusters");
            Check(StationGeometry.StyledBoxes(styleBlocks,"armor",new string(styles),'3').Count==0,"Station category filter still applies to styles");
            var longBox=new Box3(new Point3(0,0,0),new Point3(100003,1,1));var coarse=OutlineGeometry.Build(new[]{longBox},0,longBox);var center=StationGeometry.Center(longBox);double radius=StationGeometry.Radius(longBox);
            bool within=true;foreach(var point in coarse.Points){double x=point.X*radius+center.X,y=point.Y*radius+center.Y,z=point.Z*radius+center.Z;within &= x>=-1e-8 && x<=100003+1e-8 && y>=-1e-8 && y<=1+1e-8 && z>=-1e-8 && z<=1+1e-8;}
            Check(OutlineGeometry.Build(new[]{longBox},2,longBox,12).Points.Length<=24,"Explicit station line budget is enforced");
            Check(coarse.CellSize>1 && within,"Coarse outline stays within actual hull extents");
            Check(UiText.Get("es","[ Customize ]")!="[ Customize ]" && UiText.Get("es","Baseline / Saved versions")!="Baseline / Saved versions","Spanish active tabs and page headings translate");
            Check(UiText.Get("en","Replace version")=="Replace version" && UiText.Get("es","Unknown diagnostic") == "Unknown diagnostic","English and unknown diagnostic fallback remain intact");
            var modifiedHistory=saved.Copy();modifiedHistory.Blocks=(BlockSample[])saved.Blocks.Clone();modifiedHistory.Blocks[0].Definition="Temporary cockpit";modifiedHistory.Blocks[1].Definition="Temporary addition";
            Check(AutoMatcher.Find(repaired,new[]{differentSeat},new[]{p},2.5,new[]{modifiedHistory}).ProfileId==p.Id,"Exact full-baseline match survives removal of temporary remnant blocks");
            var noHistory=AutoMatcher.Find(newRemnant,new[]{newSeat},new[]{p},2.5);Check(noHistory.ProfileId==null,"No speculative recovery without explicit acceptance");
            while(history.Profiles.Count<16)ReferenceHistory.SaveNamed(history,p,"Snapshot "+history.Profiles.Count);
            bool replaceRefused=false;try{ReferenceHistory.Replace(history,history.Find(named.Id),p);}catch(InvalidOperationException){replaceRefused=true;}
            Check(replaceRefused && history.Find(named.Id).ReferenceId==changed.ReferenceId,"Full library refuses replacement before mutation");
            var another=changed.Copy();another.ReferenceId=Guid.NewGuid().ToString("N");bool refused=false;try{ReferenceHistory.Archive(history,another);}catch(InvalidOperationException){refused=true;}
            Check(refused && new ProfileStore(Path.Combine(dir,"versions")).Profiles.Count==16,"Full revision library rejects archival without deleting history");
        }
        finally{Directory.Delete(dir,true);}
        Console.WriteLine("PASS: "+checks+" persistent recovery/reference lifecycle checks.");
    }
}

using System;
using Marigold;
internal static class SnapshotChecks
{
    private static int checks;
    private static void Check(bool value,string message) { if(!value)throw new Exception(message);checks++; }
    private static BlockSample Sample(int x)
    {
        return new BlockSample { Position=new Point3(x,0,0),Bounds=new Box3(new Point3(x-.5,-.5,-.5),new Point3(x+.5,.5,.5)),
            Definition="Armor",Forward=0,Up=4,Integrity=100,BuildIntegrity=100,MaxIntegrity=100 };
    }
    private static Comparison Compare(BlockSample a,BlockSample b) { return SnapshotComparison.Compare(new[]{a},new[]{b}); }
    public static void Run()
    {
        var a=Sample(0);var b=a;
        Check(Compare(a,b).Changes.Count==0,"Identical block unchanged");
        b.Integrity=70;Check(Compare(a,b).Counts[2]==1,"Damage highlighted");
        b.BuildIntegrity=70;Check(Compare(a,b).Counts[2]==1,"Grinding is degradation even without current damage");
        Check(Compare(a,a).Changes.Count==0,"Welding to captured integrity clears damage");
        Check(Compare(b,a).Changes.Count==0,"Improvement over partial reference is not damage");
        b=a;b.HasFunctionalState=true;b.IsFunctional=false;
        Check(Compare(a,b).Counts[(int)ChangeKind.NonFunctional]==1,"Non-functional block takes orange precedence");
        b.Integrity=50;Check(Compare(a,b).Counts[2]==0,"Broken block is not also yellow");
        b.IsFunctional=true;Check(Compare(a,b).Counts[2]==1,"Repaired above functional threshold remains yellow while damaged");
        b.Integrity=100;Check(Compare(a,b).Changes.Count==0,"Functional repaired block clears marker");
        b.HasFunctionalState=false;b.IsFunctional=false;b.Integrity=50;
        Check(Compare(a,b).Counts[2]==1,"Armor without functional state remains yellow");
        b=a;b.Integrity=99.999;Check(Compare(a,b).Changes.Count==0,"Float noise ignored");
        b=a;b.Integrity=double.NaN;Check(Compare(a,b).Counts[5]==1,"Invalid integrity unknown");
        b=a;b.MaxIntegrity=0;Check(Compare(a,b).Counts[5]==1,"Invalid maximum unknown");
        b=a;b.Definition="Modded/Reactor";Check(Compare(a,b).Counts[3]==1,"Changed definition replaced");
        b=a;b.Forward=2;Check(Compare(a,b).Counts[3]==1,"Changed orientation replaced");
        b=a;b.Up=1;Check(Compare(a,b).Counts[3]==1,"Changed up replaced");
        b=a;b.Bounds.Max=new Point3(2.5,.5,.5);Check(Compare(a,b).Counts[3]==1,"Changed multi-cell footprint replaced");
        var missing=SnapshotComparison.Compare(new[]{b},new BlockSample[0]);
        Check(missing.Counts[1]==1 && missing.Changes[0].Bounds.Max.X==2.5,"Missing keeps entire multi-cell envelope");
        Check(SnapshotComparison.Compare(new[]{b},new BlockSample[0]).Counts[1]==1,"Absence is missing regardless of split or network context");
        Check(SnapshotComparison.Compare(new BlockSample[0],new[]{a}).Counts[4]==1,"Observed addition is known");
        var moved=Compare(a,Sample(3));Check(moved.Counts[1]==1 && moved.Counts[4]==1,"Moved block absent plus added");
        var original=new[]{a,Sample(2)};
        Check(SnapshotComparison.Compare(original,new[]{Sample(2),a}).Changes.Count==0,"Order independent");
        b=a;b.Integrity=50;SnapshotComparison.Compare(original,new[]{b});
        Check(original[0].Integrity==100 && original.Length==2,"Comparison preserves reference");
        var frame=new Box3(new Point3(-.5,-.5,-.5),new Point3(4.5,.5,.5));
        var outline=OutlineGeometry.Build(new[]{a.Bounds},1,frame);
        double radius=Math.Sqrt(27)/2;
        Check(Math.Abs(outline.Points[0].X-(-2.5/radius))<1e-9,"Outline uses shared reference frame");
        var controls=new SchematicControls();bool send=true;string response;
        Check(controls.TryHandle("/mg capture",ref send,out response) && !send && controls.ReferenceAction=="capture","Capture is local");
        controls.Execute("/mg clear");Check(controls.ReferenceAction=="clear","Clear recognized");
        controls.Execute("/mg report");Check(controls.ReferenceAction=="report","Report recognized");
        controls.Execute("/mg top");Check(controls.ReferenceAction==null,"No stale reference command");
        controls.Execute("/mg capture extra");Check(controls.ReferenceAction==null,"Invalid capture rejected");
        Console.WriteLine("PASS: "+checks+" snapshot comparison checks.");
    }
}

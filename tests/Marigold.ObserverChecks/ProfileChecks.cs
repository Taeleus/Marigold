using System;
using System.IO;
using Marigold;
internal static class ProfileChecks
{
    private static int checks;
    private static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
    private static AnchorFrame Anchor(Point3 right,Point3 up,Point3 back)
    {return new AnchorFrame { Position=new Point3(12,-3,8),Right=right,Up=up,Backward=back,Definition="Cockpit",Name="Bridge" };}
    private static BlockSample Block()
    {return new BlockSample { Position=new Point3(2,0,-3),Bounds=new Box3(new Point3(1.5,-.5,-3.5),new Point3(3.5,1.5,-1.5)),Definition="Modded/Block",Forward=0,Up=4,Integrity=80,BuildIntegrity=100,MaxIntegrity=100,HasFunctionalState=true,IsFunctional=true };}
    public static void Run()
    {
        var axes=new[]{new Point3(1,0,0),new Point3(-1,0,0),new Point3(0,1,0),new Point3(0,-1,0),new Point3(0,0,1),new Point3(0,0,-1)};
        var local=new[]{Block()};
        foreach(var right in axes)foreach(var up in axes)
        {
            if(right.X*up.X+right.Y*up.Y+right.Z*up.Z!=0)continue;
            var back=new Point3(right.Y*up.Z-right.Z*up.Y,right.Z*up.X-right.X*up.Z,right.X*up.Y-right.Y*up.X);
            var anchor=Anchor(right,up,back);
            var grid=ProfileAlignment.Transform(local,anchor,false);
            var roundtrip=ProfileAlignment.Transform(grid,anchor,true);
            Check(SnapshotComparison.Compare(local,roundtrip).Changes.Count==1,"Preexisting damage preserved");
            Check(roundtrip[0].Position.Equals(local[0].Position) && roundtrip[0].Bounds.Min.Equals(local[0].Bounds.Min) && roundtrip[0].Bounds.Max.Equals(local[0].Bounds.Max),"Multi-cell geometry roundtrip");
            Check(roundtrip[0].Forward==0 && roundtrip[0].Up==4,"Orientation roundtrip");
            Check(grid[0].Position.Equals(anchor.World(local[0].Position)),"Anchor translation");
        }
        var directory=Path.Combine(Path.Combine(Directory.GetCurrentDirectory(),".cache"),"MarigoldProfileTests-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store=new ProfileStore(directory);
            Check(store.Profiles.Count==0 && !File.Exists(Path.Combine(directory,"profiles.bin")),"Read-only initialization");
            var anchor=Anchor(axes[0],axes[2],axes[4]);
            var first=new ShipProfile { Id=Guid.NewGuid().ToString("N"),Name="Twin ship",GridSize=2.5,Anchor=anchor,Blocks=local };
            store.Put(first);
            var loaded=new ProfileStore(directory);var saved=loaded.Find(first.Id.Substring(0,8));
            Check(saved.Name==first.Name && saved.Blocks.Length==1 && saved.Anchor.Name=="Bridge" && saved.GridSize==2.5,"Disk roundtrip");
            Check(saved.Blocks[0].Integrity==80 && saved.Blocks[0].HasFunctionalState && saved.Blocks[0].IsFunctional,"Block state roundtrip");
            var second=new ShipProfile { Id=Guid.NewGuid().ToString("N"),Name=first.Name,GridSize=2.5,Anchor=anchor,Blocks=null };
            store.Put(second);
            Check(new ProfileStore(directory).Profiles.Count==2,"Identical names retain distinct IDs");
            Check(File.Exists(Path.Combine(directory,"profiles.bin.bak")),"Atomic replacement keeps backup");
            var invalid=new ShipProfile { Id=Guid.NewGuid().ToString("N"),Name="bad",GridSize=2.5,Anchor=anchor,Blocks=local };
            invalid.Anchor.Right=invalid.Anchor.Up;
            bool rejected=false;try{store.Put(invalid);}catch(InvalidOperationException){rejected=true;}
            Check(rejected && store.Profiles.Count==2 && new ProfileStore(directory).Profiles.Count==2,"Invalid save leaves disk and memory intact");
            store.Put(new ShipProfile { Id=first.Id,Name="Renamed",GridSize=2.5,Anchor=anchor,Blocks=null });
            Check(store.Find(first.Id).Blocks==null && store.Profiles.Count==2,"Clear baseline preserves profile");
            store.Delete(second.Id);
            Check(new ProfileStore(directory).Profiles.Count==1,"Delete persists");
            File.WriteAllBytes(Path.Combine(directory,"profiles.bin"),new byte[]{1,2,3});
            var damaged=new ProfileStore(directory);
            Check(damaged.Warning!=null && damaged.Profiles.Count==2,"Corrupt primary loads prior backup read-only");
            rejected=false;try{damaged.Put(first);}catch(InvalidOperationException){rejected=true;}
            Check(rejected && new FileInfo(Path.Combine(directory,"profiles.bin")).Length==3,"Corrupt primary preserved until recovery");
            damaged.Recover();
            Check(damaged.Warning==null && new ProfileStore(directory).Profiles.Count==2,"Explicit backup recovery");
            Check(Directory.GetFiles(directory,"*.unreadable-*").Length==1,"Recovery preserves original corrupt file");
            Check(Directory.GetFiles(directory,"*.tmp").Length==0,"No abandoned temporary files");
            var badDir=Path.Combine(directory,"unsupported");Directory.CreateDirectory(badDir);
            using(var w=new BinaryWriter(File.Create(Path.Combine(badDir,"profiles.bin")))){w.Write(0x4D475046);w.Write(999);w.Write(0);}
            var unsupported=new ProfileStore(badDir);
            Check(unsupported.Warning!=null && unsupported.Profiles.Count==0,"Unknown schema not treated as empty writable data");
            rejected=false;try{unsupported.Put(first);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Unknown schema cannot be overwritten");
            var missingDir=Path.Combine(directory,"missing-primary");Directory.CreateDirectory(missingDir);
            File.Copy(Path.Combine(directory,"profiles.bin"),Path.Combine(missingDir,"profiles.bin.bak"));
            var missingPrimary=new ProfileStore(missingDir);
            Check(missingPrimary.Warning!=null && missingPrimary.Profiles.Count==2,"Missing primary offers backup recovery");
            missingPrimary.Recover();Check(new ProfileStore(missingDir).Warning==null,"Missing primary recovered atomically");
            var controls=new SchematicControls();bool send=true;string response;
            Check(controls.TryHandle("/mg anchor",ref send,out response) && !send && controls.ReferenceAction=="anchor","Anchor command local");
            controls.Execute("/mg profile select abcdef12");Check(controls.ReferenceAction=="profile select" && controls.ProfileArgument=="abcdef12","Profile selection parsed");
            controls.Execute("/mg profile delete abcdef12");Check(controls.ReferenceAction=="profile delete","Profile deletion parsed");
            controls.Execute("/mg profile detach");Check(controls.ReferenceAction=="profile detach","Detach parsed");
            controls.Execute("/mg profiles");Check(controls.ReferenceAction=="profiles" && controls.ProfileArgument==null,"List clears stale arguments");
            controls.Execute("/mg profile select");Check(controls.ReferenceAction==null,"Malformed command rejected");
        }
        finally {Directory.Delete(directory,true);}
        Console.WriteLine("PASS: "+checks+" profile persistence/alignment checks.");
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Marigold;

internal static class DataPathChecks
{
    private static int checks;
    private static void Check(bool value,string message)
    {if(!value)throw new Exception(message);checks++;}
    private static void Write(string root,string relative,byte[] data)
    {
        string path=Path.Combine(root,relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,data);
    }
    private static Dictionary<string,byte[]> Snapshot(string root)
    {
        var result=new Dictionary<string,byte[]>();
        foreach(string file in Directory.GetFiles(root,"*",SearchOption.AllDirectories))
            result[file.Substring(root.Length+1)]=File.ReadAllBytes(file);
        return result;
    }
    private static void SameFiles(string root,Dictionary<string,byte[]> expected,string message)
    {
        var actual=Snapshot(root);Check(actual.Count==expected.Count,message+" file count");
        foreach(var item in expected)
        {
            byte[] bytes;bool equal=actual.TryGetValue(item.Key,out bytes) && bytes.Length==item.Value.Length;
            if(equal)for(int i=0;i<bytes.Length;i++)if(bytes[i]!=item.Value[i]){equal=false;break;}
            Check(equal,message+": "+item.Key);
        }
    }
    private static bool Refused(string gameRoot,string roaming)
    {
        try{DataPaths.Initialize(gameRoot,roaming);return false;}
        catch(ArgumentException){return true;}
        catch(IOException){return true;}
        catch(UnauthorizedAccessException){return true;}
        catch(InvalidOperationException){return true;}
    }
    public static void Run()
    {
        string cache=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),".cache"));
        string sandbox=Path.GetFullPath(Path.Combine(cache,"DataPathChecks-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(sandbox);
        try
        {
            string roaming=Path.Combine(sandbox,"migration","Roaming"),legacy=Path.Combine(roaming,"Marigold"),game=Path.Combine(roaming,"SpaceEngineers");
            var anchor=new AnchorFrame{Position=new Point3(0,0,0),Right=new Point3(1,0,0),Up=new Point3(0,1,0),Backward=new Point3(0,0,1),Definition="Cockpit",Name="Bridge"};
            var sample=new BlockSample{Position=new Point3(0,0,0),Bounds=new Box3(new Point3(-.5,-.5,-.5),new Point3(.5,.5,.5)),Definition="MyObjectBuilder_Cockpit/Bridge",Forward=0,Up=4,Integrity=100,BuildIntegrity=100,MaxIntegrity=100};
            var profile=new ShipProfile{Id=Guid.NewGuid().ToString("N"),Name="Migration fixture",GridSize=2.5,Anchor=anchor,Blocks=new[]{sample}};
            var store=new ProfileStore(legacy);store.Put(profile);store.Put(profile);
            Write(legacy,"display.txt",System.Text.Encoding.UTF8.GetBytes("saved display"));
            Write(legacy,"hotkeys.txt",System.Text.Encoding.UTF8.GetBytes("Ctrl+F6"));
            Write(legacy,Path.Combine("presets","My preset.txt"),new byte[]{0,1,2,255});
            Write(legacy,Path.Combine("revisions",profile.Id,"profiles.bin"),new byte[]{42,0,19});
            Write(legacy,Path.Combine("recovery","profiles.bin.bak"),new byte[]{55,17});
            Write(legacy,"profiles.bin.unreadable-test",new byte[]{13,10,0,128});
            Directory.CreateDirectory(Path.Combine(legacy,"empty-folder"));
            var original=Snapshot(legacy);
            var migrated=DataPaths.Initialize(game+Path.DirectorySeparatorChar,roaming);
            string expectedRoot=Path.Combine(game,"Storage","Marigold");
            Check(string.Equals(migrated.Root,expectedRoot,StringComparison.OrdinalIgnoreCase),"Data belongs under the selected game's Storage folder");
            Check(!string.IsNullOrWhiteSpace(migrated.Notice),"Migration produces one startup notice");
            SameFiles(migrated.Root,original,"Migrated tree preserves every file byte");
            SameFiles(legacy,original,"Original migration source remains intact");
            Check(Directory.Exists(Path.Combine(migrated.Root,"empty-folder")),"Empty source directories are preserved");
            var loaded=new ProfileStore(migrated.Root);
            Check(loaded.Warning==null && loaded.Find("1").Id==profile.Id && loaded.Find("1").Name==profile.Name,"Migrated profile store is readable with unchanged identity");
            Write(legacy,"later-source-file.txt",new byte[]{99});
            var repeated=DataPaths.Initialize(game,roaming);
            Check(repeated.Root==migrated.Root && !File.Exists(Path.Combine(repeated.Root,"later-source-file.txt")),"Repeated startup never remerges an existing destination");
            SameFiles(repeated.Root,original,"Repeated initialization preserves migrated destination");

            string existingRoaming=Path.Combine(sandbox,"existing","Roaming"),existingGame=Path.Combine(existingRoaming,"SpaceEngineers"),existingLegacy=Path.Combine(existingRoaming,"Marigold"),existingTarget=Path.Combine(existingGame,"Storage","Marigold");
            Write(existingLegacy,"display.txt",new byte[]{10});Write(existingTarget,"display.txt",new byte[]{20});
            Write(existingLegacy,"old-only.txt",new byte[]{30});var existingFiles=Snapshot(existingTarget);
            var retained=DataPaths.Initialize(existingGame,existingRoaming);
            SameFiles(retained.Root,existingFiles,"Existing target is never overwritten or merged");
            Check(File.ReadAllBytes(Path.Combine(existingLegacy,"display.txt"))[0]==10,"Existing destination does not modify legacy source");

            string customGame=Path.Combine(sandbox,"custom-game");
            var isolated=DataPaths.Initialize(customGame,roaming);
            Check(isolated.Root==Path.Combine(customGame,"Storage","Marigold"),"Custom -appdata selects its own data root");
            Check(!Directory.Exists(isolated.Root),"Custom -appdata does not automatically import shared legacy data");
            Check(!string.IsNullOrWhiteSpace(isolated.Notice),"Custom installation is informed when old data exists elsewhere");
            string otherCustom=Path.Combine(sandbox,"another-custom-game");
            Check(DataPaths.Initialize(otherCustom,roaming).Root!=isolated.Root,"Different game data roots remain isolated");

            string freshRoaming=Path.Combine(sandbox,"fresh","Roaming"),freshGame=Path.Combine(freshRoaming,"SpaceEngineers");
            var fresh=DataPaths.Initialize(freshGame,freshRoaming);
            Check(fresh.Root==Path.Combine(freshGame,"Storage","Marigold") && !Directory.Exists(fresh.Root),"No legacy data means no eager empty destination");
            string emptyRoaming=Path.Combine(sandbox,"empty","Roaming");Directory.CreateDirectory(Path.Combine(emptyRoaming,"Marigold"));
            var empty=DataPaths.Initialize(Path.Combine(emptyRoaming,"SpaceEngineers"),emptyRoaming);
            Check(Directory.Exists(empty.Root) && Directory.GetFileSystemEntries(empty.Root).Length==0,"Empty legacy directory migrates safely");
            Check(Refused(null,roaming) && Refused("",roaming) && Refused("relative-game-folder",roaming),"Uninitialized or relative game roots cannot redirect plugin writes");
            Check(Refused(game,null) && Refused(game,"relative-roaming"),"Invalid prior-data locations are rejected before migration");
            string collisionRoaming=Path.Combine(sandbox,"collision","Roaming"),collisionGame=Path.Combine(collisionRoaming,"SpaceEngineers");
            string collisionRoot=Path.Combine(collisionGame,"Storage","Marigold");
            Directory.CreateDirectory(Path.GetDirectoryName(collisionRoot));File.WriteAllBytes(collisionRoot,new byte[]{8,6});
            Check(Refused(collisionGame,collisionRoaming),"A file occupying the destination is never overwritten");
            Check(File.ReadAllBytes(collisionRoot)[0]==8,"Destination collision preserves preexisting bytes");

            if(Path.DirectorySeparatorChar=='\\')
            {
                string caseRoaming=Path.Combine(sandbox,"case","Roaming"),caseGame=Path.Combine(caseRoaming,"SpaceEngineers");
                Write(Path.Combine(caseRoaming,"Marigold"),"config.txt",new byte[]{7});
                var normalized=DataPaths.Initialize(caseGame.ToUpperInvariant()+"\\",caseRoaming);
                Check(File.Exists(Path.Combine(normalized.Root,"config.txt")),"Default root recognition ignores Windows case and trailing separators");

                string failedRoaming=Path.Combine(sandbox,"locked","Roaming"),failedGame=Path.Combine(failedRoaming,"SpaceEngineers"),failedLegacy=Path.Combine(failedRoaming,"Marigold");
                Write(failedLegacy,"locked.bin",new byte[]{3,1,4});string failedRoot=Path.Combine(failedGame,"Storage","Marigold");
                using(var locked=new FileStream(Path.Combine(failedLegacy,"locked.bin"),FileMode.Open,FileAccess.Read,FileShare.None))
                    Check(Refused(failedGame,failedRoaming),"Unreadable source aborts migration instead of publishing partial data");
                Check(!Directory.Exists(failedRoot),"Failed migration leaves no usable partial destination");
                Check(File.ReadAllBytes(Path.Combine(failedLegacy,"locked.bin"))[2]==4,"Failed migration retains original bytes");
                var retry=DataPaths.Initialize(failedGame,failedRoaming);
                Check(File.ReadAllBytes(Path.Combine(retry.Root,"locked.bin"))[2]==4,"Migration can retry after an interrupted or blocked copy");
            }
        }
        finally
        {
            if(!sandbox.StartsWith(cache+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unsafe test cleanup path");
            Directory.Delete(sandbox,true);
        }
        Console.WriteLine("PASS: "+checks+" selected game-data path and migration checks.");
    }
}

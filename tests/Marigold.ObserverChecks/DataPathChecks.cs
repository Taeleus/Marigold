using System;
using System.IO;
using Marigold;

internal static class DataPathChecks
{
    private static int checks;
    private static void Check(bool condition,string message)
    {if(!condition)throw new Exception(message);checks++;}
    public static void Run()
    {
        string cache=Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),".cache"));
        string sandbox=Path.Combine(cache,"DataPathChecks-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sandbox);
        try
        {
            string roaming=Path.Combine(sandbox,"Roaming"),old=Path.Combine(roaming,"Marigold");
            Directory.CreateDirectory(old);
            string leftover=Path.Combine(old,"profiles.bin");File.WriteAllText(leftover,"unreadable old data");
            string game=Path.Combine(roaming,"SpaceEngineers");
            DataPaths paths;
            using(var locked=new FileStream(leftover,FileMode.Open,FileAccess.Read,FileShare.None))
                paths=DataPaths.Initialize(game);
            Check(paths.Root==Path.Combine(game,"Storage","Marigold"),"Data root belongs to the selected game folder");
            Check(!Directory.Exists(paths.Root),"Resolving paths imports no files and creates no data");
            Check(File.ReadAllText(leftover)=="unreadable old data","Leftover data is neither opened nor modified");
            var empty=new ProfileStore(paths.Root);Check(empty.Profiles.Count==0 && empty.Warning==null,"Fresh selected data root has no imported profiles");
            var controls=new SchematicControls();controls.Appearance.Rendering=3;
            DisplaySettings.Save(Path.Combine(paths.Root,"display.txt"),controls);
            var loaded=new SchematicControls();DisplaySettings.Load(Path.Combine(paths.Root,"display.txt"),loaded);
            Check(loaded.Appearance.Rendering==3,"Settings persist under the selected game root");
            Check(!File.Exists(Path.Combine(old,"display.txt")),"New saves never write to a leftover folder");
            var custom=DataPaths.Initialize(Path.Combine(sandbox,"CustomGameData"));
            Check(custom.Root!=paths.Root && !Directory.Exists(custom.Root),"Custom game data remains independent and starts empty");
            Check(DataPaths.Initialize(game+Path.DirectorySeparatorChar).Root==paths.Root,"Trailing separators retain the same root");
            Check(File.Exists(Path.Combine(paths.Root,"display.txt")),"Reinitialization retains existing selected-root data");
            foreach(string invalid in new[]{null,"","relative-path"})
            {
                bool refused=false;try{DataPaths.Initialize(invalid);}catch(InvalidOperationException){refused=true;}
                Check(refused,"Uninitialized or relative game folder cannot fall back elsewhere");
            }
            string collision=Path.Combine(sandbox,"BlockedGame");Directory.CreateDirectory(Path.Combine(collision,"Storage"));
            File.WriteAllText(Path.Combine(collision,"Storage","Marigold"),"keep me");
            bool blocked=false;try{DataPaths.Initialize(collision);}catch(IOException){blocked=true;}
            Check(blocked && File.ReadAllText(Path.Combine(collision,"Storage","Marigold"))=="keep me","Destination-file collision is preserved");
        }
        finally
        {
            if(!sandbox.StartsWith(cache+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unsafe test cleanup path");
            Directory.Delete(sandbox,true);
        }
        Console.WriteLine("PASS: "+checks+" selected data-root and leftover-isolation checks.");
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Marigold;
internal static class FeedbackChecks
{
    static int count;
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
    public static void Run()
    {
        var baseline=new BlockSample[30];for(int i=0;i<baseline.Length;i++){var pos=new Point3(i*2,i%4,i%7);baseline[i]=new BlockSample{Position=pos,Bounds=new Box3(new Point3(pos.X-.5,pos.Y-.5,pos.Z-.5),new Point3(pos.X+.5,pos.Y+.5,pos.Z+.5)),Definition="Block/"+i,Forward=0,Up=4,MaxIntegrity=100,Integrity=100,BuildIntegrity=100};}
        var profile=new ShipProfile {Id=Guid.NewGuid().ToString("N"),Name="Recovery",Blocks=baseline,GridSize=2.5,Anchor=new AnchorFrame{Right=new Point3(1,0,0),Up=new Point3(0,1,0),Backward=new Point3(0,0,1),Definition="DestroyedCockpit"}};
        var axes=new[]{new Point3(1,0,0),new Point3(-1,0,0),new Point3(0,1,0),new Point3(0,-1,0),new Point3(0,0,1),new Point3(0,0,-1)};
        foreach(var r in axes)foreach(var u in axes)
        {
            if(r.X*u.X+r.Y*u.Y+r.Z*u.Z!=0)continue;
            var anchor=profile.Anchor;anchor.Position=new Point3(-32,17,55);anchor.Right=r;anchor.Up=u;anchor.Backward=new Point3(r.Y*u.Z-r.Z*u.Y,r.Z*u.X-r.X*u.Z,r.X*u.Y-r.Y*u.X);
            var live=ProfileAlignment.Transform(baseline,anchor,false).Where((b,i)=>i%3==0).ToArray();live[0].Integrity=1;
            var results=RecoveryMatcher.Find(profile,live,CancellationToken.None);
            Check(results.Count>0 && results[0].Matches==10,"Recovery finds all surviving blocks in each rotation without cockpit");
            var aligned=ProfileAlignment.Transform(baseline,results[0].Anchor,false);var comparison=SnapshotComparison.Compare(aligned,live);
            Check(comparison.Counts[(int)ChangeKind.Missing]==20 && comparison.Matching==10,"Recovered baseline retains absent blocks");
        }
        Check(RecoveryMatcher.Find(profile,new[]{baseline[0]},CancellationToken.None).Count==0,"Sparse wreck refuses match");
        bool canceled=false;try{RecoveryMatcher.Find(profile,baseline,new CancellationToken(true));}catch(OperationCanceledException){canceled=true;}Check(canceled,"Recovery cancellation");
        var repeated=(BlockSample[])baseline.Clone();for(int i=0;i<repeated.Length;i++){repeated[i].Definition="Armor";repeated[i].Position=new Point3(i,0,0);repeated[i].Bounds=new Box3(new Point3(i-.5,-.5,-.5),new Point3(i+.5,.5,.5));}
        profile.Blocks=repeated;var tied=RecoveryMatcher.Find(profile,repeated.Take(5).ToArray(),CancellationToken.None);Check(tied.Count>1 && tied[0].Matches==tied[1].Matches,"Repeated patterns retain alternatives rather than claiming identity");
        foreach(var pair in new[]{"'=OemQuotes","Ctrl+'=Ctrl+OemQuotes","+=OemPlus","Ctrl++=Ctrl+OemPlus","7=D7",";=OemSemicolon"}){int n=pair.LastIndexOf('=');Check(KeyNames.Normalize(pair.Substring(0,n))==pair.Substring(n+1),"Readable punctuation key alias");}
        Check(DisplayFilter.ValidCategories("power,gas,thrusters") && DisplayFilter.CategoryMatches("MyObjectBuilder_Thrust/Test","power,thrusters"),"Multi-system inclusion");
        Check(!DisplayFilter.ValidCategories("tools,garbage") && !DisplayFilter.CategoryMatches("MyObjectBuilder_Thrust/Test","none"),"Invalid and empty category sets");
        var styles=DisplayFilter.DefaultStyles.ToCharArray();styles[Array.IndexOf(DisplayFilter.Categories.Split(' '),"armor")]='0';
        Check(!DisplayFilter.CategoryMatches("MyObjectBuilder_CubeBlock/Test",DisplayFilter.VisibleCategories("all",new string(styles))),"Hidden category removed from all visibility");
        Check(DisplayFilter.ValidStyles(new string(styles)) && !DisplayFilter.ValidStyles("bad"),"System styles validated");
        var appearance=new HudAppearance();appearance.Set("x","0");appearance.Set("pulse","0");Check(HudAppearance.Parse(appearance.Encode()).Pulse==0,"Appearance round trip");
        bool invalid=false;try{appearance.Set("size","NaN");}catch(InvalidDataException){invalid=true;}Check(invalid && appearance.Size==.32,"Bad appearance edit is atomic");
        string dir=Path.Combine(Directory.GetCurrentDirectory(),".cache","Feedback-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try{var c=new SchematicControls{Category="power,gas",Appearance=appearance,SystemStyles=new string(styles)};string path=Path.Combine(dir,"display.txt");DisplaySettings.Save(path,c);var loaded=new SchematicControls();DisplaySettings.Load(path,loaded);var migrated=Path.Combine(dir,"old.txt");
            for(int version=1;version<=4;version++)
            {
                var old=new System.Collections.Generic.List<string>{"Marigold display "+version,"1","reference","tools","missing","True","1.5","False"};
                if(version>=2){old.Add("True");old.Add("False");}if(version>=3)old.Add("es");if(version>=4)old.Add(new HudAppearance().Encode());
                File.WriteAllLines(migrated,old);var migratedControls=new SchematicControls();DisplaySettings.Load(migrated,migratedControls);
                Check(migratedControls.Category=="tools" && migratedControls.Zoom==1.5 && !migratedControls.Visible && migratedControls.Mode=="compare","Old display version "+version+" migrates");
            }
            Check(loaded.Category=="power,gas" && loaded.Appearance.X==0 && loaded.Appearance.Pulse==0 && loaded.SystemStyles==new string(styles),"Extended display file round trip");}
        finally{Directory.Delete(dir,true);}
        Console.WriteLine("PASS: "+count+" recovery/customization feedback checks.");
    }
}

using System;
using System.IO;
using Marigold;
internal static class DisplayChecks
{
    private static int count;
    private static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
    public static void Run()
    {
        foreach(var pair in new[]{"ShipDrill:tools","ShipWelder:tools","ShipGrinder:tools","Thrust:thrusters","LargeGatlingTurret:weapons","BatteryBlock:power","OxygenTank:gas","MotorStator:mechanical","PistonBase:mechanical","Assembler:production","CargoContainer:cargo","Cockpit:control","ConveyorSorter:conveyors","CubeBlock:armor","Unrecognized:other"})
        {var a=pair.Split(':');Check(DisplayFilter.Category("MyObjectBuilder_"+a[0]+"/ModdedSubtype")==a[1],"Category "+a[0]);}
        Check(!DisplayFilter.Contains(DisplayFilter.Categories,"armor conveyors"),"Multiple category tokens rejected");
        foreach(ChangeKind kind in Enum.GetValues(typeof(ChangeKind)))
        {Check(DisplayFilter.StateMatches(kind,"broken")== (kind==ChangeKind.NonFunctional),"Only nonfunctional is broken");Check(DisplayFilter.StateMatches(kind,"missing")==(kind==ChangeKind.Missing),"Only absent is missing");}
        var block=new BlockSample {Position=new Point3(0,0,0),Bounds=new Box3(new Point3(-.5,-.5,-.5),new Point3(.5,.5,.5)),Definition="MyObjectBuilder_ShipDrill/Test",Forward=0,Up=4,Integrity=100,BuildIntegrity=100,MaxIntegrity=100,HasFunctionalState=true,IsFunctional=true};
        var original=new[]{block};Check(ObservationCache.Same(original,new[]{block}),"Unchanged sample reused");
        block.Integrity=90;Check(!ObservationCache.Same(original,new[]{block}),"Damage invalidates cache");
        Check(ProfileSuggestions.Score(original,new[]{block})==1,"Suggestion ignores damage");
        block.IsFunctional=false;Check(!ObservationCache.Same(original,new[]{block}),"Function invalidates cache");
        block.Definition="Replacement";Check(ProfileSuggestions.Score(original,new[]{block})==0,"Replacement cannot imply identity");
        Check(ProfileSuggestions.Score(original,new BlockSample[0])==0,"Empty suggestion");
        Check(!ObservationCache.Same(original,new BlockSample[0]),"Removal invalidates cache");
        var c=new SchematicControls();c.Execute("/mg isolate tools");c.Execute("/mg filter broken");c.Execute("/mg layout expanded");c.Execute("/mg mode reference");c.Execute("/mg zoom 2");
        Check(c.Category=="tools" && c.StateFilter=="broken" && c.Expanded && c.Mode=="compare" && c.Zoom==2,"Display commands applied");
        c.Execute("/mg page next");Check(c.Page==2,"Next page");c.Execute("/mg filter all");Check(c.Page==1,"Filter resets page");
        c.Execute("/mg profile suggest");Check(c.ReferenceAction=="profile suggest","Suggestion command routed");
        string folder=Path.Combine(Directory.GetCurrentDirectory(),".cache","DisplayTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string file=Path.Combine(folder,"display.txt");DisplaySettings.Save(file,c);var restored=new SchematicControls();DisplaySettings.Load(file,restored);
            Check(restored.Category=="tools" && restored.Expanded && restored.Zoom==2 && restored.Mode=="compare","Saved display round trip");
            c.Zoom=1;DisplaySettings.Save(file,c);Check(File.Exists(file+".bak"),"Previous settings backed up");
            File.WriteAllText(file,"broken");bool rejected=false;try{DisplaySettings.Load(file,restored);}catch(InvalidDataException){rejected=true;}
            Check(rejected && restored.Zoom==2 && restored.Category=="tools","Invalid file preserves all current options");
            DisplaySettings.Reset(restored);Check(restored.Mode=="compare" && restored.Category=="all" && restored.Zoom==.85 && !restored.Expanded,"Defaults restored");
        }
        finally{Directory.Delete(folder,true);}
        Console.WriteLine("PASS: "+count+" display/settings/profile-suggestion checks.");
    }
}

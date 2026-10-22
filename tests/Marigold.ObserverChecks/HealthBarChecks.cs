using System;
using System.Collections.Generic;
using System.IO;
using Marigold;
internal static class HealthBarChecks
{
    private static int count;
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
    private static BlockSample Block(int i,string type)
    {return new BlockSample{Position=new Point3(i,0,0),Bounds=new Box3(new Point3(i-.5,-.5,-.5),new Point3(i+.5,.5,.5)),Definition="MyObjectBuilder_"+type+"/Test",Forward=0,Up=4,MaxIntegrity=100,Integrity=100,BuildIntegrity=100,HasFunctionalState=true,IsFunctional=true};}
    public static void Run()
    {
        var baseline=new[]{Block(0,"CubeBlock"),Block(1,"BatteryBlock"),Block(2,"Thrust"),Block(3,"Conveyor"),Block(4,"CargoContainer"),Block(5,"ShipDrill")};
        var live=new List<BlockSample>(baseline);var b=live[1];b.Integrity=50;live[1]=b;b=live[2];b.Integrity=10;b.IsFunctional=false;live[2]=b;live.RemoveAt(3);b=live[3];b.Integrity=double.NaN;live[3]=b;b=live[4];b.Definition="Replacement";live[4]=b;live.Add(Block(99,"Reactor"));
        var health=ShipHealth.Build(baseline,live.ToArray());
        Check(health.HasBaseline && health.Total(0)==6,"Denominator retains missing blocks and excludes additions");
        Check(health.Counts[0,0]==1 && health.Counts[0,1]==1 && health.Counts[0,2]==1 && health.Counts[0,3]==2 && health.Counts[0,4]==1,"Exact blue/yellow/orange/red/gray distribution");
        Check(health.Counts[1,0]==1 && health.Counts[2,1]==1 && health.Counts[3,2]==1 && health.Counts[4,3]==2 && health.Counts[5,4]==1,"States attributed to baseline systems");
        for(int state=0;state<5;state++){int sum=0;for(int group=1;group<ShipHealth.Names.Length;group++)sum+=health.Counts[group,state];Check(sum==health.Counts[0,state],"System groups partition overall bar");}
        var healed=ShipHealth.Build(baseline,baseline);Check(healed.Counts[0,0]==6,"Repairs return to blue");
        var absent=ShipHealth.Build(baseline,new BlockSample[0]);Check(absent.Counts[0,3]==6 && absent.Total(0)==6,"Destroyed grid remains all red with a baseline");
        Check(!ShipHealth.Build(null,baseline).HasBaseline && ShipHealth.Build(new BlockSample[0],baseline).Total(0)==0,"No baseline and empty baseline are not 100 percent healthy");
        Check(ShipHealth.Group("MyObjectBuilder_ShipWelder/Modded")==4 && ShipHealth.Group("MyObjectBuilder_GasTank/Test")==4,"Utility contains tools and gas");
        var weapon=Block(0,"SmallGatlingGun");var weaponHealth=ShipHealth.Build(new[]{weapon},new BlockSample[0]);Check(weaponHealth.Counts[6,3]==1 && weaponHealth.Total(4)==0 && weaponHealth.Total(0)==1,"Missing weapons are separate from Utility");
        var shared=new SchematicControls{SchematicVisible=false};shared.HealthBars.Bars[0].Enabled=true;shared.HealthBars.Bars[0].X=.2;shared.Appearance.Size=.4;
        var imported=new SchematicControls();DisplaySettings.Import(DisplaySettings.Export(shared),imported);
        Check(!imported.SchematicVisible && imported.Visible && imported.HealthBars.Bars[0].Enabled && imported.HealthBars.Bars[0].X==.2 && imported.Appearance.Size==.4,"Clipboard round trip preserves independent visibility and sizing");
        bool invalidConfig=false;try{DisplaySettings.Import(DisplaySettings.Export(shared).Replace("Marigold display 7","Unknown"),imported);}catch(InvalidDataException){invalidConfig=true;}
        Check(invalidConfig && !imported.SchematicVisible && imported.HealthBars.Bars[0].X==.2,"Invalid clipboard config leaves settings intact");
        var settings=new HealthBarSettings();foreach(var bar in settings.Bars)Check(!bar.Enabled,"Bars start disabled");
        settings.Bars[2].Enabled=true;settings.Bars[2].Set("x","0");settings.Bars[2].Set("height","0.08");var restored=HealthBarSettings.Parse(settings.Encode());Check(restored.Bars[2].Enabled && restored.Bars[2].X==0 && restored.Bars[2].Height==.08 && !restored.Bars[1].Enabled,"Independent layouts round trip");
        var oldLayouts=new List<string>(settings.Encode().Split(';'));oldLayouts.RemoveAt(6);var migrated=HealthBarSettings.Parse(string.Join(";",oldLayouts));Check(migrated.Bars[2].Enabled && !migrated.Bars[6].Enabled,"Six-bar settings preserve layouts and add hidden Weapons");
        foreach(string invalid in new[]{"NaN","Infinity","-1","1.1"}){bool rejected=false;try{settings.Bars[2].Set("opacity",invalid);}catch(InvalidDataException){rejected=true;}Check(rejected && settings.Bars[2].Opacity==1,"Bad edits leave settings unchanged");}
        string dir=Path.Combine(Directory.GetCurrentDirectory(),".cache","Bars-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try
        {
            string workshop=Path.Combine(dir,"244850","12345");Directory.CreateDirectory(workshop);string blueprint=Path.Combine(workshop,"bp.sbc");
            File.WriteAllText(blueprint,"<Definitions><ShipBlueprint><Id Subtype=\"Test Cruiser\"/><DisplayName>Author Name</DisplayName></ShipBlueprint></Definitions>");Check(BlueprintCatalog.DisplayName(blueprint)=="Test Cruiser [Workshop 12345]","Workshop label uses name and ID");
            File.WriteAllText(blueprint,"<Definitions><ShipBlueprint><DisplayName>Author Name</DisplayName><CubeGrids><CubeGrid><CubeBlocks/><DisplayName>Actual Ship</DisplayName></CubeGrid></CubeGrids></ShipBlueprint></Definitions>");Check(BlueprintCatalog.DisplayName(blueprint)=="Actual Ship [Workshop 12345]","Grid name fallback ignores author");
            Check(BlueprintCatalog.CloudName("cloud:Blueprints/cloud/My Cruiser/bp.sbc")=="My Cruiser","Cloud label removes storage path");
            var merged=BlueprintCatalog.Merge(new[]{new BlueprintCatalog.Listing{Paths=new[]{"local","workshop"},Names=new[]{"Cruiser","Miner"}},new BlueprintCatalog.Listing{Paths=new[]{"cloud"},Names=new[]{"CRUISER Two"}}},"cruiser");Check(merged.Paths.Length==2 && merged.Paths[0]=="local" && merged.Paths[1]=="cloud","Combined sources search titles case insensitively");
            File.WriteAllText(blueprint,"<broken>");Check(BlueprintCatalog.DisplayName(blueprint)=="12345 [Workshop 12345]","Malformed Workshop XML uses folder fallback");
            File.WriteAllText(blueprint,"<!DOCTYPE x [<!ENTITY unsafe SYSTEM 'file:///not-allowed'>]><x>&unsafe;</x>");Check(BlueprintCatalog.DisplayName(blueprint)=="12345 [Workshop 12345]","Catalog refuses DTD entities");
            string path=Path.Combine(dir,"display.txt");var c=new SchematicControls{HealthBars=settings,Category="tools"};DisplaySettings.Save(path,c);var loaded=new SchematicControls();DisplaySettings.Load(path,loaded);Check(loaded.HealthBars.Bars[2].Enabled && loaded.HealthBars.Bars[2].X==0 && loaded.Category=="tools","Bars share global/profile/preset persistence");
            var legacy=new List<string>{"Marigold display 5","1","compare","all","all","False","0.85","True","False","True","en",new HudAppearance().Encode(),DisplayFilter.DefaultStyles};File.WriteAllLines(path,legacy);DisplaySettings.Load(path,loaded);foreach(var bar in loaded.HealthBars.Bars)Check(!bar.Enabled,"Old settings migrate with bars hidden");
            DisplaySettings.Save(path,c);var bad=File.ReadAllLines(path);bad[13]="bad";File.WriteAllLines(path,bad);bool refused=false;try{DisplaySettings.Load(path,loaded);}catch(InvalidDataException){refused=true;}Check(refused && loaded.Category=="all" && !loaded.HealthBars.Bars[2].Enabled,"Bad bar file does not partly apply preferences");
        }
        finally{Directory.Delete(dir,true);}
        Console.WriteLine("PASS: "+count+" health bar classification/settings checks.");
    }
}

using System;
using System.IO;
using System.IO.Compression;
using Marigold;
internal static class BlueprintChecks
{
    private static int count;
    private static void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
    public static void Run()
    {
        string folder=Path.Combine(Directory.GetCurrentDirectory(),".cache","BlueprintTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        try
        {
            string file=Path.Combine(folder,"bp.sbc");
            const string xml="<Definitions xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'><ShipBlueprints><ShipBlueprint><CubeGrids><CubeGrid><GridSizeEnum>Large</GridSizeEnum><CubeBlocks><MyObjectBuilder_CubeBlock xsi:type='MyObjectBuilder_Cockpit'><SubtypeName>Bridge</SubtypeName><Min x='-3' y='2' z='4'/><BlockOrientation Forward='Left' Up='Up'/><CustomName>Anchor</CustomName></MyObjectBuilder_CubeBlock><MyObjectBuilder_CubeBlock><SubtypeName>Armor</SubtypeName></MyObjectBuilder_CubeBlock></CubeBlocks></CubeGrid></CubeGrids></ShipBlueprint></ShipBlueprints></Definitions>";
            File.WriteAllText(file,xml);var doc=BlueprintDocument.Load(file);
            Check(doc.Grids.Count==1 && doc.Grids[0].Blocks.Count==2,"Blueprint grids and blocks");
            var b=doc.Grids[0].Blocks[0];Check(b.X==-3 && b.Forward==2 && b.Name=="Anchor" && b.Type=="MyObjectBuilder_Cockpit","Metadata parsing");
            Check(doc.Grids[0].Blocks[1].Forward==0 && doc.Grids[0].Blocks[1].Up==4,"Default orientation");
            Check(File.ReadAllText(file)==xml,"Import never modifies source");
            using(var stream=File.Create(file))using(var gzip=new GZipStream(stream,CompressionMode.Compress))using(var w=new StreamWriter(gzip))w.Write(xml);
            Check(BlueprintDocument.Load(file).Grids[0].Blocks.Count==2,"Gzip blueprint");
            Check(BlueprintDocument.FromBytes(File.ReadAllBytes(file)).Grids[0].Blocks.Count==2,"Cloud gzip bytes");
            Check(BlueprintDocument.FromBytes(System.Text.Encoding.UTF8.GetBytes(xml)).Grids[0].Blocks.Count==2,"Cloud XML bytes");
            var nested=Path.Combine(folder,"Fleet","Miners","Ship");Directory.CreateDirectory(nested);File.WriteAllText(Path.Combine(nested,"bp.sbc"),xml);
            Check(Array.IndexOf(BlueprintCatalog.Find(folder),Path.Combine(nested,"bp.sbc"))>=0,"Nested local blueprints discovered");
            foreach(string invalid in new[]{"<!DOCTYPE Definitions [<!ENTITY x SYSTEM 'file:///secret'>]><Definitions>&x;</Definitions>",xml.Replace("GridSizeEnum>Large","GridSizeEnum>Huge"),xml.Replace("Forward='Left'","Forward='Diagonal'"),xml.Replace("x='-3'","x='NaN'"),xml.Replace("MyObjectBuilder_Cockpit","MyObjectBuilder_CompoundCubeBlock"),xml.Replace("<BlockOrientation Forward='Left' Up='Up'/>","<Orientation><W>1</W></Orientation>"),"<Definitions/>"})
            {
                File.WriteAllText(file,invalid);bool rejected=false;try{BlueprintDocument.Load(file);}catch(Exception){rejected=true;}Check(rejected,"Malformed/unsupported XML rejected");
            }
            var controls=new SchematicControls();bool send=true;string response;
            Check(controls.TryHandle("/mg blueprint load \"C:\\Blueprints\\My Ship\\bp.sbc\"",ref send,out response) && !send && controls.ProfileArgument=="C:\\Blueprints\\My Ship\\bp.sbc","Quoted paths preserve spaces and slashes");
            controls.Execute("/mg blueprint preview 2");Check(controls.ReferenceAction=="blueprint preview" && controls.ProfileArgument=="2","Preview parsed");
            controls.Execute("/mg blueprint accept");Check(controls.ReferenceAction=="blueprint accept","Accept explicit");
            controls.Execute("/mg blueprint load \"unfinished");Check(controls.ReferenceAction==null,"Unclosed quote rejected");
        }
        finally{Directory.Delete(folder,true);}
        Console.WriteLine("PASS: "+count+" blueprint parser/command checks.");
    }
}

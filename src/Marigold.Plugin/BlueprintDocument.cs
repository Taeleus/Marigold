using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Marigold
{
    internal sealed class BlueprintBlock
    {
        public string Type, Subtype, Name;
        public int X,Y,Z,Forward,Up;
    }
    internal sealed class BlueprintGrid
    {
        public string Name;
        public double GridSize;
        public readonly List<BlueprintBlock> Blocks=new List<BlueprintBlock>();
    }
    internal sealed class BlueprintDocument
    {
        public readonly List<BlueprintGrid> Grids=new List<BlueprintGrid>();
        public static BlueprintDocument Load(string path)
        {
            path=Path.GetFullPath(path);
            if(path.StartsWith(@"\\",StringComparison.Ordinal) || !Path.GetExtension(path).Equals(".sbc",StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Use the XML bp.sbc file. Protobuf (.sbcB5), ZIP and .bin archives are unsupported; save a local copy through the game, then select its bp.sbc.");
            if(new FileInfo(path).Length>32*1024*1024)throw new InvalidDataException("Blueprint file exceeds 32 MB.");
            return Parse(()=>Open(path));
        }
        public static BlueprintDocument FromBytes(byte[] data)
        {
            if(data==null || data.Length==0 || data.Length>32*1024*1024)throw new InvalidDataException("Cloud blueprint is empty or exceeds 32 MB.");
            return Parse(()=>{var stream=new MemoryStream(data,false);return data.Length>1 && data[0]==31 && data[1]==139?(Stream)new GZipStream(stream,CompressionMode.Decompress):stream;});
        }
        private static BlueprintDocument Parse(Func<Stream> open)
        {
            // Validate depth separately: XElement would otherwise read arbitrarily deep content.
            using(var stream=open())using(var reader=Reader(stream))
                while(reader.Read())if(reader.Depth>64)throw new InvalidDataException("Blueprint XML is too deeply nested.");
            XDocument xml;
            using(var stream=open())using(var reader=Reader(stream))xml=XDocument.Load(reader,LoadOptions.None);
            var result=new BlueprintDocument();int count=0;
            var collection=Child(xml.Root,"ShipBlueprints");
            if(collection!=null && collection.Elements().Count(n=>n.Name.LocalName=="ShipBlueprint")!=1)throw new InvalidDataException("Choose a file containing exactly one ship blueprint.");
            var container=Child(collection,"ShipBlueprint");
            if(container==null)throw new InvalidDataException("Not a ship blueprint file.");
            var grids=Child(container,"CubeGrids");
            if(grids==null)throw new InvalidDataException("Blueprint has no grids.");
            foreach(var element in grids.Elements())
            {
                if(element.Name.LocalName!="CubeGrid")throw new InvalidDataException("Unsupported grid record.");
                if(result.Grids.Count>=64)throw new InvalidDataException("Blueprint exceeds 64 grids.");
                string size=Value(element,"GridSizeEnum","Large");
                if(size!="Large" && size!="Small")throw new InvalidDataException("Unknown grid size.");
                var grid=new BlueprintGrid { Name=Bound(Value(element,"DisplayName","Grid "+(result.Grids.Count+1)),256),GridSize=size=="Large"?2.5:.5 };
                var blocks=Child(element,"CubeBlocks");
                if(blocks==null)throw new InvalidDataException("Grid has no block collection.");
                foreach(var node in blocks.Elements())
                {
                    if(++count>100000)throw new InvalidDataException("Blueprint exceeds 100,000 blocks.");
                    var type=node.Attribute(XName.Get("type","http://www.w3.org/2001/XMLSchema-instance"));
                    string typeName=type==null?node.Name.LocalName:type.Value;
                    if(typeName.Contains(":"))typeName=typeName.Substring(typeName.IndexOf(':')+1);
                    if(!typeName.StartsWith("MyObjectBuilder_",StringComparison.Ordinal) || typeName=="MyObjectBuilder_CompoundCubeBlock")throw new InvalidDataException("Unsupported blueprint block type: "+typeName);
                    var min=Child(node,"Min");var orientation=Child(node,"BlockOrientation");
                    if(orientation==null && Child(node,"Orientation")!=null)throw new InvalidDataException("Legacy quaternion orientation is unsupported. Resave this blueprint in the current game before importing.");
                    grid.Blocks.Add(new BlueprintBlock { Type=Bound(typeName,128),Subtype=Bound(Value(node,"SubtypeName",""),512),Name=Bound(Value(node,"CustomName",""),256),
                        X=Coordinate(min,"x"),Y=Coordinate(min,"y"),Z=Coordinate(min,"z"),Forward=Direction(orientation,"Forward","Forward"),Up=Direction(orientation,"Up","Up") });
                }
                if(grid.Blocks.Count==0)throw new InvalidDataException("Empty blueprint grid.");
                result.Grids.Add(grid);
            }
            if(result.Grids.Count==0)throw new InvalidDataException("Blueprint has no grids.");
            return result;
        }
        private static Stream Open(string path)
        {
            var file=File.OpenRead(path);int a=file.ReadByte(),b=file.ReadByte();file.Position=0;
            return a==31 && b==139?(Stream)new GZipStream(file,CompressionMode.Decompress):file;
        }
        private static XmlReader Reader(Stream stream)
        { return XmlReader.Create(stream,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=64*1024*1024,IgnoreComments=true,IgnoreProcessingInstructions=true }); }
        private static XElement Child(XElement e,string name){return e==null?null:e.Elements().FirstOrDefault(n=>n.Name.LocalName==name);}
        private static string Value(XElement e,string name,string fallback){return Child(e,name)?.Value??fallback;}
        private static string Bound(string text,int max){if(text.Length>max)throw new InvalidDataException("Blueprint text is too long.");return text;}
        private static int Coordinate(XElement e,string key)
        {
            string text=e?.Attribute(key)?.Value??"0";int n;
            if(!int.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out n) || Math.Abs((long)n)>10000000)throw new InvalidDataException("Invalid blueprint block coordinate.");return n;
        }
        private static int Direction(XElement e,string key,string fallback)
        {
            string text=e?.Attribute(key)?.Value??fallback;
            switch(text){case "Forward":return 0;case "Backward":return 1;case "Left":return 2;case "Right":return 3;case "Up":return 4;case "Down":return 5;default:throw new InvalidDataException("Invalid blueprint orientation.");}
        }
    }
}

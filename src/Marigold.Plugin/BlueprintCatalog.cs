using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
namespace Marigold
{
    internal static class BlueprintCatalog
    {
        internal const int ResultsPerPage=4;
        internal sealed class Listing { public string[] Paths,Names; }
        public static Listing Scan(string root)
        {
            var paths=Find(root);
            return new Listing {Paths=paths,Names=Array.ConvertAll(paths,DisplayName)};
        }
        public static string DisplayName(string path)
        {
            string name=new DirectoryInfo(Path.GetDirectoryName(path)).Name;
            // Read only a bounded XML prefix on the catalog worker. Never resolve entities.
            try
            {
                var settings=new XmlReaderSettings {DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=32*1024*1024};
                using(var reader=XmlReader.Create(path,settings))
                {
                    int shipDepth=-1,gridDepth=-1;
                    while(reader.Read())
                    {
                        if(reader.Depth>64)break;
                        if(reader.NodeType==XmlNodeType.EndElement && reader.Depth==gridDepth)break;
                        if(reader.NodeType!=XmlNodeType.Element)continue;
                        if(reader.LocalName=="ShipBlueprint")shipDepth=reader.Depth;
                        if(reader.LocalName=="Id" && shipDepth>=0 && reader.Depth==shipDepth+1)
                        {var title=reader.GetAttribute("Subtype");if(!string.IsNullOrWhiteSpace(title)){name=title.Trim();break;}}
                        if(reader.LocalName=="CubeGrid" && gridDepth<0)gridDepth=reader.Depth;
                        if(reader.LocalName=="DisplayName" && gridDepth>=0 && reader.Depth==gridDepth+1)
                        {var title=reader.ReadElementContentAsString().Trim();if(title.Length>0)name=title;break;}
                    }
                }
            }
            catch(IOException){}catch(UnauthorizedAccessException){}catch(XmlException){}
            if(name.Length>100)name=name.Substring(0,100);
            string[] parts=path.Replace('\\','/').Split('/');
            for(int i=0;i+1<parts.Length;i++)if(parts[i]=="244850")return name+" [Workshop "+parts[i+1]+"]";
            return name;
        }
        public static string CloudName(string path)
        {
            var value=path.Replace('\\','/');if(value.StartsWith("cloud:",StringComparison.OrdinalIgnoreCase))value=value.Substring(6);
            const string prefix="Blueprints/cloud/";if(value.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))value=value.Substring(prefix.Length);
            if(value.EndsWith("/bp.sbc",StringComparison.OrdinalIgnoreCase))value=value.Substring(0,value.Length-7);
            return value;
        }
        public static Listing Merge(Listing[] sources,string search)
        {
            if(string.IsNullOrWhiteSpace(search))throw new ArgumentException("Blueprint search can't be empty.",nameof(search));
            string query=search.Trim();
            var rows=new List<Tuple<string,string>>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var source in sources)for(int i=0;i<source.Paths.Length;i++)if(seen.Add(source.Paths[i]) && source.Names[i].IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)rows.Add(Tuple.Create(source.Paths[i],source.Names[i]));
            rows.Sort((a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a.Item2,b.Item2));
            return new Listing{Paths=rows.ConvertAll(row=>row.Item1).ToArray(),Names=rows.ConvertAll(row=>row.Item2).ToArray()};
        }
        public static int PageCount(int resultCount){return Math.Max(1,(Math.Max(0,resultCount)+ResultsPerPage-1)/ResultsPerPage);}
        public static int ClampPage(int page,int resultCount){return Math.Max(0,Math.Min(page,PageCount(resultCount)-1));}
        public static int PageStart(int page,int resultCount){return ClampPage(page,resultCount)*ResultsPerPage;}
        public static string[] Find(string root)
        {
            var files=new List<string>();if(!Directory.Exists(root))return files.ToArray();int inspected=0;
            var pending=new Stack<Tuple<string,int>>();pending.Push(Tuple.Create(root,0));
            while(pending.Count>0 && inspected<2000 && files.Count<500)
            {
                var item=pending.Pop();inspected++;
                try{if((File.GetAttributes(item.Item1)&FileAttributes.ReparsePoint)!=0)continue;}catch(UnauthorizedAccessException){continue;}catch(IOException){continue;}
                string file=Path.Combine(item.Item1,"bp.sbc");if(File.Exists(file))files.Add(file);
                if(item.Item2>=16)continue;
                try {foreach(var folder in Directory.EnumerateDirectories(item.Item1)){if(pending.Count>=2000)break;pending.Push(Tuple.Create(folder,item.Item2+1));}}
                catch(UnauthorizedAccessException){} catch(IOException){}
            }
            files.Sort(StringComparer.OrdinalIgnoreCase);return files.ToArray();
        }
    }
}

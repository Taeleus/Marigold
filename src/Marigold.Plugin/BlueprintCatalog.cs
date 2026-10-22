using System;
using System.Collections.Generic;
using System.IO;
namespace Marigold
{
    internal static class BlueprintCatalog
    {
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

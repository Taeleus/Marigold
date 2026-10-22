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
            foreach(var folder in Directory.EnumerateDirectories(root))
            {
                if(++inspected>2000 || files.Count>=500)break;
                if((File.GetAttributes(folder)&FileAttributes.ReparsePoint)!=0)continue;
                string file=Path.Combine(folder,"bp.sbc");if(File.Exists(file))files.Add(file);
            }
            files.Sort(StringComparer.OrdinalIgnoreCase);return files.ToArray();
        }
    }
}

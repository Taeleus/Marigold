using System;
using System.IO;
using System.Globalization;
namespace Marigold
{
    // Explicit save avoids disk writes on animation frames or transient view commands.
    internal static class DisplaySettings
    {
        public static void Reset(SchematicControls c)
        { c.Detail=1;c.Mode="compare";c.Category="all";c.StateFilter="all";c.Expanded=false;c.Zoom=.85;c.Visible=true;c.Page=1;c.Station=false;c.AutoAssociate=true; }
        public static void Load(string path,SchematicControls c)
        {
            if(!File.Exists(path))return;
            if(new FileInfo(path).Length>2048)throw new InvalidDataException("Display settings are too large.");
            var a=File.ReadAllLines(path);int detail;double zoom;bool expanded,visible;bool station=false,auto=true;
            bool version1=a.Length==8 && a[0]=="Marigold display 1", version2=a.Length==10 && a[0]=="Marigold display 2";
            if((!version1 && !version2) || !int.TryParse(a[1],out detail) || detail<0 || detail>2 ||
                !DisplayFilter.Contains("live reference compare",a[2]) || !DisplayFilter.Contains(DisplayFilter.Categories,a[3]) ||
                !DisplayFilter.Contains(DisplayFilter.States,a[4]) || !bool.TryParse(a[5],out expanded) ||
                !double.TryParse(a[6],NumberStyles.Float,CultureInfo.InvariantCulture,out zoom) || double.IsNaN(zoom) || zoom<.25 || zoom>3 || !bool.TryParse(a[7],out visible) || (version2 && (!bool.TryParse(a[8],out station) || !bool.TryParse(a[9],out auto))))
                throw new InvalidDataException("Invalid display settings; existing settings were kept.");
            c.Detail=detail;c.Mode=a[2];c.Category=a[3];c.StateFilter=a[4];c.Expanded=expanded;c.Zoom=zoom;c.Visible=visible;c.Page=1;c.Station=station;c.AutoAssociate=auto;
        }
        public static void Save(string path,SchematicControls c)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllLines(temp,new[]{"Marigold display 2",c.Detail.ToString(CultureInfo.InvariantCulture),c.Mode,c.Category,c.StateFilter,c.Expanded.ToString(),c.Zoom.ToString("R",CultureInfo.InvariantCulture),c.Visible.ToString(),c.Station.ToString(),c.AutoAssociate.ToString()});
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}

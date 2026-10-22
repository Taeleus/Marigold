using System;
using System.IO;
using System.Globalization;
namespace Marigold
{
    // Explicit save avoids disk writes on animation frames or transient view commands.
    internal static class DisplaySettings
    {
        public static void Reset(SchematicControls c)
        { c.Appearance=new HudAppearance();c.SystemStyles=DisplayFilter.DefaultStyles;c.Detail=1;c.Mode="compare";c.Category="all";c.StateFilter="all";c.Expanded=false;c.Zoom=.85;c.Visible=true;c.Page=1;c.Station=false;c.AutoAssociate=true;c.Language="en"; }
        public static void Load(string path,SchematicControls c)
        {
            if(!File.Exists(path))return;
            if(new FileInfo(path).Length>2048)throw new InvalidDataException("Display settings are too large.");
            var a=File.ReadAllLines(path);int detail;double zoom;bool expanded,visible;bool station=false,auto=true;
            bool version1=a.Length==8 && a[0]=="Marigold display 1", version2=a.Length==10 && a[0]=="Marigold display 2",version3=a.Length==11 && a[0]=="Marigold display 3",version4=a.Length==12 && a[0]=="Marigold display 4",version5=a.Length==13 && a[0]=="Marigold display 5";
            var appearance=(version4 || version5)?HudAppearance.Parse(a[11]):new HudAppearance();
            if((!version1 && !version2 && !version3 && !version4 && !version5) || !int.TryParse(a[1],out detail) || detail<0 || detail>2 ||
                !DisplayFilter.Contains("live reference compare",a[2]) || !DisplayFilter.ValidCategories(a[3]) ||
                !DisplayFilter.Contains(DisplayFilter.States,a[4]) || !bool.TryParse(a[5],out expanded) ||
                !double.TryParse(a[6],NumberStyles.Float,CultureInfo.InvariantCulture,out zoom) || double.IsNaN(zoom) || zoom<.25 || zoom>3 || !bool.TryParse(a[7],out visible) || ((version2 || version3 || version4 || version5) && (!bool.TryParse(a[8],out station) || !bool.TryParse(a[9],out auto))) || ((version3 || version4 || version5) && a[10]!="en" && a[10]!="es"))
                throw new InvalidDataException("Invalid display settings; existing settings were kept.");
            if(version5 && !DisplayFilter.ValidStyles(a[12]))throw new InvalidDataException("Invalid system styles.");
            c.SystemStyles=version5?a[12]:DisplayFilter.DefaultStyles;c.Appearance=appearance;c.Detail=detail;c.Mode="compare";c.Category=a[3];c.StateFilter=a[4];c.Expanded=expanded;c.Zoom=zoom;c.Visible=visible;c.Page=1;c.Station=false;c.AutoAssociate=auto;c.Language=(version3 || version4 || version5)?a[10]:"en";
        }
        public static void Save(string path,SchematicControls c)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                File.WriteAllLines(temp,new[]{"Marigold display 5",c.Detail.ToString(CultureInfo.InvariantCulture),c.Mode,c.Category,c.StateFilter,c.Expanded.ToString(),c.Zoom.ToString("R",CultureInfo.InvariantCulture),c.Visible.ToString(),c.Station.ToString(),c.AutoAssociate.ToString(),c.Language,c.Appearance.Encode(),c.SystemStyles});
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
    }
}

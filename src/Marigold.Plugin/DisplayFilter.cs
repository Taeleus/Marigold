using System;
using System.Collections.Generic;
namespace Marigold
{
    internal static class DisplayFilter
    {
        public const string Categories="all armor conveyors thrusters weapons power gas mechanical production cargo control tools other";
        public const string States="all faults missing damaged broken added replaced unknown";
        private static volatile HashSet<string> offensive=new HashSet<string>(StringComparer.Ordinal);
        public static int ClassificationRevision {get;private set;}
        // Publish a new, immutable snapshot; geometry workers never query game/mod APIs.
        public static void SetOffensiveDefinitions(IEnumerable<string> definitions)
        {
            var next=new HashSet<string>(definitions,StringComparer.Ordinal);
            if(offensive.SetEquals(next))return;
            offensive=next;ClassificationRevision++;
        }
        public static string Category(string definition)
        {
            if(definition!=null && offensive.Contains(definition))return "weapons";
            string type=(definition??"").Split('/')[0].Replace("MyObjectBuilder_","").ToLowerInvariant();
            if(type.Contains("conveyor") || type.Contains("connector"))return "conveyors";
            if(type.Contains("thrust"))return "thrusters";
            if(type.Contains("turret") || type.Contains("gun") || type.Contains("launcher") || type.Contains("warhead"))return "weapons";
            if(type.Contains("reactor") || type.Contains("battery") || type.Contains("solar") || type.Contains("windturbine") || type.Contains("hydrogenengine"))return "power";
            if(type.Contains("oxygen") || type.Contains("gastank") || type.Contains("gasgenerator"))return "gas";
            if(type.Contains("motor") || type.Contains("piston"))return "mechanical";
            if(type.Contains("assembler") || type.Contains("refinery"))return "production";
            if(type.Contains("cargo"))return "cargo";
            if(type.Contains("cockpit") || type.Contains("remotecontrol") || type.Contains("gyro"))return "control";
            if(type.Contains("drill") || type.Contains("grinder") || type.Contains("welder"))return "tools";
            if(type=="cubeblock")return "armor";
            return "other";
        }
        public static string DefaultStyles {get{return new string('1',Categories.Split(' ').Length);}}
        public static bool ValidStyles(string styles){if(styles==null || styles.Length!=DefaultStyles.Length)return false;foreach(char c in styles)if(c<'0' || c>'3')return false;return true;}
        public static char Style(string definition,string styles){int index=Array.IndexOf(Categories.Split(' '),Category(definition));return styles[index<0?0:index];}
        public static string VisibleCategories(string selected,string styles){var result=new System.Collections.Generic.List<string>();var names=Categories.Split(' ');for(int i=1;i<names.Length;i++)if(styles[i]!='0' && (selected=="all" || Array.IndexOf(selected.Split(','),names[i])>=0))result.Add(names[i]);return result.Count==0?"none":string.Join(",",result);}
        public static bool ValidCategories(string value)
        {if(value=="all" || value=="none")return true;if(string.IsNullOrWhiteSpace(value))return false;foreach(var part in value.Split(','))if(part=="all" || !Contains(Categories,part))return false;return true;}
        public static bool CategoryMatches(string definition,string category){return category=="all" || Array.IndexOf(category.Split(','),Category(definition))>=0;}
        public static bool StateMatches(ChangeKind kind,string state)
        {
            switch(state)
            {
                case "all":return true;
                case "faults":return kind==ChangeKind.Missing || kind==ChangeKind.Damaged || kind==ChangeKind.NonFunctional || kind==ChangeKind.Unknown;
                case "missing":return kind==ChangeKind.Missing;
                case "damaged":return kind==ChangeKind.Damaged;
                case "broken":return kind==ChangeKind.NonFunctional;
                case "added":return kind==ChangeKind.Added;
                case "replaced":return kind==ChangeKind.Replaced;
                case "unknown":return kind==ChangeKind.Unknown;
                default:return false;
            }
        }
        public static bool Contains(string values,string value){return Array.IndexOf(values.Split(' '),value)>=0;}
    }
    internal static class ObservationCache
    {
        public static bool Equal(BlockSample x,BlockSample y)
        { return x.Position.Equals(y.Position) && x.Bounds.Min.Equals(y.Bounds.Min) && x.Bounds.Max.Equals(y.Bounds.Max) && x.Definition==y.Definition && x.Forward==y.Forward && x.Up==y.Up && x.Integrity==y.Integrity && x.BuildIntegrity==y.BuildIntegrity && x.MaxIntegrity==y.MaxIntegrity && x.HasFunctionalState==y.HasFunctionalState && x.IsFunctional==y.IsFunctional; }
        // Collection order changes may cause an extra rebuild, never a missed change.
        public static bool Same(BlockSample[] a,BlockSample[] b)
        {
            if(a==null || b==null || a.Length!=b.Length)return false;
            for(int i=0;i<a.Length;i++)
            {
                if(!Equal(a[i],b[i]))return false;
            }
            return true;
        }
    }
}

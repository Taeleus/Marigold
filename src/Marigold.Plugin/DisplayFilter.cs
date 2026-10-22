using System;
namespace Marigold
{
    internal static class DisplayFilter
    {
        public const string Categories="all armor conveyors thrusters weapons power gas mechanical production cargo control tools other";
        public const string States="all faults missing damaged broken added replaced unknown";
        public static string Category(string definition)
        {
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
        public static bool CategoryMatches(string definition,string category){return category=="all" || Category(definition)==category;}
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

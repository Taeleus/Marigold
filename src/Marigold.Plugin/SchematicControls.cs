using System;
using System.Globalization;

namespace Marigold
{
    internal sealed class SchematicControls
    {
        public bool Visible = true, Spin = true;
        private bool planView;
        private bool rotatingView = true;
        public Point3 Project(Point3 cockpitPoint)
        {
            var result=SchematicGeometry.Rotate(cockpitPoint,Yaw,Pitch);
            // Plan views keep cockpit port/starboard fixed on screen, with the nose up.
            if(planView) { result.X=cockpitPoint.X; result.Y=-cockpitPoint.Z; }
            else if(rotatingView) result.X=-result.X;
            return result;
        }
        public int Detail = 1;
        public string Mode="compare", Category="all", StateFilter="all";
        public bool Expanded;
        public bool Station=false,AutoAssociate=true;
        public int Page=1;
        public string GeometryOptions { get { return Detail+"/"+Mode+"/"+Category+"/"+StateFilter+"/"+Page+"/"+Station; } }
        public bool StatsRequested;
        public string ReferenceAction, ProfileArgument;
        public string DetailName { get { return Detail == 0 ? "low" : Detail == 1 ? "medium" : "high"; } }
        public const double DefaultYaw = 0;
        public double Yaw = DefaultYaw, Pitch = -0.4, Zoom = 0.85;
        public const string Help = "/mg show|hide|spin|reset|left|right|top|bottom|in|out. Help: /mg help display, /mg help profiles, /mg help blueprint. Diagnostics: /mg report or /mg stats. /mg station on|off; /mg auto on|off|status|retry.";

        public static bool Owns(string text)
        {
            if (text == null) return false;
            text = text.Trim();
            return Matches(text,"/mg") || Matches(text,"/marigold");
        }
        private static bool Matches(string text,string prefix)
        {
            return text.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && text.Length > prefix.Length && char.IsWhiteSpace(text[prefix.Length]));
        }

        internal static string[] CommandWords(string text)
        {
            var words=new System.Collections.Generic.List<string>();var word=new System.Text.StringBuilder();bool quoted=false;
            foreach(char c in text.Trim())
            {
                if(c=='"') { quoted=!quoted;continue; }
                if(char.IsWhiteSpace(c) && !quoted) { if(word.Length>0){words.Add(word.ToString());word.Clear();} }
                else word.Append(c);
            }
            if(quoted)return new string[0];
            if(word.Length>0)words.Add(word.ToString());return words.ToArray();
        }
        public string Execute(string text)
        {
            StatsRequested=false; ReferenceAction=null; ProfileArgument=null;
            var args = CommandWords(text);
            if(args.Length==0)return "Unclosed quotation mark. Put blueprint paths with spaces in double quotes.";
            if(args.Length==3 && args[1].Equals("help",StringComparison.OrdinalIgnoreCase))
            {
                switch(args[2].ToLowerInvariant())
                {
                    case "display":return "Display: /mg mode live|reference|compare; /mg layout compact|expanded; /mg zoom 0.25..3; /mg detail low|medium|high.\n/mg isolate "+DisplayFilter.Categories+".\n/mg filter "+DisplayFilter.States+"; /mg page next|prev|N; /mg settings save|load|reset.";
                    case "profiles":return "Sit in the anchor cockpit: /mg anchor, then /mg capture. /mg profiles lists IDs.\nAutomatic matching is on by default. /mg auto status|on|off|retry. Manual fallback: /mg profile suggest, then /mg profile select ID.\n/mg clear clears the baseline; /mg profile detach keeps saved data; /mg profile delete ID removes it. /mg recover restores the database backup.";
                    case "blueprint":return "Import: /mg blueprint load \"full path to bp.sbc\"; grids; grid N; anchors; preview N; accept or cancel. Repeat /mg blueprint before each step. Preview is unsaved until accept.";
                    default:return Help;
                }
            }
            if(args.Length==3 && args[1].Equals("settings",StringComparison.OrdinalIgnoreCase))
            {
                string action=args[2].ToLowerInvariant();
                if(action=="save" || action=="load" || action=="reset") { ReferenceAction="settings "+action;return "Settings requested."; }
                return "Settings: save, load, reset.";
            }
            if(args.Length==3)
            {
                string verb=args[1].ToLowerInvariant(),value=args[2].ToLowerInvariant();
                if(verb=="station" && (value=="on" || value=="off" || value=="status")) { ReferenceAction="station "+value;return "Station mode requested."; }
                if(verb=="auto" && (value=="on" || value=="off" || value=="status" || value=="retry")) { ReferenceAction="auto "+value;return "Reassociation requested."; }
                if(verb=="mode") { if(!DisplayFilter.Contains("live reference compare",value))return "Mode: live, reference or compare.";Mode=value;Page=1;return "View mode: "+value; }
                if(verb=="isolate") { if(!DisplayFilter.Contains(DisplayFilter.Categories,value))return "Categories: "+DisplayFilter.Categories;Category=value;Page=1;return "Visible system: "+value; }
                if(verb=="filter") { if(!DisplayFilter.Contains(DisplayFilter.States,value))return "States: "+DisplayFilter.States;StateFilter=value;Page=1;return "Marker filter: "+value; }
                if(verb=="layout") { if(value!="compact" && value!="expanded")return "Layout: compact or expanded.";Expanded=value=="expanded";return "Layout: "+value; }
                if(verb=="page")
                {
                    int page;if(value=="next")Page=Math.Min(2000,Page+1);else if(value=="prev")Page=Math.Max(1,Page-1);
                    else if(int.TryParse(value,out page) && page>=1 && page<=2000)Page=page;else return "Marker page: next, prev or a number from 1 to 2000.";
                    return "Marker page: "+Page;
                }
            }
            if(args.Length>=3 && args[1].Equals("blueprint",StringComparison.OrdinalIgnoreCase))
            {
                string verb=args[2].ToLowerInvariant();
                if((args.Length==4 && (verb=="load" || verb=="grid" || verb=="preview")) ||
                    (args.Length==3 && (verb=="grids" || verb=="anchors" || verb=="accept" || verb=="cancel")))
                { ReferenceAction="blueprint "+verb;ProfileArgument=args.Length==4?args[3]:null;return "Blueprint command requested."; }
                return "Blueprint: load \"path to bp.sbc\", grids, grid N, anchors, preview N, accept, cancel.";
            }
            if (args.Length == 1) return Help;
            if (args.Length == 3 && args[1].Equals("zoom", StringComparison.OrdinalIgnoreCase))
            {
                double zoom;
                if (!double.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out zoom) ||
                    double.IsNaN(zoom) || double.IsInfinity(zoom) || zoom < 0.25 || zoom > 3)
                    return "Zoom must be a number from 0.25 to 3.";
                Zoom = zoom;
                return "Zoom: " + Zoom.ToString("0.00", CultureInfo.InvariantCulture);
            }
            if (args.Length == 3 && args[1].Equals("detail", StringComparison.OrdinalIgnoreCase))
            {
                switch(args[2].ToLowerInvariant())
                {
                    case "low": Detail=0; break;
                    case "medium": Detail=1; break;
                    case "high": Detail=2; break;
                    default: return "Detail must be low, medium or high.";
                }
                return "Detail: " + DetailName + " (rebuilding outline).";
            }
            if(args.Length==4 && args[1].Equals("profile",StringComparison.OrdinalIgnoreCase) &&
                (args[2].Equals("select",StringComparison.OrdinalIgnoreCase) || args[2].Equals("delete",StringComparison.OrdinalIgnoreCase)))
            { ReferenceAction="profile "+args[2].ToLowerInvariant(); ProfileArgument=args[3]; return "Profile command requested."; }
            if(args.Length==3 && args[1].Equals("profile",StringComparison.OrdinalIgnoreCase) && (args[2].Equals("detach",StringComparison.OrdinalIgnoreCase) || args[2].Equals("suggest",StringComparison.OrdinalIgnoreCase)))
            { ReferenceAction="profile "+args[2].ToLowerInvariant(); return "Profile command requested."; }
            if (args.Length != 2) return Help;
            switch (args[1].ToLowerInvariant())
            {
                case "anchor": case "claim": ReferenceAction="anchor"; return "Anchor requested.";
                case "profiles": case "recover": ReferenceAction=args[1].ToLowerInvariant(); return "Profile command requested.";
                case "capture": case "clear": case "report": ReferenceAction=args[1].ToLowerInvariant(); return "Reference command requested.";
                case "stats": StatsRequested=true; return "Statistics requested.";
                case "show": Visible = true; return "Schematic shown.";
                case "hide": Visible = false; return "Schematic hidden.";
                case "spin": Spin = !Spin; if(Spin) { planView=false; rotatingView=true; } return Spin ? "Automatic rotation on." : "Automatic rotation off.";
                case "reset": rotatingView=true; planView=false; Yaw = DefaultYaw; Pitch = -0.4; Zoom = 0.85; Spin = true; return "Default view and automatic rotation restored.";
                case "left": rotatingView=false; planView=false; Yaw = Math.PI / 2; Pitch = 0; Spin = false; return "Cockpit-relative left view.";
                case "right": rotatingView=false; planView=false; Yaw = -Math.PI / 2; Pitch = 0; Spin = false; return "Cockpit-relative right view.";
                case "up":
                case "top": rotatingView=false; planView=true; Yaw = Math.PI; Pitch = -Math.PI / 2; Spin = false; return "Cockpit-relative top view.";
                case "down":
                case "bottom": rotatingView=false; planView=true; Yaw = 0; Pitch = Math.PI / 2; Spin = false; return "Cockpit-relative bottom view.";
                case "in": Zoom = Math.Min(3, Zoom * 1.2); break;
                case "out": Zoom = Math.Max(0.25, Zoom / 1.2); break;
                default: return Help;
            }
            return "View updated.";
        }

        public bool TryHandle(string text, ref bool sendToOthers, out string response)
        {
            response = null;
            if (!Owns(text)) return false;
            sendToOthers = false;
            response = Execute(text);
            return true;
        }
    }
}

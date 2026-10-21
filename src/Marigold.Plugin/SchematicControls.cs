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
        public bool StatsRequested;
        public string ReferenceAction;
        public string DetailName { get { return Detail == 0 ? "low" : Detail == 1 ? "medium" : "high"; } }
        public const double DefaultYaw = 0;
        public double Yaw = DefaultYaw, Pitch = -0.4, Zoom = 0.85;
        public const string Help = "/mg show|hide|spin|reset|left|right|top|bottom|in|out; /mg zoom 0.25..3; /mg detail low|medium|high; /mg stats; /mg capture|clear|report";

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

        public string Execute(string text)
        {
            StatsRequested=false; ReferenceAction=null;
            var args = text.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
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
            if (args.Length != 2) return Help;
            switch (args[1].ToLowerInvariant())
            {
                case "anchor": case "claim": return "Cockpit anchors and persistent profiles are not available yet. Use /mg capture for a reference that lasts until you leave this world.";
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

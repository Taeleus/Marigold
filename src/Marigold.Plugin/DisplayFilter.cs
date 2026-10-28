using System;
using System.Collections.Generic;

namespace Marigold
{
    internal static class DisplayFilter
    {
        public const string Categories = "all armor conveyors thrusters weapons power tanks production cargo control tools other";
        private const string LegacyCategories = "all armor conveyors thrusters weapons power gas mechanical production cargo control tools other";
        public const string States = "all faults missing damaged broken added replaced unknown";
        private static volatile HashSet<string> tools = new HashSet<string>(StringComparer.Ordinal);
        private static volatile HashSet<string> offensive = new HashSet<string>(StringComparer.Ordinal);

        public static int ClassificationRevision { get; private set; }

        // Publish a new, immutable snapshot; geometry workers never query game/mod APIs.
        public static void SetOffensiveDefinitions(IEnumerable<string> definitions)
        {
            var next = new HashSet<string>(definitions, StringComparer.Ordinal);
            if (offensive.SetEquals(next))
                return;
            offensive = next;
            ClassificationRevision++;
        }

        public static void SetToolSubtypes(IEnumerable<string> subtypes)
        {
            var next = new HashSet<string>(subtypes, StringComparer.Ordinal);
            if (tools.SetEquals(next))
                return;
            tools = next;
            ClassificationRevision++;
        }

        public static string Category(string definition)
        {
            string subtype = (definition ?? "");
            int separator = subtype.IndexOf('/');
            subtype = separator < 0 ? "" : subtype.Substring(separator + 1);
            if (tools.Contains(subtype))
                return "tools";
            if (definition != null && offensive.Contains(definition))
                return "weapons";
            string id = (definition ?? "").ToLowerInvariant();
            string type = id.Split('/')[0].Replace("myobjectbuilder_", "");
            bool namedTool = id.Contains("drill") || id.Contains("grinder") || id.Contains("welder") || id.Contains("toolcore");
            if (type.Contains("drill")
                || type.Contains("grinder")
                || type.Contains("welder")
                || type.Contains("toolcore")
                || (namedTool && (type == "cubeblock" || type == "conveyorsorter")))
                return "tools";
            if (type.Contains("conveyor") || type.Contains("connector"))
                return "conveyors";
            if (type.Contains("thrust"))
                return "thrusters";
            if (type.Contains("turret") || type.Contains("gun") || type.Contains("launcher") || type.Contains("warhead"))
                return "weapons";
            if (type.Contains("reactor")
                || type.Contains("battery")
                || type.Contains("solar")
                || type.Contains("windturbine")
                || type.Contains("hydrogenengine"))
                return "power";
            if (type.Contains("oxygen") || type.Contains("gastank") || type.Contains("gasgenerator"))
                return "tanks";
            if (type.Contains("assembler") || type.Contains("refinery"))
                return "production";
            if (type.Contains("cargo"))
                return "cargo";
            if (type.Contains("cockpit") || type.Contains("remotecontrol") || type.Contains("gyro"))
                return "control";
            if (type == "cubeblock")
                return "armor";
            return "other";
        }

        public static string DefaultStyles
        {
            get
            {
                return new string ('1', Categories.Split(' ').Length);
            }
        }

        public static bool ValidStyles(string styles)
        {
            if (styles == null
                || (styles.Length != DefaultStyles.Length && styles.Length != LegacyCategories.Split(' ').Length))
                return false;
            foreach (char c in styles)
                if (c < '0' || c > '3')
                    return false;
            return true;
        }

        public static char Style(string definition, string styles)
        {
            int index = Array.IndexOf(Categories.Split(' '), Category(definition));
            return styles[index < 0 ? 0 : index];
        }

        public static string VisibleCategories(string selected, string styles)
        {
            var result = new System.Collections.Generic.List<string>();
            var names = Categories.Split(' ');
            for (int i = 1; i < names.Length; i++)
                if (styles[i] != '0' && (selected == "all" || Array.IndexOf(selected.Split(','), names[i]) >= 0))
                    result.Add(names[i]);
            return result.Count == 0 ? "none" : string.Join(",", result);
        }

        public static bool ValidCategories(string value)
        {
            if (value == "all" || value == "none")
                return true;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            foreach (var part in value.Split(','))
                if (part == "all" || (!Contains(Categories, part) && part != "gas" && part != "mechanical"))
                    return false;
            return true;
        }

        public static bool CategoryMatches(string definition, string category)
        {
            category = NormalizeCategories(category);
            return category == "all" || Array.IndexOf(category.Split(','), Category(definition)) >= 0;
        }

        public static bool IsContext(string definition, string category, string styles)
        {
            return category != "none"
                && !CategoryMatches(definition, category)
                && Style(definition, styles) != '0';
        }

        public static List<Box3> ContextBoxes(BlockSample[] current, BlockSample[] reference, string category, string styles)
        {
            var result = new List<Box3>();
            if (category == "all" || category == "none" || category == VisibleCategories("all", styles))
                return result;
            var positions = new HashSet<Point3>();
            foreach (var block in current)
                if (IsContext(block.Definition, category, styles) && positions.Add(block.Position))
                    result.Add(block.Bounds);
            if (reference != null)
                foreach (var block in reference)
                    if (IsContext(block.Definition, category, styles) && positions.Add(block.Position))
                        result.Add(block.Bounds);
            return result;
        }

        public static bool StateMatches(ChangeKind kind, string state)
        {
            switch (state)
            {
                case "all":
                    return true;
                case "faults":
                    return kind == ChangeKind.Missing
                        || kind == ChangeKind.Damaged
                        || kind == ChangeKind.NonFunctional
                        || kind == ChangeKind.Unknown;
                case "missing":
                    return kind == ChangeKind.Missing;
                case "damaged":
                    return kind == ChangeKind.Damaged;
                case "broken":
                    return kind == ChangeKind.NonFunctional;
                case "added":
                    return kind == ChangeKind.Added;
                case "replaced":
                    return kind == ChangeKind.Replaced;
                case "unknown":
                    return kind == ChangeKind.Unknown;
                default:
                    return false;
            }
        }

        public static bool Contains(string values, string value)
        {
            return Array.IndexOf(values.Split(' '), value) >= 0;
        }

        public static string NormalizeCategories(string value)
        {
            if (value == "all" || value == "none")
                return value;
            var normalized = new List<string>();
            foreach (string category in value.Split(','))
            {
                string item = category.ToLowerInvariant();
                if (item == "gas")
                    item = "tanks";
                else if (item == "mechanical")
                    item = "other";
                if (!normalized.Contains(item))
                    normalized.Add(item);
            }

            return string.Join(",", normalized);
        }

        public static string NormalizeStyles(string styles)
        {
            if (styles.Length == Categories.Split(' ').Length)
                return styles;
            var oldNames = LegacyCategories.Split(' ');
            var newNames = Categories.Split(' ');
            var result = new char[newNames.Length];
            for (int i = 0; i < newNames.Length; i++)
            {
                string oldName = newNames[i] == "tanks" ? "gas" : newNames[i];
                result[i] = styles[Array.IndexOf(oldNames, oldName)];
            }

            return new string (result);
        }
    }

    internal static class ObservationCache
    {
        public static bool Equal(BlockSample x, BlockSample y)
        {
            return x.Position.Equals(y.Position)
                && x.Bounds.Min.Equals(y.Bounds.Min)
                && x.Bounds.Max.Equals(y.Bounds.Max)
                && x.Definition == y.Definition
                && x.Forward == y.Forward
                && x.Up == y.Up
                && x.Integrity == y.Integrity
                && x.BuildIntegrity == y.BuildIntegrity
                && x.MaxIntegrity == y.MaxIntegrity
                && x.HasFunctionalState == y.HasFunctionalState
                && x.IsFunctional == y.IsFunctional;
        }

        // Collection order changes may cause an extra rebuild, never a missed change.
        public static bool Same(BlockSample[] a, BlockSample[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!Equal(a[i], b[i]))
                    return false;
            }

            return true;
        }
    }
}

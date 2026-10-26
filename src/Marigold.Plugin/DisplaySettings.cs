using System;
using System.IO;
using System.Globalization;

namespace Marigold
{
    // Explicit save avoids disk writes on animation frames or transient view commands.
    internal static class DisplaySettings
    {
        public static void Reset(SchematicControls c)
        {
            c.HealthBars = new HealthBarSettings();
            c.Appearance = new HudAppearance();
            c.SystemStyles = DisplayFilter.DefaultStyles;
            c.Detail = 2;
            c.Mode = "compare";
            c.Category = "all";
            c.StateFilter = "all";
            c.Expanded = false;
            c.Zoom = .85;
            c.Visible = true;
            c.SchematicVisible = true;
            c.Page = 1;
            c.Station = false;
            c.AutoAssociate = true;
            c.Language = "en";
        }

        public static void Load(string path, SchematicControls c)
        {
            if (!File.Exists(path))
                return;
            if (new FileInfo(path).Length > 2048)
                throw new InvalidDataException("Display settings are too large.");
            Apply(File.ReadAllLines(path), c);
        }

        public static string Export(SchematicControls c)
        {
            return string.Join(
                "\n",
                new[]
                {
                    "Marigold display 7",
                    c.Detail.ToString(CultureInfo.InvariantCulture),
                    c.Mode,
                    c.Category,
                    c.StateFilter,
                    c.Expanded.ToString(),
                    c.Zoom.ToString("R", CultureInfo.InvariantCulture),
                    c.Visible.ToString(),
                    c.Station.ToString(),
                    c.AutoAssociate.ToString(),
                    c.Language,
                    c.Appearance.Encode(),
                    c.SystemStyles,
                    c.HealthBars.Encode(),
                    c.SchematicVisible.ToString()
                }
            );
        }

        public static void Import(string text, SchematicControls c)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 2048)
                throw new InvalidDataException("Clipboard does not contain a valid Marigold display config.");
            Apply(text.Trim().Replace("\r", "").Split('\n'), c);
        }

        private static void Apply(string[] a, SchematicControls c)
        {
            bool schematicVisible = true;
            if (a.Length == 15 && a[0] == "Marigold display 7")
            {
                if (!bool.TryParse(a[14], out schematicVisible))
                    throw new InvalidDataException("Invalid schematic visibility.");
                Array.Resize(ref a, 14);
                a[0] = "Marigold display 6";
            }

            int detail;
            double zoom;
            bool expanded, visible;
            bool station = false, auto = true;
            bool version1 = a.Length == 8 && a[0] == "Marigold display 1",
                version2 = a.Length == 10 && a[0] == "Marigold display 2",
                version3 = a.Length == 11 && a[0] == "Marigold display 3",
                version4 = a.Length == 12 && a[0] == "Marigold display 4",
                version5 = a.Length == 13 && a[0] == "Marigold display 5",
                version6 = a.Length == 14 && a[0] == "Marigold display 6";
            var appearance = (version4 || version5 || version6) ? HudAppearance.Parse(a[11]) : new HudAppearance();
            if ((!version1 && !version2 && !version3 && !version4 && !version5 && !version6)
                || !int.TryParse(a[1], out detail)
                || detail < 0
                || detail > 2
                || !DisplayFilter.Contains("live reference compare", a[2])
                || !DisplayFilter.ValidCategories(a[3])
                || !DisplayFilter.Contains(DisplayFilter.States, a[4])
                || !bool.TryParse(a[5], out expanded)
                || !double.TryParse(a[6], NumberStyles.Float, CultureInfo.InvariantCulture, out zoom)
                || double.IsNaN(zoom)
                || zoom < .25
                || zoom > 3
                || !bool.TryParse(a[7], out visible)
                || ((version2 || version3 || version4 || version5 || version6)
                && (!bool.TryParse(a[8], out station) || !bool.TryParse(a[9], out auto)))
                || ((version3 || version4 || version5 || version6) && a[10] != "en" && a[10] != "es"))
                throw new InvalidDataException("Invalid display settings; existing settings were kept.");
            if ((version5 || version6) && !DisplayFilter.ValidStyles(a[12]))
                throw new InvalidDataException("Invalid system styles.");
            var bars = version6 ? HealthBarSettings.Parse(a[13]) : new HealthBarSettings();
            c.SchematicVisible = schematicVisible;
            c.HealthBars = bars;
            c.SystemStyles = (version5 || version6) ? DisplayFilter.NormalizeStyles(a[12]) : DisplayFilter.DefaultStyles;
            c.Appearance = appearance;
            c.Detail = detail;
            c.Mode = "compare";
            c.Category = DisplayFilter.NormalizeCategories(a[3]);
            c.StateFilter = a[4];
            c.Expanded = expanded;
            c.Zoom = zoom;
            c.Visible = visible;
            c.Page = 1;
            c.Station = false;
            c.AutoAssociate = auto;
            c.Language = (version3 || version4 || version5 || version6) ? a[10] : "en";
        }

        public static void Save(string path, SchematicControls c)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, Export(c));
                if (File.Exists(path))
                    File.Replace(temp, path, path + ".bak");
                else
                    File.Move(temp, path);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }
}

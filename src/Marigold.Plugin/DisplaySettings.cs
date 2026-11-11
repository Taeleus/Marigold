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
            ApplyLayout(new SchematicControls(), c);
            c.Station = false;
            c.AutoAssociate = true;
            c.Language = "en";
        }

        public static void Load(string path, SchematicControls c)
        {
            if (File.Exists(path))
                Apply(ReadSettings(path), c);
        }

        private static string[] ReadSettings(string path)
        {
            if (new FileInfo(path).Length > 2048)
                throw new InvalidDataException("Display settings are too large.");
            return File.ReadAllLines(path);
        }

        public static bool UsesShipLayout(string shipPath)
        {
            return File.Exists(shipPath) && !File.Exists(DefaultMarker(shipPath));
        }

        public static void LoadShipLayout(string defaultPath, string shipPath, SchematicControls c)
        {
            if (File.Exists(shipPath))
                ApplyLayout(ReadLayout(defaultPath, shipPath, UsesShipLayout(shipPath)), c);
        }

        public static void SelectShipLayout(string defaultPath, string shipPath, SchematicControls c, bool useSaved)
        {
            if (useSaved && !File.Exists(shipPath))
                throw new InvalidOperationException("This ship has no saved display layout.");

            // Validate the display before changing what this ship loads on reconnect.
            var selected = ReadLayout(defaultPath, shipPath, useSaved);
            if (useSaved)
                EnableShipLayout(shipPath);
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(shipPath));
                File.WriteAllText(DefaultMarker(shipPath), string.Empty);
            }
            ApplyLayout(selected, c);
        }

        public static void EnableShipLayout(string shipPath)
        {
            File.Delete(DefaultMarker(shipPath));
        }

        private static string DefaultMarker(string shipPath)
        {
            return shipPath + ".default";
        }

        private static SchematicControls ReadLayout(string defaultPath, string shipPath, bool useSaved)
        {
            var selected = new SchematicControls();
            if (useSaved)
                Apply(ReadSettings(shipPath), selected);
            else
                Load(defaultPath, selected);
            return selected;
        }

        public static void SavePreferences(string path, SchematicControls c)
        {
            var defaults = new SchematicControls();
            Load(path, defaults);
            defaults.AutoAssociate = c.AutoAssociate;
            defaults.Language = c.Language;
            Save(path, defaults);
        }

        public static void LoadTexturedShipRenderer(string displaySettingsPath, SchematicControls c)
        {
            string path = TexturedShipRendererPath(displaySettingsPath);
            if (!File.Exists(path))
                return;
            if (new FileInfo(path).Length > 32)
                throw new InvalidDataException("Invalid Textured Ship renderer setting.");

            string renderer = File.ReadAllText(path).Trim();
            if (renderer == "CPU")
                c.UseGpuTexturedShip = false;
            else if (renderer == "GPU")
                c.UseGpuTexturedShip = true;
            else
                throw new InvalidDataException("Invalid Textured Ship renderer setting.");
        }

        public static void SaveTexturedShipRenderer(string displaySettingsPath, bool useGpu)
        {
            SaveText(TexturedShipRendererPath(displaySettingsPath), useGpu ? "GPU" : "CPU");
        }

        private static string TexturedShipRendererPath(string displaySettingsPath)
        {
            return Path.Combine(Path.GetDirectoryName(displaySettingsPath), "textured-renderer.txt");
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
            var appearance = (version4 || version5 || version6)
                ? HudAppearance.Parse(a[11])
                : new HudAppearance { ScaleBackgroundWithShip = false };
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
            ApplyLayout(new SchematicControls
            {
                SchematicVisible = schematicVisible,
                HealthBars = version6 ? HealthBarSettings.Parse(a[13]) : new HealthBarSettings(),
                SystemStyles = (version5 || version6) ? DisplayFilter.NormalizeStyles(a[12]) : DisplayFilter.DefaultStyles,
                Appearance = appearance,
                Detail = detail,
                Category = DisplayFilter.NormalizeCategories(a[3]),
                StateFilter = a[4],
                Expanded = expanded,
                Zoom = zoom,
                Visible = visible
            }, c);
            c.Station = false;
            c.AutoAssociate = auto;
            c.Language = (version3 || version4 || version5 || version6) ? a[10] : "en";
        }

        private static void ApplyLayout(SchematicControls source, SchematicControls target)
        {
            target.HealthBars = source.HealthBars;
            target.Appearance = source.Appearance;
            target.SystemStyles = source.SystemStyles;
            target.Detail = source.Detail;
            target.Mode = source.Mode;
            target.Category = source.Category;
            target.StateFilter = source.StateFilter;
            target.Expanded = source.Expanded;
            target.Zoom = source.Zoom;
            target.Visible = source.Visible;
            target.SchematicVisible = source.SchematicVisible;
            target.Page = source.Page;
        }

        public static void Save(string path, SchematicControls c)
        {
            SaveText(path, Export(c));
        }

        private static void SaveText(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, text);
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

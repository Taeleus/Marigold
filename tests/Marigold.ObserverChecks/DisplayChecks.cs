using System;
using System.IO;
using Marigold;

internal static class DisplayChecks
{
    private static int count;

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        count++;
    }

    public static void Run()
    {
        foreach (var pair in new[]
        {
            "ShipDrill:tools",
            "ShipWelder:tools",
            "ShipGrinder:tools",
            "ToolCore:tools",
            "Thrust:thrusters",
            "LargeGatlingTurret:weapons",
            "BatteryBlock:power",
            "OxygenTank:tanks",
            "MotorStator:other",
            "PistonBase:other",
            "Assembler:production",
            "CargoContainer:cargo",
            "Cockpit:control",
            "ConveyorSorter:conveyors",
            "CubeBlock:armor",
            "Unrecognized:other"
        })
        {
            var a = pair.Split(':');
            Check(DisplayFilter.Category("MyObjectBuilder_" + a[0] + "/ModdedSubtype") == a[1], "Category " + a[0]);
        }

        Check(
            DisplayFilter.Category("MyObjectBuilder_CubeBlock/DeepWelderTypeIIToolcoreSmall") == "tools",
            "CubeBlock ToolCore subtype is classified as a tool"
        );
        Check(
            !DisplayFilter.Contains(DisplayFilter.Categories, "mechanical")
                && DisplayFilter.Category("MyObjectBuilder_MotorStator/ModdedSubtype") == "other",
            "Mechanical blocks are included in Utility rather than a separate system"
        );
        Check(
            DisplayFilter.ValidCategories("gas,mechanical")
                && DisplayFilter.NormalizeCategories("gas,mechanical") == "tanks,other",
            "Legacy category settings map to Tanks and Utility"
        );
        Check(!DisplayFilter.Contains(DisplayFilter.Categories, "armor conveyors"), "Multiple category tokens rejected");
        foreach (ChangeKind kind in Enum.GetValues(typeof(ChangeKind)))
        {
            Check(
                DisplayFilter.StateMatches(kind, "broken") == (kind == ChangeKind.NonFunctional),
                "Only nonfunctional is broken"
            );
            Check(DisplayFilter.StateMatches(kind, "missing") == (kind == ChangeKind.Missing), "Only absent is missing");
        }

        var block = new BlockSample
        {
            Position = new Point3(0, 0, 0),
            Bounds = new Box3(new Point3(-.5, -.5, -.5), new Point3(.5, .5, .5)),
            Definition = "MyObjectBuilder_ShipDrill/Test",
            Forward = 0,
            Up = 4,
            Integrity = 100,
            BuildIntegrity = 100,
            MaxIntegrity = 100,
            HasFunctionalState = true,
            IsFunctional = true
        };
        var original = new[]
        {
            block
        };
        Check(ObservationCache.Same(original, new[] { block }), "Unchanged sample reused");
        block.Integrity = 90;
        Check(!ObservationCache.Same(original, new[] { block }), "Damage invalidates cache");
        Check(ProfileSuggestions.Score(original, new[] { block }) == 1, "Suggestion ignores damage");
        block.IsFunctional = false;
        Check(!ObservationCache.Same(original, new[] { block }), "Function invalidates cache");
        block.Definition = "Replacement";
        Check(ProfileSuggestions.Score(original, new[] { block }) == 0, "Replacement cannot imply identity");
        Check(ProfileSuggestions.Score(original, new BlockSample[0]) == 0, "Empty suggestion");
        Check(!ObservationCache.Same(original, new BlockSample[0]), "Removal invalidates cache");
        var c = new SchematicControls();
        c.Execute("/mg isolate tools");
        c.Execute("/mg filter broken");
        c.Execute("/mg layout expanded");
        c.Execute("/mg mode reference");
        c.Execute("/mg zoom 2");
        Check(
            c.Category == "tools" && c.StateFilter == "broken" && c.Expanded && c.Mode == "compare" && c.Zoom == 2,
            "Display commands applied"
        );
        c.Execute("/mg page next");
        Check(c.Page == 2, "Next page");
        c.Execute("/mg filter all");
        Check(c.Page == 1, "Filter resets page");
        c.Execute("/mg profile suggest");
        Check(c.ReferenceAction == "profile suggest", "Suggestion command routed");
        string folder = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "DisplayTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string file = Path.Combine(folder, "display.txt");
            DisplaySettings.Save(file, c);
            var restored = new SchematicControls();
            DisplaySettings.Load(file, restored);
            Check(
                restored.Category == "tools" && restored.Expanded && restored.Zoom == 2 && restored.Mode == "compare",
                "Saved display round trip"
            );
            c.Zoom = 1;
            DisplaySettings.Save(file, c);
            Check(File.Exists(file + ".bak"), "Previous settings backed up");
            File.WriteAllText(file, "broken");
            bool rejected = false;
            try
            {
                DisplaySettings.Load(file, restored);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check(
                rejected && restored.Zoom == 2 && restored.Category == "tools",
                "Invalid file preserves all current options"
            );
            DisplaySettings.Reset(restored);
            Check(
                restored.Mode == "compare" && restored.Category == "all" && restored.Zoom == .85 && !restored.Expanded,
                "Defaults restored"
            );
            CheckDisplaySources(folder);
        }
        finally
        {
            Directory.Delete(folder, true);
        }

        var defaults = new SchematicControls();
        Check(defaults.Appearance.Rendering == HudAppearance.HdWireframe && defaults.Appearance.DrawFills
            && defaults.Appearance.DrawHull, "New layouts default to HD Wireframe with full damage shading");
        var wireframe = DisplaySettings.Export(defaults);
        var styled = new SchematicControls();
        styled.Appearance.Rendering = HudAppearance.Textured;
        styled.Appearance.ShipOpacity = .48;
        styled.Appearance.ExpandedX = .23;
        styled.Appearance.ShowShipLabel = false;
        styled.Appearance.BackgroundWidth = .43;
        styled.Appearance.BackgroundHeight = .16;
        styled.Appearance.ScaleBackgroundWithShip = false;
        DisplaySettings.Import(DisplaySettings.Export(styled), c);
        Check(c.Appearance.Rendering == HudAppearance.Textured && c.Appearance.ShipOpacity == .48
            && c.Appearance.DrawFills && c.Appearance.DrawHull, "Textured style and opacity round-trip with the layout");
        Check(c.Appearance.ExpandedX == .23 && c.Appearance.X == .98,
            "Expanded horizontal placement round-trips independently of compact placement");
        Check(!c.Appearance.ShowShipLabel && c.SchematicVisible, "A layout can hide the label while keeping the schematic visible");
        Check(c.Appearance.BackgroundWidth == .43 && c.Appearance.BackgroundHeight == .16 && c.Appearance.Size == .32
            && !c.Appearance.ScaleBackgroundWithShip, "Independent background dimensions and link state round-trip");
        c.Appearance.Set("size", "0.5");
        Check(c.Appearance.Rendering == HudAppearance.Textured && c.Appearance.ShipOpacity == .48,
            "Resizing retains textured style and opacity");
        Check(c.Appearance.ExpandedX == .23, "Other appearance edits retain expanded horizontal placement");
        Check(!c.Appearance.ShowShipLabel, "Other appearance edits retain a hidden ship label");
        Check(c.Appearance.BackgroundWidth == .43 && c.Appearance.BackgroundHeight == .16,
            "Changing ship scale leaves background dimensions intact");
        DisplaySettings.Import(wireframe, c);
        Check(c.Appearance.Rendering == HudAppearance.HdWireframe && c.Appearance.ShipOpacity == .7
            && c.Appearance.ExpandedX == .5, "HD Wireframe round-trips with default model opacity and centered expanded view");
        Check(c.Appearance.ShowShipLabel, "New layouts show the ship label by default");
        Check(defaults.Appearance.ScaleBackgroundWithShip && c.Appearance.ScaleBackgroundWithShip,
            "New layouts link the background by default and preserve the link when shared");
        string oldAppearance = string.Join(";", styled.Appearance.Encode().Split(';'), 0, 18);
        DisplaySettings.Import(DisplaySettings.Export(styled).Replace(styled.Appearance.Encode(), oldAppearance), c);
        Check(!c.Appearance.ScaleBackgroundWithShip && c.Appearance.BackgroundWidth == .43 && c.Appearance.BackgroundHeight == .16,
            "Older saved layouts keep their independent background dimensions");
        var firstDisplay = wireframe.Split('\n');
        firstDisplay[0] = "Marigold display 1";
        DisplaySettings.Import(string.Join("\n", firstDisplay, 0, 8), c);
        Check(!c.Appearance.ScaleBackgroundWithShip, "Saved layouts from before the appearance record remain independent");

        var explicitSd = new HudAppearance { Rendering = HudAppearance.SdWireframe, Size = .5 }.Encode();
        Check(HudAppearance.Parse(explicitSd).Rendering == HudAppearance.SdWireframe,
            "Explicit SD Wireframe remains selected after saving");
        string oldSquare = string.Join(";", explicitSd.Split(';'), 0, 16);
        var migratedSquare = HudAppearance.Parse(oldSquare);
        Check(!migratedSquare.ScaleBackgroundWithShip && migratedSquare.BackgroundWidth == .5 && migratedSquare.BackgroundHeight == .5,
            "Older layouts retain their saved square background dimensions");
        var positionedSd = oldSquare.Substring(0, oldSquare.LastIndexOf(';'));
        Check(HudAppearance.Parse(positionedSd).ShowShipLabel, "Saved layouts from before the label toggle keep showing the label");
        var unifiedSd = positionedSd.Substring(0, positionedSd.LastIndexOf(';'));
        var oldFullDisplay = unifiedSd.Substring(0, unifiedSd.LastIndexOf(';'));
        Check(HudAppearance.Parse(unifiedSd).Rendering == HudAppearance.SdWireframe
            && HudAppearance.Parse(unifiedSd).ExpandedX == .5, "Saved unified mode keeps explicit SD and centers expanded view");
        Check(HudAppearance.Parse(oldFullDisplay).Rendering == HudAppearance.HdWireframe,
            "Saved Full display migrates to HD Wireframe");
        var oldTextured = HudAppearance.Parse(oldFullDisplay + ";True;0.48");
        Check(oldTextured.Rendering == HudAppearance.Textured && oldTextured.ShipOpacity == .48 && oldTextured.ExpandedX == .5,
            "Saved separate textured toggle maps to the unified rendering mode");

        Console.WriteLine("PASS: " + count + " display/settings/profile-suggestion checks.");
    }

    private static void CheckDisplaySources(string folder)
    {
        string defaultsPath = Path.Combine(folder, "defaults.txt");
        string shipPath = Path.Combine(folder, "display-ship.txt");
        var defaults = new SchematicControls();
        var defaultPower = defaults.HealthBars.Bars[2];
        defaultPower.Enabled = true;
        defaultPower.X = .999;
        defaultPower.Y = .57;
        defaultPower.Width = .28;
        defaultPower.Height = .02;
        defaultPower.PowerMask = 3;
        DisplaySettings.Save(defaultsPath, defaults);

        var savedShip = new SchematicControls();
        savedShip.HealthBars.Bars[2].Enabled = true;
        savedShip.HealthBars.Bars[2].X = .75;
        DisplaySettings.Save(shipPath, savedShip);
        string savedShipFile = File.ReadAllText(shipPath);

        var current = new SchematicControls();
        DisplaySettings.Load(defaultsPath, current);
        current.Station = true;
        DisplaySettings.LoadShipLayout(defaultsPath, shipPath, current);
        Check(DisplaySettings.UsesShipLayout(shipPath) && current.HealthBars.Bars[2].X == .75,
            "An existing ship setup still loads over the default by default");

        DisplaySettings.SelectShipLayout(defaultsPath, shipPath, current, false);
        Check(!DisplaySettings.UsesShipLayout(shipPath) && current.HealthBars.Bars[2].X == .999,
            "Using the default for this ship restores the intended Power bar position");
        Check(File.ReadAllText(shipPath) == savedShipFile,
            "Using the default leaves the saved ship setup available");

        var reconnected = new SchematicControls();
        DisplaySettings.Load(defaultsPath, reconnected);
        reconnected.Station = true;
        DisplaySettings.LoadShipLayout(defaultsPath, shipPath, reconnected);
        Check(reconnected.HealthBars.Bars[2].X == .999 && reconnected.Station,
            "Reconnecting honors the default selection and retains station mode");

        File.WriteAllText(shipPath, "invalid display");
        string before = DisplaySettings.Export(reconnected);
        bool rejected = false;
        try
        {
            DisplaySettings.SelectShipLayout(defaultsPath, shipPath, reconnected, true);
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }
        Check(rejected && !DisplaySettings.UsesShipLayout(shipPath) && DisplaySettings.Export(reconnected) == before,
            "An invalid saved ship setup cannot switch the active layout or reconnect source");

        File.WriteAllText(shipPath, savedShipFile);
        DisplaySettings.SelectShipLayout(defaultsPath, shipPath, reconnected, true);
        Check(DisplaySettings.UsesShipLayout(shipPath) && reconnected.HealthBars.Bars[2].X == .75,
            "Selecting the saved ship setup restores its bar and reconnect preference");

        reconnected.Language = "es";
        reconnected.AutoAssociate = false;
        DisplaySettings.SavePreferences(defaultsPath, reconnected);
        var preferences = new SchematicControls();
        DisplaySettings.Load(defaultsPath, preferences);
        Check(preferences.Language == "es" && !preferences.AutoAssociate
            && preferences.HealthBars.Encode() == defaults.HealthBars.Encode(),
            "Saving preferences while a ship setup is active keeps the default bar positions, sizes, and sections");
        preferences.Station = true;
        DisplaySettings.LoadShipLayout(defaultsPath, shipPath, preferences);
        Check(preferences.HealthBars.Bars[2].X == .75 && preferences.Language == "es"
            && !preferences.AutoAssociate && preferences.Station,
            "Loading the saved ship layout preserves global preferences and station mode");

        DisplaySettings.SelectShipLayout(defaultsPath, shipPath, preferences, false);
        preferences.HealthBars.Bars[2].X = .9;
        DisplaySettings.Save(shipPath, preferences);
        DisplaySettings.EnableShipLayout(shipPath);
        var nextGrid = new SchematicControls();
        DisplaySettings.Load(defaultsPath, nextGrid);
        DisplaySettings.LoadShipLayout(defaultsPath, shipPath, nextGrid);
        Check(DisplaySettings.UsesShipLayout(shipPath) && nextGrid.HealthBars.Bars[2].X == .9,
            "Saving and enabling a ship layout replaces its earlier default selection");

        string missingShipPath = Path.Combine(folder, "display-absent.txt");
        nextGrid.HealthBars.Bars[2].X = .8;
        DisplaySettings.LoadShipLayout(defaultsPath, missingShipPath, nextGrid);
        Check(nextGrid.HealthBars.Bars[2].X == .8 && !DisplaySettings.UsesShipLayout(missingShipPath),
            "Attaching a ship without a saved layout preserves unsaved adjustments");
        nextGrid.Station = true;
        DisplaySettings.SelectShipLayout(Path.Combine(folder, "absent-default.txt"), missingShipPath, nextGrid, false);
        Check(nextGrid.HealthBars.Bars[2].X == .75 && nextGrid.Language == "es"
            && !nextGrid.AutoAssociate && nextGrid.Station,
            "Selecting defaults with no file restores built-in placement and preserves preferences");
    }
}

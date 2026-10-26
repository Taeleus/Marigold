using System;
using System.Collections.Generic;
using System.IO;
using Marigold;

internal static class HealthBarChecks
{
    private static int count;

    private static void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        count++;
    }

    private static BlockSample Block(int i, string type)
    {
        return new BlockSample
        {
            Position = new Point3(i, 0, 0),
            Bounds = new Box3(new Point3(i - .5, -.5, -.5), new Point3(i + .5, .5, .5)),
            Definition = "MyObjectBuilder_" + type + "/Test",
            Forward = 0,
            Up = 4,
            MaxIntegrity = 100,
            Integrity = 100,
            BuildIntegrity = 100,
            HasFunctionalState = true,
            IsFunctional = true
        };
    }

    public static void Run()
    {
        for (int mode = 0; mode < 4; mode++)
        {
            var configured = new SchematicControls();
            configured.Appearance.Rendering = mode;
            var restoredMode = new SchematicControls();
            DisplaySettings.Import(DisplaySettings.Export(configured), restoredMode);
            Check(
                restoredMode.Appearance.Rendering == mode
                    && restoredMode.Appearance.DrawFills == ((mode & 1) == 0)
                    && restoredMode.Appearance.DrawHull == ((mode & 2) == 0),
                "Performance modes persist and select intended layers"
            );
        }

        var projection = new SchematicControls();
        Check(projection.Detail == 2, "New displays default to High");
        foreach (string command in new[]
        {
            "/mg reset",
            "/mg top",
            "/mg bottom",
            "/mg left",
            "/mg right"
        })
        {
            projection.Execute(command);
            var x = projection.Project(new Point3(1, 0, 0));
            var y = projection.Project(new Point3(0, 1, 0));
            var z = projection.Project(new Point3(0, 0, 1));
            var p = new Point3(-.23, .71, .39);
            var actual = projection.Project(p);
            Check(
                Math.Abs(actual.X - (p.X * x.X + p.Y * y.X + p.Z * z.X)) < 1e-12
                    && Math.Abs(actual.Y - (p.X * x.Y + p.Y * y.Y + p.Z * z.Y)) < 1e-12,
                "Axis projection preserves cockpit views"
            );
        }

        var pulseBlock = Block(200, "Reactor");
        var pulseReference = new[]
        {
            pulseBlock
        };
        var pulse = new DamagePulse();
        var region = new BlockChange
        {
            Bounds = pulseBlock.Bounds,
            Kind = ChangeKind.Damaged
        };
        pulse.Observe(pulseReference, pulseReference, 0);
        Check(double.IsNegativeInfinity(pulse.Start(region)), "Loading does not pulse");
        var hit = pulseBlock;
        hit.Integrity = 75;
        pulse.Observe(pulseReference, new[] { hit }, .5);
        Check(
            double.IsNegativeInfinity(pulse.Start(region)) && !DamagePulse.VisibleFor(ChangeKind.Damaged),
            "Yellow damage never pulses"
        );
        hit.IsFunctional = false;
        pulse.Observe(pulseReference, new[] { hit }, 1);
        Check(
            DamagePulse.VisibleFor(ChangeKind.NonFunctional) && DamagePulse.VisibleFor(ChangeKind.Missing),
            "Orange and red retain fresh alerts"
        );
        Check(
            pulse.Start(region) == 1 && DamagePulse.Strength(1, 1) == 1 && DamagePulse.Strength(1, 1.5) == 0,
            "Fresh damage has one-second cycles"
        );
        foreach (double time in new[]
        {
            2.0,
            2.25,
            2.5,
            2.75,
            3.0
        })
            Check(
                DamagePulse.Strength(1, time) == DamagePulse.Strength(1.73, time)
                    && DamagePulse.Strength(1, time) == DamagePulse.Wave(time),
                "Staggered hits and normal missing pulse share the same global phase"
            );
        Check(
            DamagePulse.Strength(1, 5) == 0 && DamagePulse.Strength(2, 5) == 1,
            "Expiration stays individual while rhythm stays shared"
        );
        hit.Integrity = 50;
        pulse.Observe(pulseReference, new[] { hit }, 2);
        Check(pulse.Start(region) == 1, "Continuous fire does not freeze pulse phase");
        pulse.Observe(pulseReference, new[] { hit }, 5);
        Check(
            double.IsNegativeInfinity(pulse.Start(region)) && DamagePulse.Strength(1, 5) == 0,
            "Pulse expires after four seconds without a new hit"
        );
        hit.Integrity = 25;
        pulse.Observe(pulseReference, new[] { hit }, 6);
        Check(pulse.Start(region) == 6, "Later damage retriggers");
        pulse.Observe(pulseReference, pulseReference, 7);
        Check(double.IsNegativeInfinity(pulse.Start(region)), "Repair clears pulse");
        pulse.Observe(pulseReference, new BlockSample[0], 8);
        Check(pulse.Start(region) == 8, "New missing block pulses");
        pulse.Observe(new[] { pulseBlock }, new BlockSample[0], 9);
        Check(double.IsNegativeInfinity(pulse.Start(region)), "Changing baseline suppresses false alerts");
        var pulseSettings = new SchematicControls();
        pulseSettings.Appearance.NewDamagePulse = false;
        var pulseLoaded = new SchematicControls();
        DisplaySettings.Import(DisplaySettings.Export(pulseSettings), pulseLoaded);
        Check(!pulseLoaded.Appearance.NewDamagePulse, "Pulse toggle round trips independently");
        var powerBaseline = new[]
        {
            Block(100, "Reactor"),
            Block(101, "BatteryBlock"),
            Block(102, "SolarPanel"),
            Block(103, "WindTurbine"),
            Block(104, "HydrogenEngine")
        };
        var powerLive = (BlockSample[])powerBaseline.Clone();
        var damagedPower = powerLive[1];
        damagedPower.Integrity = 50;
        powerLive[1] = damagedPower;
        var power = ShipHealth.Build(powerBaseline, new[]
        {
            powerLive[1],
            powerLive[2],
            powerLive[3],
            powerLive[4]
        });
        for (int mask = 0; mask < 8; mask++)
        {
            var sections = power.PowerSections(mask);
            for (int state = 0; state < 5; state++)
            {
                int sum = 0;
                for (int type = 0; type < 4; type++)
                    sum += sections[type, state];
                Check(sum == power.Counts[2, state], "Every selection retains all power states");
            }

            Check(sections[(mask & 1) != 0 ? 0 : 3, 3] == 1, "Missing reactor stays in its selected or generic section");
            Check(sections[(mask & 2) != 0 ? 1 : 3, 1] == 1, "Damaged battery stays in its selected or generic section");
            var config = new SchematicControls();
            config.HealthBars.Bars[2].PowerMask = mask;
            var roundtrip = new SchematicControls();
            DisplaySettings.Import(DisplaySettings.Export(config), roundtrip);
            Check(roundtrip.HealthBars.Bars[2].PowerMask == mask, "Power selection survives clipboard and saved format");
        }

        Check(
            HealthBarLayout.Parse("False,True,0.75,0.48,0.22,0.014,1").PowerMask == 0,
            "Old layouts retain unsplit power bars"
        );
        Check(
            ShipHealth.PowerType("MyObjectBuilder_Reactor/ModdedFusion") == 0
                && ShipHealth.PowerType("MyObjectBuilder_CubeBlock/ReactorDecoration") == 3,
            "Power classification uses builder type, not subtype hints"
        );
        var baseline = new[]
        {
            Block(0, "CubeBlock"),
            Block(1, "BatteryBlock"),
            Block(2, "Thrust"),
            Block(3, "Conveyor"),
            Block(4, "CargoContainer"),
            Block(5, "ShipDrill"),
            Block(6, "ShipWelder"),
            Block(7, "ShipGrinder"),
            Block(8, "CubeBlock")
        };
        var toolcore = baseline[8];
        toolcore.Definition = "MyObjectBuilder_CubeBlock/DeepWelderTypeIIToolcoreSmall";
        baseline[8] = toolcore;
        var live = new List<BlockSample>(baseline);
        var b = live[1];
        b.Integrity = 50;
        live[1] = b;
        b = live[2];
        b.Integrity = 10;
        b.IsFunctional = false;
        live[2] = b;
        live.RemoveAt(3);
        b = live[3];
        b.Integrity = double.NaN;
        live[3] = b;
        b = live[4];
        b.Definition = "Replacement";
        live[4] = b;
        live.Add(Block(99, "Reactor"));
        var health = ShipHealth.Build(baseline, live.ToArray());
        Check(health.HasBaseline && health.Total(0) == 9, "Denominator retains missing blocks and excludes additions");
        Check(
            health.Counts[0, 0] == 4
                && health.Counts[0, 1] == 1
                && health.Counts[0, 2] == 1
                && health.Counts[0, 3] == 2
                && health.Counts[0, 4] == 1,
            "Exact blue/yellow/orange/red/gray distribution"
        );
        Check(
            health.Counts[1, 0] == 1
                && health.Counts[2, 1] == 1
                && health.Counts[3, 2] == 1
                && health.Counts[4, 3] == 1
                && health.Counts[5, 4] == 1
                && health.Counts[7, 0] == 3
                && health.Counts[7, 3] == 1,
            "States attributed to baseline systems, including Tools"
        );
        for (int state = 0; state < 5; state++)
        {
            int sum = 0;
            for (int group = 1; group < ShipHealth.Names.Length; group++)
                sum += health.Counts[group, state];
            Check(sum == health.Counts[0, state], "System groups partition overall bar");
        }

        var healed = ShipHealth.Build(baseline, baseline);
        Check(healed.Counts[0, 0] == 9, "Repairs return to blue");
        var absent = ShipHealth.Build(baseline, new BlockSample[0]);
        Check(absent.Counts[0, 3] == 9 && absent.Total(0) == 9, "Destroyed grid remains all red with a baseline");
        Check(
            !ShipHealth.Build(null, baseline).HasBaseline && ShipHealth.Build(new BlockSample[0], baseline).Total(0) == 0,
            "No baseline and empty baseline are not 100 percent healthy"
        );
        Check(
            ShipHealth.Group("MyObjectBuilder_ShipWelder/Modded") == 7
                && ShipHealth.Group("MyObjectBuilder_ToolCore/SDX2Tool") == 7
                && ShipHealth.Group("MyObjectBuilder_CubeBlock/DeepWelderTypeIIToolcoreSmall") == 7
                && ShipHealth.Group("MyObjectBuilder_GasTank/Test") == 4
                && ShipHealth.Group("MyObjectBuilder_MotorStator/Test") == 4
                && ShipHealth.Group("MyObjectBuilder_PistonBase/Test") == 4,
            "ToolCore subtypes use the Tools group, while tanks and mechanical blocks stay Utility"
        );
        foreach (string id in new[]
        {
            "MyObjectBuilder_ConveyorSorter/sdx_welderVanillaLg",
            "MyObjectBuilder_ConveyorSorter/sdx_grinderImprovisedLg",
            "MyObjectBuilder_CubeBlock/DeepWelderTypeIIToolcoreSmall"
        })
        {
            Check(DisplayFilter.Category(id) == "tools", "Modded tools take priority over generic carrier type");
            var sample = Block(0, "CubeBlock");
            sample.Definition = id;
            Check(
                ShipHealth.Build(new[] { sample }, new BlockSample[0]).Counts[7, 3] == 1,
                "Missing modded tool remains red in Tools"
            );
        }

        Check(
            DisplayFilter.Category("MyObjectBuilder_ConveyorSorter/Normal") == "conveyors"
                && DisplayFilter.Category("MyObjectBuilder_CargoContainer/WelderSupplies") == "cargo",
            "Utility name hints do not reclassify ordinary cargo/sorters"
        );
        var weapon = Block(0, "SmallGatlingGun");
        var weaponHealth = ShipHealth.Build(new[] { weapon }, new BlockSample[0]);
        Check(
            weaponHealth.Counts[6, 3] == 1 && weaponHealth.Total(4) == 0 && weaponHealth.Total(0) == 1,
            "Missing weapons are separate from Utility"
        );
        var shared = new SchematicControls
        {
            SchematicVisible = false
        };
        shared.HealthBars.Bars[0].Enabled = true;
        shared.HealthBars.Bars[0].X = .2;
        shared.Appearance.Size = .4;
        var imported = new SchematicControls();
        DisplaySettings.Import(DisplaySettings.Export(shared), imported);
        Check(
            !imported.SchematicVisible
                && imported.Visible
                && imported.HealthBars.Bars[0].Enabled
                && imported.HealthBars.Bars[0].X == .2
                && imported.Appearance.Size == .4,
            "Clipboard round trip preserves independent visibility and sizing"
        );
        bool invalidConfig = false;
        try
        {
            DisplaySettings.Import(DisplaySettings.Export(shared).Replace("Marigold display 7", "Unknown"), imported);
        }
        catch (InvalidDataException)
        {
            invalidConfig = true;
        }

        Check(
            invalidConfig && !imported.SchematicVisible && imported.HealthBars.Bars[0].X == .2,
            "Invalid clipboard config leaves settings intact"
        );
        var settings = new HealthBarSettings();
        foreach (var bar in settings.Bars)
            Check(!bar.Enabled, "Bars start disabled");
        settings.Bars[2].Enabled = true;
        settings.Bars[2].Set("x", "0");
        settings.Bars[2].Set("height", "0.08");
        var restored = HealthBarSettings.Parse(settings.Encode());
        Check(
            restored.Bars[2].Enabled
                && restored.Bars[2].X == 0
                && restored.Bars[2].Height == .08
                && !restored.Bars[1].Enabled,
            "Independent layouts round trip"
        );
        var currentLayouts = settings.Encode().Split(';');
        var legacySeven = new string[7];
        Array.Copy(currentLayouts, legacySeven, legacySeven.Length);
        legacySeven[6] = new HealthBarLayout
        {
            Enabled = true
        }.Encode();
        var migratedSeven = HealthBarSettings.Parse(string.Join(";", legacySeven));
        Check(
            migratedSeven.Bars[6].Enabled && !migratedSeven.Bars[7].Enabled,
            "Seven-bar settings preserve existing bars and add hidden Tools"
        );
        var legacySix = new List<string>(legacySeven);
        legacySix.RemoveAt(6);
        var migratedSix = HealthBarSettings.Parse(string.Join(";", legacySix));
        Check(
            migratedSix.Bars[2].Enabled && !migratedSix.Bars[6].Enabled && !migratedSix.Bars[7].Enabled,
            "Six-bar settings preserve layouts and add hidden groups"
        );
        foreach (string invalid in new[]
        {
            "NaN",
            "Infinity",
            "-1",
            "1.1"
        })
        {
            bool rejected = false;
            try
            {
                settings.Bars[2].Set("opacity", invalid);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check(rejected && settings.Bars[2].Opacity == 1, "Bad edits leave settings unchanged");
        }

        string dir = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "Bars-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            string workshop = Path.Combine(dir, "244850", "12345");
            Directory.CreateDirectory(workshop);
            string blueprint = Path.Combine(workshop, "bp.sbc");
            File.WriteAllText(
                blueprint,
                "<Definitions><ShipBlueprint><Id Subtype=\"Test Cruiser\"/><DisplayName>Author Name</DisplayName></ShipBlueprint></Definitions>"
            );
            Check(
                BlueprintCatalog.DisplayName(blueprint) == "Test Cruiser [Workshop 12345]",
                "Workshop label uses name and ID"
            );
            File.WriteAllText(
                blueprint,
                "<Definitions><ShipBlueprint><DisplayName>Author Name</DisplayName><CubeGrids><CubeGrid><CubeBlocks/><DisplayName>Actual Ship</DisplayName></CubeGrid></CubeGrids></ShipBlueprint></Definitions>"
            );
            Check(
                BlueprintCatalog.DisplayName(blueprint) == "Actual Ship [Workshop 12345]",
                "Grid name fallback ignores author"
            );
            Check(
                BlueprintCatalog.CloudName("cloud:Blueprints/cloud/My Cruiser/bp.sbc") == "My Cruiser",
                "Cloud label removes storage path"
            );
            var merged = BlueprintCatalog.Merge(
                new[]
                {
                    new BlueprintCatalog.Listing { Paths = new[] { "local", "workshop" }, Names = new[] { "Cruiser", "Miner" } },
                    new BlueprintCatalog.Listing { Paths = new[] { "cloud" }, Names = new[] { "CRUISER Two" } }
                },
                "cruiser"
            );
            Check(
                merged.Paths.Length == 2 && merged.Paths[0] == "local" && merged.Paths[1] == "cloud",
                "Combined sources search titles case insensitively"
            );
            bool emptySearchRejected = false;
            try
            {
                BlueprintCatalog.Merge(
                    new[] { new BlueprintCatalog.Listing { Paths = new[] { "local" }, Names = new[] { "Cruiser" } } },
                    "  "
                );
            }
            catch (ArgumentException)
            {
                emptySearchRejected = true;
            }

            Check(emptySearchRejected, "Empty blueprint searches are rejected");
            foreach (int resultCount in new[]
            {
                9,
                21,
                24
            })
            {
                int page = 0, pages = BlueprintCatalog.PageCount(resultCount);
                Check(
                    pages == (resultCount + BlueprintCatalog.ResultsPerPage - 1) / BlueprintCatalog.ResultsPerPage,
                    "Blueprint page count"
                );
                for (int expected = 1; expected < pages; expected++)
                {
                    page = BlueprintCatalog.ClampPage(page + 1, resultCount);
                    Check(page == expected, "Next advances one result page");
                }

                int pageStart = BlueprintCatalog.PageStart(page, resultCount);
                Check(
                    page == pages - 1
                        && pageStart < resultCount
                        && pageStart + BlueprintCatalog.ResultsPerPage >= resultCount,
                    "Next reaches the last result page"
                );
                for (int expected = pages - 2; expected >= 0; expected--)
                {
                    page = BlueprintCatalog.ClampPage(page - 1, resultCount);
                    Check(page == expected, "Previous returns one result page");
                }

                Check(
                    page == 0
                        && BlueprintCatalog.ClampPage(-1, resultCount) == 0
                        && BlueprintCatalog.ClampPage(pages, resultCount) == pages - 1,
                    "Previous and page bounds"
                );
            }

            File.WriteAllText(blueprint, "<broken>");
            Check(
                BlueprintCatalog.DisplayName(blueprint) == "12345 [Workshop 12345]",
                "Malformed Workshop XML uses folder fallback"
            );
            File.WriteAllText(blueprint, "<!DOCTYPE x [<!ENTITY unsafe SYSTEM 'file:///not-allowed'>]><x>&unsafe;</x>");
            Check(BlueprintCatalog.DisplayName(blueprint) == "12345 [Workshop 12345]", "Catalog refuses DTD entities");
            string path = Path.Combine(dir, "display.txt");
            var c = new SchematicControls
            {
                HealthBars = settings,
                Category = "tools"
            };
            DisplaySettings.Save(path, c);
            var loaded = new SchematicControls();
            DisplaySettings.Load(path, loaded);
            Check(
                loaded.HealthBars.Bars[2].Enabled && loaded.HealthBars.Bars[2].X == 0 && loaded.Category == "tools",
                "Bars share global/profile/preset persistence"
            );
            var legacy = new List<string>
            {
                "Marigold display 5",
                "1",
                "compare",
                "all",
                "all",
                "False",
                "0.85",
                "True",
                "False",
                "True",
                "en",
                new HudAppearance().Encode(),
                DisplayFilter.DefaultStyles
            };
            File.WriteAllLines(path, legacy);
            DisplaySettings.Load(path, loaded);
            foreach (var bar in loaded.HealthBars.Bars)
                Check(!bar.Enabled, "Old settings migrate with bars hidden");
            DisplaySettings.Save(path, c);
            var bad = File.ReadAllLines(path);
            bad[13] = "bad";
            File.WriteAllLines(path, bad);
            bool refused = false;
            try
            {
                DisplaySettings.Load(path, loaded);
            }
            catch (InvalidDataException)
            {
                refused = true;
            }

            Check(
                refused && loaded.Category == "all" && !loaded.HealthBars.Bars[2].Enabled,
                "Bad bar file does not partly apply preferences"
            );
        }
        finally
        {
            Directory.Delete(dir, true);
        }

        Console.WriteLine("PASS: " + count + " health bar classification/settings checks.");
    }
}

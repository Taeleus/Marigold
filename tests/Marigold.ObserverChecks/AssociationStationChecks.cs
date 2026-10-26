using System;
using System.Collections.Generic;
using System.IO;
using Marigold;

internal static class AssociationStationChecks
{
    private static int checks;

    private static void Check(bool ok, string reason)
    {
        if (!ok)
            throw new Exception(reason);
        checks++;
    }

    private static BlockSample[] Ship(int count)
    {
        var blocks = new BlockSample[count];
        for (int i = 0; i < count; i++)
            blocks[i] = new BlockSample
            {
                Position = new Point3(i, i % 3, i % 5),
                Bounds = new Box3(new Point3(i - .5, i % 3 - .5, i % 5 - .5), new Point3(i + .5, i % 3 + .5, i % 5 + .5)),
                Definition = "Block/" + i,
                Forward = 0,
                Up = 4,
                Integrity = 100,
                BuildIntegrity = 100,
                MaxIntegrity = 100
            };
        return blocks;
    }

    public static void Run()
    {
        var a = new AnchorFrame
        {
            Position = new Point3(0, 0, 0),
            Right = new Point3(1, 0, 0),
            Up = new Point3(0, 1, 0),
            Backward = new Point3(0, 0, 1),
            Definition = "Cockpit",
            Name = "Seat"
        };
        var original = Ship(100);
        var profile = new ShipProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Ship",
            Anchor = a,
            GridSize = 2.5,
            Blocks = original
        };
        var profiles = new List<ShipProfile>
        {
            profile
        };
        Check(AutoMatcher.Find(original, new[] { a }, profiles, 2.5).ProfileId == profile.Id, "Unique intact ship matches");
        var moved = a;
        moved.Position = new Point3(10, 20, 30);
        moved.Right = new Point3(0, 0, -1);
        moved.Backward = new Point3(1, 0, 0);
        var rotated = ProfileAlignment.Transform(original, moved, false);
        Check(
            AutoMatcher.Find(rotated, new[] { moved }, profiles, 2.5).ProfileId == profile.Id,
            "Rotated translated anchor matches without world IDs"
        );
        var damaged = (BlockSample[])original.Clone();
        damaged[3].Integrity = 2;
        damaged[3].IsFunctional = false;
        Check(
            AutoMatcher.Find(damaged, new[] { a }, profiles, 2.5).ProfileId == profile.Id,
            "Damage does not change layout identity"
        );
        var missing = new List<BlockSample>(original);
        missing.RemoveRange(0, 5);
        Check(
            AutoMatcher.Find(missing.ToArray(), new[] { a }, profiles, 2.5).ProfileId == profile.Id,
            "Small losses tolerated"
        );
        missing.RemoveRange(0, 20);
        Check(
            AutoMatcher.Find(missing.ToArray(), new[] { a }, profiles, 2.5).ProfileId == profile.Id,
            "75 percent overlap qualifies"
        );
        missing.RemoveAt(0);
        Check(
            AutoMatcher.Find(missing.ToArray(), new[] { a }, profiles, 2.5).ProfileId == null,
            "Below 75 percent remains manual"
        );
        Check(AutoMatcher.Find(original, new[] { a }, profiles, .5).ProfileId == null, "Grid sizes never mixed");
        var wrong = a;
        wrong.Definition = "DifferentCockpit";
        Check(AutoMatcher.Find(original, new[] { wrong }, profiles, 2.5).ProfileId == null, "Cockpit definition required");
        Check(AutoMatcher.Find(Ship(7), new[] { a }, profiles, 2.5).ProfileId == null, "Tiny partial observations rejected");
        Check(
            AutoMatcher.Find(original, new[] { a, a }, profiles, 2.5).ProfileId == null,
            "Ambiguous cockpit pairing rejected"
        );
        var twin = new ShipProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Other",
            Anchor = a,
            GridSize = 2.5,
            Blocks = original
        };
        profiles.Add(twin);
        Check(
            AutoMatcher.Find(original, new[] { a }, profiles, 2.5).ProfileId == null,
            "Identical saved profiles never auto-selected"
        );
        var near = (BlockSample[])original.Clone();
        near[3].Definition = "Changed";
        twin.Blocks = near;
        Check(AutoMatcher.Find(original, new[] { a }, profiles, 2.5).ProfileId == null, "Near tie requires manual choice");
        var eightyFour = new List<BlockSample>(original);
        eightyFour.RemoveRange(84, 16);
        Check(
            AutoMatcher.Find(eightyFour.ToArray(), new[] { a }, new List<ShipProfile> { profile }, 2.5).ProfileId == profile.Id,
            "84 percent occupied-seat overlap qualifies"
        );
        var reordered = (BlockSample[])original.Clone();
        Array.Reverse(reordered);
        Check(MatchStability.SameStructure(original, reordered), "Enumeration order cannot reset structural matching");
        Check(
            !MatchStability.SameStructure(original, eightyFour.ToArray()),
            "Actual structural changes still reset matching"
        );
        var stable = new MatchStability();
        Check(!stable.Observe(profile.Id, original), "First observation waits");
        Check(stable.Observe(profile.Id, original), "Repeated observation ready");
        Check(stable.Observe(profile.Id, damaged), "Health changes do not reset structural stability");
        stable.Reset();
        Check(!stable.Observe(profile.Id, damaged), "Reconnect resets stability");
        Check(!stable.Observe(null, damaged), "No candidate never accepted");
        var station = new StationGrid
        {
            Blocks = Ship(1),
            Origin = new Point3(10, 0, 0),
            X = new Point3(0, 0, -.2),
            Y = new Point3(0, .2, 0),
            Z = new Point3(.2, 0, 0)
        };
        var bounds = station.Bounds(station.Blocks[0].Bounds);
        Check(
            Math.Abs(bounds.Min.X - 9.9) < 1e-9 && Math.Abs(bounds.Max.Z - .1) < 1e-9,
            "Small-to-large grid rotated bounds"
        );
        var frame = new Box3(new Point3(-1, -1, -1), new Point3(11, 1, 1));
        var lines = StationGeometry.Build(station, "all", frame);
        Check(lines.Length == 24, "Connected cube has twelve outline edges");
        var center = StationGeometry.Center(frame);
        double radius = StationGeometry.Radius(frame);
        foreach (var p in lines)
        {
            double x = p.X * radius + center.X;
            Check(x >= 9.9 - 1e-8 && x <= 10.1 + 1e-8, "Connected outline translated correctly");
        }

        Check(StationGeometry.Build(station, "tools", frame).Length == 0, "Connected outline respects category filter");
        var controls = new SchematicControls();
        Check(!controls.Station && controls.AutoAssociate, "Opt-in station; auto matching default on");
        controls.Execute("/mg station on");
        Check(controls.ReferenceAction == "station on", "Station command routing");
        controls.Execute("/mg auto off");
        Check(controls.ReferenceAction == "auto off", "Auto command routing");
        string folder = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "AssociationTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "display.txt");
            File.WriteAllLines(path, new[]
            {
                "Marigold display 1",
                "1",
                "compare",
                "all",
                "all",
                "False",
                "0.85",
                "True"
            });
            DisplaySettings.Load(path, controls);
            Check(!controls.Station && controls.AutoAssociate, "Old preferences migrate with safe station default");
            controls.Station = true;
            controls.AutoAssociate = false;
            DisplaySettings.Save(path, controls);
            var loaded = new SchematicControls();
            DisplaySettings.Load(path, loaded);
            Check(!loaded.Station && !loaded.AutoAssociate, "Global settings do not enable station mode on every ship");
            var store = new ProfileStore(folder);
            store.Put(profile);
            var reloaded = new ProfileStore(folder);
            Check(
                AutoMatcher.Find(rotated, new[] { moved }, reloaded.Profiles, 2.5).ProfileId == profile.Id,
                "Disk reload can automatically match a transformed ship"
            );
            Check(reloaded.Find(profile.Id).Blocks[3].Integrity == 100, "Matching does not recapture baseline");
        }
        finally
        {
            Directory.Delete(folder, true);
        }

        Console.WriteLine("PASS: " + checks + " association/station checks.");
    }
}

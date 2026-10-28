using System;
using System.Collections.Generic;
using System.IO;
using Marigold;

internal static class CustomHealthBarChecks
{
    private static int checks;

    private static void Check(bool value, string reason)
    {
        if (!value)
            throw new Exception(reason);
        checks++;
    }

    private static void Reject(Action action, string reason)
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }
        Check(rejected, reason);
    }

    private static BlockSample Block(int x)
    {
        return new BlockSample
        {
            Position = new Point3(x, 0, 0),
            Bounds = new Box3(new Point3(x - .5, -.5, -.5), new Point3(x + .5, .5, .5)),
            Definition = "MyObjectBuilder_SmallGatlingGun/ModdedPdc",
            Forward = 0,
            Up = 4,
            Integrity = 100,
            BuildIntegrity = 100,
            MaxIntegrity = 100,
            HasFunctionalState = true,
            IsFunctional = true
        };
    }

    private static ShipProfile Profile(BlockSample[] blocks)
    {
        return new ShipProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Custom-bar test ship",
            GridSize = 2.5,
            Anchor = new AnchorFrame
            {
                Position = new Point3(0, 0, 0),
                Right = new Point3(1, 0, 0),
                Up = new Point3(0, 1, 0),
                Backward = new Point3(0, 0, 1),
                Definition = "MyObjectBuilder_Cockpit/Bridge",
                Name = "Bridge"
            },
            Blocks = blocks
        };
    }

    public static void Run()
    {
        var baseline = new BlockSample[6];
        for (int i = 0; i < baseline.Length; i++)
            baseline[i] = Block(i);
        var bar = CustomHealthBars.Create("Port PDCs", "Port PDC group", baseline);
        Check(bar.Layout.Enabled && bar.Layout.Y >= 0 && bar.Layout.Y <= 1, "A new custom bar is visible and on-screen");
        Check(bar.Members.Length == 6 && !ReferenceEquals(bar.Members, baseline), "Selection membership is copied");

        var live = new List<BlockSample>(baseline);
        var damaged = live[1];
        damaged.Integrity = 50;
        live[1] = damaged;
        var broken = live[2];
        broken.IsFunctional = false;
        live[2] = broken;
        live.RemoveAt(3);
        var replaced = live[3];
        replaced.Definition = "MyObjectBuilder_Decoy/Replacement";
        live[3] = replaced;
        var unknown = live[4];
        unknown.MaxIntegrity = double.NaN;
        live[4] = unknown;
        live.Add(Block(99));
        var counts = CustomHealthBars.Build(bar, live.ToArray());
        Check(
            counts[0] == 1 && counts[1] == 1 && counts[2] == 1 && counts[3] == 2 && counts[4] == 1,
            "Custom bars retain all five states, count replacements red and ignore added blocks"
        );
        IDictionary<Point3, BlockSample> lookup = new Dictionary<Point3, BlockSample>();
        foreach (var observed in live)
            lookup[observed.Position] = observed;
        var sharedCounts = CustomHealthBars.Build(bar, lookup);
        bool sameCounts = true;
        for (int state = 0; state < counts.Length; state++)
            sameCounts &= counts[state] == sharedCounts[state];
        Check(sameCounts, "Shared observation lookup produces the same custom-bar states as the array overload");
        Check(lookup.Count == live.Count, "Custom bar classification leaves the shared lookup intact");
        counts = CustomHealthBars.Build(bar, new BlockSample[0]);
        Check(counts[3] == 6, "Losing an entire group does not shrink the saved denominator");
        counts = CustomHealthBars.Build(bar, baseline);
        Check(counts[0] == 6 && counts[3] == 0, "Repairing expected members returns the full group to healthy");
        var orientation = (BlockSample[])baseline.Clone();
        orientation[0].Forward = 1;
        counts = CustomHealthBars.Build(bar, orientation);
        Check(counts[0] == 5 && counts[3] == 1, "Wrong block orientation remains a missing expected member");

        var moved = Profile(baseline).Anchor;
        moved.Position = new Point3(40, -12, 8);
        moved.Right = new Point3(0, 0, -1);
        moved.Backward = new Point3(1, 0, 0);
        var world = ProfileAlignment.Transform(baseline, moved, false);
        counts = CustomHealthBars.Build(bar, ProfileAlignment.Transform(world, moved, true));
        Check(counts[0] == 6, "Recovered alignment keeps custom membership in baseline coordinates");

        bar.Layout.X = .22;
        var refreshed = CustomHealthBars.Refresh(bar, new[] { baseline[1] });
        Check(
            refreshed.Id == bar.Id && refreshed.Name == bar.Name && refreshed.GroupName == bar.GroupName
                && refreshed.Layout.X == .22 && refreshed.Members.Length == 1 && bar.Members.Length == 6,
            "Explicit refresh preserves identity, label and layout while leaving the previous snapshot intact"
        );
        refreshed.Layout.X = .6;
        refreshed.Members[0] = baseline[3];
        Check(bar.Layout.X == .22 && bar.Members[0].Position.X == 0, "Refreshed layout and membership are independent copies");
        Reject(() => CustomHealthBars.Create("", "", baseline), "Unnamed custom bar rejected");
        Reject(() => CustomHealthBars.Create("Empty", "", new BlockSample[0]), "Empty custom selection rejected");
        Reject(() => CustomHealthBars.Create("Duplicate", "", new[] { baseline[0], baseline[0] }), "Duplicate selected member rejected");

        string directory = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "CustomBars-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var profile = Profile(baseline);
            profile.CustomBars.Add(bar);
            profile.CustomBars.Add(CustomHealthBars.Create("Coilgun – proa", "", new[] { baseline[5] }));
            profile.CustomBars[1].Layout.Opacity = .65;
            var store = new ProfileStore(directory);
            store.Put(profile);
            var saved = new ProfileStore(directory).Find(profile.Id);
            Check(saved.CustomBars.Count == 2, "Custom bars survive profile reload");
            Check(
                saved.CustomBars[0].Name == "Port PDCs" && saved.CustomBars[0].GroupName == "Port PDC group"
                    && saved.CustomBars[1].Name == "Coilgun – proa" && saved.CustomBars[1].GroupName == "",
                "Single-block labels, group metadata and Unicode names persist"
            );
            Check(
                saved.CustomBars[0].Members.Length == 6 && saved.CustomBars[1].Members[0].Position.X == 5
                    && saved.CustomBars[1].Layout.Opacity == .65,
                "Per-bar layout and expected baseline membership persist"
            );
            counts = CustomHealthBars.Build(saved.CustomBars[0], new[] { baseline[0] });
            Check(counts[0] == 1 && counts[3] == 5, "Reloading a damaged group preserves its missing members");

            var copy = saved.Copy();
            copy.CustomBars[0].Name = "Changed";
            copy.CustomBars[0].Layout.Width = .4;
            copy.CustomBars[0].Members[0] = baseline[1];
            Check(
                saved.CustomBars[0].Name == "Port PDCs" && saved.CustomBars[0].Layout.Width == .22
                    && saved.CustomBars[0].Members[0].Position.X == 0,
                "Profile copies deeply isolate bar metadata, layout and members"
            );

            var revisions = new ProfileStore(Path.Combine(directory, "versions"));
            var named = ReferenceHistory.SaveNamed(revisions, saved, "Before refit");
            saved.CustomBars[0].Name = "Starboard PDCs";
            saved.CustomBars[0].Layout.Width = .3;
            Check(named.CustomBars[0].Name == "Port PDCs" && named.CustomBars[0].Layout.Width == .22,
                "Named revisions retain independent custom bars");
            var restored = new ProfileStore(Path.Combine(directory, "versions")).Find(named.Id).Copy();
            restored.Id = profile.Id;
            store.Put(restored);
            Check(new ProfileStore(directory).Find(profile.Id).CustomBars[0].Name == "Port PDCs",
                "Restoring a revision restores its saved bar definitions");
            ReferenceHistory.Archive(revisions, saved);
            int archivedCount = revisions.Profiles.Count;
            var editedBar = saved.Copy();
            editedBar.CustomBars[0].Name = "New grouping";
            ReferenceHistory.Archive(revisions, editedBar);
            Check(revisions.Profiles.Count == archivedCount + 1, "Archive distinguishes changed custom-bar definitions");

            string before = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(directory, "profiles.bin")));
            var invalid = restored.Copy();
            invalid.CustomBars[0].Members[0] = Block(99);
            Reject(() => store.Put(invalid), "Off-baseline custom members cannot be persisted");
            Check(Convert.ToBase64String(File.ReadAllBytes(Path.Combine(directory, "profiles.bin"))) == before,
                "Rejected membership leaves the saved profile intact");
            invalid = restored.Copy();
            invalid.CustomBars[0].Layout.Opacity = double.NaN;
            Reject(() => store.Put(invalid), "Non-finite custom layout cannot be persisted");
            invalid = restored.Copy();
            invalid.CustomBars[0].Layout.PowerMask = 1;
            Reject(() => store.Put(invalid), "Power partitions do not apply to custom groups");
            invalid = restored.Copy();
            invalid.CustomBars[1].Id = invalid.CustomBars[0].Id;
            Reject(() => store.Put(invalid), "Duplicate custom bar IDs cannot be persisted");
            invalid = restored.Copy();
            invalid.CustomBars.Clear();
            for (int i = 0; i <= CustomHealthBars.MaximumBars; i++)
                invalid.CustomBars.Add(CustomHealthBars.Create("Bar " + i, "", new[] { baseline[0] }));
            Reject(() => store.Put(invalid), "Per-ship custom bar count is bounded");

            VerifyLegacy(directory, baseline);
            VerifyAlignment(directory, baseline);
            VerifyMalformed(directory, profile);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
        Console.WriteLine("PASS: " + checks + " custom health-bar persistence/membership checks.");
    }

    private static void VerifyAlignment(string directory, BlockSample[] source)
    {
        var baseline = new BlockSample[10];
        for (int i = 0; i < baseline.Length; i++)
        {
            baseline[i] = i < source.Length ? source[i] : Block(i);
            baseline[i].Definition = "MyObjectBuilder_SmallGatlingGun/Pdc" + i;
        }
        var profile = Profile(baseline);
        profile.Anchor.Position = new Point3(10, 2, 3);
        profile.Anchor.Right = new Point3(0, 0, -1);
        profile.Anchor.Backward = new Point3(1, 0, 0);
        profile.CustomBars.Add(CustomHealthBars.Create("Tracked trio", "PDC group", new[]
        {
            baseline[0],
            baseline[2],
            baseline[5]
        }));
        var store = new ProfileStore(Path.Combine(directory, "realigned"));
        store.Put(profile);
        var history = new ProfileStore(Path.Combine(directory, "realigned-versions"));
        var originalVersion = ReferenceHistory.SaveNamed(history, profile, "Original cockpit");

        var seat = profile.Anchor;
        seat.Position = new Point3(8, -5, -9);
        seat.Right = new Point3(-1, 0, 0);
        seat.Up = new Point3(0, 0, 1);
        seat.Backward = new Point3(0, 1, 0);
        seat.Definition = "MyObjectBuilder_Cockpit/ReplacementSeat";
        var grid = ProfileAlignment.Transform(baseline, profile.Anchor, false);
        var realigned = profile.Copy();
        realigned.Anchor = seat;
        realigned.Blocks = ProfileAlignment.Transform(grid, seat, true);
        foreach (var bar in realigned.CustomBars)
            bar.Members = ProfileAlignment.Transform(
                ProfileAlignment.Transform(bar.Members, profile.Anchor, false),
                seat,
                true
            );
        ProfileAlignment.Validate(realigned);
        Check(CustomHealthBars.Validate(realigned) == 3, "Reanchored custom members remain valid baseline identities");
        Check(!realigned.CustomBars[0].Members[0].Position.Equals(profile.CustomBars[0].Members[0].Position),
            "Changing the cockpit frame changes the saved member coordinates");
        store.Put(realigned);
        var loaded = new ProfileStore(Path.Combine(directory, "realigned")).Find(profile.Id);
        Check(CustomHealthBars.Build(loaded.CustomBars[0], loaded.Blocks)[0] == 3,
            "Reanchored membership persists and starts healthy");

        var observed = new List<BlockSample>(grid);
        var damaged = observed[0];
        damaged.Integrity = 35;
        observed[0] = damaged;
        var broken = observed[2];
        broken.IsFunctional = false;
        observed[2] = broken;
        observed.RemoveAt(5);
        var local = ProfileAlignment.Transform(observed.ToArray(), seat, true);
        var counts = CustomHealthBars.Build(loaded.CustomBars[0], local);
        Check(counts[1] == 1 && counts[2] == 1 && counts[3] == 1,
            "Damage, structural non-function and missing members remain correctly located after reanchoring");
        counts = CustomHealthBars.Build(loaded.CustomBars[0], ProfileAlignment.Transform(grid, seat, true));
        Check(counts[0] == 3 && counts[1] == 0 && counts[2] == 0 && counts[3] == 0,
            "Repairing all tracked members returns a reanchored custom bar to healthy");

        var restored = new ProfileStore(Path.Combine(directory, "realigned-versions")).Find(originalVersion.Id).Copy();
        restored.Id = profile.Id;
        store.Put(restored);
        loaded = new ProfileStore(Path.Combine(directory, "realigned")).Find(profile.Id);
        Check(ReferenceHistory.SameAnchor(loaded.Anchor, profile.Anchor)
            && CustomHealthBars.Same(loaded.CustomBars, profile.CustomBars),
            "Restoring an older cockpit revision restores its original membership frame");

        var signature = profile.Copy();
        signature.Anchor = ReferenceHistory.Relative(profile.Anchor, seat);
        signature.Blocks = ProfileAlignment.Transform(observed.ToArray(), seat, true);
        signature.CustomBars.Clear();
        for (int i = 0; i < signature.Blocks.Length; i++)
        {
            signature.Blocks[i].Integrity = signature.Blocks[i].BuildIntegrity = signature.Blocks[i].MaxIntegrity = 1;
            signature.Blocks[i].HasFunctionalState = false;
            signature.Blocks[i].IsFunctional = true;
        }
        var recoveries = new ProfileStore(Path.Combine(directory, "custom-recovery"));
        recoveries.Put(signature);
        var remembered = new ProfileStore(Path.Combine(directory, "custom-recovery")).Find(profile.Id);
        Check(remembered.Blocks.Length == baseline.Length - 1 && remembered.CustomBars.Count == 0,
            "A recovery signature with surviving blocks persists after clearing custom bars");
        Check(profile.CustomBars.Count == 1 && profile.CustomBars[0].Members.Length == 3,
            "Clearing recovery signature bars leaves the complete saved baseline membership intact");

        var nextSeat = seat;
        nextSeat.Position = new Point3(-30, 4, 10);
        // Signature health is intentionally discarded; drawing uses fresh live observations.
        var remnant = ProfileAlignment.Transform(local, nextSeat, false);
        var match = AutoMatcher.Find(remnant, new[] { nextSeat }, new[] { profile }, 2.5, new[] { remembered });
        Check(match.ProfileId == profile.Id && match.Recovered,
            "A saved remnant can reassociate to a profile that retains its custom bars");
        var expectedAnchor = ReferenceHistory.Compose(remembered.Anchor, nextSeat);
        Check(ReferenceHistory.SameAnchor(match.BindingAnchor, expectedAnchor),
            "Recovery restores the original baseline frame for custom-bar members");
        var alignedBar = profile.CustomBars[0].Copy();
        alignedBar.Members = ProfileAlignment.Transform(alignedBar.Members, match.BindingAnchor, false);
        counts = CustomHealthBars.Build(alignedBar, remnant);
        Check(counts[1] == 1 && counts[2] == 1 && counts[3] == 1,
            "Reassociated custom bars use live health and retain destroyed members from the original group");
        counts = CustomHealthBars.Build(alignedBar, ProfileAlignment.Transform(baseline, match.BindingAnchor, false));
        Check(counts[0] == 3 && counts[3] == 0,
            "Rebuilt members become healthy again after persistent recovery");
    }

    private static void VerifyLegacy(string directory, BlockSample[] baseline)
    {
        string legacyDirectory = Path.Combine(directory, "version3");
        var original = Profile(baseline);
        var store = new ProfileStore(legacyDirectory);
        store.Put(original);
        string path = Path.Combine(legacyDirectory, "profiles.bin");
        var bytes = File.ReadAllBytes(path);
        Array.Copy(BitConverter.GetBytes(3), 0, bytes, 4, 4);
        Array.Resize(ref bytes, bytes.Length - sizeof(int));
        File.WriteAllBytes(path, bytes);
        var legacy = new ProfileStore(legacyDirectory);
        Check(legacy.Warning == null && legacy.Find(original.Id).CustomBars.Count == 0,
            "Version 3 baselines load with no custom bars");
        legacy.Find(original.Id).CustomBars.Add(CustomHealthBars.Create("New bar", "", new[] { baseline[0] }));
        legacy.Put(legacy.Find(original.Id));
        Check(new ProfileStore(legacyDirectory).Find(original.Id).CustomBars.Count == 1,
            "An older profile can save custom bars in the current format");
    }

    private static void VerifyMalformed(string directory, ShipProfile profile)
    {
        string goodDirectory = Path.Combine(directory, "valid-members");
        var saved = profile.Copy();
        saved.CustomBars.Clear();
        saved.CustomBars.Add(CustomHealthBars.Create("Pair", "", new[] { profile.Blocks[0], profile.Blocks[1] }));
        var store = new ProfileStore(goodDirectory);
        store.Put(saved);
        byte[] bytes = File.ReadAllBytes(Path.Combine(goodDirectory, "profiles.bin"));

        string impossible = Path.Combine(directory, "impossible-member-count");
        Directory.CreateDirectory(impossible);
        var impossibleBytes = (byte[])bytes.Clone();
        Array.Copy(BitConverter.GetBytes(profile.Blocks.Length + 1), 0, impossibleBytes, impossibleBytes.Length - 12, 4);
        File.WriteAllBytes(Path.Combine(impossible, "profiles.bin"), impossibleBytes);
        string warning = new ProfileStore(impossible).Warning;
        Check(warning != null && warning.Contains("Invalid custom-bar member count."),
            "Impossible member counts are rejected before allocating or reading their index array");

        string outOfRange = Path.Combine(directory, "bad-member-index");
        Directory.CreateDirectory(outOfRange);
        Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, bytes, bytes.Length - 4, 4);
        File.WriteAllBytes(Path.Combine(outOfRange, "profiles.bin"), bytes);
        var corrupt = new ProfileStore(outOfRange);
        Check(corrupt.Warning != null && corrupt.Profiles.Count == 0, "Out-of-range persisted member index is rejected");
        Reject(() => corrupt.Put(profile), "Unreadable custom-bar data cannot be overwritten implicitly");

        string duplicate = Path.Combine(directory, "duplicate-member-index");
        Directory.CreateDirectory(duplicate);
        Array.Copy(BitConverter.GetBytes(0), 0, bytes, bytes.Length - 4, 4);
        File.WriteAllBytes(Path.Combine(duplicate, "profiles.bin"), bytes);
        Check(new ProfileStore(duplicate).Warning != null, "Duplicate persisted membership is rejected");
    }
}

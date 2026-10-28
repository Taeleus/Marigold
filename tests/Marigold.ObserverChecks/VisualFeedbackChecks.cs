using System;
using System.IO;
using Marigold;

internal static class VisualFeedbackChecks
{
    private static int checks;

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        checks++;
    }

    private static BlockSample Block(int x, string definition)
    {
        return new BlockSample
        {
            Position = new Point3(x, 0, 0),
            Bounds = new Box3(new Point3(x - .5, -.5, -.5), new Point3(x + .5, .5, .5)),
            Definition = definition,
            Forward = 0,
            Up = 4,
            Integrity = 100,
            BuildIntegrity = 100,
            MaxIntegrity = 100,
            IsFunctional = true
        };
    }

    public static void Run()
    {
        var appearance = new HudAppearance();
        Check(
            appearance.ContextOpacity == .15 && appearance.HealthBarColor(0) == 0x3296FF,
            "Sensible context and existing blue-bar defaults");
        appearance.Set("context", ".28");
        appearance.Set("healthy", "22AA66");
        appearance.Set("matchhealthy", "true");
        var settings = new SchematicControls { Appearance = appearance };
        var restored = new SchematicControls();
        DisplaySettings.Import(DisplaySettings.Export(settings), restored);
        Check(restored.Appearance.ContextOpacity == .28, "Context opacity survives display sharing");
        Check(
            restored.Appearance.HealthyBarColor == "22AA66" && restored.Appearance.MatchHealthyBarColor,
            "Healthy color and link survive display sharing");
        Check(
            restored.Appearance.HealthBarColor(0) == restored.Appearance.Color((int)ChangeKind.Hull),
            "Linked healthy bar follows healthy schematic color");
        restored.Appearance.Set("matchhealthy", "false");
        Check(restored.Appearance.HealthBarColor(0) == 0x22AA66, "Independent healthy bar color is retained while linked");
        for (int state = 1; state <= 4; state++)
        {
            var kind = state == 1 ? ChangeKind.Damaged
                : state == 2 ? ChangeKind.NonFunctional
                : state == 3 ? ChangeKind.Missing : ChangeKind.Unknown;
            Check(restored.Appearance.HealthBarColor(state) == restored.Appearance.Color((int)kind), "Fault bars still follow state colors");
        }

        foreach (int length in new[] { 8, 9, 10 })
        {
            var old = appearance.Encode().Split(';');
            Array.Resize(ref old, length);
            var compatible = HudAppearance.Parse(string.Join(";", old));
            Check(
                compatible.ContextOpacity == .15
                    && compatible.HealthyBarColor == "3296FF"
                    && !compatible.MatchHealthyBarColor,
                "Old appearances gain safe new defaults");
        }

        foreach (string invalid in new[] { "NaN", "Infinity", "-0.1", "1.1" })
        {
            string before = appearance.Encode();
            bool rejected = false;
            try
            {
                appearance.Set("context", invalid);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            Check(rejected && appearance.Encode() == before, "Invalid context edits are atomic");
        }

        foreach (string invalid in new[] { "blue", "1234567", "GG0000", "" })
        {
            string before = appearance.Encode();
            bool rejected = false;
            try
            {
                appearance.Set("healthy", invalid);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }
            Check(rejected && appearance.Encode() == before, "Invalid healthy bar edits are atomic");
        }

        uint orange = 0xFF8214, red = 0xFF3737, yellow = 0xDCCD41;
        Check(
            DamagePulse.Tint(orange, ChangeKind.NonFunctional, 1, true, 1) == orange,
            "Fresh pulse peaks at the original orange hue");
        Check(DamagePulse.Tint(red, ChangeKind.Missing, 1, true, 1) == red, "Fresh pulse peaks at the original red hue");
        uint freshLow = DamagePulse.Tint(red, ChangeKind.Missing, 0, true, 1);
        uint normalLow = DamagePulse.Tint(red, ChangeKind.Missing, 0, false, 1);
        Check((freshLow >> 16) < (normalLow >> 16), "Fresh alert has more color contrast than ongoing missing pulse");
        Check(DamagePulse.Tint(red, ChangeKind.Missing, 0, false, 0) == red, "Ongoing missing pulse switch stops color animation");
        Check(
            DamagePulse.Tint(orange, ChangeKind.NonFunctional, 0, false, 1) == orange,
            "Orange returns to its steady color after alert");
        Check(DamagePulse.Tint(yellow, ChangeKind.Damaged, 0, true, 1) == yellow, "Yellow stays steady even during fresh alerts");
        double now = 7.25;
        Check(DamagePulse.Strength(6, now) == DamagePulse.Strength(7, now), "Different fresh blocks share one brightness phase");
        Check(DamagePulse.Strength(3, now) == 0, "Fresh alert stops after four seconds");

        var reactor = Block(0, "MyObjectBuilder_Reactor/Large");
        var armor = Block(1, "MyObjectBuilder_CubeBlock/Armor");
        var missingArmor = Block(2, "MyObjectBuilder_CubeBlock/Armor");
        var reference = new[] { reactor, armor, missingArmor };
        var current = new[] { reactor, armor };
        string styles = DisplayFilter.DefaultStyles;
        var context = DisplayFilter.ContextBoxes(current, reference, "power", styles);
        Check(context.Count == 2, "Focused context includes surviving and missing other-system blocks once");
        Check(
            DisplayFilter.ContextBoxes(current, reference, "all", styles).Count == 0,
            "All-systems view adds no duplicate context");
        Check(
            DisplayFilter.ContextBoxes(current, reference, "none", styles).Count == 0,
            "No-systems view really hides every system");
        var hidden = styles.ToCharArray();
        hidden[Array.IndexOf(DisplayFilter.Categories.Split(' '), "armor")] = '0';
        Check(
            DisplayFilter.ContextBoxes(current, reference, "power", new string(hidden)).Count == 0,
            "Explicitly hidden systems stay hidden");
        Check(!DisplayFilter.IsContext(reactor.Definition, "power", styles), "Selected system never becomes dimmed context");
        var health = ShipHealth.Build(reference, current);
        Check(health.Total(0) == 3 && health.Counts[0, 3] == 1, "Context selection does not alter baseline condition counts");
        for (int mode = 0; mode < 4; mode++)
        {
            appearance.Rendering = mode;
            Check(appearance.DrawHull == (mode < 2), "Fault-only performance modes continue to omit context hulls");
        }
        OutlineAlerts();
        Console.WriteLine("PASS: " + checks + " visual feedback/context/color checks.");
    }

    private static void OutlineAlerts()
    {
        var near = Block(0, "MyObjectBuilder_Reactor/Test");
        var far = Block(40, near.Definition);
        near.HasFunctionalState = far.HasFunctionalState = true;
        var reference = new[] { near, far };
        var oldFault = far;
        oldFault.IsFunctional = false;
        var pulse = new DamagePulse();
        pulse.Observe(reference, new[] { near, oldFault }, 0);
        var fresh = near;
        fresh.IsFunctional = false;
        pulse.Observe(reference, new[] { fresh, oldFault }, 1);
        var changes = SnapshotComparison.Compare(reference, new[] { fresh, oldFault }).Changes;
        var frame = SchematicGeometry.Union(near.Bounds, far.Bounds);
        var outline = OutlineGeometry.Faults(changes, ChangeKind.NonFunctional, frame);
        int activeEdges = 0, oldEdges = 0;
        for (int edge = 0; edge < outline.Points.Length; edge += 2)
        {
            var region = DamagePulse.OutlineRegion(
                ChangeKind.NonFunctional,
                outline.Points[edge],
                outline.Points[edge + 1],
                frame,
                outline.CellSize
            );
            double start = pulse.StartOutline(region);
            if (region.Bounds.Max.X < 20)
            {
                Check(start == 1, "Fresh orange edges receive spatially matched outline alerts");
                activeEdges++;
            }
            else
            {
                Check(double.IsNegativeInfinity(start), "Distant existing faults do not pulse with a new hit");
                oldEdges++;
            }
        }
        Check(activeEdges > 0 && oldEdges > 0, "Merged outline mapping checks both fresh and existing fault geometry");
        pulse.Observe(reference, new[] { oldFault }, 1.5);
        Check(pulse.StartOutline(new BlockChange { Kind = ChangeKind.Missing, Bounds = near.Bounds }) == 1,
            "Orange-to-missing changes preserve the original fresh alert window");
        Check(double.IsNegativeInfinity(pulse.StartOutline(new BlockChange
        {
            Kind = ChangeKind.NonFunctional,
            Bounds = near.Bounds
        })), "Fresh outline lookup does not mix orange and red states");
        Check(double.IsNegativeInfinity(pulse.StartOutline(new BlockChange
        {
            Kind = ChangeKind.Damaged,
            Bounds = near.Bounds
        })), "Yellow outlines never receive fresh alerts");

        // A coarse cell may extend past the original block envelope. Its mapped edge
        // still represents that fault and must be able to breathe with it.
        var coarseFrame = new Box3(new Point3(-4, -4, -4), new Point3(4, 4, 4));
        double radius = StationGeometry.Radius(coarseFrame);
        var coarse = DamagePulse.OutlineRegion(
            ChangeKind.Missing,
            new Point3(2 / radius, -2 / radius, -2 / radius),
            new Point3(2 / radius, 2 / radius, -2 / radius),
            coarseFrame,
            4
        );
        Check(pulse.StartOutline(coarse) == 1, "Coarse cell alert envelopes retain fresh fault coverage");

        var many = new BlockSample[600];
        var hit = new BlockSample[many.Length];
        for (int i = 0; i < many.Length; i++)
        {
            many[i] = Block(i * 3, near.Definition);
            many[i].HasFunctionalState = true;
            hit[i] = many[i];
            hit[i].IsFunctional = i % 3 != 0;
        }
        pulse.Observe(many, many, 0);
        pulse.Observe(many, hit, 2);
        for (int i = 0; i < many.Length; i++)
        {
            double start = pulse.StartOutline(new BlockChange
            {
                Kind = ChangeKind.NonFunctional,
                Bounds = many[i].Bounds
            });
            Check(i % 3 == 0 ? start == 2 : double.IsNegativeInfinity(start),
                "Indexed fresh-outline lookup matches exact block membership");
        }
        pulse.Observe(many, hit, 6);
        Check(double.IsNegativeInfinity(pulse.StartOutline(new BlockChange
        {
            Kind = ChangeKind.NonFunctional,
            Bounds = many[0].Bounds
        })), "Outline eligibility expires with the same four-second rule");
    }
}

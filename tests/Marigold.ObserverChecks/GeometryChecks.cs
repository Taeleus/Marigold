using System;
using System.Collections.Generic;
using System.Diagnostics;
using Marigold;

internal static class GeometryChecks
{
    private static int checks;

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        checks++;
    }

    private static Box3 Block(int x, int y, int z)
    {
        return new Box3(new Point3(x - .5, y - .5, z - .5), new Point3(x + .5, y + .5, z + .5));
    }

    private static double Radius(Point3 p)
    {
        return Math.Sqrt(p.X * p.X + p.Y * p.Y + p.Z * p.Z);
    }

    private static void Validate(OutlineModel model, int detail)
    {
        Check(model.Points.Length % 2 == 0 && model.Points.Length / 2 <= OutlineGeometry.LineLimit(detail), "Line budget");
        var unique = new HashSet<string>();
        for (int i = 0; i < model.Points.Length; i += 2)
        {
            var a = model.Points[i];
            var b = model.Points[i + 1];
            Check(Radius(a) <= 1.000001 && Radius(b) <= 1.000001, "Normalized fit");
            Check(!a.Equals(b), "No zero length lines");
            Check(unique.Add(a.X + "," + a.Y + "," + a.Z + ":" + b.X + "," + b.Y + "," + b.Z), "No duplicate lines");
        }
    }

    public static void Run()
    {
        // As destruction grows beyond a whole detail page, both surviving fault layers remain.
        var faults = new List<BlockChange>();
        var combatFrame = new Box3(new Point3(-.5, -.5, -.5), new Point3(1024.5, 2.5, .5));
        faults.Add(new BlockChange { Kind = ChangeKind.Damaged, Bounds = Block(0, 1, 0) });
        faults.Add(new BlockChange { Kind = ChangeKind.NonFunctional, Bounds = Block(0, 2, 0) });
        for (int wave = 0; wave < 8; wave++)
        {
            for (int i = 0; i < 128; i++)
                faults.Add(new BlockChange { Kind = ChangeKind.Missing, Bounds = Block(wave * 128 + i, 0, 0) });
            Check(
                OutlineGeometry.Faults(faults, ChangeKind.Damaged, combatFrame).Points.Length == 24,
                "Yellow survives growing missing counts beyond page limit"
            );
            Check(
                OutlineGeometry.Faults(faults, ChangeKind.NonFunctional, combatFrame).Points.Length == 24,
                "Orange survives growing missing counts beyond page limit"
            );
        }

        faults.RemoveAll(c => c.Kind == ChangeKind.Damaged);
        Check(
            OutlineGeometry.Faults(faults, ChangeKind.Damaged, combatFrame).Points.Length == 0,
            "Repaired fault layer clears rather than retaining stale damage"
        );
        Check(OutlineGeometry.Build(new Box3[0], 1).Points.Length == 0, "Empty model");
        var cube = OutlineGeometry.Build(new[] { Block(0, 0, 0) }, 1);
        Check(cube.Points.Length == 24, "Cube has twelve edges");
        var joined = OutlineGeometry.Build(new[] { Block(0, 0, 0), Block(1, 0, 0) }, 1);
        Check(joined.Points.Length == 24, "Adjacent blocks merge without surface seams");
        var separate = OutlineGeometry.Build(new[] { Block(0, 0, 0), Block(4, 0, 0) }, 1);
        Check(separate.Points.Length == 48 && separate.CellSize == 1, "Separated blocks stay separated");
        var elbow = OutlineGeometry.Build(new[]
        {
            Block(0, 0, 0),
            Block(1, 0, 0),
            Block(0, 1, 0)
        }, 1);
        Check(elbow.Points.Length == 36, "L shape preserves concave corner");
        var ring = new List<Box3>();
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                if (x != 1 || y != 1)
                    ring.Add(Block(x, y, 0));
        var hole = OutlineGeometry.Build(ring, 1);
        Check(hole.Points.Length == 48 && hole.CellSize == 1, "Ring preserves opening");
        ring.Reverse();
        var reversed = OutlineGeometry.Build(ring, 1);
        Check(reversed.Points.Length == hole.Points.Length, "Order independent count");
        for (int i = 0; i < hole.Points.Length; i++)
            Check(hole.Points[i].Equals(reversed.Points[i]), "Deterministic order");
        var envelope = OutlineGeometry.Build(new[] { new Box3(new Point3(-.5, -.5, -.5), new Point3(9.5, 1.5, .5)) }, 1);
        Check(envelope.Cells == 20 && envelope.Points.Length == 24, "Multi-cell block");
        foreach (var model in new[]
        {
            cube,
            joined,
            separate,
            elbow,
            hole,
            envelope
        })
            Validate(model, 1);
        var rotated = SchematicGeometry.Rotate(new Point3(1, 0, 0), Math.PI / 2, 0);
        Check(Math.Abs(rotated.X) < 1e-9 && Math.Abs(rotated.Z + 1) < 1e-9, "Quarter turn");
        double x0 = -2, y0 = 0, x1 = 2, y1 = 0;
        Check(SchematicGeometry.Clip(ref x0, ref y0, ref x1, ref y1) && x0 == -1 && x1 == 1, "Crossing clipped");
        x0 = -2;
        y0 = 2;
        x1 = 2;
        y1 = 2;
        Check(!SchematicGeometry.Clip(ref x0, ref y0, ref x1, ref y1), "Outside clipped");
        var scattered = new List<Box3>();
        for (int x = 0; x < 12; x++)
            for (int y = 0; y < 12; y++)
                scattered.Add(Block(x * 3, y * 3, 0));
        var low = OutlineGeometry.Build(scattered, 0);
        var high = OutlineGeometry.Build(scattered, 2);
        Check(low.CellSize > high.CellSize && high.CellSize == 1, "Detail adapts to line budget");
        Validate(low, 0);
        Validate(high, 2);
        var huge = OutlineGeometry.Build(new[] { new Box3(new Point3(-1000000, -1000000, -1000000), new Point3(1000000, 1000000, 1000000)) }, 0);
        Check(huge.Cells <= 100000 && huge.Points.Length == 24 && huge.CellSize > 1, "Huge envelope bounded");
        foreach (int count in new[]
        {
            1000,
            10000,
            50000
        })
        {
            var blocks = new List<Box3>();
            for (int i = 0; i < count; i++)
                blocks.Add(Block(i % 50 - 25, (i / 50) % 20 - 10, i / 1000 - 25));
            var watch = Stopwatch.StartNew();
            var result = OutlineGeometry.Build(blocks, 1);
            watch.Stop();
            Check(result.Points.Length == 24 && result.CellSize == 1, "Solid hull stays exact at scale");
            Validate(result, 1);
            Console.WriteLine("Outline benchmark: " + count + " blocks -> " + result.Points.Length / 2 + " lines, "
                + watch.Elapsed.TotalMilliseconds.ToString("F2")
                + " ms (synthetic worker CPU, not game frame time).");
        }

        var controls = new SchematicControls();
        bool send = true;
        string response;
        controls.TryHandle("/mg detail high", ref send, out response);
        Check(!send && controls.Detail == 2, "Detail commands local");
        controls.Execute("/mg detail invalid");
        Check(controls.Detail == 2, "Invalid detail unchanged");
        controls.Execute("/mg detail low");
        Check(controls.Detail == 0, "Low detail");
        controls.Execute("/mg reset");
        Check(controls.Detail == 0, "Reset preserves detail");
        Console.WriteLine("PASS: " + checks + " outline/geometry checks.");
    }
}

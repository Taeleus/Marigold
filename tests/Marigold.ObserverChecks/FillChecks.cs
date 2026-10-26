using System;
using System.Collections.Generic;
using Marigold;

internal static class FillChecks
{
    public static void Run()
    {
        var corners = new Point3[8];
        for (int i = 0; i < 8; i++)
            corners[i] = new Point3((i & 1) == 0 ? -.5 : .5, (i & 2) == 0 ? -.5 : .5, (i & 4) == 0 ? -.5 : .5);
        var edges = new List<Point3>();
        for (int i = 0; i < 8; i++)
            for (int axis = 1; axis <= 4; axis *= 2)
                if ((i & axis) == 0)
                {
                    edges.Add(corners[i]);
                    edges.Add(corners[i | axis]);
                }

        var bands = new List<FillBand>();
        MarkerFill.Build(edges.ToArray(), new[] { 0 }, 1, 400, bands);
        double area = 0;
        foreach (var band in bands)
            area += band.Width * band.Height;
        if (Math.Abs(area - 1) > 1e-9)
            throw new Exception("Face-on fill must cover projected square exactly");
        var rotated = edges.ToArray();
        for (int i = 0; i < rotated.Length; i++)
            rotated[i] = SchematicGeometry.Rotate(rotated[i], .6, -.4);
        MarkerFill.Build(rotated, new[] { 0 }, 3, 400, bands);
        foreach (var band in bands)
            if (band.X < -1.000001
                || band.Y < -1.000001
                || band.X + band.Width > 1.000001
                || band.Y + band.Height > 1.000001)
                throw new Exception("Fill escapes panel");
        var many = new List<Point3>();
        var starts = new int[3072];
        for (int i = 0; i < starts.Length; i++)
        {
            starts[i] = many.Count;
            many.AddRange(rotated);
        }

        MarkerFill.Build(many.ToArray(), starts, 3, 2160, bands);
        if (bands.Count == 0 || bands.Count > MarkerFill.Budget)
            throw new Exception("Fill budget violated");
        var represented = new HashSet<int>();
        foreach (var band in bands)
            represented.Add(band.MarkerStart);
        if (represented.Count != starts.Length)
            throw new Exception("Fill budget starved later fault regions");
        var changes = new List<BlockChange>();
        for (int i = 0; i < 3000; i++)
            changes.Add(new BlockChange { Kind = ChangeKind.Damaged, Bounds = new Box3(new Point3(i, 0, 0), new Point3(i + 1, 1, 1)) });
        var regions = MarkerFill.Regions(changes, ChangeKind.Damaged);
        if (regions.Count > MarkerFill.RegionsPerState)
            throw new Exception("Region budget exceeded");
        foreach (var c in changes)
        {
            bool covered = false;
            foreach (var r in regions)
                if (r.Bounds.Min.X <= c.Bounds.Min.X
                    && r.Bounds.Max.X >= c.Bounds.Max.X
                    && r.Bounds.Min.Y <= c.Bounds.Min.Y
                    && r.Bounds.Max.Y >= c.Bounds.Max.Y
                    && r.Bounds.Min.Z <= c.Bounds.Min.Z
                    && r.Bounds.Max.Z >= c.Bounds.Max.Z)
                {
                    covered = true;
                    break;
                }

            if (!covered)
                throw new Exception("Aggregation omitted a damaged block");
        }

        if (MarkerFill.Regions(changes, ChangeKind.Missing).Count != 0)
            throw new Exception("States mixed during aggregation");
        MarkerFill.Build(rotated, new int[0], 1, 400, bands);
        if (bands.Count != 0)
            throw new Exception("Old fill was retained");
        for (int i = 0; i < rotated.Length; i++)
            rotated[i].Y += 20;
        MarkerFill.Build(rotated, new[] { 0 }, 1, 400, bands);
        if (bands.Count != 0)
            throw new Exception("Offscreen marker shaded");
        var raster = new FillRaster();
        var source = new List<FillBand>();
        var palette = new ChangeKind[3072];
        var pulseStarts = new double[3072];
        for (int i = 0; i < 3072; i++)
        {
            palette[i] = i < 1024 ? ChangeKind.Damaged : i < 2048 ? ChangeKind.NonFunctional : ChangeKind.Missing;
            pulseStarts[i] = double.NegativeInfinity;
            source.Add(new FillBand
            {
                MarkerStart = i * 2,
                X = -.5,
                Y = -.5,
                Width = 1,
                Height = 1
            });
        }

        raster.Merge(source, palette, pulseStarts, 10, true);
        if (source.Count != 3)
            throw new Exception("3072 coincident fills should become three independent state rectangles");
        var stateKinds = new HashSet<ChangeKind>();
        foreach (var b in source)
        {
            stateKinds.Add(palette[b.MarkerStart / 2]);
            if (b.X > -.5 || b.Y > -.5 || b.X + b.Width < .5 || b.Y + b.Height < .5)
                throw new Exception("Union lost coverage");
        }

        if (stateKinds.Count != 3)
            throw new Exception("Fill union hid a damage state");
        var random = new Random(51);
        source.Clear();
        for (int i = 0; i < 3072; i++)
        {
            double x = random.NextDouble() * 1.8 - .9, y = random.NextDouble() * 1.8 - .9;
            pulseStarts[i] = i % 2 == 0 ? 9 : double.NegativeInfinity;
            source.Add(new FillBand
            {
                MarkerStart = i * 2,
                X = x,
                Y = y,
                Width = .005,
                Height = .005
            });
        }

        var originalBands = source.ToArray();
        raster.Merge(source, palette, pulseStarts, 10, true);
        if (source.Count > MarkerFill.Budget)
            throw new Exception("Union exceeded submission budget");
        foreach (var input in originalBands)
        {
            // Sample all corners and center: every occupied input cell must retain its state.
            foreach (double dx in new[]
            {
                0.00001,
                .0025,
                .00499
            })
                foreach (double dy in new[]
                {
                    0.00001,
                    .0025,
                    .00499
                })
                {
                    bool found = false;
                    foreach (var output in source)
                        if (palette[output.MarkerStart / 2] == palette[input.MarkerStart / 2]
                            && input.X + dx >= output.X - 1e-10
                            && input.X + dx <= output.X + output.Width + 1e-10
                            && input.Y + dy >= output.Y - 1e-10
                            && input.Y + dy <= output.Y + output.Height + 1e-10)
                        {
                            found = true;
                            break;
                        }

                    if (!found)
                        throw new Exception("Sparse raster omitted damage coverage");
                }
        }

        source.Clear();
        source.Add(new FillBand
        {
            MarkerStart = 2048,
            X = -.8,
            Y = -.5,
            Width = .2,
            Height = .2
        });
        source.Add(new FillBand
        {
            MarkerStart = 2050,
            X = .6,
            Y = .5,
            Width = .2,
            Height = .2
        });
        pulseStarts[1024] = 9;
        pulseStarts[1025] = double.NegativeInfinity;
        raster.Merge(source, palette, pulseStarts, 10, true);
        if (source.Count != 2)
            throw new Exception("Separate pulsing and steady faults should remain separate");
        bool active = false, steady = false;
        foreach (var b in source)
        {
            if (pulseStarts[b.MarkerStart / 2] == 9)
                active = true;
            else
                steady = true;
        }

        if (!active || !steady)
            throw new Exception("Union lost pulse eligibility");
        Console.WriteLine("PASS: marker fill checks including 3072-region fairness and 3000-block aggregation coverage.");
    }
}

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

        CheckPreciseRaster(edges.ToArray());
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

    private static void CheckPreciseRaster(Point3[] edges)
    {
        const int size = 32;
        var raster = new SchematicRaster(size);
        var palette = new uint[] { 0xFFFF0040, 0xFFFF0080, 0xFF800040, 0xFF800080, 0xFF000040, 0xFF000080 };
        int center = (size * (size / 2) + size / 2) * 4;
        CheckTexturedCoverage(raster, size, edges, palette);
        CheckTexturedDepth(raster, size, edges, palette);
        raster.Begin();
        raster.Fill(edges, 0, 1, 0);
        raster.Fill(edges, 0, 1, 0);
        raster.ResolveFills(palette);
        raster.Finish();
        if (raster.Pixels[center] != 64 || raster.Pixels[center + 3] != 64)
            throw new Exception("Overlapping fills of the same state must shade once in premultiplied color");
        if (raster.Pixels[3] != 0)
            throw new Exception("Fill must leave the area outside the marker transparent");

        raster.Begin();
        raster.Fill(edges, 0, 1, 0);
        raster.Fill(edges, 0, 1, 1);
        raster.ResolveFills(palette);
        raster.Finish();
        if (raster.Pixels[center + 3] != 128)
            throw new Exception("Fresh fill must take precedence over its steady state at the same point");

        var rotated = Array.ConvertAll(edges, p => SchematicGeometry.Rotate(p, .52, -.37));
        raster.Begin();
        raster.Fill(rotated, 0, 1, 4);
        raster.ResolveFills(palette);
        raster.Finish();
        bool hasPartialFill = false;
        for (int i = 3; i < raster.Pixels.Length; i += 4)
            hasPartialFill |= raster.Pixels[i] > 0 && raster.Pixels[i] < 64;
        if (!hasPartialFill || raster.Pixels[center + 3] != 64)
            throw new Exception("Rotated fills need fractional edges and solid coverage inside");

        var tiny = Array.ConvertAll(edges, p => new Point3(p.X * .001, p.Y * .001, p.Z * .001));
        raster.Begin();
        raster.Fill(tiny, 0, 1, 4);
        raster.ResolveFills(palette);
        raster.Finish();
        bool hasTinyFill = false;
        for (int i = 3; i < raster.Pixels.Length; i += 4)
            hasTinyFill |= raster.Pixels[i] > 0;
        if (!hasTinyFill)
            throw new Exception("A visible marker smaller than one raster sample must retain some fill coverage");

        raster.Begin();
        raster.Stroke(-.8, -.32, .01, .02, 1, 0x00FFFFFF);
        raster.Stroke(.01, .02, .65, -.41, 1, 0x00FFFFFF);
        raster.Finish();
        bool hasPartialLine = false;
        for (int i = 3; i < raster.Pixels.Length; i += 4)
            hasPartialLine |= raster.Pixels[i] > 0 && raster.Pixels[i] < 255;
        if (!hasPartialLine || raster.Pixels[center + 3] == 0)
            throw new Exception("Sloped strokes need fractional coverage and a connected shared corner");
    }

    private static void CheckTexturedDepth(SchematicRaster raster, int size, Point3[] edges, uint[] palette)
    {
        const float gap = float.NegativeInfinity;
        var mixedDepth = new[] { 1f, 0f, gap, 1f, 0f, gap, 1f, 0f, gap };
        int row = size * (size / 2);
        int left = (row + 9) * 4 + 3;
        int center = (row + size / 2) * 4 + 3;
        int right = (row + 22) * 4 + 3;
        for (int layer = 0; layer < palette.Length; layer++)
        {
            raster.Begin(mixedDepth, 3);
            raster.Fill(edges, 0, 1, layer);
            raster.ResolveFills(palette);
            raster.Finish();
            int fullAlpha = (byte)palette[layer];
            int coveredAlpha = raster.Pixels[left];
            if (coveredAlpha < fullAlpha / 4 || coveredAlpha >= fullAlpha
                || raster.Pixels[left - 3] != coveredAlpha
                || raster.Pixels[center] != fullAlpha || raster.Pixels[right] != fullAlpha)
                throw new Exception("A covered fill must stay visible and dimmer than exposed portions or gaps");
        }

        // The projected front of a rotated box is nearer in the middle than on either side.
        var rotated = Array.ConvertAll(edges, p => SchematicGeometry.Rotate(p, Math.PI / 4, 0));
        raster.Begin(new[] { .55f }, 1);
        raster.Fill(rotated, 0, 1, 0);
        raster.ResolveFills(palette);
        raster.Finish();
        if (raster.Pixels[center] != 64 || raster.Pixels[left] == 0 || raster.Pixels[left] >= 64
            || raster.Pixels[right] == 0 || raster.Pixels[right] >= 64)
            throw new Exception("A rotated fill must compare the front face depth at each sample");

        var coveringDepth = new[] { 1f };
        raster.Begin(coveringDepth, 1);
        raster.Fill(edges, 0, 1, 0);
        raster.ResolveFills(palette);
        raster.Finish();
        int hiddenAlpha = raster.Pixels[center];
        if (hiddenAlpha < 16 || hiddenAlpha >= 64)
            throw new Exception("A fully covered marker must keep its colored fill");

        var closer = Array.ConvertAll(edges, p => new Point3(p.X, p.Y, p.Z + 1));
        raster.Begin(coveringDepth, 1);
        raster.Fill(closer, 0, 1, 0);
        raster.Fill(edges, 0, 1, 0);
        raster.ResolveFills(palette);
        raster.Finish();
        if (raster.Pixels[center] != 64)
            throw new Exception("An overlapping covered fill must not darken or stack with the exposed fill of the same state");

        raster.Begin(new[] { .55f }, 1, .1);
        raster.Fill(edges, 0, 1, 0);
        raster.ResolveFills(palette);
        raster.Finish();
        if (raster.Pixels[center] != 64)
            throw new Exception("A fill within the surface depth tolerance must keep its normal intensity");

        double lineY = 1.0 / size;
        raster.Begin(new[] { .5f }, 1);
        raster.Stroke(-.45, lineY, .45, lineY, 1, 0xFF0000FF, -.5, 1.5);
        raster.Finish();
        if (raster.Pixels[left] == 0 || raster.Pixels[left] >= 255 || raster.Pixels[right] != 255)
            throw new Exception("A stroke must keep covered samples visible and interpolate depth along its length");

        raster.Begin();
        raster.Fill(edges, 0, 1, 0);
        raster.ResolveFills(palette);
        raster.Stroke(-.45, lineY, .45, lineY, 1, 0xFF0000FF, -.5, -.5);
        raster.Finish();
        int offLine = center + 3 * size * 4;
        if (raster.Pixels[left] != 255 || raster.Pixels[right] != 255 || raster.Pixels[offLine] != 64)
            throw new Exception("Beginning a wireframe must clear the previous textured depth");
    }

    private static void CheckTexturedCoverage(SchematicRaster raster, int size, Point3[] edges, uint[] palette)
    {
        // A 3x3 model with only its center pixel covered exercises unequal resolutions and partial edges.
        var surface = new byte[3 * 3 * 4];
        surface[4 * 4 + 3] = 255;
        int row = size * (size / 2);
        int center = (row + size / 2) * 4 + 3;
        int outside = (row + 9) * 4 + 3;
        int edge = (row + 10) * 4 + 3;
        for (int layer = 0; layer < 4; layer++)
        {
            raster.Begin();
            raster.Fill(edges, 0, 1, layer);
            raster.ResolveFills(palette, surface, 3);
            raster.Finish();
            int alpha = (byte)palette[layer];
            if (raster.Pixels[center] != alpha || raster.Pixels[outside] != 0 || raster.Pixels[edge] != alpha / 2)
                throw new Exception("Live fault fills must stay within the visible model coverage");
        }

        raster.Begin();
        raster.Fill(edges, 0, 1, 4);
        raster.ResolveFills(palette, surface, 3);
        raster.Finish();
        if (raster.Pixels[outside] != 64)
            throw new Exception("Missing fills must remain visible beyond the hull");

        raster.Begin();
        raster.ResolveFills(palette, surface, 3);
        raster.Stroke(-.5, 0, -.25, 0, 1, 0x00FFFFFF);
        raster.Finish();
        if (raster.Pixels[outside] == 0)
            throw new Exception("Hull coverage must leave diagnostic outlines visible");

        var replacementSurface = new byte[surface.Length];
        replacementSurface[3 * 4 + 3] = 255;
        raster.Begin();
        raster.Fill(edges, 0, 1, 0);
        raster.ResolveFills(palette, replacementSurface, 3);
        raster.Finish();
        if (raster.Pixels[center] != 0 || raster.Pixels[outside] != 64)
            throw new Exception("A new textured image must update the coverage mask");

        raster.Begin();
        raster.Fill(edges, 0, 1, 0);
        raster.ResolveFills(palette);
        raster.Finish();
        if (raster.Pixels[center] != 64 || raster.Pixels[outside] != 64)
            throw new Exception("Wireframe fills must ignore previously applied textured coverage");
    }
}

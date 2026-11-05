using System;

namespace Marigold
{
    // Build a single premultiplied RGBA image in panel coordinates. Two samples per
    // axis keep outline joints and fill silhouettes on the same pixel grid.
    internal sealed class SchematicRaster
    {
        private const int FillLayers = 6;
        private const int MissingFillLayer = 4;
        private const uint CoveredFillAlpha = 128;
        private const uint CoveredOutlineAlpha = 102;
        private readonly int size;
        private readonly int sampleSide;
        private readonly int wordsPerRow;
        private readonly int wordsPerLayer;
        private readonly ulong[] fillMasks;
        private readonly uint[] samples;
        private readonly FillEdge[] fillEdges = new FillEdge[12];
        private ulong[] surfaceCoverage;
        private int[] surfaceCoordinates;
        private byte[] maskedSurface;
        private int maskedSurfaceSize;
        private ulong[] coveredFillMasks;
        private bool hasCoveredFills;
        private float[] surfaceDepth;
        private int depthSize;
        private int[] depthCoordinates;
        private double depthTolerance;
        private readonly DepthPlane[] frontPlanes = new DepthPlane[3];

        private struct FillEdge
        {
            public double X, Top, Bottom, Slope;
        }

        private struct DepthPlane
        {
            public double Origin, StepX, StepY;
        }

        public byte[] Pixels { get; }

        public SchematicRaster(int size)
        {
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));
            this.size = size;
            sampleSide = size * 2;
            wordsPerRow = (sampleSide + 63) / 64;
            wordsPerLayer = wordsPerRow * sampleSide;
            fillMasks = new ulong[FillLayers * wordsPerLayer];
            samples = new uint[sampleSide * sampleSide];
            Pixels = new byte[size * size * 4];
        }

        public void Begin(float[] surfaceDepth = null, int surfaceSize = 0, double depthTolerance = 0)
        {
            Array.Clear(fillMasks, 0, fillMasks.Length);
            Array.Clear(samples, 0, samples.Length);
            hasCoveredFills = false;
            this.surfaceDepth = surfaceDepth != null && surfaceSize > 0 && (long)surfaceSize * surfaceSize <= surfaceDepth.Length
                ? surfaceDepth : null;
            this.depthTolerance = depthTolerance;
            if (this.surfaceDepth == null)
                return;

            if (coveredFillMasks == null)
                coveredFillMasks = new ulong[fillMasks.Length];
            else
                Array.Clear(coveredFillMasks, 0, coveredFillMasks.Length);
            if (depthSize != surfaceSize)
            {
                depthSize = surfaceSize;
                if (depthCoordinates == null)
                    depthCoordinates = new int[sampleSide];
                for (int sample = 0; sample < sampleSide; sample++)
                    depthCoordinates[sample] = (int)((2L * sample + 1) * surfaceSize / (2L * sampleSide));
            }
        }

        public void Fill(Point3[] projected, int start, double zoom, int layer)
        {
            int edgeCount = 0;
            double top = sampleSide, bottom = 0;
            for (int i = start; i < start + 24; i += 2)
            {
                Point3 a = projected[i], b = projected[i + 1];
                double ax = (1 + a.X * zoom) * size, ay = (1 - a.Y * zoom) * size;
                double bx = (1 + b.X * zoom) * size, by = (1 - b.Y * zoom) * size;
                if (Math.Abs(ay - by) < 1e-9)
                    continue;
                double low = Math.Min(ay, by), high = Math.Max(ay, by);
                top = Math.Min(top, low);
                bottom = Math.Max(bottom, high);
                fillEdges[edgeCount++] = new FillEdge
                {
                    X = ay < by ? ax : bx,
                    Top = low,
                    Bottom = high,
                    Slope = (bx - ax) / (by - ay)
                };
            }

            top = Math.Max(0, top);
            bottom = Math.Min(sampleSide, bottom);
            if (bottom <= top)
                return;

            if (surfaceDepth != null)
                SetFrontPlanes(projected, start, zoom);

            bool covered = false;
            int fallbackX = -1, fallbackY = -1;
            int endRow = (int)Math.Ceiling(bottom - .5);
            for (int y = (int)Math.Ceiling(top - .5); y < endRow; y++)
            {
                double left, right;
                if (!FillSpan(edgeCount, y + .5, out left, out right))
                    continue;
                int first = (int)Math.Ceiling(left - .5);
                int end = (int)Math.Ceiling(right - .5);
                if (first < end)
                {
                    MarkSpan(layer, y, first, end);
                    covered = true;
                }
                else
                {
                    fallbackX = (int)((left + right) * .5);
                    fallbackY = y;
                }
            }

            // A visible region smaller than one sample still contributes to the fill.
            if (!covered)
            {
                double middle = (top + bottom) * .5;
                double left, right;
                if (fallbackY < 0 && FillSpan(edgeCount, middle, out left, out right))
                {
                    fallbackX = (int)((left + right) * .5);
                    fallbackY = (int)middle;
                }
                if (fallbackY >= 0)
                    MarkSpan(layer, fallbackY, fallbackX, fallbackX + 1);
            }
        }

        private bool FillSpan(int edgeCount, double y, out double left, out double right)
        {
            left = sampleSide;
            right = 0;
            for (int i = 0; i < edgeCount; i++)
            {
                var edge = fillEdges[i];
                if (y < edge.Top || y >= edge.Bottom)
                    continue;
                double x = edge.X + (y - edge.Top) * edge.Slope;
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
            left = Math.Max(0, left);
            right = Math.Min(sampleSide, right);
            return left < right;
        }

        private void MarkSpan(int layer, int row, int first, int end)
        {
            int rowStart = layer * wordsPerLayer + row * wordsPerRow;
            int firstWord = first / 64, lastWord = (end - 1) / 64;
            bool depthAware = surfaceDepth != null;
            int depthRow = depthAware ? depthCoordinates[row] * depthSize : 0;
            double y = row + .5;
            double depth0 = frontPlanes[0].Origin + y * frontPlanes[0].StepY;
            double depth1 = frontPlanes[1].Origin + y * frontPlanes[1].StepY;
            double depth2 = frontPlanes[2].Origin + y * frontPlanes[2].StepY;
            for (int word = firstWord; word <= lastWord; word++)
            {
                int start = word * 64;
                int low = Math.Max(0, first - start);
                int high = Math.Min(64, end - start);
                ulong visible = (ulong.MaxValue << low) & (ulong.MaxValue >> (64 - high));
                if (depthAware)
                {
                    ulong covered = 0;
                    for (int bit = low; bit < high; bit++)
                    {
                        int sample = start + bit;
                        float hull = surfaceDepth[depthRow + depthCoordinates[sample]];
                        if (float.IsNegativeInfinity(hull))
                            continue;
                        double x = sample + .5;
                        double front = Math.Min(depth0 + x * frontPlanes[0].StepX,
                            Math.Min(depth1 + x * frontPlanes[1].StepX, depth2 + x * frontPlanes[2].StepX));
                        if (hull > front + depthTolerance)
                            covered |= 1UL << bit;
                    }
                    coveredFillMasks[rowStart + word] |= covered;
                    hasCoveredFills |= covered != 0;
                    visible &= ~covered;
                }
                fillMasks[rowStart + word] |= visible;
            }
        }

        private void SetFrontPlanes(Point3[] projected, int start, double zoom)
        {
            // The first three edges share the box's first corner. Each points toward
            // the opposite face along one axis. Their nearer faces bound the ray in Z.
            Point3 corner = projected[start];
            for (int axis = 0; axis < frontPlanes.Length; axis++)
            {
                Point3 end = projected[start + 2 * axis + 1];
                double dz = end.Z - corner.Z;
                if (Math.Abs(dz) < 1e-10)
                {
                    frontPlanes[axis] = new DepthPlane { Origin = double.PositiveInfinity };
                    continue;
                }
                Point3 face = dz > 0 ? end : corner;
                double stepX = -(end.X - corner.X) / (dz * size * zoom);
                double stepY = (end.Y - corner.Y) / (dz * size * zoom);
                double x = (1 + face.X * zoom) * size, y = (1 - face.Y * zoom) * size;
                frontPlanes[axis] = new DepthPlane
                {
                    Origin = face.Z - stepX * x - stepY * y,
                    StepX = stepX,
                    StepY = stepY
                };
            }
        }

        public void ResolveFills(uint[] colors, byte[] surfacePixels = null, int surfaceSize = 0)
        {
            bool clipToSurface = surfacePixels != null && surfaceSize > 0
                && (long)surfaceSize * surfaceSize <= surfacePixels.Length / 4;
            if (clipToSurface)
                UpdateSurfaceCoverage(surfacePixels, surfaceSize);
            else
                maskedSurface = null;

            if (hasCoveredFills)
                ApplyFills(colors, clipToSurface, coveredFillMasks, covered: true);
            ApplyFills(colors, clipToSurface, fillMasks, covered: false);
        }

        private void ApplyFills(uint[] colors, bool clipToSurface, ulong[] masks, bool covered)
        {
            for (int layer = 0; layer < FillLayers; layer += 2)
            {
                uint normal = Premultiply(covered ? ScaleAlpha(colors[layer], CoveredFillAlpha) : colors[layer]);
                uint fresh = Premultiply(covered ? ScaleAlpha(colors[layer + 1], CoveredFillAlpha) : colors[layer + 1]);
                if ((normal | fresh) == 0)
                    continue;
                int layerStart = layer * wordsPerLayer;
                for (int y = 0; y < sampleSide; y++)
                    for (int word = 0; word < wordsPerRow; word++)
                    {
                        int rowWord = y * wordsPerRow + word;
                        int maskIndex = layerStart + rowWord;
                        ulong active = masks[maskIndex + wordsPerLayer];
                        ulong regular = masks[maskIndex] & ~active;
                        if (covered)
                        {
                            // An exposed marker of the same state takes precedence.
                            ulong exposed = fillMasks[maskIndex] | fillMasks[maskIndex + wordsPerLayer];
                            active &= ~exposed;
                            regular &= ~exposed;
                        }
                        // Missing blocks show where the hull used to be, including open space.
                        if (clipToSurface && layer < MissingFillLayer)
                        {
                            ulong coverage = surfaceCoverage[rowWord];
                            active &= coverage;
                            regular &= coverage;
                        }
                        int first = y * sampleSide + word * 64;
                        if (regular != 0 && normal != 0)
                            ApplyMask(first, regular, normal);
                        if (active != 0 && fresh != 0)
                            ApplyMask(first, active, fresh);
                    }
            }
        }

        private void UpdateSurfaceCoverage(byte[] surfacePixels, int surfaceSize)
        {
            if (maskedSurface == surfacePixels && maskedSurfaceSize == surfaceSize)
                return;

            if (surfaceCoverage == null)
            {
                surfaceCoverage = new ulong[wordsPerLayer];
                surfaceCoordinates = new int[sampleSide];
            }

            if (maskedSurfaceSize != surfaceSize)
                for (int sample = 0; sample < sampleSide; sample++)
                    surfaceCoordinates[sample] = (int)((2L * sample + 1) * surfaceSize / (2L * sampleSide));

            int previousSourceY = -1;
            for (int y = 0; y < sampleSide; y++)
            {
                int sourceY = surfaceCoordinates[y];
                int rowStart = y * wordsPerRow;
                if (sourceY == previousSourceY)
                {
                    Array.Copy(surfaceCoverage, rowStart - wordsPerRow, surfaceCoverage, rowStart, wordsPerRow);
                    continue;
                }

                previousSourceY = sourceY;
                int alphaOffset = sourceY * surfaceSize * 4 + 3;
                for (int word = 0; word < wordsPerRow; word++)
                {
                    int first = word * 64;
                    int count = Math.Min(64, sampleSide - first);
                    ulong coverage = 0;
                    for (int bit = 0; bit < count; bit++)
                        if (surfacePixels[alphaOffset + surfaceCoordinates[first + bit] * 4] != 0)
                            coverage |= 1UL << bit;

                    surfaceCoverage[rowStart + word] = coverage;
                }
            }

            // The textured renderer publishes a new immutable pixel buffer for each image.
            maskedSurface = surfacePixels;
            maskedSurfaceSize = surfaceSize;
        }

        private void ApplyMask(int first, ulong mask, uint color)
        {
            // Sparse spans can skip empty bytes without touching the pixel buffer.
            while (mask != 0)
            {
                int bits = (byte)mask;
                for (int bit = 0; bits != 0; bit++, bits >>= 1)
                    if ((bits & 1) != 0)
                        samples[first + bit] = Over(color, samples[first + bit]);
                mask >>= 8;
                first += 8;
            }
        }

        public void Stroke(double x0, double y0, double x1, double y1, double thickness, uint color,
            double startDepth = double.PositiveInfinity, double endDepth = double.PositiveInfinity)
        {
            uint source = Premultiply(color);
            if (source == 0 || thickness <= 0)
                return;
            bool depthAware = surfaceDepth != null && !double.IsInfinity(startDepth) && !double.IsInfinity(endDepth);
            uint covered = depthAware ? Premultiply(ScaleAlpha(color, CoveredOutlineAlpha)) : 0;
            x0 = (x0 + 1) * size;
            y0 = (y0 + 1) * size;
            x1 = (x1 + 1) * size;
            y1 = (y1 + 1) * size;
            bool horizontal = Math.Abs(x1 - x0) >= Math.Abs(y1 - y0);
            double major0 = horizontal ? x0 : y0, major1 = horizontal ? x1 : y1;
            double minor0 = horizontal ? y0 : x0, minor1 = horizontal ? y1 : x1;
            double majorDelta = major1 - major0, minorDelta = minor1 - minor0;
            double lengthSquared = majorDelta * majorDelta + minorDelta * minorDelta;
            double radius = thickness; // A pixel spans two samples.
            double radiusSquared = radius * radius;
            double slope = majorDelta == 0 ? 0 : minorDelta / majorDelta;
            double minorRadius = radius * Math.Sqrt(1 + slope * slope);
            double low = Math.Min(major0, major1), high = Math.Max(major0, major1);
            int first = Math.Max(0, (int)Math.Ceiling(low - radius - .5));
            int last = Math.Min(sampleSide - 1, (int)Math.Floor(high + radius - .5));
            for (int major = first; major <= last; major++)
            {
                double majorCenter = major + .5;
                double along = majorCenter - major0;
                // Every sample in the stroke lies in this narrow strip. At each
                // end, keep only the samples inside the round cap.
                double minorCenter = minor0 + slope * along;
                int firstMinor = Math.Max(0, (int)Math.Ceiling(minorCenter - minorRadius - .5));
                int endMinor = Math.Min(sampleSide, (int)Math.Ceiling(minorCenter + minorRadius - .5));
                double endAlong = majorCenter - major1;
                for (int minor = firstMinor; minor < endMinor; minor++)
                {
                    double across = minor + .5 - minor0;
                    double dot = along * majorDelta + across * minorDelta;
                    if (dot <= 0)
                    {
                        if (along * along + across * across > radiusSquared)
                            continue;
                    }
                    else if (dot >= lengthSquared)
                    {
                        double endAcross = minor + .5 - minor1;
                        if (endAlong * endAlong + endAcross * endAcross > radiusSquared)
                            continue;
                    }
                    int index = horizontal ? minor * sampleSide + major : major * sampleSide + minor;
                    uint ink = source;
                    if (depthAware)
                    {
                        int x = horizontal ? major : minor, y = horizontal ? minor : major;
                        double t = dot <= 0 ? 0 : dot >= lengthSquared ? 1 : dot / lengthSquared;
                        double z = startDepth + (endDepth - startDepth) * t;
                        float hull = surfaceDepth[depthCoordinates[y] * depthSize + depthCoordinates[x]];
                        if (hull > z + depthTolerance)
                            ink = covered;
                    }
                    samples[index] = Over(ink, samples[index]);
                }
            }
        }

        public void Finish()
        {
            for (int y = 0; y < size; y++)
            {
                int top = y * 2 * sampleSide;
                int bottom = top + sampleSide;
                for (int x = 0; x < size; x++)
                {
                    int offset = x * 2;
                    uint a = samples[top + offset], b = samples[top + offset + 1];
                    uint c = samples[bottom + offset], d = samples[bottom + offset + 1];
                    uint color = a == b && a == c && a == d ? a : Average(a, b, c, d);
                    int pixel = (y * size + x) * 4;
                    Pixels[pixel] = (byte)color;
                    Pixels[pixel + 1] = (byte)(color >> 8);
                    Pixels[pixel + 2] = (byte)(color >> 16);
                    Pixels[pixel + 3] = (byte)(color >> 24);
                }
            }
        }

        // Internally the channels are packed as AABBGGRR.
        private static uint ScaleAlpha(uint rgba, uint strength)
        {
            return (rgba & 0xffffff00) | (((rgba & 255) * strength + 127) / 255);
        }

        private static uint Premultiply(uint rgba)
        {
            uint alpha = rgba & 255;
            uint red = ((rgba >> 24) * alpha + 127) / 255;
            uint green = (((rgba >> 16) & 255) * alpha + 127) / 255;
            uint blue = (((rgba >> 8) & 255) * alpha + 127) / 255;
            return red | (green << 8) | (blue << 16) | (alpha << 24);
        }

        private static uint Over(uint source, uint destination)
        {
            uint inverse = 255 - (source >> 24);
            if (destination == 0 || inverse == 0)
                return source;
            uint red = ((destination & 255) * inverse + 127) / 255;
            uint green = (((destination >> 8) & 255) * inverse + 127) / 255;
            uint blue = (((destination >> 16) & 255) * inverse + 127) / 255;
            uint alpha = ((destination >> 24) * inverse + 127) / 255;
            return source + (red | (green << 8) | (blue << 16) | (alpha << 24));
        }

        private static uint Average(uint a, uint b, uint c, uint d)
        {
            const uint channels = 0x00ff00ff;
            // Sum two channels at a time, with room for four samples in each half.
            uint redBlue = (a & channels) + (b & channels) + (c & channels) + (d & channels) + 0x00020002;
            uint greenAlpha = ((a >> 8) & channels) + ((b >> 8) & channels)
                + ((c >> 8) & channels) + ((d >> 8) & channels) + 0x00020002;
            return ((redBlue >> 2) & channels) | (((greenAlpha >> 2) & channels) << 8);
        }
    }
}

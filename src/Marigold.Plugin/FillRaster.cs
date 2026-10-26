using System;
using System.Collections.Generic;

namespace Marigold
{
    // Union screen-space fills per state. Raster cells conservatively cover every input band;
    // no state competes with another for a shared quota. Outline geometry is unaffected.
    internal sealed class FillRaster
    {
        private const int MaximumSide = 128;
        private readonly int[] cells = new int[3 * MaximumSide * MaximumSide];
        private readonly ulong[] masks = new ulong[6 * MaximumSide * 2];
        private readonly int[] representatives = new int[6];
        private readonly bool[] pulse = new bool[3 * MaximumSide * MaximumSide];
        private readonly List<FillBand> output = new List<FillBand>(MarkerFill.Budget);

        internal int Side { get; private set; }

        public void Merge(List<FillBand> bands, ChangeKind[] kinds, double[] starts, double now, bool animate)
        {
            if (bands.Count == 0)
            {
                Side = 0;
                return;
            }

            for (int side = MaximumSide;; side /= 2)
            {
                Array.Clear(cells, 0, cells.Length);
                Array.Clear(pulse, 0, pulse.Length);
                Array.Clear(masks, 0, masks.Length);
                Array.Clear(representatives, 0, representatives.Length);
                output.Clear();
                foreach (var band in bands)
                {
                    int line = band.MarkerStart / 2;
                    int state = kinds[line] == ChangeKind.Damaged ? 0 : kinds[line] == ChangeKind.NonFunctional ? 1 : 2;
                    bool fresh = animate && state != 0 && line < starts.Length && now >= starts[line] && now - starts[line] < 4;
                    int x0 = Math.Max(0, (int)Math.Floor((band.X + 1) * side / 2));
                    int x1 = Math.Min(side, (int)Math.Ceiling((band.X + band.Width + 1) * side / 2 - 1e-10));
                    int y0 = Math.Max(0, (int)Math.Floor((band.Y + 1) * side / 2));
                    int y1 = Math.Min(side, (int)Math.Ceiling((band.Y + band.Height + 1) * side / 2 - 1e-10));
                    int layer = state * 2 + (fresh ? 1 : 0);
                    representatives[layer] = band.MarkerStart + 1;
                    for (int word = 0; word < 2; word++)
                    {
                        int lo = Math.Max(0, x0 - word * 64), hi = Math.Min(64, x1 - word * 64);
                        if (hi <= lo)
                            continue;
                        ulong bits = (ulong.MaxValue << lo) & (hi == 64 ? ulong.MaxValue : (1UL << hi) - 1);
                        for (int y = y0; y < y1; y++)
                            masks[(layer * side + y) * 2 + word] |= bits;
                    }
                }

                for (int state = 0; state < 3; state++)
                    for (int y = 0; y < side; y++)
                        for (int x = 0; x < side; x++)
                        {
                            ulong bit = 1UL << (x % 64);
                            int index = (state * 2 * side + y) * 2 + x / 64;
                            bool fresh = (masks[index + side * 2] & bit) != 0;
                            if (fresh || (masks[index] & bit) != 0)
                            {
                                int cell = (state * side + y) * side + x;
                                cells[cell] = representatives[state * 2 + (fresh ? 1 : 0)];
                                pulse[cell] = fresh;
                            }
                        }

                for (int state = 0; state < 3; state++)
                    for (int y = 0; y < side; y++)
                        for (int x = 0; x < side; x++)
                        {
                            int cell = (state * side + y) * side + x, marker = cells[cell];
                            if (marker == 0)
                                continue;
                            bool fresh = pulse[cell];
                            int width = 1;
                            while (x + width < side && cells[cell + width] != 0 && pulse[cell + width] == fresh)
                                width++;
                            int height = 1;
                            for (; y + height < side; height++)
                            {
                                bool same = true;
                                for (int dx = 0; dx < width; dx++)
                                {
                                    int other = cell + height * side + dx;
                                    if (cells[other] == 0 || pulse[other] != fresh)
                                    {
                                        same = false;
                                        break;
                                    }
                                }

                                if (!same)
                                    break;
                            }

                            for (int dy = 0; dy < height; dy++)
                                for (int dx = 0; dx < width; dx++)
                                    cells[cell + dy * side + dx] = 0;
                            output.Add(new FillBand
                            {
                                MarkerStart = marker - 1,
                                X = 2.0 * x / side - 1,
                                Y = 2.0 * y / side - 1,
                                Width = 2.0 * width / side,
                                Height = 2.0 * height / side
                            });
                        }

                // At side 32, even one rectangle per occupied cell fits the 4096 budget.
                if (output.Count > MarkerFill.Budget)
                    continue;
                bands.Clear();
                bands.AddRange(output);
                Side = side;
                return;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace Marigold
{
    // Only live observations produce alerts; loading an already damaged ship seeds history.
    internal sealed class DamagePulse
    {
        private BlockSample[] baseline;
        private Dictionary<Point3, BlockSample> previous;
        private readonly Dictionary<Point3, double> starts = new Dictionary<Point3, double>();

        public void Reset()
        {
            baseline = null;
            previous = null;
            starts.Clear();
        }

        public void Observe(BlockSample[] reference, BlockSample[] current, double now)
        {
            if (!ReferenceEquals(reference, baseline))
            {
                Reset();
                baseline = reference;
            }

            var live = new Dictionary<Point3, BlockSample>();
            foreach (var b in current)
                live[b.Position] = b;
            if (reference != null && previous != null)
                foreach (var expected in reference)
                {
                    BlockSample old, block;
                    bool had = previous.TryGetValue(expected.Position, out old),
                        has = live.TryGetValue(expected.Position, out block);
                    var kind = SnapshotComparison.Classify(expected, has ? (BlockSample? )block : null);
                    bool fault = VisibleFor(kind);
                    if (!fault)
                    {
                        starts.Remove(expected.Position);
                        continue;
                    }

                    bool worse = had
                        && (!has
                        || (old.Definition == block.Definition
                        && (block.Integrity + .001 < old.Integrity
                        || block.BuildIntegrity + .001 < old.BuildIntegrity
                        || (old.IsFunctional && !block.IsFunctional))));
                    double start;
                    // Further hits do not restart the animation every sample. After four seconds a new hit can alert again.
                    if (worse && (!starts.TryGetValue(expected.Position, out start) || now - start >= 4))
                        starts[expected.Position] = now;
                }

            previous = live;
            var expired = new List<Point3>();
            foreach (var pair in starts)
                if (now - pair.Value >= 4)
                    expired.Add(pair.Key);
            foreach (var key in expired)
                starts.Remove(key);
        }

        public double Start(BlockChange region)
        {
            double latest = double.NegativeInfinity;
            foreach (var pair in starts)
            {
                var p = pair.Key;
                var b = region.Bounds;
                if (p.X >= b.Min.X && p.X <= b.Max.X && p.Y >= b.Min.Y && p.Y <= b.Max.Y && p.Z >= b.Min.Z && p.Z <= b.Max.Z)
                    latest = Math.Max(latest, pair.Value);
            }

            return latest;
        }

        // One display-wide phase; per-block timestamps only control alert eligibility.
        public static bool VisibleFor(ChangeKind kind)
        {
            return kind == ChangeKind.NonFunctional || kind == ChangeKind.Missing;
        }

        public static double Wave(double now)
        {
            return .5 + .5 * Math.Cos(now * Math.PI * 2);
        }

        public static double Strength(double start, double now)
        {
            double age = now - start;
            return age < 0 || age >= 4 ? 0 : Wave(now);
        }
    }
}

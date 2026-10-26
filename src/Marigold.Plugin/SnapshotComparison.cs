using System;
using System.Collections.Generic;

namespace Marigold
{
    internal enum ChangeKind : byte
    {
        Hull,
        Missing,
        Damaged,
        Replaced,
        Added,
        Unknown,
        NonFunctional
    }

    internal struct BlockSample
    {
        public Point3 Position;
        public Box3 Bounds;
        public string Definition;
        public int Forward, Up;
        public bool HasFunctionalState, IsFunctional;
        public double Integrity, BuildIntegrity, MaxIntegrity;
    }

    internal struct BlockChange
    {
        public Box3 Bounds;
        public ChangeKind Kind;
        public string Definition;
    }

    internal sealed class Comparison
    {
        public int Expected, Observed;

        public int Matching
        {
            get
            {
                return Expected - Counts[(int)ChangeKind.Missing] - Counts[(int)ChangeKind.Replaced];
            }
        }

        public readonly List<BlockChange> Changes = new List<BlockChange>();
        public readonly int[] Counts = new int[7];

        public void Add(Box3 bounds, ChangeKind kind, string definition)
        {
            Changes.Add(new BlockChange
            {
                Bounds = bounds,
                Kind = kind,
                Definition = definition
            });
            Counts[(int)kind]++;
        }

        public string Summary
        {
            get
            {
                return Expected + " expected, " + Observed + " observed, " + Matching + " matching ("
                    + (Expected == 0
                    ? "n/a"
                    : (100.0 * Matching / Expected).ToString("F1") + "% structurally complete; not health") + "); " + Counts[1] + " missing, " + Counts[2]
                    + " damaged/degraded, " + Counts[6] + " non-functional, " + Counts[3] + " replaced, "
                    + Counts[4] + " added, " + Counts[5] + " unknown";
            }
        }
    }

    internal static class SnapshotComparison
    {
        public static BlockSample[] HealthyBaseline(BlockSample[] source)
        {
            var result = (BlockSample[])source.Clone();
            for (int i = 0; i < result.Length; i++)
            {
                result[i].Integrity = result[i].MaxIntegrity;
                result[i].BuildIntegrity = result[i].MaxIntegrity;
                result[i].IsFunctional = true;
            }

            return result;
        }

        public static Comparison Compare(BlockSample[] reference, BlockSample[] current)
        {
            var result = new Comparison
            {
                Expected = reference.Length,
                Observed = current.Length
            };
            var observed = new Dictionary<Point3, BlockSample>();
            foreach (var block in current)
                observed.Add(block.Position, block);
            foreach (var old in reference)
            {
                BlockSample now;
                if (!observed.TryGetValue(old.Position, out now))
                {
                    result.Add(old.Bounds, ChangeKind.Missing, old.Definition);
                    continue;
                }

                observed.Remove(old.Position);
                var kind = Classify(old, now);
                if (kind != ChangeKind.Hull)
                    result.Add(
                        kind == ChangeKind.Replaced ? SchematicGeometry.Union(old.Bounds, now.Bounds) : now.Bounds,
                        kind,
                        now.Definition
                    );
            }

            foreach (var pair in observed)
                result.Add(pair.Value.Bounds, ChangeKind.Added, pair.Value.Definition);
            result.Changes.Sort((a, b) =>
            {
                int c = Priority(a.Kind).CompareTo(Priority(b.Kind));
                if (c != 0)
                    return c;
                c = a.Bounds.Min.X.CompareTo(b.Bounds.Min.X);
                if (c != 0)
                    return c;
                c = a.Bounds.Min.Y.CompareTo(b.Bounds.Min.Y);
                return c != 0 ? c : a.Bounds.Min.Z.CompareTo(b.Bounds.Min.Z);
            });
            return result;
        }

        public static ChangeKind Classify(BlockSample expected, BlockSample? observed)
        {
            if (!observed.HasValue)
                return ChangeKind.Missing;
            var now = observed.Value;
            if (expected.Definition != now.Definition
                || expected.Forward != now.Forward
                || expected.Up != now.Up
                || !expected.Bounds.Min.Equals(now.Bounds.Min)
                || !expected.Bounds.Max.Equals(now.Bounds.Max))
                return ChangeKind.Replaced;
            if (!Valid(now) || !Valid(expected))
                return ChangeKind.Unknown;
            if (now.HasFunctionalState && !now.IsFunctional)
                return ChangeKind.NonFunctional;
            double tolerance = Math.Max(.001, now.MaxIntegrity * .0001);
            return now.Integrity + tolerance < expected.Integrity
                || now.BuildIntegrity + tolerance < expected.BuildIntegrity
                || now.Integrity + tolerance < now.MaxIntegrity
                || now.BuildIntegrity + tolerance < now.MaxIntegrity
                ? ChangeKind.Damaged
                : ChangeKind.Hull;
        }

        private static int Priority(ChangeKind kind)
        {
            return kind == ChangeKind.NonFunctional ? 2 : kind >= ChangeKind.Damaged ? (int)kind + 1 : (int)kind;
        }

        private static bool Valid(BlockSample b)
        {
            return b.MaxIntegrity > 0 && Finite(b.MaxIntegrity) && Finite(b.Integrity) && Finite(b.BuildIntegrity);
        }

        private static bool Finite(double n)
        {
            return !double.IsNaN(n) && !double.IsInfinity(n);
        }
    }
}

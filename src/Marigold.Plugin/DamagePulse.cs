using System;
using System.Collections.Generic;

namespace Marigold
{
    // Only live observations produce alerts; loading an already damaged ship seeds history.
    internal sealed class DamagePulse
    {
        private BlockSample[] baseline;
        private Dictionary<Point3, BlockSample> previous;
        private readonly Dictionary<Point3, Alert> starts = new Dictionary<Point3, Alert>();
        private Alert[] indexedAlerts;
        private AlertNode alertIndex;

        private struct Alert
        {
            public Point3 Position;
            public Box3 Bounds;
            public ChangeKind Kind;
            public double Time;
        }

        private sealed class AlertNode
        {
            public Box3 Bounds;
            public double Latest, Missing, NonFunctional;
            public int Start, Count;
            public AlertNode Left, Right;
        }

        private sealed class AlertOrder : IComparer<Alert>
        {
            private readonly int axis;

            public AlertOrder(int axis)
            {
                this.axis = axis;
            }

            public int Compare(Alert a, Alert b)
            {
                double first = axis == 0 ? a.Position.X : axis == 1 ? a.Position.Y : a.Position.Z;
                double second = axis == 0 ? b.Position.X : axis == 1 ? b.Position.Y : b.Position.Z;
                return first.CompareTo(second);
            }
        }

        private static readonly IComparer<Alert>[] AlertOrders =
        {
            new AlertOrder(0), new AlertOrder(1), new AlertOrder(2)
        };

        public void Reset()
        {
            baseline = null;
            previous = null;
            starts.Clear();
            indexedAlerts = null;
            alertIndex = null;
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
                    Alert alert;
                    bool active = starts.TryGetValue(expected.Position, out alert);
                    // Further hits do not restart the animation every sample. After four seconds a new hit can alert again.
                    if (worse && (!active || now - alert.Time >= 4))
                        starts[expected.Position] = new Alert
                        {
                            Position = expected.Position,
                            Bounds = expected.Bounds,
                            Kind = kind,
                            Time = now
                        };
                    else if (active && alert.Kind != kind)
                    {
                        alert.Kind = kind;
                        starts[expected.Position] = alert;
                    }
                }

            previous = live;
            var expired = new List<Point3>();
            foreach (var pair in starts)
                if (now - pair.Value.Time >= 4)
                    expired.Add(pair.Key);
            foreach (var key in expired)
                starts.Remove(key);
            indexedAlerts = null;
            alertIndex = null;
        }

        public double Start(BlockChange region)
        {
            return Latest(region.Bounds, ChangeKind.Unknown, false);
        }

        public double StartOutline(BlockChange region)
        {
            return VisibleFor(region.Kind) ? Latest(region.Bounds, region.Kind, true) : double.NegativeInfinity;
        }

        private double Latest(Box3 bounds, ChangeKind kind, bool intersectBounds)
        {
            if (starts.Count == 0)
                return double.NegativeInfinity;
            if (alertIndex == null)
            {
                indexedAlerts = new Alert[starts.Count];
                starts.Values.CopyTo(indexedAlerts, 0);
                alertIndex = BuildIndex(0, indexedAlerts.Length, 0);
            }
            return Query(alertIndex, bounds, kind, intersectBounds, double.NegativeInfinity);
        }

        private AlertNode BuildIndex(int start, int count, int axis)
        {
            var node = new AlertNode
            {
                Start = start,
                Count = count,
                Bounds = indexedAlerts[start].Bounds,
                Latest = double.NegativeInfinity,
                Missing = double.NegativeInfinity,
                NonFunctional = double.NegativeInfinity
            };
            for (int i = start; i < start + count; i++)
            {
                var alert = indexedAlerts[i];
                node.Bounds = SchematicGeometry.Union(node.Bounds, alert.Bounds);
                node.Latest = Math.Max(node.Latest, alert.Time);
                if (alert.Kind == ChangeKind.Missing)
                    node.Missing = Math.Max(node.Missing, alert.Time);
                else if (alert.Kind == ChangeKind.NonFunctional)
                    node.NonFunctional = Math.Max(node.NonFunctional, alert.Time);
            }
            if (count > 8)
            {
                Array.Sort(indexedAlerts, start, count, AlertOrders[axis]);
                int half = count / 2;
                node.Left = BuildIndex(start, half, (axis + 1) % 3);
                node.Right = BuildIndex(start + half, count - half, (axis + 1) % 3);
            }
            return node;
        }

        private double Query(AlertNode node, Box3 bounds, ChangeKind kind, bool intersectBounds, double latest)
        {
            double maximum = kind == ChangeKind.Missing ? node.Missing
                : kind == ChangeKind.NonFunctional ? node.NonFunctional : node.Latest;
            if (maximum <= latest || !Intersects(bounds, node.Bounds))
                return latest;
            if (Contains(bounds, node.Bounds))
                return maximum;
            if (node.Left != null)
            {
                latest = Query(node.Left, bounds, kind, intersectBounds, latest);
                return Query(node.Right, bounds, kind, intersectBounds, latest);
            }
            for (int i = node.Start; i < node.Start + node.Count; i++)
            {
                var alert = indexedAlerts[i];
                if (kind != ChangeKind.Unknown && alert.Kind != kind)
                    continue;
                bool inside = intersectBounds ? Intersects(bounds, alert.Bounds)
                    : Contains(bounds, new Box3(alert.Position, alert.Position));
                if (inside)
                    latest = Math.Max(latest, alert.Time);
            }
            return latest;
        }

        private static bool Intersects(Box3 a, Box3 b)
        {
            return a.Min.X <= b.Max.X && a.Max.X >= b.Min.X
                && a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y
                && a.Min.Z <= b.Max.Z && a.Max.Z >= b.Min.Z;
        }

        private static bool Contains(Box3 outer, Box3 inner)
        {
            return outer.Min.X <= inner.Min.X && outer.Max.X >= inner.Max.X
                && outer.Min.Y <= inner.Min.Y && outer.Max.Y >= inner.Max.Y
                && outer.Min.Z <= inner.Min.Z && outer.Max.Z >= inner.Max.Z;
        }

        // Coarse outlines represent occupied cells. Expand their alert envelope by the
        // extra cell width, without adding draw geometry or suppressing any fault edges.
        public static BlockChange OutlineRegion(ChangeKind kind, Point3 from, Point3 to, Box3 frame, double cellSize)
        {
            double radius = StationGeometry.Radius(frame);
            var center = StationGeometry.Center(frame);
            double padding = Math.Max(1e-7, cellSize - 1);
            return new BlockChange
            {
                Kind = kind,
                Bounds = new Box3(
                    new Point3(
                        center.X + Math.Min(from.X, to.X) * radius - padding,
                        center.Y + Math.Min(from.Y, to.Y) * radius - padding,
                        center.Z + Math.Min(from.Z, to.Z) * radius - padding),
                    new Point3(
                        center.X + Math.Max(from.X, to.X) * radius + padding,
                        center.Y + Math.Max(from.Y, to.Y) * radius + padding,
                        center.Z + Math.Max(from.Z, to.Z) * radius + padding)
                )
            };
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

        // Change brightness, not transparency or hue, so bright outlines can still breathe.
        // A fresh alert has a deeper contrast range than the gentle ongoing missing pulse.
        public static uint Tint(uint color, ChangeKind kind, double wave, bool fresh, double missingAmount)
        {
            if (!VisibleFor(kind))
                return color;
            wave = Math.Max(0, Math.Min(1, wave));
            double strength = fresh ? .70 : kind == ChangeKind.Missing ? .18 * missingAmount : 0;
            double brightness = 1 - strength * (1 - wave);
            return (uint)((byte)(((color >> 16) & 255) * brightness) << 16)
                | (uint)((byte)(((color >> 8) & 255) * brightness) << 8)
                | (byte)((color & 255) * brightness);
        }
    }
}

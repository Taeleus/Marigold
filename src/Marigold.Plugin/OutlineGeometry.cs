using System;
using System.Collections.Generic;

namespace Marigold
{
    internal sealed class OutlineModel
    {
        // Consecutive pairs are independent line segments, normalized about the model bounds.
        public Point3[] Points = new Point3[0];
        public int Cells;
        public double CellSize;
    }

    internal static class OutlineGeometry
    {
        // Overview includes every matching fault, independently of paginated individual markers.
        internal static OutlineModel Faults(IList<BlockChange> changes, ChangeKind kind, Box3 frame)
        {
            var boxes = new List<Box3>();
            foreach (var change in changes)
                if (change.Kind == kind)
                    boxes.Add(change.Bounds);
            return Build(boxes, 2, frame);
        }

        private const int CellLimit = 100000;

        internal static int LineLimit(int detail)
        {
            return detail == 0 ? 768 : detail == 1 ? 2048 : 4096;
        }

        private struct Cell : IEquatable<Cell>
        {
            public long X, Y, Z;

            public Cell(long x, long y, long z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public Cell Offset(int axis, int amount)
            {
                return new Cell(X + (axis == 0 ? amount : 0), Y + (axis == 1 ? amount : 0), Z + (axis == 2 ? amount : 0));
            }

            public long At(int axis)
            {
                return axis == 0 ? X : axis == 1 ? Y : Z;
            }

            public bool Equals(Cell other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object other)
            {
                return other is Cell && Equals((Cell)other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode();
                }
            }
        }

        private struct Edge : IEquatable<Edge>
        {
            public Cell Start;
            public int Axis;

            public Edge(Cell start, int axis)
            {
                Start = start;
                Axis = axis;
            }

            public bool Equals(Edge other)
            {
                return Axis == other.Axis && Start.Equals(other.Start);
            }

            public override bool Equals(object other)
            {
                return other is Edge && Equals((Edge)other);
            }

            public override int GetHashCode()
            {
                return Start.GetHashCode() ^ (Axis * 7919);
            }
        }

        public static OutlineModel Build(IList<Box3> blocks, int detail, Box3? frame = null, int lineBudget = 0)
        {
            if (blocks.Count == 0)
                return new OutlineModel
                {
                    CellSize = 1
                };
            // Anchoring to the minimum keeps negative coordinates and distant ships equivalent.
            var bounds = blocks[0];
            foreach (var box in blocks)
                bounds = SchematicGeometry.Union(bounds, box);
            double size = 1;
            for (int pass = 0; pass < 64; pass++, size *= 2)
            {
                var cells = Occupy(blocks, bounds.Min, size);
                if (cells == null)
                    continue;
                var edges = new Dictionary<Edge, int>();
                foreach (var cell in cells)
                    for (int normal = 0; normal < 3; normal++)
                        for (int sign = 0; sign < 2; sign++)
                        {
                            if (cells.Contains(cell.Offset(normal, sign == 0 ? -1 : 1)))
                                continue;
                            var corner = cell.Offset(normal, sign);
                            int u = (normal + 1) % 3, v = (normal + 2) % 3, mask = 1 << (normal * 2 + sign);
                            Toggle(edges, new Edge(corner, u), mask);
                            Toggle(edges, new Edge(corner.Offset(v, 1), u), mask);
                            Toggle(edges, new Edge(corner, v), mask);
                            Toggle(edges, new Edge(corner.Offset(u, 1), v), mask);
                        }

                // XOR cancels shared edges on coplanar faces. Creases retain distinct normals.
                var remaining = new List<Edge>();
                foreach (var pair in edges)
                    if (pair.Value != 0)
                        remaining.Add(pair.Key);
                remaining.Sort((a, b) =>
                {
                    int cmp = a.Axis.CompareTo(b.Axis);
                    if (cmp != 0)
                        return cmp;
                    for (int offset = 1; offset <= 3; offset++)
                    {
                        int axis = (a.Axis + offset) % 3;
                        cmp = a.Start.At(axis).CompareTo(b.Start.At(axis));
                        if (cmp != 0)
                            return cmp;
                    }

                    return 0;
                });
                var points = new List<Point3>();
                for (int i = 0; i < remaining.Count;)
                {
                    var first = remaining[i++];
                    var end = first.Start.Offset(first.Axis, 1);
                    while (i < remaining.Count && remaining[i].Axis == first.Axis && remaining[i].Start.Equals(end))
                    {
                        end = end.Offset(first.Axis, 1);
                        i++;
                    }

                    points.Add(Position(first.Start, bounds.Min, size));
                    points.Add(Position(end, bounds.Min, size));
                }

                if (points.Count / 2 > (lineBudget > 0 ? Math.Max(12, lineBudget) : LineLimit(detail)))
                    continue;
                // Coarse occupancy must not extend the hull past its actual outer bounds.
                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    points[i] = new Point3(Math.Min(bounds.Max.X, p.X), Math.Min(bounds.Max.Y, p.Y), Math.Min(bounds.Max.Z, p.Z));
                }

                Normalize(points, frame);
                return new OutlineModel
                {
                    Points = points.ToArray(),
                    Cells = cells.Count,
                    CellSize = size
                };
            }

            throw new InvalidOperationException("Grid coordinates exceed outline limits.");
        }

        private static HashSet<Cell> Occupy(IList<Box3> blocks, Point3 origin, double size)
        {
            var cells = new HashSet<Cell>();
            foreach (var box in blocks)
            {
                long x0 = (long)Math.Floor((box.Min.X - origin.X) / size),
                    x1 = (long)Math.Ceiling((box.Max.X - origin.X) / size) - 1;
                long y0 = (long)Math.Floor((box.Min.Y - origin.Y) / size),
                    y1 = (long)Math.Ceiling((box.Max.Y - origin.Y) / size) - 1;
                long z0 = (long)Math.Floor((box.Min.Z - origin.Z) / size),
                    z1 = (long)Math.Ceiling((box.Max.Z - origin.Z) / size) - 1;
                // Bound work even for a single enormous modded block envelope.
                if ((double)(x1 - x0 + 1) * (y1 - y0 + 1) * (z1 - z0 + 1) > CellLimit)
                    return null;
                for (long x = x0; x <= x1; x++)
                    for (long y = y0; y <= y1; y++)
                        for (long z = z0; z <= z1; z++)
                        {
                            cells.Add(new Cell(x, y, z));
                            if (cells.Count > CellLimit)
                                return null;
                        }
            }

            return cells;
        }

        private static void Toggle(Dictionary<Edge, int> edges, Edge edge, int mask)
        {
            int old;
            if (edges.TryGetValue(edge, out old))
            {
                int next = old ^ mask;
                if (next == 0)
                    edges.Remove(edge);
                else
                    edges[edge] = next;
            }
            else
                edges.Add(edge, mask);
        }

        private static Point3 Position(Cell cell, Point3 origin, double size)
        {
            return new Point3(origin.X + cell.X * size, origin.Y + cell.Y * size, origin.Z + cell.Z * size);
        }

        private static void Normalize(List<Point3> points, Box3? frame)
        {
            if (points.Count == 0)
                return;
            var bounds = new Box3(points[0], points[0]);
            foreach (var p in points)
                bounds = SchematicGeometry.Union(bounds, new Box3(p, p));
            if (frame.HasValue)
                bounds = frame.Value;
            double x = (bounds.Min.X + bounds.Max.X) / 2,
                y = (bounds.Min.Y + bounds.Max.Y) / 2,
                z = (bounds.Min.Z + bounds.Max.Z) / 2;
            double dx = bounds.Max.X - bounds.Min.X, dy = bounds.Max.Y - bounds.Min.Y, dz = bounds.Max.Z - bounds.Min.Z;
            double radius = Math.Max(.5, Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2);
            for (int i = 0; i < points.Count; i++)
                points[i] = new Point3((points[i].X - x) / radius, (points[i].Y - y) / radius, (points[i].Z - z) / radius);
        }
    }
}

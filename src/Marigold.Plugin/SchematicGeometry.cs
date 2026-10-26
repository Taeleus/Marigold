using System;
using System.Collections.Generic;

namespace Marigold
{
    internal struct Point3 : IEquatable<Point3>
    {
        public double X, Y, Z;

        public Point3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(Point3 other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object value)
        {
            return value is Point3 && Equals((Point3)value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode();
            }
        }
    }

    internal struct Box3
    {
        public Point3 Min, Max;

        public Box3(Point3 min, Point3 max)
        {
            Min = min;
            Max = max;
        }
    }

    internal static class SchematicGeometry
    {
        public static Box3 Union(Box3 a, Box3 b)
        {
            return new Box3(
                new Point3(Math.Min(a.Min.X, b.Min.X), Math.Min(a.Min.Y, b.Min.Y), Math.Min(a.Min.Z, b.Min.Z)),
                new Point3(Math.Max(a.Max.X, b.Max.X), Math.Max(a.Max.Y, b.Max.Y), Math.Max(a.Max.Z, b.Max.Z))
            );
        }

        public static Point3 ToBasis(Point3 p, Point3 right, Point3 up, Point3 backward)
        {
            return new Point3(
                p.X * right.X + p.Y * right.Y + p.Z * right.Z,
                p.X * up.X + p.Y * up.Y + p.Z * up.Z,
                p.X * backward.X + p.Y * backward.Y + p.Z * backward.Z
            );
        }

        public static Point3 Rotate(Point3 p, double yaw, double pitch)
        {
            double x = p.X * Math.Cos(yaw) + p.Z * Math.Sin(yaw), z = -p.X * Math.Sin(yaw) + p.Z * Math.Cos(yaw);
            return new Point3(x, p.Y * Math.Cos(pitch) - z * Math.Sin(pitch), p.Y * Math.Sin(pitch) + z * Math.Cos(pitch));
        }

        // Liang-Barsky clipping to the schematic's normalized square viewport.
        public static bool Clip(ref double x0, ref double y0, ref double x1, ref double y1)
        {
            double dx = x1 - x0, dy = y1 - y0, lo = 0, hi = 1;
            if (!Bound(-dx, x0 + 1, ref lo, ref hi)
                || !Bound(dx, 1 - x0, ref lo, ref hi)
                || !Bound(-dy, y0 + 1, ref lo, ref hi)
                || !Bound(dy, 1 - y0, ref lo, ref hi))
                return false;
            x1 = x0 + hi * dx;
            y1 = y0 + hi * dy;
            x0 += lo * dx;
            y0 += lo * dy;
            return true;
        }

        private static bool Bound(double p, double q, ref double lo, ref double hi)
        {
            if (Math.Abs(p) < 1e-12)
                return q >= 0;
            double r = q / p;
            if (p < 0)
            {
                if (r > hi)
                    return false;
                lo = Math.Max(lo, r);
            }
            else
            {
                if (r < lo)
                    return false;
                hi = Math.Min(hi, r);
            }

            return true;
        }
    }
}

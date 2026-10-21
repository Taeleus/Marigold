using System;
using System.Collections.Generic;

namespace Marigold
{
    internal enum ChangeKind : byte { Hull, Missing, Damaged, Replaced, Added, Unknown, NonFunctional }
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
        public readonly List<BlockChange> Changes=new List<BlockChange>();
        public readonly int[] Counts=new int[7];
        public void Add(Box3 bounds,ChangeKind kind,string definition) { Changes.Add(new BlockChange { Bounds=bounds,Kind=kind,Definition=definition }); Counts[(int)kind]++; }
        public string Summary { get { return Counts[1]+" missing, "+Counts[2]+" damaged/degraded, "+Counts[6]+" non-functional, "+Counts[3]+" replaced, "+Counts[4]+" added, "+Counts[5]+" unknown"; } }
    }
    internal static class SnapshotComparison
    {
        public static Comparison Compare(BlockSample[] reference,BlockSample[] current)
        {
            var result=new Comparison();
            var observed=new Dictionary<Point3,BlockSample>();
            foreach(var block in current) observed.Add(block.Position,block);
            foreach(var old in reference)
            {
                BlockSample now;
                if(!observed.TryGetValue(old.Position,out now))
                { result.Add(old.Bounds,ChangeKind.Missing,old.Definition); continue; }
                observed.Remove(old.Position);
                if(old.Definition!=now.Definition || old.Forward!=now.Forward || old.Up!=now.Up ||
                    !old.Bounds.Min.Equals(now.Bounds.Min) || !old.Bounds.Max.Equals(now.Bounds.Max))
                    result.Add(SchematicGeometry.Union(old.Bounds,now.Bounds),ChangeKind.Replaced,now.Definition);
                else if(!Valid(now) || !Valid(old)) result.Add(now.Bounds,ChangeKind.Unknown,now.Definition);
                else if(now.HasFunctionalState && !now.IsFunctional) result.Add(now.Bounds,ChangeKind.NonFunctional,now.Definition);
                else
                {
                    double tolerance=Math.Max(0.001,now.MaxIntegrity*0.0001);
                    if(now.Integrity+tolerance<old.Integrity || now.BuildIntegrity+tolerance<old.BuildIntegrity ||
                        now.Integrity+tolerance<now.BuildIntegrity) result.Add(now.Bounds,ChangeKind.Damaged,now.Definition);
                }
            }
            foreach(var pair in observed) result.Add(pair.Value.Bounds,ChangeKind.Added,pair.Value.Definition);
            result.Changes.Sort((a,b)=>
            {
                int c=Priority(a.Kind).CompareTo(Priority(b.Kind)); if(c!=0)return c;
                c=a.Bounds.Min.X.CompareTo(b.Bounds.Min.X);if(c!=0)return c;
                c=a.Bounds.Min.Y.CompareTo(b.Bounds.Min.Y);return c!=0?c:a.Bounds.Min.Z.CompareTo(b.Bounds.Min.Z);
            });
            return result;
        }
        private static int Priority(ChangeKind kind) { return kind==ChangeKind.NonFunctional?2:kind>=ChangeKind.Damaged?(int)kind+1:(int)kind; }
        private static bool Valid(BlockSample b)
        { return b.MaxIntegrity>0 && Finite(b.MaxIntegrity) && Finite(b.Integrity) && Finite(b.BuildIntegrity); }
        private static bool Finite(double n) { return !double.IsNaN(n) && !double.IsInfinity(n); }
    }
}

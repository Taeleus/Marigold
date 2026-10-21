using System;
using System.Collections.Generic;
namespace Marigold
{
    internal static class ProfileSuggestions
    {
        // Jaccard-like structural overlap ignores health, names and runtime entity IDs.
        // Deliberately never selects a profile: identical ships cannot prove identity.
        public static double Score(BlockSample[] baseline,BlockSample[] current)
        {
            if(baseline==null || current==null || baseline.Length==0 || current.Length==0)return 0;
            var positions=new Dictionary<Point3,BlockSample>();foreach(var b in baseline)positions[b.Position]=b;
            int matched=0;
            foreach(var b in current)
            {
                BlockSample a;
                if(positions.TryGetValue(b.Position,out a) && a.Definition==b.Definition && a.Forward==b.Forward && a.Up==b.Up && a.Bounds.Min.Equals(b.Bounds.Min) && a.Bounds.Max.Equals(b.Bounds.Max))matched++;
            }
            return (double)matched/(baseline.Length+current.Length-matched);
        }
    }
}

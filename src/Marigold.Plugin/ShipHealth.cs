using System.Collections.Generic;
namespace Marigold
{
    // Baseline block counts, not summed integrity or a combat-survivability estimate.
    internal sealed class ShipHealth
    {
        public static readonly string[] Names={"Overall","Armor","Power","Thrust","Utility","Cargo","Offensive"};
        // Blue, yellow, orange, red, gray. Replacements count as an absent expected block.
        public readonly int[,] Counts=new int[Names.Length,5];
        public bool HasBaseline;
        public int Total(int group){int total=0;for(int i=0;i<5;i++)total+=Counts[group,i];return total;}
        public static int Group(string definition)
        {
            switch(DisplayFilter.Category(definition)){case "armor":return 1;case "power":return 2;case "thrusters":return 3;case "cargo":return 5;case "weapons":return 6;default:return 4;}
        }
        public static ShipHealth Build(BlockSample[] reference,BlockSample[] current)
        {
            var result=new ShipHealth {HasBaseline=reference!=null};if(reference==null)return result;
            var live=new Dictionary<Point3,BlockSample>();foreach(var b in current)live[b.Position]=b;
            foreach(var expected in reference)
            {
                BlockSample block;var kind=SnapshotComparison.Classify(expected,live.TryGetValue(expected.Position,out block)?(BlockSample?)block:null);
                int state=kind==ChangeKind.Hull?0:kind==ChangeKind.Damaged?1:kind==ChangeKind.NonFunctional?2:kind==ChangeKind.Unknown?4:3;
                result.Counts[0,state]++;result.Counts[Group(expected.Definition),state]++;
            }
            return result;
        }
    }
}

using System.Collections.Generic;
namespace Marigold
{
    // Baseline block counts, not summed integrity or a combat-survivability estimate.
    internal sealed class ShipHealth
    {
        public static readonly string[] Names={"Overall","Armor","Power","Thrusters","Utility","Cargo","Offensive","Tools"};
        // Blue, yellow, orange, red, gray. Replacements count as an absent expected block.
        public readonly int[,] Counts=new int[Names.Length,5];
        public bool HasBaseline;
        public static readonly string[] PowerNames={"Reactors","Batteries","Solar","Other"};
        public readonly int[,] PowerCounts=new int[4,5];
        public static int PowerType(string definition)
        {
            string type=(definition??"").Split('/')[0].ToLowerInvariant();
            return type.Contains("reactor")?0:type.Contains("battery")?1:type.Contains("solar")?2:3;
        }
        public int[,] PowerSections(int mask)
        {
            var counts=new int[4,5];
            for(int type=0;type<4;type++)for(int state=0;state<5;state++)
                counts[type<3 && (mask & (1<<type))!=0?type:3,state]+=PowerCounts[type,state];
            return counts;
        }
        public int Total(int group){int total=0;for(int i=0;i<5;i++)total+=Counts[group,i];return total;}
        public static int Group(string definition)
        {
            switch(DisplayFilter.Category(definition)){case "armor":return 1;case "power":return 2;case "thrusters":return 3;case "cargo":return 5;case "weapons":return 6;case "tools":return 7;default:return 4;}
        }
        public static ShipHealth Build(BlockSample[] reference,BlockSample[] current)
        {
            var result=new ShipHealth {HasBaseline=reference!=null};if(reference==null)return result;
            var live=new Dictionary<Point3,BlockSample>();foreach(var b in current)live[b.Position]=b;
            foreach(var expected in reference)
            {
                BlockSample block;var kind=SnapshotComparison.Classify(expected,live.TryGetValue(expected.Position,out block)?(BlockSample?)block:null);
                int state=kind==ChangeKind.Hull?0:kind==ChangeKind.Damaged?1:kind==ChangeKind.NonFunctional?2:kind==ChangeKind.Unknown?4:3;
                result.Counts[0,state]++;int group=Group(expected.Definition);result.Counts[group,state]++;
                if(group==2)result.PowerCounts[PowerType(expected.Definition),state]++;
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
namespace Marigold
{
    internal sealed class RecoveryCandidate
    {
        public AnchorFrame Anchor;
        public int Matches, Observed, Expected;
        public string Summary { get { return Matches+" matching blocks / "+Observed+" observed / "+Expected+" expected ("+(Observed==0?0:100.0*Matches/Observed).ToString("F1")+"% of observed)."; } }
    }
    internal static class RecoveryMatcher
    {
        private static readonly Point3[] Axes={new Point3(1,0,0),new Point3(-1,0,0),new Point3(0,1,0),new Point3(0,-1,0),new Point3(0,0,1),new Point3(0,0,-1)};
        private static string Signature(BlockSample b){return b.Definition+"|"+b.Forward+"|"+b.Up;}
        public static int Score(BlockSample[] baseline,BlockSample[] current)
        {
            var live=new Dictionary<Point3,BlockSample>();foreach(var b in current)live[b.Position]=b;
            int count=0;foreach(var b in baseline){BlockSample now;if(live.TryGetValue(b.Position,out now) && now.Definition==b.Definition && now.Forward==b.Forward && now.Up==b.Up && now.Bounds.Min.Equals(b.Bounds.Min) && now.Bounds.Max.Equals(b.Bounds.Max))count++;}return count;
        }
        // Quarter turns about the selected baseline origin preserve the block lattice.
        internal static AnchorFrame Rotate(AnchorFrame frame,string axis)
        {
            Func<Point3,Point3> turn=p=>axis=="x"?new Point3(p.X,-p.Z,p.Y):axis=="y"?new Point3(p.Z,p.Y,-p.X):new Point3(-p.Y,p.X,p.Z);
            if(axis!="x" && axis!="y" && axis!="z")throw new ArgumentException("Choose X, Y or Z.");
            frame.Right=turn(frame.Right);frame.Up=turn(frame.Up);frame.Backward=turn(frame.Backward);return frame;
        }
        private static Point3 Shift(Point3 p,Point3 d){return new Point3(p.X+d.X,p.Y+d.Y,p.Z+d.Z);}
        private static int ScoreShift(BlockSample[] baseline,Dictionary<Point3,BlockSample> live,Point3 delta,CancellationToken token)
        {
            int count=0;for(int i=0;i<baseline.Length;i++){if((i&2047)==0)token.ThrowIfCancellationRequested();var b=baseline[i];BlockSample now;
                if(live.TryGetValue(Shift(b.Position,delta),out now) && now.Definition==b.Definition && now.Forward==b.Forward && now.Up==b.Up && now.Bounds.Min.Equals(Shift(b.Bounds.Min,delta)) && now.Bounds.Max.Equals(Shift(b.Bounds.Max,delta)))count++;}
            return count;
        }
        public static List<RecoveryCandidate> Find(ShipProfile profile,BlockSample[] current,CancellationToken token)
        {
            var result=new List<RecoveryCandidate>();if(profile.Blocks==null || current.Length<3)return result;
            var live=new Dictionary<Point3,BlockSample>();foreach(var block in current)live[block.Position]=block;
            var frequencies=new Dictionary<string,int>();foreach(var b in profile.Blocks){int n;frequencies.TryGetValue(b.Definition,out n);frequencies[b.Definition]=n+1;}
            var seeds=new List<BlockSample>();foreach(var b in current)if(frequencies.ContainsKey(b.Definition))seeds.Add(b);
            seeds.Sort((a,b)=>{int c=frequencies[a.Definition].CompareTo(frequencies[b.Definition]);if(c!=0)return c;c=a.Position.X.CompareTo(b.Position.X);if(c!=0)return c;c=a.Position.Y.CompareTo(b.Position.Y);return c!=0?c:a.Position.Z.CompareTo(b.Position.Z);});
            if(seeds.Count>64)seeds.RemoveRange(64,seeds.Count-64);
            foreach(var right in Axes)foreach(var up in Axes)
            {
                if(right.X*up.X+right.Y*up.Y+right.Z*up.Z!=0)continue;token.ThrowIfCancellationRequested();
                var frame=profile.Anchor;frame.Position=new Point3();frame.Right=right;frame.Up=up;frame.Backward=new Point3(right.Y*up.Z-right.Z*up.Y,right.Z*up.X-right.X*up.Z,right.X*up.Y-right.Y*up.X);
                var rotated=ProfileAlignment.Transform(profile.Blocks,frame,false);
                var groups=new Dictionary<string,List<Point3>>();foreach(var b in rotated){string key=Signature(b);List<Point3> group;if(!groups.TryGetValue(key,out group))groups[key]=group=new List<Point3>();if(group.Count<256)group.Add(b.Position);}
                var votes=new Dictionary<Point3,int>();foreach(var seed in seeds){List<Point3> group;if(!groups.TryGetValue(Signature(seed),out group))continue;foreach(var p in group){var delta=new Point3(seed.Position.X-p.X,seed.Position.Y-p.Y,seed.Position.Z-p.Z);int n;votes.TryGetValue(delta,out n);votes[delta]=n+1;}}
                var ranked=new List<KeyValuePair<Point3,int>>(votes);ranked.Sort((a,b)=>b.Value.CompareTo(a.Value));
                for(int i=0;i<Math.Min(4,ranked.Count);i++)
                {
                    token.ThrowIfCancellationRequested();var candidate=frame;candidate.Position=ranked[i].Key;
                    int score=ScoreShift(rotated,live,candidate.Position,token);
                    if(score>=3)result.Add(new RecoveryCandidate {Anchor=candidate,Matches=score,Observed=current.Length,Expected=profile.Blocks.Length});
                }
            }
            result.Sort((a,b)=>b.Matches.CompareTo(a.Matches));if(result.Count>8)result.RemoveRange(8,result.Count-8);return result;
        }
    }
}

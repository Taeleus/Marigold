using System;
using System.Collections.Generic;
namespace Marigold
{
    internal sealed class AutoMatch
    {
        public string ProfileId,Message;
        public int AnchorIndex;
        public double Score;
        public AnchorFrame BindingAnchor;public bool Recovered;
    }
    internal static class AutoMatcher
    {
        public static AutoMatch Find(BlockSample[] current,AnchorFrame[] anchors,IList<ShipProfile> profiles,double gridSize,IList<ShipProfile> recovered=null)
        {
            var result=new AutoMatch {Message="No sufficiently close saved layout. Select a profile or preview recovery."};
            if(current.Length<8){result.Message="Automatic matching needs at least 8 observed blocks.";return result;}
            if(anchors.Length>16){result.Message="More than 16 cockpits; select a profile manually.";return result;}
            var matches=new List<AutoMatch>();
            Action<AutoMatch> add=candidate=>{foreach(var previous in matches)if(previous.ProfileId==candidate.ProfileId && previous.AnchorIndex==candidate.AnchorIndex && ReferenceHistory.SameAnchor(previous.BindingAnchor,candidate.BindingAnchor)){if(candidate.Score>previous.Score){previous.Score=candidate.Score;previous.AnchorIndex=candidate.AnchorIndex;previous.Recovered=candidate.Recovered;}return;}matches.Add(candidate);};
            for(int i=0;i<anchors.Length;i++)
            {
                var local=ProfileAlignment.Transform(current,anchors[i],true);
                foreach(var p in profiles)
                {
                    if(p.Blocks==null || p.Blocks.Length<8 || Math.Abs(p.GridSize-gridSize)>.001)continue;
                    if(p.Anchor.Definition==anchors[i].Definition)add(new AutoMatch{ProfileId=p.Id,AnchorIndex=i,BindingAnchor=anchors[i],Score=Score(p.Blocks,local)});
                    if(recovered==null)continue;
                    foreach(var saved in recovered)
                    {
                        if(saved.Id!=p.Id || saved.ReferenceId!=p.ReferenceId || saved.Anchor.Definition!=anchors[i].Definition || saved.Blocks==null || saved.Blocks.Length<8 || Math.Abs(saved.GridSize-gridSize)>.001)continue;
                        var baselineInSeat=ProfileAlignment.Transform(p.Blocks,saved.Anchor,false);
                        double score=Math.Max(Score(saved.Blocks,local),Score(baselineInSeat,local));
                        add(new AutoMatch{ProfileId=p.Id,AnchorIndex=i,BindingAnchor=ReferenceHistory.Compose(saved.Anchor,anchors[i]),Score=score,Recovered=true});
                    }
                }
            }
            matches.Sort((a,b)=>b.Score.CompareTo(a.Score));
            if(matches.Count==0 || matches[0].Score<.90)return result;
            if(matches.Count>1 && matches[0].Score-matches[1].Score<.08){result.Message="Ambiguous profiles or cockpit alignment. Select manually.";return result;}
            result=matches[0];result.Message="Unique layout match; waiting for stable observations.";return result;
        }
        private static double Score(BlockSample[] expected,BlockSample[] current)
        {return (double)Math.Min(expected.Length,current.Length)/Math.Max(expected.Length,current.Length)<.82?0:ProfileSuggestions.Score(expected,current);}
    }

    internal sealed class MatchStability
    {
        private string key;private BlockSample[] sample;
        public void Reset(){key=null;sample=null;}
        public bool Observe(string candidate,BlockSample[] current)
        {
            bool stable=candidate!=null && candidate==key && ObservationCache.Same(sample,current);
            key=candidate;sample=current;return stable;
        }
    }
}

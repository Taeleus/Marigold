using System;
using System.Collections.Generic;
namespace Marigold
{
    internal sealed class AutoMatch
    {
        public string ProfileId,Message;
        public int AnchorIndex;
        public double Score;
    }
    internal static class AutoMatcher
    {
        public static AutoMatch Find(BlockSample[] current,AnchorFrame[] anchors,IList<ShipProfile> profiles,double gridSize)
        {
            var result=new AutoMatch { Message="No sufficiently close saved layout. Use /mg profile suggest or select ID." };
            if(current.Length<8){result.Message="Automatic matching needs at least 8 observed blocks.";return result;}
            if(anchors.Length>16){result.Message="More than 16 cockpits; select a profile manually.";return result;}
            double best=0,second=0;string id=null;int index=0;
            for(int i=0;i<anchors.Length;i++)
            {
                bool compatible=false;
                foreach(var p in profiles)if(p.Blocks!=null && p.Blocks.Length>=8 && p.Anchor.Definition==anchors[i].Definition && Math.Abs(p.GridSize-gridSize)<.001 && (double)Math.Min(p.Blocks.Length,current.Length)/Math.Max(p.Blocks.Length,current.Length)>=.82){compatible=true;break;}
                if(!compatible)continue;
                var local=ProfileAlignment.Transform(current,anchors[i],true);
                foreach(var p in profiles)
                {
                    if(p.Blocks==null || p.Blocks.Length<8 || p.Anchor.Definition!=anchors[i].Definition || Math.Abs(p.GridSize-gridSize)>.001)continue;
                    double score=ProfileSuggestions.Score(p.Blocks,local);
                    if(score>best){second=best;best=score;id=p.Id;index=i;}else if(score>second)second=score;
                }
            }
            if(best<.90)return result;
            if(best-second<.08){result.Message="Ambiguous profiles or cockpit alignment. Use /mg profile select ID.";return result;}
            result.ProfileId=id;result.AnchorIndex=index;result.Score=best;result.Message="Unique layout match; waiting for stable observations.";return result;
        }
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

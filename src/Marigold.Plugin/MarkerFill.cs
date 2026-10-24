using System;
using System.Collections.Generic;
namespace Marigold
{
    internal struct FillBand
    {
        public double X,Y,Width,Height;
        public int MarkerStart;
    }
    internal static class MarkerFill
    {
        public const int Budget=4096;
        internal const int RegionsPerState=1024;
        internal static List<BlockChange> Regions(IList<BlockChange> changes,ChangeKind kind)
        {
            var selected=new List<BlockChange>();foreach(var c in changes)if(c.Kind==kind)selected.Add(c);
            if(selected.Count<=RegionsPerState)return selected;
            var bounds=selected[0].Bounds;foreach(var c in selected)bounds=SchematicGeometry.Union(bounds,c.Bounds);
            for(double size=1;;size*=2)
            {
                var groups=new Dictionary<Point3,Box3>();
                foreach(var c in selected)
                {
                    var b=c.Bounds;var key=new Point3(Math.Floor(((b.Min.X+b.Max.X)/2-bounds.Min.X)/size),Math.Floor(((b.Min.Y+b.Max.Y)/2-bounds.Min.Y)/size),Math.Floor(((b.Min.Z+b.Max.Z)/2-bounds.Min.Z)/size));
                    Box3 old;groups[key]=groups.TryGetValue(key,out old)?SchematicGeometry.Union(old,b):b;
                }
                if(groups.Count>RegionsPerState)continue;
                var result=new List<BlockChange>();foreach(var b in groups.Values)result.Add(new BlockChange{Kind=kind,Bounds=b});return result;
            }
        }
        // Fill the projected silhouette once, rather than alpha-blending six overlapping faces.
        public static void Build(Point3[] projected,int[] starts,double zoom,double pixels,List<FillBand> bands)
        {
            bands.Clear();
            if(starts.Length==0 || pixels<=0)return;
            double totalHeight=0;
            foreach(int start in starts)
            {
                double lo,hi; Bounds(projected,start,zoom,out lo,out hi);
                totalHeight+=Math.Max(0,hi-lo);
            }
            double step=Math.Max(2/pixels,totalHeight/Math.Max(1,Budget-starts.Length));
            foreach(int start in starts)
            {
                double lo,hi; Bounds(projected,start,zoom,out lo,out hi);
                for(double y=lo;y<hi && bands.Count<Budget;y+=step)
                {
                    double height=Math.Min(step,hi-y),middle=y+height/2;
                    double left=double.PositiveInfinity,right=double.NegativeInfinity;
                    for(int e=0;e<24;e+=2)
                    {
                        var a=projected[start+e];var b=projected[start+e+1];
                        double ay=-a.Y*zoom,by=-b.Y*zoom;
                        if(Math.Abs(ay-by)<1e-12 || middle<Math.Min(ay,by) || middle>Math.Max(ay,by))continue;
                        double x=(a.X+(b.X-a.X)*(middle-ay)/(by-ay))*zoom;
                        left=Math.Min(left,x);right=Math.Max(right,x);
                    }
                    left=Math.Max(-1,left);right=Math.Min(1,right);
                    if(right>left)bands.Add(new FillBand { MarkerStart=start,X=left,Y=y,Width=right-left,Height=height });
                }
            }
        }
        private static void Bounds(Point3[] points,int start,double zoom,out double lo,out double hi)
        {
            lo=double.PositiveInfinity;hi=double.NegativeInfinity;
            for(int i=0;i<24;i++) { double y=-points[start+i].Y*zoom;lo=Math.Min(lo,y);hi=Math.Max(hi,y); }
            lo=Math.Max(-1,lo);hi=Math.Min(1,hi);
        }
    }
}

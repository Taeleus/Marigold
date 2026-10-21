using System;
using System.Collections.Generic;
using Marigold;
internal static class FillChecks
{
    public static void Run()
    {
        var corners=new Point3[8];
        for(int i=0;i<8;i++)corners[i]=new Point3((i&1)==0?-.5:.5,(i&2)==0?-.5:.5,(i&4)==0?-.5:.5);
        var edges=new List<Point3>();
        for(int i=0;i<8;i++)for(int axis=1;axis<=4;axis*=2)if((i&axis)==0){edges.Add(corners[i]);edges.Add(corners[i|axis]);}
        var bands=new List<FillBand>();
        MarkerFill.Build(edges.ToArray(),new[]{0},1,400,bands);
        double area=0;
        foreach(var band in bands)area+=band.Width*band.Height;
        if(Math.Abs(area-1)>1e-9)throw new Exception("Face-on fill must cover projected square exactly");
        var rotated=edges.ToArray();
        for(int i=0;i<rotated.Length;i++)rotated[i]=SchematicGeometry.Rotate(rotated[i],.6,-.4);
        MarkerFill.Build(rotated,new[]{0},3,400,bands);
        foreach(var band in bands)
            if(band.X < -1.000001 || band.Y < -1.000001 || band.X+band.Width>1.000001 || band.Y+band.Height>1.000001)
                throw new Exception("Fill escapes panel");
        var many=new List<Point3>();var starts=new int[128];
        for(int i=0;i<starts.Length;i++){starts[i]=many.Count;many.AddRange(rotated);}
        MarkerFill.Build(many.ToArray(),starts,3,2160,bands);
        if(bands.Count==0 || bands.Count>MarkerFill.Budget)throw new Exception("Fill budget violated");
        MarkerFill.Build(rotated,new int[0],1,400,bands);
        if(bands.Count!=0)throw new Exception("Old fill was retained");
        for(int i=0;i<rotated.Length;i++)rotated[i].Y+=20;
        MarkerFill.Build(rotated,new[]{0},1,400,bands);
        if(bands.Count!=0)throw new Exception("Offscreen marker shaded");
        Console.WriteLine("PASS: 5 marker fill checks (coverage, clipping, budget, clearing, offscreen).");
    }
}

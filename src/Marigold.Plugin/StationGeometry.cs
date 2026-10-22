using System;
using System.Collections.Generic;
namespace Marigold
{
    internal sealed class StationGrid
    {
        public BlockSample[] Blocks, Baseline=null;
        public AnchorFrame[] Anchors=null;
        public ShipProfile[] Profiles=null;
        public double GridSize=0;
        public long EntityId=0;
        public string MatchedProfile=null;
        public string Name=null;
        // Affine transform from this grid's cells into the controlled grid's cells.
        public Point3 Origin,X,Y,Z;
        public Point3 Transform(Point3 p){return new Point3(Origin.X+p.X*X.X+p.Y*Y.X+p.Z*Z.X,Origin.Y+p.X*X.Y+p.Y*Y.Y+p.Z*Z.Y,Origin.Z+p.X*X.Z+p.Y*Y.Z+p.Z*Z.Z);}
        public Box3 Bounds(Box3 box)
        {
            Box3? result=null;
            for(int i=0;i<8;i++){var p=Transform(new Point3((i&1)==0?box.Min.X:box.Max.X,(i&2)==0?box.Min.Y:box.Max.Y,(i&4)==0?box.Min.Z:box.Max.Z));var b=new Box3(p,p);result=result.HasValue?SchematicGeometry.Union(result.Value,b):b;}
            return result.Value;
        }
    }
    internal static class StationGeometry
    {
        public static double Radius(Box3 frame){double x=frame.Max.X-frame.Min.X,y=frame.Max.Y-frame.Min.Y,z=frame.Max.Z-frame.Min.Z;return Math.Max(.5,Math.Sqrt(x*x+y*y+z*z)/2);}
        public static Point3 Center(Box3 frame){return new Point3((frame.Min.X+frame.Max.X)/2,(frame.Min.Y+frame.Max.Y)/2,(frame.Min.Z+frame.Max.Z)/2);}
        public static Point3[] Build(StationGrid grid,string category,Box3 frame,IList<Box3> overrideBoxes=null)
        {
            var boxes=new List<Box3>();Box3? local=null;
            foreach(var b in grid.Blocks){local=local.HasValue?SchematicGeometry.Union(local.Value,b.Bounds):b.Bounds;if(DisplayFilter.CategoryMatches(b.Definition,category))boxes.Add(b.Bounds);}
            if(overrideBoxes!=null){boxes=new List<Box3>(overrideBoxes);foreach(var box in boxes)local=local.HasValue?SchematicGeometry.Union(local.Value,box):box;}
            if(!local.HasValue)return new Point3[0];
            var points=OutlineGeometry.Build(boxes,0,local).Points;var center=Center(local.Value);double radius=Radius(local.Value),targetRadius=Radius(frame);var target=Center(frame);
            for(int i=0;i<points.Length;i++)
            {var p=points[i];p=grid.Transform(new Point3(center.X+p.X*radius,center.Y+p.Y*radius,center.Z+p.Z*radius));points[i]=new Point3((p.X-target.X)/targetRadius,(p.Y-target.Y)/targetRadius,(p.Z-target.Z)/targetRadius);}
            return points;
        }
    }
}

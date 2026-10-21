using System;

namespace Marigold
{
    internal struct AnchorFrame
    {
        public Point3 Position, Right, Up, Backward;
        public string Definition, Name;
        public Point3 Local(Point3 p)
        { return SchematicGeometry.ToBasis(new Point3(p.X-Position.X,p.Y-Position.Y,p.Z-Position.Z),Right,Up,Backward); }
        public Point3 World(Point3 p)
        { var v=Vector(p); return new Point3(v.X+Position.X,v.Y+Position.Y,v.Z+Position.Z); }
        public Point3 Vector(Point3 p)
        { return new Point3(p.X*Right.X+p.Y*Up.X+p.Z*Backward.X,p.X*Right.Y+p.Y*Up.Y+p.Z*Backward.Y,p.X*Right.Z+p.Y*Up.Z+p.Z*Backward.Z); }
    }
    internal sealed class ShipProfile
    {
        public string Id, Name;
        public double GridSize;
        public AnchorFrame Anchor;
        // Baseline coordinates and orientations are relative to Anchor, not world/grid IDs.
        public BlockSample[] Blocks;
    }
    internal static class ProfileAlignment
    {
        private static readonly Point3[] Directions={new Point3(0,0,-1),new Point3(0,0,1),new Point3(-1,0,0),new Point3(1,0,0),new Point3(0,1,0),new Point3(0,-1,0)};
        public static BlockSample[] Transform(BlockSample[] blocks,AnchorFrame anchor,bool toLocal)
        {
            if(blocks==null)return null;
            var result=new BlockSample[blocks.Length];
            for(int i=0;i<blocks.Length;i++)
            {
                var block=blocks[i];
                var min=new Point3(double.PositiveInfinity,double.PositiveInfinity,double.PositiveInfinity);
                var max=new Point3(double.NegativeInfinity,double.NegativeInfinity,double.NegativeInfinity);
                for(int corner=0;corner<8;corner++)
                {
                    var p=new Point3((corner&1)==0?block.Bounds.Min.X:block.Bounds.Max.X,(corner&2)==0?block.Bounds.Min.Y:block.Bounds.Max.Y,(corner&4)==0?block.Bounds.Min.Z:block.Bounds.Max.Z);
                    p=toLocal?anchor.Local(p):anchor.World(p);
                    min=new Point3(Math.Min(min.X,p.X),Math.Min(min.Y,p.Y),Math.Min(min.Z,p.Z));
                    max=new Point3(Math.Max(max.X,p.X),Math.Max(max.Y,p.Y),Math.Max(max.Z,p.Z));
                }
                block.Position=toLocal?anchor.Local(block.Position):anchor.World(block.Position);
                block.Bounds=new Box3(min,max);
                block.Forward=Direction(toLocal?SchematicGeometry.ToBasis(Directions[block.Forward],anchor.Right,anchor.Up,anchor.Backward):anchor.Vector(Directions[block.Forward]));
                block.Up=Direction(toLocal?SchematicGeometry.ToBasis(Directions[block.Up],anchor.Right,anchor.Up,anchor.Backward):anchor.Vector(Directions[block.Up]));
                result[i]=block;
            }
            return result;
        }
        private static int Direction(Point3 p)
        {
            for(int i=0;i<Directions.Length;i++)if(p.Equals(Directions[i]))return i;
            throw new InvalidOperationException("Anchor orientation must follow grid axes.");
        }
        public static void Validate(ShipProfile profile)
        {
            Guid id;
            if(!Finite(profile.GridSize) || profile.GridSize<=0)throw new InvalidOperationException("Invalid grid size.");
            if(!Guid.TryParseExact(profile.Id,"N",out id) || string.IsNullOrWhiteSpace(profile.Name) || string.IsNullOrEmpty(profile.Anchor.Definition))throw new InvalidOperationException("Invalid profile identity.");
            var a=profile.Anchor;
            Position(a.Position,false);
            Direction(a.Right);Direction(a.Up);Direction(a.Backward);
            var cross=new Point3(a.Right.Y*a.Up.Z-a.Right.Z*a.Up.Y,a.Right.Z*a.Up.X-a.Right.X*a.Up.Z,a.Right.X*a.Up.Y-a.Right.Y*a.Up.X);
            if(!cross.Equals(a.Backward))throw new InvalidOperationException("Invalid anchor axes.");
            if(profile.Blocks==null)return;
            var seen=new System.Collections.Generic.HashSet<Point3>();
            foreach(var b in profile.Blocks)
            {
                Position(b.Position,false);Position(b.Bounds.Min,true);Position(b.Bounds.Max,true);
                if(!seen.Add(b.Position) || string.IsNullOrEmpty(b.Definition) || b.Forward<0 || b.Forward>5 || b.Up<0 || b.Up>5 ||
                    Math.Abs(Dot(Directions[b.Forward],Directions[b.Up]))>.01 ||
                    b.Bounds.Min.X>=b.Bounds.Max.X || b.Bounds.Min.Y>=b.Bounds.Max.Y || b.Bounds.Min.Z>=b.Bounds.Max.Z ||
                    !Finite(b.Integrity) || !Finite(b.BuildIntegrity) || !Finite(b.MaxIntegrity) || b.MaxIntegrity<=0)
                    throw new InvalidOperationException("Invalid baseline block.");
            }
        }
        private static double Dot(Point3 a,Point3 b){return a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
        private static bool Finite(double x){return !double.IsNaN(x) && !double.IsInfinity(x);}
        private static void Position(Point3 p,bool half)
        {
            foreach(double x in new[]{p.X,p.Y,p.Z})if(!Finite(x) || Math.Abs(x)>10000000 || x*(half?2:1)!=Math.Round(x*(half?2:1)))throw new InvalidOperationException("Invalid grid coordinate.");
        }
    }
}

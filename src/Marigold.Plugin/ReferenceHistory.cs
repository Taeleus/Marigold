using System;
namespace Marigold
{
    internal static class ReferenceHistory
    {
        public static bool SameAnchor(AnchorFrame a,AnchorFrame b)
        {return a.Position.Equals(b.Position) && a.Right.Equals(b.Right) && a.Up.Equals(b.Up) && a.Backward.Equals(b.Backward);}
        public static void Archive(ProfileStore library,ShipProfile profile)
        {
            if(profile.Blocks==null)return;
            foreach(var p in library.Profiles)
                if(p.ReferenceId==profile.ReferenceId && SameAnchor(p.Anchor,profile.Anchor) && ObservationCache.Same(p.Blocks,profile.Blocks))return;
            var copy=profile.Copy();copy.Id=Guid.NewGuid().ToString("N");copy.Name=string.IsNullOrWhiteSpace(profile.ReferenceName)?"Previous baseline":profile.ReferenceName;
            library.Put(copy);
        }
        public static ShipProfile SaveNamed(ProfileStore library,ShipProfile profile,string name)
        {
            if(profile.Blocks==null)throw new InvalidOperationException("No baseline to save.");
            var copy=profile.Copy();copy.Id=Guid.NewGuid().ToString("N");copy.ReferenceId=copy.Id;copy.ReferenceName=name;copy.Name=name;
            library.Put(copy);return copy;
        }
        // Store the baseline frame relative to the recovery seat, not a world/grid ID.
        public static AnchorFrame Relative(AnchorFrame frame,AnchorFrame seat)
        {return new AnchorFrame{Position=seat.Local(frame.Position),Right=SchematicGeometry.ToBasis(frame.Right,seat.Right,seat.Up,seat.Backward),Up=SchematicGeometry.ToBasis(frame.Up,seat.Right,seat.Up,seat.Backward),Backward=SchematicGeometry.ToBasis(frame.Backward,seat.Right,seat.Up,seat.Backward),Definition=seat.Definition,Name=seat.Name};}
        public static AnchorFrame Compose(AnchorFrame relative,AnchorFrame seat)
        {return new AnchorFrame{Position=seat.World(relative.Position),Right=seat.Vector(relative.Right),Up=seat.Vector(relative.Up),Backward=seat.Vector(relative.Backward),Definition=relative.Definition,Name=relative.Name};}
    }
}

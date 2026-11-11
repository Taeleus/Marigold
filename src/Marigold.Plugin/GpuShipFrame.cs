using VRageMath;

namespace Marigold
{
    // Published to the render thread after construction. The mesh and overlay arrays are never mutated.
    internal sealed class GpuShipFrame
    {
        public readonly TexturedShipMesh Mesh;
        public readonly TexturedShipFilter Filter;
        public readonly byte[] PartAlphas;
        public readonly Box3 Bounds;
        public readonly ShipProjection View;
        public readonly int Size;
        public readonly float ShipOpacity, DepthTolerance;
        public readonly GpuShipOverlayVertex[] Fills, Lines;

        public GpuShipFrame(TexturedShipMesh mesh, TexturedShipFilter filter, byte[] partAlphas, Box3 bounds,
            ShipProjection view, int size,
            float shipOpacity, float depthTolerance, GpuShipOverlayVertex[] fills, GpuShipOverlayVertex[] lines)
        {
            Mesh = mesh;
            Filter = filter;
            PartAlphas = partAlphas;
            Bounds = bounds;
            View = view;
            Size = size;
            ShipOpacity = shipOpacity;
            DepthTolerance = depthTolerance;
            Fills = fills;
            Lines = lines;
        }

        // Sprite messages can outlive the ship or its layout. A small fixed set of sizes
        // lets the game keep each named HUD texture registered for the device's lifetime.
        public static int OutputSize(int requested)
        {
            if (requested <= 384)
                return 384;
            if (requested <= 768)
                return 768;
            if (requested <= 1024)
                return 1024;
            return 1536;
        }
    }

    internal readonly struct GpuShipOverlayVertex
    {
        public readonly Vector3 Position;
        public readonly uint ColorRgba;
        // 0 bypasses hull depth, 1 dims covered pixels, 2 also clips pixels outside the hull.
        public readonly float DepthAware;

        public GpuShipOverlayVertex(Point3 point, uint colorRgba, bool depthAware, bool requireHull = false)
        {
            Position = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
            ColorRgba = colorRgba;
            DepthAware = requireHull ? 2 : depthAware ? 1 : 0;
        }
    }
}

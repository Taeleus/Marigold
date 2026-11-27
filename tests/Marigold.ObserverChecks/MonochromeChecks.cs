using System;

namespace Marigold
{
    // A triangle with a material path exercises the production raster without game assets.
    internal class TexturedShipMesh
    {
        internal class ModelPart { public Point3? BlockPosition { get; set; } }
        public ModelPart[] Parts = { new ModelPart() };
        public (Point3 Position, VRageMath.Vector2 Uv)[] Vertices =
        {
            (new Point3(-1,-1,0), default), (new Point3(1,-1,0), default), (new Point3(0,1,0), default)
        };
        public (int A, int B, int C, int Part, string ColorTexture, uint PaintRgb)[] Triangles =
            { (0, 1, 2, 0, "material.dds", 0xffffff) };
        public int ReducedModels => 0;
        public int SimplifiedModels => 0;
        public string SimplificationReason => null;
    }

    internal class TexturedShipTexture
    {
        public static int Loads;
        public static bool TryLoad(string path, out TexturedShipTexture texture, int size)
        {
            Loads++;
            texture = null;
            return false;
        }
        public TexturedShipTexture SelectMip(float a, float b, float c, float d) => this;
        public uint Sample(float u, float v) => 0;
    }

    internal static class MonochromeChecks
    {
        public static void Run()
        {
            var mesh = new TexturedShipMesh();
            var raster = new TexturedShipRaster();
            var view = new ShipProjection { X = new Point3(1,0,0), Y = new Point3(0,1,0), Z = new Point3(0,0,1), Zoom = 1 };
            var bounds = new Box3(new Point3(-1,-1,-1), new Point3(1,1,1));
            var appearance = new HudAppearance();
            foreach (string color in new[] { "FF0000", "00FF00" })
            {
                appearance.Colors = color + appearance.Colors.Substring(6);
                var filter = new TexturedShipFilter("all", "all", TexturedShipFilter.DefaultStyles,
                    255, null, null, appearance.Color((int)ChangeKind.Hull));
                var image = raster.Render(mesh, bounds, view, 64, 64, filter, false);
                int center = 4 * (32 * 64 + 32);
                if (image.Status != "Monochrome ship." || TexturedShipTexture.Loads != 0
                    || image.Pixels[center + 3] == 0 || image.Pixels[center + (color == "FF0000" ? 0 : 1)] == 0
                    || image.Pixels[center + (color == "FF0000" ? 1 : 0)] != 0)
                    throw new Exception("Monochrome raster failed after selecting " + color);
            }
            raster.Render(mesh, bounds, view, 64, 64,
                new TexturedShipFilter("all", "all", TexturedShipFilter.DefaultStyles, 255, null, null), false);
            if (TexturedShipTexture.Loads != 1)
                throw new Exception("Textured mode should request the triangle material.");
        }
    }
}

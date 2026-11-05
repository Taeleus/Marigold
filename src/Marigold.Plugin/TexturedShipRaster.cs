using System;
using System.Collections.Generic;

namespace Marigold
{
    internal struct ShipProjection
    {
        public Point3 X, Y, Z;
        public double Zoom;

        public Point3 Project(Point3 p)
        {
            return new Point3(
                p.X * X.X + p.Y * Y.X + p.Z * Z.X,
                p.X * X.Y + p.Y * Y.Y + p.Z * Z.Y,
                p.X * X.Z + p.Y * Y.Z + p.Z * Z.Z);
        }

        public bool SameView(ShipProjection other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && Zoom == other.Zoom;
        }
    }

    internal sealed class TexturedShipImage
    {
        public ShipProjection View;
        public Box3 Bounds;
        public int Size, RequestedSize;
        public byte[] Pixels;
        // Projected depth in ship-radius units. Open pixels are negative infinity.
        public float[] Depth;
        public string Status;
        public string Caption;
    }

    // A small orthographic preview. All inputs are copied mesh data, so it can run off the game thread.
    internal sealed class TexturedShipRaster
    {
        private const int TextureCacheLimit = 256;
        private const int DetailedTextureLimit = 96;
        private const int SampleBudget = 2500000;
        private readonly Dictionary<string, TexturedShipTexture> textures = new Dictionary<string, TexturedShipTexture>(StringComparer.OrdinalIgnoreCase);
        private float[] depth = new float[0];
        private Vertex[] projectedVertices = Array.Empty<Vertex>();

        private struct Vertex
        {
            public float X, Y, Z, U, V;
        }

        public TexturedShipImage Render(TexturedShipMesh mesh, Box3 bounds, ShipProjection view, int size, int preferredSize = 0)
        {
            var image = new TexturedShipImage
            {
                Bounds = bounds, View = view, Size = preferredSize > 0 ? preferredSize : size, RequestedSize = size
            };
            try
            {
                int initialSize = image.Size;
                for (int attempt = 0; ; attempt++)
                {
                    if (Draw(mesh, image, out bool overBudget) || !overBudget || attempt == 3 || image.Size <= 80)
                        break;
                    // Try a modest reduction first. A dense view can fall back to a smaller image.
                    image.Size = Math.Max(80, attempt == 0 ? initialSize * 3 / 4
                        : attempt == 1 ? initialSize / 2 : initialSize / 4);
                }
                if (image.Pixels != null && image.Size < image.RequestedSize)
                    image.Status += " Preview resolution reduced to fit the drawing budget.";
            }
            catch (Exception e)
            {
                image.Pixels = null;
                image.Depth = null;
                image.Status = "Textured ship unavailable: " + e.Message;
            }
            return image;
        }

        private bool Draw(TexturedShipMesh mesh, TexturedShipImage image, out bool overBudget)
        {
            overBudget = false;
            int size = image.Size;
            int count = size * size;
            if (depth.Length != count)
                depth = new float[count];
            for (int i = 0; i < depth.Length; i++)
                depth[i] = float.NegativeInfinity;

            var pixels = new byte[count * 4];
            var min = image.Bounds.Min;
            var max = image.Bounds.Max;
            var center = new Point3((min.X + max.X) / 2, (min.Y + max.Y) / 2, (min.Z + max.Z) / 2);
            double dx = max.X - min.X, dy = max.Y - min.Y, dz = max.Z - min.Z;
            double radius = Math.Max(.5, Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2);
            var view = image.View;
            float half = size * .5f;
            float scale = (float)(half * view.Zoom / radius);
            var vertices = mesh.Vertices;
            if (projectedVertices.Length < vertices.Length)
                projectedVertices = new Vertex[vertices.Length];
            var projected = projectedVertices;
            var axisX = view.X;
            var axisY = view.Y;
            var axisZ = view.Z;
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                double x = vertex.Position.X - center.X;
                double y = vertex.Position.Y - center.Y;
                double z = vertex.Position.Z - center.Z;
                projected[i] = new Vertex
                {
                    X = half + (float)(x * axisX.X + y * axisY.X + z * axisZ.X) * scale,
                    Y = half - (float)(x * axisX.Y + y * axisY.Y + z * axisZ.Y) * scale,
                    Z = (float)(x * axisX.Z + y * axisY.Z + z * axisZ.Z) * scale,
                    U = vertex.Uv.X,
                    V = vertex.Uv.Y
                };
            }

            int samples = SampleBudget;
            bool foundTexture = false;
            bool foundColorPath = false;
            bool usedPaintFallback = false;
            bool drewSurface = false;
            string lastPath = null;
            TexturedShipTexture texture = null;
            foreach (var triangle in mesh.Triangles)
            {
                var a = projected[triangle.A];
                var b = projected[triangle.B];
                var c = projected[triangle.C];
                float abX = b.X - a.X, abY = b.Y - a.Y, abZ = b.Z - a.Z;
                float acX = c.X - a.X, acY = c.Y - a.Y, acZ = c.Z - a.Z;
                float area = abX * acY - abY * acX;
                if (Math.Abs(area) < .002f)
                    continue;

                // Samples are at pixel centers. Clip first so integer truncation floors the upper bounds.
                float minX = Math.Max(0f, Math.Min(a.X, Math.Min(b.X, c.X)) - .5f);
                float maxX = Math.Min(size - 1f, Math.Max(a.X, Math.Max(b.X, c.X)) - .5f);
                float minY = Math.Max(0f, Math.Min(a.Y, Math.Min(b.Y, c.Y)) - .5f);
                float maxY = Math.Min(size - 1f, Math.Max(a.Y, Math.Max(b.Y, c.Y)) - .5f);
                if (minX > maxX || minY > maxY)
                    continue;
                int x0 = (int)minX, x1 = (int)maxX;
                int y0 = (int)minY, y1 = (int)maxY;
                if (x0 < minX)
                    x0++;
                if (y0 < minY)
                    y0++;
                if (x0 > x1 || y0 > y1)
                    continue;

                samples -= (x1 - x0 + 1) * (y1 - y0 + 1);
                if (samples < 0)
                {
                    overBudget = true;
                    image.Status = "Textured ship exceeded the preview drawing budget. Zoom out or use Wireframe.";
                    return false;
                }

                if (triangle.ColorTexture != lastPath)
                {
                    lastPath = triangle.ColorTexture;
                    texture = GetTexture(lastPath);
                }
                foundColorPath |= !string.IsNullOrEmpty(triangle.ColorTexture);
                float nx = abY * acZ - abZ * acY;
                float ny = abZ * acX - abX * acZ;
                float light = .60f + .40f * Math.Abs(area) / (float)Math.Sqrt(nx * nx + ny * ny + area * area);
                if (DrawTriangle(a, b, c, x0, x1, y0, y1, area, texture, triangle.PaintRgb, light, size, pixels))
                {
                    drewSurface = true;
                    foundTexture |= texture != null;
                    usedPaintFallback |= texture == null;
                }
            }

            if (!drewSurface)
            {
                image.Status = "No textured ship surfaces are visible at this angle and zoom.";
                return false;
            }

            if (!foundTexture)
            {
                image.Caption = "Painted model";
                image.Status = foundColorPath
                    ? "Painted ship model (no color texture could be sampled for visible surfaces)."
                    : "Painted ship model (no color texture paths found for visible meshes).";
            }
            else
            {
                image.Caption = mesh.SimplifiedModels > 0 ? "Some painted shapes" : null;
                image.Status = usedPaintFallback ? "Textured ship (paint used on surfaces with unavailable textures)." : "Textured ship.";
            }
            if (mesh.ReducedModels > 0)
                image.Status += " " + mesh.ReducedModels + " parts use reduced meshes.";
            if (mesh.SimplifiedModels > 0)
                image.Status += " " + mesh.SimplifiedModels + " parts use painted shapes.";
            if (!string.IsNullOrEmpty(mesh.SimplificationReason))
                image.Status += " " + mesh.SimplificationReason;

            // Convert pixel depth to the schematic's normalized coordinates and
            // give the finished image ownership before the next frame can draw.
            float normalizeDepth = (float)(2 / (size * view.Zoom));
            for (int i = 0; i < depth.Length; i++)
                depth[i] *= normalizeDepth;
            image.Depth = depth;
            depth = Array.Empty<float>();
            image.Pixels = pixels;
            return true;
        }

        private TexturedShipTexture GetTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            TexturedShipTexture texture;
            if (textures.TryGetValue(path, out texture))
                return texture;
            if (textures.Count >= TextureCacheLimit)
                return null;
            // Keep sharper mips for ordinary ships while bounding the decoded cache on diverse grids.
            TexturedShipTexture.TryLoad(path, out texture, textures.Count < DetailedTextureLimit ? 256 : 128);
            textures.Add(path, texture);
            return texture;
        }

        private bool DrawTriangle(Vertex a, Vertex b, Vertex c, int x0, int x1, int y0, int y1, float area,
            TexturedShipTexture texture, uint paint, float light, int size, byte[] pixels)
        {
            float invArea = 1 / area;
            float dBx = (c.Y - a.Y) * invArea, dBy = (a.X - c.X) * invArea;
            float dCx = (a.Y - b.Y) * invArea, dCy = (b.X - a.X) * invArea;
            float red = ((paint >> 16) & 255) / 255f;
            float green = ((paint >> 8) & 255) / 255f;
            float blue = (paint & 255) / 255f;
            float texRed = (.25f + .75f * red) * light;
            float texGreen = (.25f + .75f * green) * light;
            float texBlue = (.25f + .75f * blue) * light;
            bool drewPixel = false;
            for (int y = y0; y <= y1; y++)
            {
                float localX = x0 + .5f - a.X, localY = y + .5f - a.Y;
                float wb = localX * dBx + localY * dBy;
                float wc = localX * dCx + localY * dCy;
                for (int x = x0; x <= x1; x++, wb += dBx, wc += dCx)
                {
                    float wa = 1 - wb - wc;
                    if (wa < -.00001f || wb < -.00001f || wc < -.00001f)
                        continue;
                    float z = wa * a.Z + wb * b.Z + wc * c.Z;
                    int pixel = y * size + x;
                    if (z <= depth[pixel])
                        continue;
                    depth[pixel] = z;
                    drewPixel = true;
                    int offset = pixel * 4;
                    if (texture == null)
                    {
                        pixels[offset] = (byte)(255 * red * light);
                        pixels[offset + 1] = (byte)(255 * green * light);
                        pixels[offset + 2] = (byte)(255 * blue * light);
                    }
                    else
                    {
                        uint rgb = texture.Sample(wa * a.U + wb * b.U + wc * c.U, wa * a.V + wb * b.V + wc * c.V);
                        pixels[offset] = (byte)(((rgb >> 16) & 255) * texRed);
                        pixels[offset + 1] = (byte)(((rgb >> 8) & 255) * texGreen);
                        pixels[offset + 2] = (byte)((rgb & 255) * texBlue);
                    }
                    pixels[offset + 3] = 255;
                }
            }
            return drewPixel;
        }
    }
}

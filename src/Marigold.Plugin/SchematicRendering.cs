using System;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private const byte MissingOutlineAlpha = 205;
        private const int MaximumRasterSize = 512;
        private const int MaximumTexturedOverlaySize = 768;
        private static readonly byte[] LinearRgb = BuildLinearRgb();
        private static readonly byte[] Srgb = BuildSrgb();
        private static readonly ChangeKind[] FillKinds =
        {
            ChangeKind.Damaged, ChangeKind.NonFunctional, ChangeKind.Missing
        };

        private bool hdUnavailable;
        private bool renderedPreciseSchematic;
        private readonly string schematicTexture = "marigoldschematic" + Guid.NewGuid().ToString("N");
        private readonly uint[] fillPalette = new uint[6];
        private SchematicRaster schematicRaster;
        private bool schematicTextureCreated;
        private int schematicRasterSize;
        private Point3[] rasterPoints;
        private double rasterAnimationTick;
        private bool rasterHasMissing;
        private double rasterLatestFreshPulse;
        private ShipProjection rasterProjection;
        private TexturedShipImage rasterSurface;
        private bool rasterRecovery, rasterTextured;
        private float rasterSide;
        private (string Colors, double Outline, double ContextOpacity, double FillOpacity,
            double MissingPulse, bool FreshPulse, int Mode, bool Station) rasterStyle;

        private void DrawSchematicModel(ShipProjection projection, float left, float top, float side, bool recoveryFocus, bool textured)
        {
            if (textured && mainHullLines == points.Length / 2)
            {
                renderedLines = renderedFills = unmergedFills = 0;
                rasterPoints = null;
                return;
            }

            var appearance = DrawingAppearance;
            double fillOpacity = appearance.Fill * (textured ? appearance.ShipOpacity : 1);
            int mode = appearance.Rendering;
            if (!hdUnavailable && (mode == HudAppearance.HdWireframe || mode == HudAppearance.Textured) && points.Length != 0)
            {
                try
                {
                    DrawPreciseSchematic(projection, left, top, side, recoveryFocus, textured, fillOpacity);
                    renderedPreciseSchematic = true;
                    return;
                }
                catch (Exception e)
                {
                    hdUnavailable = true;
                    ReleaseSchematicTexture();
                    MyLog.Default.WriteLine("[Marigold] HD Wireframe fell back to SD: " + e.Message);
                }
            }

            rasterPoints = null;
            ProjectSchematic(projection);
            DrawModel(projection.Zoom, left, top, side, recoveryFocus, null, fillOpacity, hideMainHull: textured);
        }

        private void DrawPreciseSchematic(ShipProjection projection, float left, float top, float side, bool recoveryFocus,
            bool textured, double fillOpacity)
        {
            int limit = textured ? MaximumTexturedOverlaySize : MaximumRasterSize;
            int size = Math.Min(limit, Math.Max(1, (int)Math.Ceiling(side)));
            if (schematicRasterSize != size)
            {
                ReleaseSchematicTexture();
                schematicRaster = new SchematicRaster(size);
                schematicRasterSize = size;
            }

            var surface = textured ? texturedImage : null;
            var appearance = DrawingAppearance;
            var style = (appearance.Colors, appearance.Outline, appearance.ContextOpacity, fillOpacity,
                appearance.Pulse, appearance.NewDamagePulse, appearance.Rendering, controls.Station);
            // All raster inputs for a published model (lines, fills, and alerts) share the points array's lifetime.
            bool modelChanged = rasterPoints != points;
            if (modelChanged)
                ReadRasterAlerts();
            double now = clock.Elapsed.TotalSeconds;
            bool animating = (rasterHasMissing && appearance.Pulse > 0)
                || (appearance.NewDamagePulse && now - rasterLatestFreshPulse < 4);
            // Redraw at most once per update for a pulse, and once more when the final fresh pulse expires.
            double animationTick = animating ? lastTime : double.NegativeInfinity;
            if (modelChanged || rasterAnimationTick != animationTick || !rasterProjection.SameView(projection) || rasterSide != side
                || rasterSurface != surface || rasterRecovery != recoveryFocus || rasterTextured != textured
                || rasterStyle != style)
            {
                ProjectSchematic(projection);
                schematicRaster.Begin(surface?.Depth, surface?.Size ?? 0, TexturedDepthTolerance(surface));
                DrawModel(projection.Zoom, left, top, side, recoveryFocus, schematicRaster, fillOpacity, hideMainHull: textured);
                schematicRaster.Finish();

                var upload = EncodeSchematicTexture(schematicRaster.Pixels);
                if (schematicTextureCreated)
                    MyRenderProxy.ResetGeneratedTexture(schematicTexture, upload);
                else
                {
                    // The engine creates RGBA_Linear as BGRA, which its texture reset path rejects.
                    MyRenderProxy.CreateGeneratedTexture(schematicTexture, size, size,
                        MyGeneratedTextureType.RGBA, 1, upload, generateMipmaps: false, immediatelyReady: true);
                    schematicTextureCreated = true;
                }

                rasterPoints = points;
                rasterAnimationTick = animationTick;
                rasterProjection = projection;
                rasterSurface = surface;
                rasterRecovery = recoveryFocus;
                rasterTextured = textured;
                rasterSide = side;
                rasterStyle = style;
            }

            var rectangle = new RectangleF(left, top, side, side);
            MyRenderProxy.DrawSprite(schematicTexture, ref rectangle, null, Color.White, 0, false, false);
        }

        private void ProjectSchematic(ShipProjection projection)
        {
            for (int i = 0; i < points.Length; i++)
                rotated[i] = projection.Project(points[i]);
        }

        private void ReadRasterAlerts()
        {
            rasterHasMissing = false;
            rasterLatestFreshPulse = double.NegativeInfinity;
            for (int i = 0; i < lineKinds.Length; i++)
            {
                var kind = lineKinds[i];
                if (!DamagePulse.VisibleFor(kind))
                    continue;

                rasterHasMissing |= kind == ChangeKind.Missing;
                if (i < pulseStarts.Length)
                    rasterLatestFreshPulse = Math.Max(rasterLatestFreshPulse, pulseStarts[i]);
            }
        }

        private static double TexturedDepthTolerance(TexturedShipImage surface)
        {
            if (surface?.Depth == null)
                return 0;

            var bounds = surface.Bounds;
            double dx = bounds.Max.X - bounds.Min.X;
            double dy = bounds.Max.Y - bounds.Min.Y;
            double dz = bounds.Max.Z - bounds.Min.Z;
            double radius = Math.Max(.5, Math.Sqrt(dx * dx + dy * dy + dz * dz) / 2);
            // Keep highlights on the surface bright despite small mesh and pixel sampling differences.
            return Math.Max(.12 / radius, .5 / (surface.Size * surface.View.Zoom));
        }

        private void ReleaseSchematicTexture()
        {
            if (schematicTextureCreated)
                MyRenderProxy.DestroyGeneratedTexture(schematicTexture);
            schematicTextureCreated = false;
            schematicRaster = null;
            schematicRasterSize = 0;
            rasterPoints = null;
            rasterSurface = null;
        }

        private void DrawModel(double zoom, float left, float top, float side, bool recoveryFocus, SchematicRaster raster,
            double fillOpacity, bool hideMainHull)
        {
            double now = clock.Elapsed.TotalSeconds;
            double wave = DamagePulse.Wave(now);
            float half = side / 2;
            var appearance = DrawingAppearance;
            bool drawFills = (recoveryFocus || appearance.DrawFills) && fillOpacity > 0;
            bool drawHull = recoveryFocus || appearance.DrawHull;
            bool shadeOutlineDepth = raster != null && hideMainHull && texturedImage?.Depth != null;

            if (raster == null)
                DrawFillSprites(zoom, left, top, side, drawFills, now, wave, fillOpacity);
            else
                // The main ship's image does not cover docked grids drawn in station mode.
                RasterizeFills(raster, zoom, drawFills, now, wave, fillOpacity,
                    hideMainHull && !controls.Station ? texturedImage : null);

            double rasterThickness = raster == null ? 0 : Math.Max(1, schematicRasterSize / (double)side);
            renderedLines = 0;
            for (int b = 0; b < points.Length; b += 2)
            {
                int index = b / 2;
                var kind = lineKinds[index];
                if ((kind == ChangeKind.Hull && (!drawHull || (hideMainHull && index < mainHullLines)))
                    || (lineWeights[index] == 0 && appearance.ContextOpacity <= 0)
                    || (index < drawLine.Length && !drawLine[index]))
                    continue;

                var a = rotated[b];
                var c = rotated[b + 1];
                double x0 = a.X * zoom, y0 = -a.Y * zoom;
                double x1 = c.X * zoom, y1 = -c.Y * zoom;
                if (!SchematicGeometry.Clip(ref x0, ref y0, ref x1, ref y1, out double start, out double end))
                    continue;

                var from = new Vector2(left + half + (float)x0 * half, top + half + (float)y0 * half);
                var to = new Vector2(left + half + (float)x1 * half, top + half + (float)y1 * half);
                var delta = to - from;
                float length = delta.Length();
                if (length < .25f)
                    continue;

                var color = LineColor(kind, MissingOutlineAlpha, lineWeights[index]);
                uint rgb = PulseColor(kind, wave, FreshDamage(index, now));
                color.R = (byte)(rgb >> 16);
                color.G = (byte)(rgb >> 8);
                color.B = (byte)rgb;
                renderedLines++;

                if (shadeOutlineDepth && kind != ChangeKind.Hull)
                {
                    double depth = c.Z - a.Z;
                    raster.Stroke(x0, y0, x1, y1, rasterThickness, PackLinear(color), a.Z + start * depth, a.Z + end * depth);
                }
                else if (raster != null)
                    raster.Stroke(x0, y0, x1, y1, rasterThickness, PackLinear(color));
                else
                {
                    const float thickness = 1;
                    var line = new RectangleF((from.X + to.X - length) / 2, (from.Y + to.Y - thickness) / 2, length, thickness);
                    MyRenderProxy.DrawSprite(Texture, ref line, null, color, (float)Math.Atan2(delta.Y, delta.X), false, false);
                }
            }
        }

        private void DrawFillSprites(double zoom, float left, float top, float side, bool enabled, double now, double wave,
            double fillOpacity)
        {
            if (enabled)
                MarkerFill.Build(rotated, shadedMarkers, zoom, side, fillBands);
            else
                fillBands.Clear();
            unmergedFills = fillBands.Count;
            fillRaster.Merge(fillBands, lineKinds, pulseStarts, now, DrawingAppearance.NewDamagePulse);
            renderedFills = 0;
            float half = side / 2;
            foreach (var band in fillBands)
            {
                var fill = new RectangleF(
                    left + half + (float)band.X * half, top + half + (float)band.Y * half,
                    (float)band.Width * half, (float)band.Height * half);
                int index = band.MarkerStart / 2;
                var shade = FillColor(lineKinds[index], wave, FreshDamage(index, now), fillOpacity);
                renderedFills++;
                MyRenderProxy.DrawSprite(Texture, ref fill, null, shade, 0, false, false);
            }
        }

        private void RasterizeFills(SchematicRaster raster, double zoom, bool enabled, double now, double wave,
            double fillOpacity, TexturedShipImage surface)
        {
            unmergedFills = renderedFills = 0;
            if (!enabled || shadedMarkers.Length == 0)
                return;

            for (int state = 0; state < FillKinds.Length; state++)
            {
                var kind = FillKinds[state];
                fillPalette[2 * state] = PackLinear(FillColor(kind, wave, false, fillOpacity));
                fillPalette[2 * state + 1] = PackLinear(FillColor(kind, wave, true, fillOpacity));
            }

            foreach (int start in shadedMarkers)
            {
                int index = start / 2;
                var kind = lineKinds[index];
                int state = kind == ChangeKind.Damaged ? 0 : kind == ChangeKind.NonFunctional ? 1 : 2;
                raster.Fill(rotated, start, zoom, state * 2 + (FreshDamage(index, now) ? 1 : 0));
                renderedFills++;
            }
            unmergedFills = shadedMarkers.Length;
            raster.ResolveFills(fillPalette, surface?.Pixels, surface?.Size ?? 0);
        }

        private bool FreshDamage(int index, double now)
        {
            return DrawingAppearance.NewDamagePulse && DamagePulse.VisibleFor(lineKinds[index])
                && index < pulseStarts.Length && now - pulseStarts[index] >= 0 && now - pulseStarts[index] < 4;
        }

        private uint PulseColor(ChangeKind kind, double wave, bool fresh)
        {
            var appearance = DrawingAppearance;
            return DamagePulse.Tint(appearance.Color((int)kind), kind, wave, fresh, appearance.Pulse);
        }

        private Color FillColor(ChangeKind kind, double wave, bool fresh, double fillOpacity)
        {
            uint rgb = PulseColor(kind, wave, fresh);
            int alpha = fresh ? 160 : kind == ChangeKind.Missing ? MissingOutlineAlpha / 4 : kind == ChangeKind.Damaged ? 12 : 60;
            return new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb,
                (byte)Math.Min(255, alpha * fillOpacity));
        }

        private static uint PackLinear(Color color)
        {
            return (uint)LinearRgb[color.R] << 24 | (uint)LinearRgb[color.G] << 16
                | (uint)LinearRgb[color.B] << 8 | color.A;
        }

        private static byte[] BuildLinearRgb()
        {
            var result = new byte[256];
            for (int i = 0; i < result.Length; i++)
            {
                double srgb = i / 255.0;
                double linear = srgb <= .04045 ? srgb / 12.92 : Math.Pow((srgb + .055) / 1.055, 2.4);
                result[i] = (byte)Math.Round(linear * 255);
            }
            return result;
        }

        private static byte[] EncodeSchematicTexture(byte[] linearPixels)
        {
            // The GPU samples RGBA as sRGB. Encode the premultiplied RGB and keep alpha linear.
            // Each render message owns its bytes until the render thread consumes it.
            var pixels = new byte[linearPixels.Length];
            for (int i = 0; i < linearPixels.Length; i += 4)
            {
                pixels[i] = Srgb[linearPixels[i]];
                pixels[i + 1] = Srgb[linearPixels[i + 1]];
                pixels[i + 2] = Srgb[linearPixels[i + 2]];
                pixels[i + 3] = linearPixels[i + 3];
            }
            return pixels;
        }

        private static byte[] BuildSrgb()
        {
            var result = new byte[256];
            for (int i = 0; i < result.Length; i++)
            {
                double linear = i / 255.0;
                double srgb = linear <= .0031308 ? 12.92 * linear : 1.055 * Math.Pow(linear, 1 / 2.4) - .055;
                result[i] = (byte)Math.Round(srgb * 255);
            }
            return result;
        }
    }
}

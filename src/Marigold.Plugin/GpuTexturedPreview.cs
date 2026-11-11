using System;
using System.Collections.Generic;
using VRageMath;
using VRageRender;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private const int GpuExpandedPreviewSize = 1536;
        private const int GpuTextureRefreshLimit = 8;
        private GpuShipRenderer gpuShipRenderer;
        private bool gpuRendererFaulted;
        private GpuShipFrame requestedGpuFrame, displayedGpuFrame;
        private Point3[] gpuOverlayPoints;
        private GpuShipOverlayVertex[] gpuLines = Array.Empty<GpuShipOverlayVertex>();
        private GpuShipOverlayVertex[] gpuFills = Array.Empty<GpuShipOverlayVertex>();
        private double gpuOverlayTick;
        private double nextGpuTextureRefresh;
        private int gpuTextureRefreshes;
        private int gpuOverlayFacing;
        private (string Colors, double Outline, double Context, double Fill, double Pulse, bool FreshPulse,
            bool DrawHull, bool Station) gpuOverlayStyle;

        private bool IsGpuTexturedShip(bool recoveryFocus)
        {
            return controls.UseGpuTexturedShip && DrawingAppearance.Rendering == HudAppearance.Textured
                && !recoveryFocus && recoveryPreview == null && previewBlocks == null;
        }

        private bool DrawGpuTexturedShip(Box3 bounds, ShipProjection view, TexturedShipFilter filter,
            float left, float top, float side, int size, float opacity, double now)
        {
            renderedLines = renderedFills = unmergedFills = 0;
            if (texturedMesh == null)
                return false;
            if (gpuShipRenderer == null)
                gpuShipRenderer = new GpuShipRenderer();

            var result = gpuShipRenderer.Latest;
            if (result?.Error != null)
            {
                FailGpuPreview(result.Error);
                return false;
            }

            if (now >= nextTexturedFrame)
            {
                UpdateGpuOverlay(view, opacity, now);
                var previous = requestedGpuFrame;
                bool changed = previous == null || previous.Mesh != texturedMesh || previous.Filter != filter
                    || !SameBounds(previous.Bounds, bounds) || !previous.View.SameView(view) || previous.Size != size
                    || previous.ShipOpacity != opacity || previous.Lines != gpuLines || previous.Fills != gpuFills;
                if (previous?.Mesh != texturedMesh)
                    gpuTextureRefreshes = 0;
                // A paused preview may need a few redraws while the game streams its textures.
                bool refreshTextures = !changed && result != null && result.Frame == previous && result.TexturesPending
                    && gpuTextureRefreshes < GpuTextureRefreshLimit && now >= nextGpuTextureRefresh;
                if (changed || refreshTextures)
                {
                    var alphas = previous != null && previous.Mesh == texturedMesh && previous.Filter == filter
                        ? previous.PartAlphas : filter.PartAlphas(texturedMesh.Parts);
                    var frame = new GpuShipFrame(texturedMesh, filter, alphas, bounds, view, size,
                        opacity, GpuDepthTolerance(bounds, view, size), gpuFills, gpuLines);
                    gpuShipRenderer.Submit(frame);
                    requestedGpuFrame = frame;
                    nextTexturedFrame = now + 1.0 / 30;
                    nextGpuTextureRefresh = now + .25;
                    if (refreshTextures)
                        gpuTextureRefreshes++;
                }
            }

            var ready = result?.Frame;
            if (ready == null || !MatchesTexturedOptions(ready.Filter) || ready.Filter.Baseline != filter.Baseline
                || !SameBounds(ready.Bounds, bounds))
                return false;

            if (ready != displayedGpuFrame)
            {
                RecordTexturedFrame(ready.View, displayedGpuFrame?.View, now);
                displayedGpuFrame = ready;
                texturedRasterMs = result.RenderMilliseconds;
                texturedFallback = false;
                var mesh = ready.Mesh;
                texturedStatus = "Textured ship.";
                if (mesh.ReducedModels > 0)
                    texturedStatus += " " + mesh.ReducedModels + " parts use reduced meshes.";
                if (mesh.SimplifiedModels > 0)
                    texturedStatus += " " + mesh.SimplifiedModels + " parts use painted shapes.";
                if (!string.IsNullOrEmpty(mesh.SimplificationReason))
                    texturedStatus += " " + mesh.SimplificationReason;
            }

            renderedLines = ready.Lines.Length / 2;
            renderedFills = unmergedFills = ready.Fills.Length / 18;
            var rect = new RectangleF(left, top, side, side);
            MyRenderProxy.DrawSprite(result.TextureName, ref rect, null, Color.White, 0, false, false);
            return true;
        }

        private void UpdateGpuOverlay(ShipProjection view, float opacity, double now)
        {
            var appearance = DrawingAppearance;
            double fillOpacity = appearance.DrawFills ? appearance.Fill * opacity : 0;
            var style = (appearance.Colors, appearance.Outline, appearance.ContextOpacity, fillOpacity,
                appearance.Pulse, appearance.NewDamagePulse, appearance.DrawHull, controls.Station);
            bool changed = gpuOverlayPoints != points;
            if (changed)
                ReadRasterAlerts();
            bool animating = (rasterHasMissing && appearance.Pulse > 0)
                || (appearance.NewDamagePulse && now - rasterLatestFreshPulse < 4);
            double tick = animating ? lastTime : double.NegativeInfinity;
            int facing = (view.X.Z >= 0 ? 1 : 0) | (view.Y.Z >= 0 ? 2 : 0) | (view.Z.Z >= 0 ? 4 : 0);
            if (!changed && gpuOverlayTick == tick && gpuOverlayFacing == facing && gpuOverlayStyle == style)
                return;

            double wave = DamagePulse.Wave(now);
            var lines = new List<GpuShipOverlayVertex>();
            for (int i = mainHullLines; i < lineKinds.Length; i++)
            {
                var kind = lineKinds[i];
                if ((kind == ChangeKind.Hull && !appearance.DrawHull) || (i < drawLine.Length && !drawLine[i]))
                    continue;

                var color = LineColor(kind, MissingOutlineAlpha, lineWeights[i]);
                if (color.A == 0)
                    continue;
                uint rgb = PulseColor(kind, wave, FreshDamage(i, now));
                color.R = (byte)(rgb >> 16);
                color.G = (byte)(rgb >> 8);
                color.B = (byte)rgb;
                uint rgba = GpuOverlayColor(color);
                bool depthAware = kind != ChangeKind.Hull;
                lines.Add(new GpuShipOverlayVertex(points[i * 2], rgba, depthAware));
                lines.Add(new GpuShipOverlayVertex(points[i * 2 + 1], rgba, depthAware));
            }
            gpuLines = lines.ToArray();

            var fills = new List<GpuShipOverlayVertex>();
            if (fillOpacity > 0)
            {
                var corners = new Point3[8];
                // Match the CPU order so missing markers have the last fill tint.
                foreach (var kind in FillKinds)
                    foreach (int start in shadedMarkers)
                    {
                        int line = start / 2;
                        if (lineKinds[line] != kind)
                            continue;
                        var color = FillColor(kind, wave, FreshDamage(line, now), fillOpacity);
                        if (color.A == 0)
                            continue;
                        var min = points[start];
                        // The first three edges of AddMarker extend from its minimum corner along X, Y and Z.
                        var max = new Point3(points[start + 1].X, points[start + 3].Y, points[start + 5].Z);
                        for (int i = 0; i < corners.Length; i++)
                            corners[i] = new Point3((i & 1) == 0 ? min.X : max.X,
                                (i & 2) == 0 ? min.Y : max.Y, (i & 4) == 0 ? min.Z : max.Z);
                        uint rgba = GpuOverlayColor(color);
                        bool depthAware = !controls.Station;
                        bool requireHull = depthAware && kind != ChangeKind.Missing;
                        // Only draw the three front faces so each box tints its silhouette once.
                        AddGpuFillFace(fills, corners, facing & 1, 2, 4, rgba, depthAware, requireHull);
                        AddGpuFillFace(fills, corners, facing & 2, 1, 4, rgba, depthAware, requireHull);
                        AddGpuFillFace(fills, corners, facing & 4, 1, 2, rgba, depthAware, requireHull);
                    }
            }
            gpuFills = fills.ToArray();
            gpuOverlayPoints = points;
            gpuOverlayStyle = style;
            gpuOverlayTick = tick;
            gpuOverlayFacing = facing;
        }

        private static void AddGpuFillFace(List<GpuShipOverlayVertex> fills, Point3[] corners, int face,
            int u, int v, uint rgba, bool depthAware, bool requireHull)
        {
            var a = new GpuShipOverlayVertex(corners[face], rgba, depthAware, requireHull);
            var b = new GpuShipOverlayVertex(corners[face | u], rgba, depthAware, requireHull);
            var c = new GpuShipOverlayVertex(corners[face | u | v], rgba, depthAware, requireHull);
            var d = new GpuShipOverlayVertex(corners[face | v], rgba, depthAware, requireHull);
            fills.Add(a);
            fills.Add(b);
            fills.Add(c);
            fills.Add(a);
            fills.Add(c);
            fills.Add(d);
        }

        private static uint GpuOverlayColor(Color color)
        {
            return (uint)color.R | (uint)color.G << 8 | (uint)color.B << 16 | (uint)color.A << 24;
        }

        private static float GpuDepthTolerance(Box3 bounds, ShipProjection view, int size)
        {
            double x = bounds.Max.X - bounds.Min.X;
            double y = bounds.Max.Y - bounds.Min.Y;
            double z = bounds.Max.Z - bounds.Min.Z;
            double radius = Math.Max(.5, Math.Sqrt(x * x + y * y + z * z) / 2);
            return (float)Math.Max(.12 / radius, .5 / (size * view.Zoom));
        }

        private void FailGpuPreview(string reason)
        {
            gpuRendererFaulted = true;
            ClearGpuFrame();
            ReportTexturedFallback("GPU preview unavailable: " + reason);
        }

        private void ClearGpuFrame()
        {
            gpuShipRenderer?.Clear();
            requestedGpuFrame = displayedGpuFrame = null;
            gpuTextureRefreshes = 0;
            nextGpuTextureRefresh = 0;
            gpuOverlayPoints = null;
            gpuLines = gpuFills = Array.Empty<GpuShipOverlayVertex>();
        }

        private string GpuTexturedStatistics()
        {
            string frame = displayedGpuFrame == null ? "" : " " + displayedGpuFrame.Size + " px preview."
                + " Last GPU submission CPU time " + texturedRasterMs.ToString("F2") + " ms.";
            string cadence = displayedGpuFrame != null && texturedFrameInterval > 0
                && clock.Elapsed.TotalSeconds - lastTexturedFrame < 1
                ? " " + (1 / texturedFrameInterval).ToString("F1") + " preview fps." : "";
            return " GPU textured renderer (experimental). " + texturedStatus + frame + " Textured grid " + texturedBlockCount
                + " blocks, snapshot " + texturedSnapshotMs.ToString("F2") + " ms, last mesh " + texturedMeshMs.ToString("F2")
                + " ms." + cadence;
        }
    }
}

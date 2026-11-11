using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private const int CompactTexturedPreviewSize = 768;
        private const int ExpandedTexturedPreviewSize = 1024;
        private const int LargeCompactTexturedPreviewSize = 384;
        private const int LargeExpandedTexturedPreviewSize = 640;
        private readonly string texturedTexture = "marigoldshipmodel" + Guid.NewGuid().ToString("N");
        private TexturedShipRaster texturedRaster;
        private TexturedShipMesh.AssetCache texturedAssets;
        private TexturedShipMesh texturedMesh, displayedTexturedMesh;
        private Task<TexturedFrameJob> pendingTexturedImage;
        private TexturedShipImage texturedImage;
        private TexturedShipFilter texturedFilter;
        private Box3? schematicBounds;
        private int texturedRevision = -1;
        private int texturedTextureSize, texturedBlockCount;
        private double nextMeshCapture, nextTexturedFrame, nextResolutionProbe, refreshPaintAfter;
        private double texturedSnapshotMs, texturedMeshMs, texturedRasterMs, texturedFrameInterval, lastTexturedFrame;
        private string texturedStatus = "Textured ship preparing.";
        private bool texturedFallback, loggedTexturedFallback, pendingTexturedCurrent;
        private bool texturedUsingGpu;

        private sealed class TexturedFrameJob
        {
            public TexturedShipMesh Mesh;
            public TexturedShipImage Image;
            public string Failure;
            public double? MeshMilliseconds;
            public double RasterMilliseconds;
        }

        private string TexturedCaption(bool renderedModel)
        {
            if (DrawingAppearance.Rendering != HudAppearance.Textured)
                return "";
            if (IsGpuTexturedShip(false))
                return renderedModel ? " [GPU]" : texturedFallback ? " [GPU preview unavailable]" : " [Preparing GPU preview]";
            if (!renderedModel)
                return texturedFallback ? " [Wireframe fallback]" : " [Preparing textured ship]";
            var note = texturedImage?.Caption;
            return string.IsNullOrEmpty(note) ? "" : " [" + note + "]";
        }

        private bool TryDrawTexturedShip(ref ShipProjection view, float left, float top, float side, bool recoveryFocus, bool expanded)
        {
            var appearance = DrawingAppearance;
            if (appearance.Rendering != HudAppearance.Textured)
            {
                ReleaseTexturedShip();
                return false;
            }
            if (recoveryFocus || recoveryPreview != null || previewBlocks != null)
            {
                ReleaseTexturedShip();
                texturedFallback = true;
                texturedStatus = "Alignment previews use Wireframe.";
                return false;
            }
            if (!schematicBounds.HasValue)
                return false;

            bool useGpu = controls.UseGpuTexturedShip;
            if (texturedUsingGpu != useGpu)
            {
                ReleaseTexturedShip();
                ReleaseSchematicTexture();
                texturedUsingGpu = useGpu;
            }
            if (useGpu && gpuRendererFaulted)
                return false;

            if (!useGpu && appearance.ShipOpacity <= 0)
            {
                ReleaseTexturedShip();
                texturedStatus = "Textured ship model opacity is zero.";
                return true;
            }

            try
            {
                return DrawTexturedShip(ref view, left, top, side, appearance.ShipOpacity, expanded, useGpu);
            }
            catch (Exception e)
            {
                if (useGpu)
                {
                    FailGpuPreview(e.Message);
                    return false;
                }
                bool alreadyLogged = loggedTexturedFallback;
                ReleaseTexturedShip();
                loggedTexturedFallback = alreadyLogged;
                texturedRevision = observationRevision;
                nextMeshCapture = refreshPaintAfter = clock.Elapsed.TotalSeconds + 10;
                ReportTexturedFallback("Textured ship unavailable: " + e.Message);
                return false;
            }
        }

        private bool DrawTexturedShip(ref ShipProjection view, float left, float top, float side, double opacity,
            bool expanded, bool useGpu)
        {
            var bounds = schematicBounds.Value;
            int sizeLimit = expanded ? (useGpu ? GpuExpandedPreviewSize : ExpandedTexturedPreviewSize) : CompactTexturedPreviewSize;
            // Extra samples help on 1080p layouts that already fit under the old size limit.
            float targetSide = expanded ? side * 1.25f : side;
            int size = Math.Min(sizeLimit, Math.Max(1, (int)Math.Ceiling(targetSide)));
            if (useGpu)
                size = GpuShipFrame.OutputSize(size);
            double now = clock.Elapsed.TotalSeconds;
            var filter = CurrentTexturedFilter();

            if (pendingTexturedImage != null && pendingTexturedImage.IsCompleted)
            {
                var completed = pendingTexturedImage;
                pendingTexturedImage = null;
                var result = completed.GetAwaiter().GetResult();
                if (pendingTexturedCurrent)
                {
                    texturedMesh = result.Mesh;
                    if (result.MeshMilliseconds.HasValue)
                        texturedMeshMs = result.MeshMilliseconds.Value;
                    texturedRasterMs = result.RasterMilliseconds;
                    if (result.Failure != null)
                    {
                        texturedImage = null;
                        if (useGpu)
                            ClearGpuFrame();
                        ReportTexturedFallback(result.Failure);
                    }
                    else if (!useGpu && MatchesTexturedOptions(result.Image.Filter) && result.Image.Filter.Baseline == filter.Baseline
                        && SameBounds(result.Image.Bounds, bounds) && result.Image.RequestedSize == size
                        && result.Image.Expanded == expanded)
                    {
                        var image = result.Image;
                        texturedStatus = image.Status;
                        if (image.Pixels != null)
                        {
                            RecordTexturedFrame(image.View, texturedImage?.View, now);
                            UploadTexturedImage(image);
                            displayedTexturedMesh = result.Mesh;
                            texturedFallback = false;
                        }
                        else
                        {
                            texturedImage = null;
                            nextTexturedFrame = now + 1;
                            ReportTexturedFallback(image.Status);
                        }
                    }
                }
                pendingTexturedCurrent = false;
            }

            TexturedShipMesh.Snapshot snapshot = null;
            int currentBlockCount = grid.BlocksCount;
            bool largeGrid = currentBlockCount > 8192;
            if (pendingTexturedImage == null && now >= nextMeshCapture
                && (texturedBlockCount != currentBlockCount || now >= refreshPaintAfter
                    || (!largeGrid && texturedRevision != observationRevision)))
            {
                texturedRevision = observationRevision;
                texturedBlockCount = currentBlockCount;
                nextMeshCapture = now + (largeGrid ? 3 : 1);
                refreshPaintAfter = now + (largeGrid ? 30 : 10);
                string reason;
                var timer = Stopwatch.StartNew();
                if (!TexturedShipMesh.TrySnapshot(grid, out snapshot, out reason))
                {
                    texturedMesh = null;
                    texturedImage = null;
                    if (useGpu)
                        ClearGpuFrame();
                    ReportTexturedFallback("Textured ship unavailable: " + reason);
                }
                else
                    texturedBlockCount = snapshot.BlockCount;
                texturedSnapshotMs = timer.Elapsed.TotalMilliseconds;
            }

            if (useGpu)
            {
                if (snapshot != null)
                    QueueTexturedJob(snapshot, bounds, view, size, size, filter, expanded, true);
                return DrawGpuTexturedShip(bounds, view, filter, left, top, side, size, (float)opacity, now);
            }

            bool compatibleImage = texturedImage != null && MatchesTexturedOptions(texturedImage.Filter)
                && texturedImage.Filter.Baseline == filter.Baseline && SameBounds(texturedImage.Bounds, bounds);
            bool matchingImage = compatibleImage && texturedImage.RequestedSize == size && texturedImage.Expanded == expanded;
            bool needsFrame = !matchingImage || texturedImage.Filter != filter || !ReferenceEquals(displayedTexturedMesh, texturedMesh) || !texturedImage.View.SameView(view);
            if (pendingTexturedImage == null && (snapshot != null || (texturedMesh != null && now >= nextTexturedFrame && needsFrame)))
            {
                var mesh = texturedMesh;
                var requestedView = view;
                int largeGridLimit = expanded ? LargeExpandedTexturedPreviewSize : LargeCompactTexturedPreviewSize;
                int preferredSize = largeGrid ? Math.Min(size, largeGridLimit) : size;
                if (matchingImage && ReferenceEquals(displayedTexturedMesh, mesh) && texturedImage.View.Zoom == requestedView.Zoom)
                {
                    if (texturedImage.Size < preferredSize && !texturedImage.View.SameView(requestedView) && now >= nextResolutionProbe)
                    {
                        preferredSize = Math.Min(preferredSize, texturedImage.Size * 4 / 3);
                        nextResolutionProbe = now + 2;
                    }
                    else
                        preferredSize = Math.Min(preferredSize, texturedImage.Size);
                }
                nextTexturedFrame = now + 1.0 / (largeGrid || expanded ? 20 : 30);
                QueueTexturedJob(snapshot, bounds, requestedView, size, preferredSize, filter, expanded, false);
            }

            // Keep the compatible frame while a size or layout change is being rendered.
            if (!compatibleImage)
                return false;

            // Use the finished image's camera for the damage overlay, so async rendering cannot shift markers off the hull.
            view = texturedImage.View;
            var rect = new RectangleF(left, top, side, side);
            var tint = new Color(255, 255, 255, (byte)(opacity * 255));
            MyRenderProxy.DrawSprite(texturedTexture, ref rect, null, tint, 0, false, false);
            return true;
        }

        private void QueueTexturedJob(TexturedShipMesh.Snapshot snapshot, Box3 bounds, ShipProjection view, int size,
            int preferredSize, TexturedShipFilter filter, bool expanded, bool useGpu)
        {
            if (!useGpu && texturedRaster == null)
                texturedRaster = new TexturedShipRaster();
            if (texturedAssets == null)
                texturedAssets = new TexturedShipMesh.AssetCache();

            var raster = texturedRaster;
            var assets = texturedAssets;
            var mesh = texturedMesh;
            pendingTexturedCurrent = true;
            pendingTexturedImage = Task.Run(() => BuildTexturedFrame(assets, snapshot, raster, mesh, bounds,
                view, size, preferredSize, filter, expanded, useGpu));
        }

        private bool MatchesTexturedOptions(TexturedShipFilter filter)
        {
            return filter.Category == controls.Category && filter.State == controls.StateFilter
                && filter.Styles == controls.SystemStyles
                && filter.ClassificationRevision == DisplayFilter.ClassificationRevision
                && filter.ContextAlpha == (byte)(DrawingAppearance.ContextOpacity * 255);
        }

        private TexturedShipFilter CurrentTexturedFilter()
        {
            references.TryGetValue(grid, out var reference);
            var baseline = controls.StateFilter == "all" ? null : reference?.Blocks;
            var current = controls.Category == "all" && controls.StateFilter == "all"
                && controls.SystemStyles == TexturedShipFilter.DefaultStyles ? null : lastObservation;
            if (texturedFilter == null || !MatchesTexturedOptions(texturedFilter)
                || texturedFilter.Current != current || texturedFilter.Baseline != baseline)
                texturedFilter = new TexturedShipFilter(controls.Category, controls.StateFilter, controls.SystemStyles,
                    (byte)(DrawingAppearance.ContextOpacity * 255), current, baseline);
            return texturedFilter;
        }

        private static TexturedFrameJob BuildTexturedFrame(TexturedShipMesh.AssetCache assets, TexturedShipMesh.Snapshot snapshot,
            TexturedShipRaster raster, TexturedShipMesh mesh, Box3 bounds, ShipProjection view, int size, int preferredSize,
            TexturedShipFilter filter, bool expanded, bool useGpu)
        {
            var timer = Stopwatch.StartNew();
            var job = new TexturedFrameJob();
            if (snapshot != null)
            {
                string reason;
                bool built = useGpu ? assets.TryBuildGpu(snapshot, out mesh, out reason) : assets.TryBuild(snapshot, out mesh, out reason);
                job.MeshMilliseconds = timer.Elapsed.TotalMilliseconds;
                if (!built)
                {
                    job.Failure = "Textured ship unavailable: " + reason;
                    return job;
                }
                timer.Restart();
            }
            job.Mesh = mesh;
            if (!useGpu)
            {
                job.Image = raster.Render(mesh, bounds, view, size, preferredSize, filter, expanded);
                job.RasterMilliseconds = timer.Elapsed.TotalMilliseconds;
            }
            return job;
        }

        private void RecordTexturedFrame(ShipProjection view, ShipProjection? previous, double now)
        {
            double interval = now - lastTexturedFrame;
            if (!previous.HasValue || previous.Value.SameView(view) || interval <= 0 || interval > 1)
                texturedFrameInterval = 0;
            else
                texturedFrameInterval = texturedFrameInterval == 0 ? interval : texturedFrameInterval * .8 + interval * .2;
            lastTexturedFrame = now;
        }

        private string TexturedStatistics()
        {
            if (controls.UseGpuTexturedShip)
                return GpuTexturedStatistics();

            string resolution = texturedImage == null ? "" : " " + texturedImage.Size + " px preview";
            if (texturedImage != null)
                resolution += texturedImage.Size < texturedImage.RequestedSize
                    ? " (" + texturedImage.RequestedSize + " px target)." : ".";
            string cadence = texturedImage != null && texturedFrameInterval > 0 && clock.Elapsed.TotalSeconds - lastTexturedFrame < 1
                ? " " + (1 / texturedFrameInterval).ToString("F1") + " preview fps." : "";
            return " CPU textured renderer. " + texturedStatus + resolution + " Textured grid " + texturedBlockCount + " blocks, snapshot "
                + texturedSnapshotMs.ToString("F2") + " ms, last mesh " + texturedMeshMs.ToString("F2")
                + " ms, last raster " + texturedRasterMs.ToString("F2") + " ms." + cadence;
        }

        private void ReportTexturedFallback(string reason)
        {
            texturedFallback = true;
            texturedStatus = reason;
            if (!loggedTexturedFallback)
            {
                MyLog.Default.WriteLine("[Marigold] " + reason);
                loggedTexturedFallback = true;
            }
        }

        private void UploadTexturedImage(TexturedShipImage image)
        {
            if (texturedTextureSize != image.Size)
            {
                ReleaseTexturedTexture();
                MyRenderProxy.CreateGeneratedTexture(texturedTexture, image.Size, image.Size,
                    MyGeneratedTextureType.RGBA, 1, image.Pixels, generateMipmaps: false, immediatelyReady: true);
                texturedTextureSize = image.Size;
            }
            else
                MyRenderProxy.ResetGeneratedTexture(texturedTexture, image.Pixels);
            texturedImage = image;
        }

        private void ReleaseTexturedTexture()
        {
            if (texturedTextureSize > 0)
                MyRenderProxy.DestroyGeneratedTexture(texturedTexture);
            texturedTextureSize = 0;
            texturedImage = null;
            displayedTexturedMesh = null;
        }

        private void ReleaseTexturedShip()
        {
            ReleaseTexturedTexture();
            if (texturedUsingGpu)
                ClearGpuFrame();
            texturedUsingGpu = gpuRendererFaulted = false;
            if (pendingTexturedImage != null && pendingTexturedImage.IsCompleted)
                pendingTexturedImage = null;
            texturedAssets = null;
            texturedRaster = null;
            texturedFilter = null;
            texturedMesh = null;
            pendingTexturedCurrent = false;
            texturedRevision = -1;
            texturedBlockCount = 0;
            texturedSnapshotMs = texturedMeshMs = texturedRasterMs = texturedFrameInterval = lastTexturedFrame = 0;
            nextMeshCapture = nextTexturedFrame = nextResolutionProbe = refreshPaintAfter = 0;
            texturedStatus = "Textured ship preparing.";
            texturedFallback = loggedTexturedFallback = false;
        }

        private static bool SameBounds(Box3 first, Box3 second)
        {
            return first.Min.Equals(second.Min) && first.Max.Equals(second.Max);
        }
    }
}

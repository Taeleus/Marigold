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
        private const int TexturedPreviewSize = 768;
        private const int LargeTexturedPreviewSize = 384;
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
            if (!renderedModel)
                return texturedFallback ? " [Wireframe fallback]" : " [Preparing textured ship]";
            var note = texturedImage?.Caption;
            return string.IsNullOrEmpty(note) ? "" : " [" + note + "]";
        }

        private bool TryDrawTexturedShip(ref ShipProjection view, float left, float top, float side, bool recoveryFocus)
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

            if (appearance.ShipOpacity <= 0)
            {
                ReleaseTexturedShip();
                texturedStatus = "Textured ship model opacity is zero.";
                return true;
            }

            try
            {
                return DrawTexturedShip(ref view, left, top, side, appearance.ShipOpacity);
            }
            catch (Exception e)
            {
                bool alreadyLogged = loggedTexturedFallback;
                ReleaseTexturedShip();
                loggedTexturedFallback = alreadyLogged;
                texturedRevision = observationRevision;
                nextMeshCapture = refreshPaintAfter = clock.Elapsed.TotalSeconds + 10;
                ReportTexturedFallback("Textured ship unavailable: " + e.Message);
                return false;
            }
        }

        private bool DrawTexturedShip(ref ShipProjection view, float left, float top, float side, double opacity)
        {
            var bounds = schematicBounds.Value;
            int size = Math.Min(TexturedPreviewSize, Math.Max(1, (int)Math.Ceiling(side)));
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
                        ReportTexturedFallback(result.Failure);
                    }
                    else if (MatchesTexturedOptions(result.Image.Filter) && result.Image.Filter.Baseline == filter.Baseline
                        && SameBounds(result.Image.Bounds, bounds) && result.Image.RequestedSize == size)
                    {
                        var image = result.Image;
                        texturedStatus = image.Status;
                        if (image.Pixels != null)
                        {
                            RecordTexturedFrame(image, now);
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
                    ReportTexturedFallback("Textured ship unavailable: " + reason);
                }
                else
                    texturedBlockCount = snapshot.BlockCount;
                texturedSnapshotMs = timer.Elapsed.TotalMilliseconds;
            }

            bool matchingOptions = texturedImage != null && MatchesTexturedOptions(texturedImage.Filter)
                && texturedImage.Filter.Baseline == filter.Baseline;
            bool matchingImage = matchingOptions && SameBounds(texturedImage.Bounds, bounds) && texturedImage.RequestedSize == size;
            bool needsFrame = !matchingImage || texturedImage.Filter != filter || !ReferenceEquals(displayedTexturedMesh, texturedMesh) || !texturedImage.View.SameView(view);
            if (pendingTexturedImage == null && (snapshot != null || (texturedMesh != null && now >= nextTexturedFrame && needsFrame)))
            {
                var mesh = texturedMesh;
                var requestedView = view;
                int preferredSize = largeGrid ? Math.Min(size, LargeTexturedPreviewSize) : size;
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
                nextTexturedFrame = now + 1.0 / (largeGrid ? 20 : 30);
                if (texturedRaster == null)
                    texturedRaster = new TexturedShipRaster();
                if (texturedAssets == null)
                    texturedAssets = new TexturedShipMesh.AssetCache();
                var raster = texturedRaster;
                var assets = texturedAssets;
                pendingTexturedCurrent = true;
                pendingTexturedImage = Task.Run(() => BuildTexturedFrame(assets, snapshot, raster, mesh, bounds, requestedView, size, preferredSize, filter));
            }

            // While dragging the scale slider, keep the last image for this ship until a new size is ready.
            if (!matchingImage && (hudLayoutPreview == null || !matchingOptions || !SameBounds(texturedImage.Bounds, bounds)))
                return false;

            // Use the finished image's camera for the damage overlay, so async rendering cannot shift markers off the hull.
            view = texturedImage.View;
            var rect = new RectangleF(left, top, side, side);
            var tint = new Color(255, 255, 255, (byte)(opacity * 255));
            MyRenderProxy.DrawSprite(texturedTexture, ref rect, null, tint, 0, false, false);
            return true;
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
            TexturedShipRaster raster, TexturedShipMesh mesh, Box3 bounds, ShipProjection view, int size, int preferredSize, TexturedShipFilter filter)
        {
            var timer = Stopwatch.StartNew();
            var job = new TexturedFrameJob();
            if (snapshot != null)
            {
                bool built = assets.TryBuild(snapshot, out mesh, out string reason);
                job.MeshMilliseconds = timer.Elapsed.TotalMilliseconds;
                if (!built)
                {
                    job.Failure = "Textured ship unavailable: " + reason;
                    return job;
                }
                timer.Restart();
            }
            job.Mesh = mesh;
            job.Image = raster.Render(mesh, bounds, view, size, preferredSize, filter);
            job.RasterMilliseconds = timer.Elapsed.TotalMilliseconds;
            return job;
        }

        private void RecordTexturedFrame(TexturedShipImage image, double now)
        {
            double interval = now - lastTexturedFrame;
            if (texturedImage == null || texturedImage.View.SameView(image.View) || interval <= 0 || interval > 1)
                texturedFrameInterval = 0;
            else
                texturedFrameInterval = texturedFrameInterval == 0 ? interval : texturedFrameInterval * .8 + interval * .2;
            lastTexturedFrame = now;
        }

        private string TexturedStatistics()
        {
            string resolution = texturedImage == null ? "" : " " + texturedImage.Size + " px preview.";
            string cadence = texturedImage != null && texturedFrameInterval > 0 && clock.Elapsed.TotalSeconds - lastTexturedFrame < 1
                ? " " + (1 / texturedFrameInterval).ToString("F1") + " preview fps." : "";
            return " " + texturedStatus + resolution + " Textured grid " + texturedBlockCount + " blocks, snapshot "
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

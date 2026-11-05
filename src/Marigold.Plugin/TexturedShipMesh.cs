using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Import;
using VRage.Utils;
using VRageMath;
using VRageMath.PackedVector;
using VRageRender.Import;
using VRageRender.Messages;

namespace Marigold
{
    internal readonly struct TexturedShipVertex
    {
        public readonly Vector3 Position;
        public readonly Vector2 Uv;

        public TexturedShipVertex(Vector3 position, Vector2 uv)
        {
            Position = position;
            Uv = uv;
        }
    }

    internal readonly struct TexturedShipTriangle
    {
        public readonly string ColorTexture;
        public readonly uint PaintRgb;
        public readonly int A, B, C, Part;

        public TexturedShipTriangle(int a, int b, int c, string colorTexture, uint paintRgb, int part)
        {
            A = a;
            B = b;
            C = c;
            ColorTexture = colorTexture;
            PaintRgb = paintRgb;
            Part = part;
        }
    }

    // Only value data and texture paths leave the capture. Mesh coordinates are grid cells.
    internal sealed class TexturedShipMesh
    {
        private const int MaximumBlocks = 50000;
        private const int MaximumTriangles = 720000;
        private const int MaximumParts = MaximumTriangles / 2;
        private const int MaximumMaterialParts = 12000;
        private const int BoxTriangles = 12;
        private const int MaximumAssets = 256;
        private const int MaximumRoots = 256;
        private const int MaximumLods = 16;
        private const int MaximumSkinMaterials = 256;
        private const int MaximumTexturePaths = 4096;
        private const long MaximumModelBytes = 16 * 1024 * 1024;
        private const long MaximumTotalModelBytes = 128 * 1024 * 1024;
        private static readonly string[] ModelTags = { "Vertices", "TexCoords0", "MeshParts", "GeometryDataAsset" };
        private static readonly string[] LodTags = { "LODs" };
        private static readonly int[] BoxIndices =
        {
            0, 2, 3, 0, 3, 1, 4, 5, 7, 4, 7, 6,
            0, 4, 6, 0, 6, 2, 1, 3, 7, 1, 7, 5,
            0, 1, 5, 0, 5, 4, 2, 6, 7, 2, 7, 3
        };

        public readonly TexturedShipVertex[] Vertices;
        public readonly TexturedShipTriangle[] Triangles;
        public readonly ModelPart[] Parts;
        public readonly int SimplifiedModels, ReducedModels;
        public readonly string SimplificationReason;

        private TexturedShipMesh(TexturedShipVertex[] vertices, TexturedShipTriangle[] triangles, ModelPart[] parts,
            int simplifiedModels, int reducedModels, string simplificationReason)
        {
            Vertices = vertices;
            Triangles = triangles;
            Parts = parts;
            SimplifiedModels = simplifiedModels;
            ReducedModels = reducedModels;
            SimplificationReason = simplificationReason;
        }

        private sealed class CaptureLimitException : Exception
        {
            public CaptureLimitException(string message) : base(message)
            {
            }
        }

        internal sealed class Snapshot
        {
            internal readonly ModelPart[] Parts;
            internal readonly string[] Roots;
            internal readonly string ContentRoot;
            internal readonly float CellsPerMeter;
            internal readonly string CaptureNote;

            public int BlockCount { get; }

            internal Snapshot(ModelPart[] parts, int blockCount, string[] roots, string contentRoot,
                float cellsPerMeter, string captureNote)
            {
                Parts = parts;
                BlockCount = blockCount;
                Roots = roots;
                ContentRoot = contentRoot;
                CellsPerMeter = cellsPerMeter;
                CaptureNote = captureNote;
            }

            internal bool SameRenderParts(Snapshot other)
            {
                if (ReferenceEquals(this, other))
                    return true;
                if (other == null || BlockCount != other.BlockCount || CellsPerMeter != other.CellsPerMeter
                    || ContentRoot != other.ContentRoot || CaptureNote != other.CaptureNote
                    || !SamePaths(Roots, other.Roots) || Parts.Length != other.Parts.Length)
                    return false;

                for (int i = 0; i < Parts.Length; i++)
                    if (!Parts[i].SameRenderPart(other.Parts[i]))
                        return false;
                return true;
            }
        }

        internal sealed class ModelAsset
        {
            public readonly string Name;
            public readonly string[] Lods;
            public readonly float Scale, PatternScale;

            public ModelAsset(MyModel model)
            {
                Name = model.AssetName ?? "";
                Scale = model.ScaleFactor;
                PatternScale = model.PatternScale;
                Lods = CopyLods(model.LODs);
            }

            public bool SameAsset(ModelAsset other)
            {
                return other != null && Name == other.Name && Scale == other.Scale
                    && PatternScale == other.PatternScale && SamePaths(Lods, other.Lods);
            }
        }

        internal readonly struct ModelPart
        {
            public readonly ModelAsset Asset;
            public readonly Point3? BlockPosition;
            public readonly Matrix Transform;
            public readonly BoundingBox Bounds;
            public readonly Vector2 UvOffset;
            public readonly uint Paint;
            public readonly Dictionary<string, string> SkinColors;

            public ModelPart(ModelAsset asset, Matrix transform, BoundingBox bounds, Vector2 uvOffset, uint paint,
                Dictionary<string, string> skinColors, Point3? blockPosition)
            {
                Asset = asset;
                Transform = transform;
                Bounds = bounds;
                UvOffset = uvOffset;
                Paint = paint;
                SkinColors = skinColors;
                BlockPosition = blockPosition;
            }

            public bool SameRenderPart(ModelPart other)
            {
                return Nullable.Equals(BlockPosition, other.BlockPosition) && Paint == other.Paint
                    && Transform.Equals(other.Transform) && Bounds.Equals(other.Bounds)
                    && UvOffset.Equals(other.UvOffset)
                    && (ReferenceEquals(Asset, other.Asset) || (Asset != null && Asset.SameAsset(other.Asset)))
                    && SameSkin(other.SkinColors);
            }

            private bool SameSkin(Dictionary<string, string> other)
            {
                if (ReferenceEquals(SkinColors, other))
                    return true;
                if (SkinColors == null || other == null || SkinColors.Count != other.Count)
                    return false;
                foreach (var item in SkinColors)
                    if (!other.TryGetValue(item.Key, out string path) || path != item.Value)
                        return false;
                return true;
            }
        }

        public static bool TrySnapshot(MyCubeGrid grid, out Snapshot snapshot, out string reason)
        {
            snapshot = null;
            reason = null;
            if (grid == null || grid.Closed || grid.MarkedForClose || grid.GridSize <= 0)
            {
                reason = "A live grid is required for textured view.";
                return false;
            }

            try
            {
                int count = grid.GetBlocks().Count;
                if (count == 0 || count > MaximumBlocks)
                {
                    reason = count == 0 ? "No ship blocks are available for textured view."
                        : "This ship exceeds the textured preview block limit (50,000).";
                    return false;
                }
                snapshot = new SnapshotBuilder(grid).Build(count);
                return true;
            }
            catch (Exception error) when (IsUnreadableAsset(error))
            {
                reason = "The ship's render data could not be captured for textured view.";
                return false;
            }
            catch (CaptureLimitException error)
            {
                reason = error.Message;
                return false;
            }
        }

        // One background build at a time owns the cache and private model importer.
        internal sealed class AssetCache
        {
            private readonly Dictionary<string, Template> models = new Dictionary<string, Template>(StringComparer.OrdinalIgnoreCase);
            private Snapshot previousSnapshot;
            private TexturedShipMesh previousMesh;

            public bool TryBuild(Snapshot snapshot, out TexturedShipMesh mesh, out string reason)
            {
                mesh = null;
                reason = null;
                if (snapshot == null)
                {
                    reason = "No ship render data was available.";
                    return false;
                }
                // A paint or damage refresh can capture the same render parts.
                // Reuse their mesh and let the diagnostic overlay update independently.
                if (snapshot.SameRenderParts(previousSnapshot))
                {
                    mesh = previousMesh;
                    return true;
                }
                try
                {
                    mesh = new MeshCapture(snapshot, models).Build();
                    previousSnapshot = snapshot;
                    previousMesh = mesh;
                    return true;
                }
                catch (InvalidDataException error)
                {
                    reason = error.Message;
                }
                catch (CaptureLimitException error)
                {
                    reason = error.Message;
                }
                catch (Exception error) when (IsUnreadableAsset(error))
                {
                    reason = "The ship's model assets could not be read for textured view.";
                }
                return false;
            }
        }

        private sealed class Template
        {
            public Vector3[] Positions;
            public HalfVector2[] Uvs;
            public List<MyMeshPartInfo> Parts;
            public string Root;
            public int TriangleCount;
            public long SourceBytes;

            public Template Reduce(int maximumTriangles)
            {
                if (TriangleCount <= maximumTriangles)
                    return this;
                if (maximumTriangles <= 0 || !TryGetBounds(out var min, out var max, out float span))
                    return null;

                var snapped = new Vector3[Positions.Length];
                for (int cells = 64; cells >= 1; cells /= 2)
                {
                    double cellSize = (double)span / cells;
                    double stepX = GridStep(min.X, max.X, cellSize);
                    double stepY = GridStep(min.Y, max.Y, cellSize);
                    double stepZ = GridStep(min.Z, max.Z, cellSize);
                    for (int i = 0; i < Positions.Length; i++)
                    {
                        var position = Positions[i];
                        snapped[i] = new Vector3(Snap(position.X, min.X, stepX),
                            Snap(position.Y, min.Y, stepY), Snap(position.Z, min.Z, stepZ));
                    }

                    int surviving = CountTriangles(snapped, maximumTriangles);
                    if (surviving > 0 && surviving <= maximumTriangles)
                    {
                        return new Template
                        {
                            Positions = snapped, Uvs = Uvs, Parts = ReducedParts(snapped),
                            Root = Root, TriangleCount = surviving
                        };
                    }
                }
                return null;
            }

            private bool TryGetBounds(out Vector3 min, out Vector3 max, out float span)
            {
                min = max = Positions[0];
                span = 0;
                foreach (var position in Positions)
                {
                    if (!Finite(position))
                        return false;
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);
                }
                var size = max - min;
                span = Math.Max(size.X, Math.Max(size.Y, size.Z));
                return span > 0 && Finite(span);
            }

            private static double GridStep(float min, float max, double cellSize)
            {
                // Keep a cell across thin faces and preserve the original bounds on each axis.
                double span = (double)max - min;
                return span / Math.Max(1, Math.Round(span / cellSize));
            }

            private static float Snap(float value, float min, double step)
            {
                return step == 0 ? min : (float)(min + Math.Round(((double)value - min) / step) * step);
            }

            private int CountTriangles(Vector3[] snapped, int maximumTriangles)
            {
                int count = 0;
                foreach (var part in Parts)
                {
                    var indices = part.m_indices;
                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        if (Survives(snapped, indices[i], indices[i + 1], indices[i + 2])
                            && ++count > maximumTriangles)
                            return count;
                    }
                }
                return count;
            }

            private List<MyMeshPartInfo> ReducedParts(Vector3[] snapped)
            {
                var parts = new List<MyMeshPartInfo>(Parts.Count);
                foreach (var part in Parts)
                {
                    MyMeshPartInfo reduced = null;
                    var indices = part.m_indices;
                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                        if (!Survives(snapped, a, b, c))
                            continue;

                        if (reduced == null)
                        {
                            reduced = new MyMeshPartInfo
                            {
                                m_MaterialHash = part.m_MaterialHash,
                                m_MaterialDesc = part.m_MaterialDesc,
                                Technique = part.Technique
                            };
                            parts.Add(reduced);
                        }
                        // Keep corner indices so texture coordinates and material seams are retained.
                        reduced.m_indices.Add(a);
                        reduced.m_indices.Add(b);
                        reduced.m_indices.Add(c);
                    }
                }
                return parts;
            }

            private static bool Survives(Vector3[] snapped, int a, int b, int c)
            {
                var normal = Vector3.Cross(snapped[b] - snapped[a], snapped[c] - snapped[a]);
                return normal != Vector3.Zero && Finite(normal);
            }
        }

        private sealed class SnapshotBuilder
        {
            private readonly MyCubeGrid grid;
            private readonly MatrixD inverseGrid;
            private readonly List<ModelPart> parts = new List<ModelPart>();
            private readonly Dictionary<MyModel, ModelAsset> assets = new Dictionary<MyModel, ModelAsset>();
            private readonly Dictionary<MyStringHash, Dictionary<string, string>> skins
                = new Dictionary<MyStringHash, Dictionary<string, string>>();
            private string captureNote;

            public SnapshotBuilder(MyCubeGrid grid)
            {
                this.grid = grid;
                inverseGrid = MatrixD.Invert(grid.WorldMatrix);
            }

            public Snapshot Build(int blockCount)
            {
                string contentRoot = LocalRoot(MyFileSystem.ContentPath);
                if (contentRoot == null)
                    throw new CaptureLimitException("The game model folder is unavailable.");
                var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { contentRoot };
                foreach (var block in grid.GetBlocks())
                {
                    var context = block.BlockDefinition?.Context;
                    var root = context != null && !context.IsBaseGame ? LocalRoot(context.ModPath) : null;
                    if (root != null && roots.Count < MaximumRoots)
                        roots.Add(root);
                }

                // The game's render cells already omit hidden armor faces.
                var visibleTiles = new HashSet<MyCubePart>();
                foreach (var cell in grid.RenderData.Cells)
                    foreach (var entry in cell.Value.CubeParts)
                    {
                        if (visibleTiles.Add(entry.Key) && visibleTiles.Count > MaximumParts)
                            throw new CaptureLimitException("This ship exceeds the textured preview model part limit.");
                    }

                // Some slope models are translated away from their cell, so use cube ownership.
                var blocks = grid.GetBlocks();
                foreach (var block in blocks)
                {
                    if (visibleTiles.Count == 0)
                        break;
                    if (block.BlockDefinition.CubeDefinition == null)
                        continue;
                    var position = block.Position;
                    var owner = new Point3(position.X, position.Y, position.Z);
                    for (int x = block.Min.X; x <= block.Max.X; x++)
                        for (int y = block.Min.Y; y <= block.Max.Y; y++)
                            for (int z = block.Min.Z; z <= block.Max.Z; z++)
                                if (grid.TryGetCube(new Vector3I(x, y, z), out var cube) && cube.CubeBlock == block)
                                    foreach (var tile in cube.Parts)
                                        if (visibleTiles.Remove(tile))
                                            CollectTile(tile, owner);
                }
                foreach (var tile in visibleTiles)
                    CollectTile(tile, null);

                foreach (var block in blocks)
                    if (block.FatBlock != null)
                    {
                        var position = block.Position;
                        AddEntity(block.FatBlock, block.ColorMaskHSV, block.SkinSubtypeId,
                            new Point3(position.X, position.Y, position.Z), 0);
                    }

                var rootPaths = new string[roots.Count];
                roots.CopyTo(rootPaths);
                return new Snapshot(parts.ToArray(), blockCount, rootPaths, contentRoot, 1 / grid.GridSize, captureNote);
            }

            private void CollectTile(MyCubePart tile, Point3? position)
            {
                var instance = tile.InstanceData;
                var paint = instance.ColorMaskHSV;
                Collect(tile.Model, instance.LocalMatrix,
                    new Vector2(instance.GetTextureOffset(0), instance.GetTextureOffset(1)),
                    new Vector3(paint.X, paint.Y, paint.Z), tile.SkinSubtypeId, position);
            }

            private void AddEntity(MyEntity entity, Vector3 paint, MyStringHash skin, Point3 position, int depth)
            {
                if (depth > 12)
                {
                    captureNote = captureNote ?? "Deeply nested model parts were skipped.";
                    return;
                }
                Collect(entity.Model, (Matrix)(entity.WorldMatrix * inverseGrid), Vector2.Zero, paint, skin, position);
                if (entity.Subparts != null)
                    foreach (var subpart in entity.Subparts.Values)
                        AddEntity(subpart, paint, skin, position, depth + 1);
            }

            private void Collect(MyModel model, Matrix transform, Vector2 uvOffset, Vector3 paint, MyStringHash skin, Point3? position)
            {
                if (model == null)
                    return;
                if (parts.Count >= MaximumParts)
                    throw new CaptureLimitException("This ship exceeds the textured preview model part limit.");
                if (!assets.TryGetValue(model, out var asset) && assets.Count < MaximumAssets)
                {
                    asset = new ModelAsset(model);
                    assets.Add(model, asset);
                }
                parts.Add(new ModelPart(asset, transform, model.BoundingBox, uvOffset, PaintRgb(paint),
                    SnapshotSkin(skin), position));
            }

            private Dictionary<string, string> SnapshotSkin(MyStringHash skin)
            {
                if (skin == MyStringHash.NullOrEmpty)
                    return null;
                if (skins.TryGetValue(skin, out var copy))
                    return copy;
                if (skins.Count >= MaximumAssets)
                    return null;

                var changes = MyDefinitionManager.Static.GetAssetModifierDefinitionForRender(skin).SkinTextureChanges;
                if (changes != null && changes.Count > 0)
                {
                    copy = new Dictionary<string, string>(StringComparer.Ordinal);
                    foreach (var item in changes)
                    {
                        if (copy.Count >= MaximumSkinMaterials)
                            break;
                        copy.Add(item.Key.ToString(), item.Value.ColorMetalFileName);
                    }
                }
                skins.Add(skin, copy);
                return copy;
            }
        }

        private sealed class MeshCapture
        {
            private readonly Snapshot snapshot;
            private readonly Dictionary<string, Template> models;
            private readonly HashSet<string> roots;
            private readonly string workshopRoot;
            private readonly Dictionary<string, string> textures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly List<TexturedShipVertex> vertices = new List<TexturedShipVertex>();
            private readonly List<TexturedShipTriangle> triangles = new List<TexturedShipTriangle>();
            private readonly MyModelImporter importer = new MyModelImporter();
            private readonly Vector3[] boxCorners = new Vector3[8];
            private Vector3[] positions = new Vector3[0];
            private int[] vertexIndices = new int[0];
            private long modelBytes;
            private int simplifiedModels, reducedModels, realModels;
            private string simplificationReason;

            public MeshCapture(Snapshot snapshot, Dictionary<string, Template> models)
            {
                this.snapshot = snapshot;
                this.models = models;
                roots = new HashSet<string>(snapshot.Roots, StringComparer.OrdinalIgnoreCase);
                var steamApps = new DirectoryInfo(snapshot.ContentRoot).Parent?.Parent?.Parent;
                if (steamApps != null && steamApps.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                    workshopRoot = Path.Combine(steamApps.FullName, "workshop", "content", WorkshopAssetPath.SpaceEngineersId);
                simplificationReason = snapshot.CaptureNote;
                foreach (var template in models.Values)
                    if (template != null)
                        modelBytes += template.SourceBytes;
            }

            public TexturedShipMesh Build()
            {
                int proxyTriangles = snapshot.Parts.Length <= MaximumTriangles / BoxTriangles ? BoxTriangles : 2;
                var templates = new Template[snapshot.Parts.Length];
                var occurrences = new Dictionary<Template, int>();
                for (int i = 0; i < templates.Length; i++)
                {
                    var template = FindTemplate(snapshot.Parts[i].Asset);
                    templates[i] = template;
                    if (template != null)
                    {
                        occurrences.TryGetValue(template, out int count);
                        occurrences[template] = count + 1;
                    }
                }

                int modelBudget = ModelTriangleBudget(occurrences, proxyTriangles);
                var reduced = new Dictionary<Template, Template>();
                for (int partIndex = 0; partIndex < templates.Length; partIndex++)
                {
                    var source = snapshot.Parts[partIndex];
                    var template = templates[partIndex];
                    if (template == null)
                    {
                        AddProxy(source, partIndex, proxyTriangles, source.Asset == null
                            ? "The ship exceeds the preview model asset limit."
                            : "Some model assets could not be loaded.");
                        continue;
                    }

                    bool needsReduction = template.TriangleCount > modelBudget;
                    if (needsReduction)
                    {
                        if (!reduced.TryGetValue(template, out var preview))
                        {
                            preview = template.Reduce(modelBudget);
                            reduced.Add(template, preview);
                        }
                        if (preview == null)
                        {
                            AddProxy(source, partIndex, proxyTriangles, "Some models exceeded the preview triangle budget.");
                            continue;
                        }
                        template = preview;
                    }
                    AddModel(source, partIndex, template, needsReduction, proxyTriangles);
                }

                if (realModels == 0)
                    throw new CaptureLimitException(simplificationReason ?? "No readable ship meshes were available.");
                return new TexturedShipMesh(vertices.ToArray(), triangles.ToArray(), snapshot.Parts, simplifiedModels, reducedModels, simplificationReason);
            }

            private int ModelTriangleBudget(Dictionary<Template, int> occurrences, int proxyTriangles)
            {
                // Give each part room for a fallback. Let detailed models share the rest
                // so the first few large meshes cannot crowd out every later block.
                long reserved = (long)snapshot.Parts.Length * proxyTriangles;
                int low = proxyTriangles, high = MaximumTriangles;
                while (low < high)
                {
                    int limit = low + (high - low + 1) / 2;
                    long used = reserved;
                    foreach (var entry in occurrences)
                        used += (long)Math.Max(0, Math.Min(limit, entry.Key.TriangleCount) - proxyTriangles) * entry.Value;
                    if (used <= MaximumTriangles)
                        low = limit;
                    else
                        high = limit - 1;
                }
                return low;
            }

            private Template FindTemplate(ModelAsset model)
            {
                if (model == null)
                    return null;
                string assetName = model.Name;
                if (!models.TryGetValue(assetName, out var template) && models.Count < MaximumAssets)
                {
                    try
                    {
                        template = LoadTemplate(model);
                    }
                    catch (Exception error) when (error is CaptureLimitException || IsUnreadableAsset(error))
                    {
                        template = null;
                        if (error is CaptureLimitException)
                            simplificationReason = simplificationReason ?? error.Message;
                    }
                    models.Add(assetName, template);
                }
                return template;
            }

            private void AddModel(ModelPart source, int partIndex, Template template, bool reduced, int proxyTriangles)
            {
                var model = source.Asset;
                var modelPositions = template.Positions;
                if (positions.Length < modelPositions.Length)
                {
                    positions = new Vector3[modelPositions.Length];
                    vertexIndices = new int[modelPositions.Length];
                }
                else
                    Array.Clear(vertexIndices, 0, modelPositions.Length);
                for (int i = 0; i < modelPositions.Length; i++)
                {
                    Vector3 position = Vector3.Transform(modelPositions[i] * model.Scale, source.Transform) * snapshot.CellsPerMeter;
                    if (!Finite(position))
                    {
                        AddProxy(source, partIndex, proxyTriangles, "Some parts had invalid model coordinates.");
                        return;
                    }
                    positions[i] = position;
                }

                float scale = Finite(model.PatternScale) && model.PatternScale > .0001f ? model.PatternScale : 1;
                foreach (var part in template.Parts)
                {
                    var material = part.m_MaterialDesc;
                    string colorPath = TextureSlot(material, "ColorMetalTexture");
                    string colorTexture = ResolveTexture(colorPath, template.Root);
                    if (source.SkinColors != null && material?.MaterialName != null
                        && source.SkinColors.TryGetValue(material.MaterialName, out var skinColor))
                        colorTexture = ResolveTexture(skinColor, template.Root) ?? colorTexture;

                    var indices = part.m_indices;
                    for (int i = 0; i < indices.Count; i += 3)
                    {
                        int a = VertexIndex(template, indices[i], scale, source.UvOffset);
                        int b = VertexIndex(template, indices[i + 1], scale, source.UvOffset);
                        int c = VertexIndex(template, indices[i + 2], scale, source.UvOffset);
                        triangles.Add(new TexturedShipTriangle(a, b, c, colorTexture, source.Paint, partIndex));
                    }
                }
                realModels++;
                if (reduced)
                    reducedModels++;
            }

            private int VertexIndex(Template template, int index, float scale, Vector2 offset)
            {
                int stored = vertexIndices[index];
                if (stored != 0)
                    return stored - 1;

                int next = vertices.Count;
                vertices.Add(new TexturedShipVertex(positions[index], TextureCoordinate(template, index, scale, offset)));
                vertexIndices[index] = next + 1;
                return next;
            }

            private void AddProxy(ModelPart source, int partIndex, int proxyTriangles, string reason)
            {
                simplificationReason = simplificationReason ?? reason;
                simplifiedModels++;
                AddPaintedShape(vertices, triangles, boxCorners, source, partIndex, snapshot.CellsPerMeter, proxyTriangles);
            }

            private Template LoadTemplate(ModelAsset model)
            {
                string asset = ResolveModel(model.Name, snapshot.ContentRoot);
                if (asset == null)
                    throw new InvalidDataException("A ship model is unavailable for textured view.");
                string root = FindRoot(asset);
                var lods = model.Lods;
                if (lods.Length == 0 && new FileInfo(asset).Length <= MaximumModelBytes)
                {
                    // Partially loaded game models can omit LODs even when the local asset provides them.
                    importer.ImportData(asset, LodTags);
                    if (importer.GetTagData().TryGetValue("LODs", out var tag))
                        lods = CopyLods(tag as MyLODDescriptor[]);
                }
                // Use the coarsest model the game ships for this asset at HUD scale.
                foreach (string name in lods)
                {
                    string lod = ResolveLod(name, asset, root);
                    if (lod != null && !string.Equals(lod, asset, StringComparison.OrdinalIgnoreCase))
                    {
                        var preview = Import(lod, root);
                        if (preview != null)
                            return preview;
                    }
                }
                var template = Import(asset, root);
                if (template == null && importer.GetTagData().TryGetValue("GeometryDataAsset", out var geometryTag))
                {
                    // A model descriptor can keep its vertex and texture data in a separate file.
                    string geometry = ResolveModel(geometryTag as string, root);
                    if (geometry != null && !string.Equals(geometry, asset, StringComparison.OrdinalIgnoreCase))
                        template = Import(geometry, root);
                }
                return template ?? throw new InvalidDataException("A ship model has no readable mesh.");
            }

            private string ResolveLod(string name, string asset, string root)
            {
                string declared = ResolveModel(name, root);
                if (declared != null || string.IsNullOrWhiteSpace(name))
                    return declared;

                if (name.EndsWith(".mwm", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4);
                int suffix = name.LastIndexOf("_LOD", StringComparison.OrdinalIgnoreCase);
                if (suffix < 0 || !int.TryParse(name.Substring(suffix + 4), NumberStyles.None,
                    CultureInfo.InvariantCulture, out int level) || level <= 0)
                    return null;

                // A mod can move or rename a model and its LODs without updating the embedded paths.
                string sibling = Path.Combine(Path.GetDirectoryName(asset),
                    Path.GetFileNameWithoutExtension(asset) + name.Substring(suffix) + ".mwm");
                return ResolveModel(sibling, root);
            }

            private string ResolveModel(string asset, string root)
            {
                if (string.IsNullOrWhiteSpace(asset))
                    return null;
                if (!asset.EndsWith(".mwm", StringComparison.OrdinalIgnoreCase))
                    asset += ".mwm";
                return ResolveAsset(asset, root, ".mwm");
            }

            private Template Import(string path, string root)
            {
                long bytes = new FileInfo(path).Length;
                if (bytes > MaximumModelBytes)
                    throw new InvalidDataException("A model exceeds the textured preview asset size limit.");
                if (bytes > MaximumTotalModelBytes - modelBytes)
                    throw new CaptureLimitException("The ship exceeds the preview model file budget.");
                modelBytes += bytes;
                // Read mesh tags on a private importer. Shared models and game render state stay untouched.
                importer.ImportData(path, ModelTags);
                var data = importer.GetTagData();
                if (!data.TryGetValue("Vertices", out var verticesTag) || !(verticesTag is HalfVector4[] vertices)
                    || vertices.Length == 0 || vertices.Length > 3 * MaximumTriangles
                    || !data.TryGetValue("MeshParts", out var partsTag) || !(partsTag is List<MyMeshPartInfo> parts)
                    || parts.Count == 0 || parts.Count > MaximumMaterialParts)
                    return null;

                int triangleCount = 0;
                foreach (var part in parts)
                {
                    if (part.m_indices.Count % 3 != 0 || part.m_indices.Count / 3 > MaximumTriangles - triangleCount)
                        return null;
                    triangleCount += part.m_indices.Count / 3;
                    foreach (int index in part.m_indices)
                        if (index < 0 || index >= vertices.Length)
                            return null;
                }

                if (triangleCount == 0)
                    return null;
                var unpacked = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                    unpacked[i] = VF_Packer.UnpackPosition(vertices[i]);
                data.TryGetValue("TexCoords0", out var uvTag);
                return new Template
                {
                    Positions = unpacked, Uvs = uvTag as HalfVector2[], Parts = parts,
                    Root = root, TriangleCount = triangleCount, SourceBytes = bytes
                };
            }

            private string ResolveTexture(string texture, string root)
            {
                if (string.IsNullOrWhiteSpace(texture))
                    return null;
                string key = root + "|" + texture;
                if (!textures.TryGetValue(key, out var path))
                {
                    if (textures.Count >= MaximumTexturePaths)
                        return null;
                    path = ResolveAsset(texture, root, ".dds");
                    textures.Add(key, path);
                }
                return path;
            }

            private string ResolveAsset(string asset, string preferredRoot, string extension)
            {
                if (string.IsNullOrWhiteSpace(asset) || !asset.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    return null;
                try
                {
                    bool rooted = Path.IsPathRooted(asset);
                    string path = Path.GetFullPath(rooted ? asset : Path.Combine(preferredRoot, asset));
                    if (IsSafeAsset(path))
                        return path;
                    if (!rooted && !string.Equals(preferredRoot, snapshot.ContentRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        path = Path.GetFullPath(Path.Combine(snapshot.ContentRoot, asset));
                        if (path.StartsWith(snapshot.ContentRoot, StringComparison.OrdinalIgnoreCase) && IsSafeAsset(path))
                            return path;
                    }

                    string folder = extension == ".mwm" ? "Models" : "Textures";
                    if (WorkshopAssetPath.TrySplit(asset, folder, out string modId, out string relativePath))
                        return ResolveWorkshopAsset(modId, relativePath);
                }
                catch (Exception error) when (IsUnreadableAsset(error))
                {
                    // Unsupported paths leave the material without its texture.
                }
                return null;
            }

            private bool IsSafeAsset(string path)
            {
                return IsSafeAsset(path, FindRoot(path));
            }

            private static bool IsSafeAsset(string path, string root)
            {
                if (root == null || IsNetworkPath(path) || !path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                    return false;
                for (string entry = path; entry != null && entry.Length >= root.Length - 1; entry = Path.GetDirectoryName(entry))
                    if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                        return false;
                return true;
            }

            private string ResolveWorkshopAsset(string modId, string relativePath)
            {
                // Prefer the installed root captured from the grid's active mod context.
                foreach (string root in roots)
                {
                    var directory = new DirectoryInfo(root);
                    if (directory.Name != modId || directory.Parent?.Name != WorkshopAssetPath.SpaceEngineersId)
                        continue;
                    string candidate = Path.GetFullPath(Path.Combine(root, relativePath));
                    if (IsSafeAsset(candidate, root))
                        return candidate;
                }

                if (workshopRoot == null || roots.Count >= MaximumRoots)
                    return null;
                string installedRoot = LocalRoot(Path.Combine(workshopRoot, modId));
                string path = Path.GetFullPath(Path.Combine(installedRoot, relativePath));
                if (!IsSafeAsset(path, installedRoot))
                    return null;
                roots.Add(installedRoot);
                return path;
            }

            private string FindRoot(string path)
            {
                string best = null;
                foreach (string root in roots)
                    if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && (best == null || root.Length > best.Length))
                        best = root;
                return best;
            }
        }

        private static string[] CopyLods(MyLODDescriptor[] lods)
        {
            int count = Math.Min(MaximumLods, lods?.Length ?? 0);
            var paths = new string[count];
            for (int i = 0; i < count; i++)
                paths[i] = lods[lods.Length - 1 - i]?.Model;
            return paths;
        }

        private static bool SamePaths(string[] first, string[] second)
        {
            if (first.Length != second.Length)
                return false;
            for (int i = 0; i < first.Length; i++)
                if (first[i] != second[i])
                    return false;
            return true;
        }

        private static string LocalRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || IsNetworkPath(path))
                return null;
            string absolute = Path.GetFullPath(path);
            return absolute.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        private static bool IsNetworkPath(string path)
        {
            return path.StartsWith("\\\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal);
        }

        private static string TextureSlot(MyMaterialDescriptor material, string name)
        {
            return material?.Textures != null && material.Textures.TryGetValue(name, out var value) ? value : null;
        }

        private static Vector2 TextureCoordinate(Template template, int index, float scale, Vector2 offset)
        {
            if (template.Uvs == null || index >= template.Uvs.Length)
                return offset;
            var uv = template.Uvs[index].ToVector2() / scale + offset;
            return Finite(uv.X) && Finite(uv.Y) ? uv : Vector2.Zero;
        }

        private static void AddPaintedShape(List<TexturedShipVertex> vertices, List<TexturedShipTriangle> triangles, Vector3[] corners,
            ModelPart source, int partIndex, float cellsPerMeter, int triangleCount)
        {
            var bounds = source.Bounds;
            if (triangles.Count > MaximumTriangles - triangleCount || !Finite(bounds.Min) || !Finite(bounds.Max)
                || bounds.Min.X > bounds.Max.X || bounds.Min.Y > bounds.Max.Y || bounds.Min.Z > bounds.Max.Z
                || bounds.Min == bounds.Max)
                return;

            int flatAxis = 0;
            if (triangleCount == 2)
            {
                // Preserve the orientation of the broadest face when a full box would exhaust the budget.
                var size = bounds.Max - bounds.Min;
                flatAxis = size.X <= size.Y && size.X <= size.Z ? 1 : size.Y <= size.Z ? 2 : 4;
            }

            Vector3 center = (bounds.Min + bounds.Max) * .5f;
            for (int i = 0; i < corners.Length; i++)
            {
                var point = new Vector3(flatAxis == 1 ? center.X : (i & 1) == 0 ? bounds.Min.X : bounds.Max.X,
                    flatAxis == 2 ? center.Y : (i & 2) == 0 ? bounds.Min.Y : bounds.Max.Y,
                    flatAxis == 4 ? center.Z : (i & 4) == 0 ? bounds.Min.Z : bounds.Max.Z);
                corners[i] = Vector3.Transform(point, source.Transform) * cellsPerMeter;
                if (!Finite(corners[i]))
                    return;
            }

            int first = vertices.Count;
            if (flatAxis != 0)
            {
                int u = flatAxis == 1 ? 2 : 1, v = flatAxis == 4 ? 2 : 4;
                vertices.Add(new TexturedShipVertex(corners[0], Vector2.Zero));
                vertices.Add(new TexturedShipVertex(corners[u], Vector2.Zero));
                vertices.Add(new TexturedShipVertex(corners[u | v], Vector2.Zero));
                vertices.Add(new TexturedShipVertex(corners[v], Vector2.Zero));
                triangles.Add(new TexturedShipTriangle(first, first + 1, first + 2, null, source.Paint, partIndex));
                triangles.Add(new TexturedShipTriangle(first, first + 2, first + 3, null, source.Paint, partIndex));
                return;
            }
            foreach (var corner in corners)
                vertices.Add(new TexturedShipVertex(corner, Vector2.Zero));
            for (int i = 0; i < BoxIndices.Length; i += 3)
                triangles.Add(new TexturedShipTriangle(first + BoxIndices[i], first + BoxIndices[i + 1],
                    first + BoxIndices[i + 2], null, source.Paint, partIndex));
        }

        private static uint PaintRgb(Vector3 paint)
        {
            var color = MyColorPickerConstants.HSVOffsetToHSV(Finite(paint) ? paint : Vector3.Zero).HSVtoColor();
            return (uint)color.R << 16 | (uint)color.G << 8 | color.B;
        }

        private static bool IsUnreadableAsset(Exception error)
        {
            return error is InvalidDataException || error is IOException || error is UnauthorizedAccessException
                || error is ArgumentException || error is InvalidOperationException || error is FormatException
                || error is OverflowException || error is InvalidCastException || error is NotSupportedException;
        }

        private static bool Finite(Vector3 value)
        {
            return Finite(value.X) && Finite(value.Y) && Finite(value.Z);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

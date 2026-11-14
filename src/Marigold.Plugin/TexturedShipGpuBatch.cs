using VRageMath;

namespace Marigold
{
    internal readonly struct TexturedShipGpuInstance
    {
        // Local model positions are transformed directly into grid cells.
        public readonly Matrix Transform;
        public readonly Vector2 UvOffset;
        public readonly float UvScale;
        public readonly uint PaintRgb;
        public readonly int Part;

        public TexturedShipGpuInstance(Matrix transform, Vector2 uvOffset, float uvScale, uint paintRgb, int part)
        {
            Transform = transform;
            UvOffset = uvOffset;
            UvScale = uvScale;
            PaintRgb = paintRgb;
            Part = part;
        }
    }

    internal sealed class TexturedShipGpuBatch
    {
        // Skin variants can share these immutable geometry arrays.
        public readonly TexturedShipVertex[] Vertices;
        public readonly int[] Indices;
        public readonly string ColorTexture;
        public readonly TexturedShipGpuInstance[] Instances;

        public TexturedShipGpuBatch(TexturedShipVertex[] vertices, int[] indices, string colorTexture,
            TexturedShipGpuInstance[] instances)
        {
            Vertices = vertices;
            Indices = indices;
            ColorTexture = colorTexture;
            Instances = instances;
        }
    }
}

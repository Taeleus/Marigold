using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VRageMath.PackedVector;
using VRageRender.Import;

namespace Marigold
{
    // Read only the indexed MWM tags the preview uses. Keen's general importer
    // allocates from embedded counts before checking that their payload exists.
    internal sealed class TexturedModelReader
    {
        private const int MinIndexedVersion = 1066002;
        private const int MaxKnownVersion = 1157002;
        private const int MaxHeaderEntries = 256;
        private const int MaxMaterialEntries = 256;
        private const int MaxTextBytes = 8192;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        private readonly BinaryReader reader;
        private readonly Stream stream;
        private readonly Dictionary<string, int> offsets = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly int version;
        private long end;

        public TexturedModelReader(BinaryReader reader)
        {
            this.reader = reader;
            stream = reader.BaseStream;
            end = stream.Length;
            if (ReadText() != "Debug")
                throw InvalidModel();
            int debugCount = ReadCount(MaxHeaderEntries, 1);
            string header = debugCount > 0 ? ReadText() : "";
            const string prefix = "Version:";
            if (!header.StartsWith(prefix, StringComparison.Ordinal)
                || !int.TryParse(header.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out version)
                || version < MinIndexedVersion || version > MaxKnownVersion)
                throw new InvalidDataException("The textured preview does not support this model format.");
            for (int i = 1; i < debugCount; i++)
                SkipText();

            int tagCount = ReadCount(MaxHeaderEntries, 5);
            var uniqueOffsets = new HashSet<int>();
            for (int i = 0; i < tagCount; i++)
            {
                string name = ReadText();
                Require(4);
                int offset = reader.ReadInt32();
                if (offsets.ContainsKey(name) || !uniqueOffsets.Add(offset))
                    throw InvalidModel();
                offsets.Add(name, offset);
            }
            foreach (int offset in offsets.Values)
                if (offset < stream.Position || offset >= stream.Length)
                    throw InvalidModel();
        }

        public string[] ReadLods(int limit)
        {
            if (!OpenTag("LODs"))
                return Array.Empty<string>();
            int count = ReadCount(MaxHeaderEntries, 6);
            var paths = new string[Math.Min(limit, count)];
            for (int i = 0; i < count; i++)
            {
                Skip(4); // Distance.
                int index = count - 1 - i;
                if (index < paths.Length)
                    paths[index] = ReadText();
                else
                    SkipText();
                SkipText(); // Render quality.
            }
            return paths;
        }

        public string ReadGeometryAsset()
        {
            return OpenTag("GeometryDataAsset") ? ReadText() : null;
        }

        public HalfVector4[] ReadVertices(int limit)
        {
            if (!OpenTag("Vertices"))
                return Array.Empty<HalfVector4>();
            var vertices = new HalfVector4[ReadCount(limit, 8)];
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].PackedValue = reader.ReadUInt64();
            return vertices;
        }

        public HalfVector2[] ReadUvs(int limit)
        {
            if (!OpenTag("TexCoords0"))
                return null;
            var uvs = new HalfVector2[ReadCount(limit, 4)];
            for (int i = 0; i < uvs.Length; i++)
                uvs[i].PackedValue = reader.ReadUInt32();
            return uvs;
        }

        public List<MyMeshPartInfo> ReadParts(int partLimit, int indexLimit, int vertexCount, out int triangles)
        {
            triangles = 0;
            if (!OpenTag("MeshParts"))
                return null;
            int count = ReadCount(partLimit, 9);
            var parts = new List<MyMeshPartInfo>(count);
            for (int i = 0; i < count; i++)
            {
                Skip(4); // Material hash.
                int indexCount = ReadCount(indexLimit, 4);
                if (indexCount % 3 != 0)
                    throw InvalidModel();
                indexLimit -= indexCount;
                triangles += indexCount / 3;
                var part = new MyMeshPartInfo { m_indices = new List<int>(indexCount) };
                for (int j = 0; j < indexCount; j++)
                {
                    int index = reader.ReadInt32();
                    if ((uint)index >= (uint)vertexCount)
                        throw InvalidModel();
                    part.m_indices.Add(index);
                }
                Require(1);
                if (reader.ReadBoolean())
                    part.m_MaterialDesc = ReadMaterial();
                parts.Add(part);
            }
            return parts;
        }

        private MyMaterialDescriptor ReadMaterial()
        {
            string name = ReadText();
            string color = null;
            int textureCount = ReadCount(MaxMaterialEntries, 2);
            for (int i = 0; i < textureCount; i++)
            {
                if (ReadText() == "ColorMetalTexture")
                    color = ReadText();
                else
                    SkipText();
            }
            if (version >= 1068001)
            {
                int userCount = ReadCount(MaxMaterialEntries, 2);
                for (int i = 0; i < userCount; i++)
                {
                    SkipText();
                    SkipText();
                }
            }
            if (version < 1157001)
                Skip(7 * 4); // Legacy material parameters.
            if (ReadText() == "GLASS")
            {
                SkipText();
                SkipText();
                Skip(1);
            }
            var material = new MyMaterialDescriptor(string.IsNullOrEmpty(name) ? null : name);
            if (!string.IsNullOrEmpty(color))
                material.Textures.Add("ColorMetalTexture", color);
            return material;
        }

        private bool OpenTag(string name)
        {
            if (!offsets.TryGetValue(name, out int offset))
                return false;
            end = stream.Length;
            foreach (int candidate in offsets.Values)
                if (candidate > offset && candidate < end)
                    end = candidate;
            stream.Position = offset;
            if (ReadText() != name)
                throw InvalidModel();
            return true;
        }

        private int ReadCount(int limit, int minimumBytes)
        {
            Require(4);
            int count = reader.ReadInt32();
            if (count < 0 || count > limit || count > (end - stream.Position) / minimumBytes)
                throw InvalidModel();
            return count;
        }

        private string ReadText()
        {
            int length = ReadTextLength();
            byte[] bytes = reader.ReadBytes(length);
            if (bytes.Length != length)
                throw InvalidModel();
            return Utf8.GetString(bytes);
        }

        private void SkipText()
        {
            int length = ReadTextLength();
            stream.Position += length;
        }

        private int ReadTextLength()
        {
            int length = 0;
            for (int shift = 0; shift <= 14; shift += 7)
            {
                Require(1);
                int next = reader.ReadByte();
                length |= (next & 127) << shift;
                if ((next & 128) == 0)
                {
                    if (length > MaxTextBytes)
                        throw InvalidModel();
                    Require(length);
                    return length;
                }
            }
            throw InvalidModel();
        }

        private void Skip(int bytes)
        {
            Require(bytes);
            stream.Position += bytes;
        }

        private void Require(int bytes)
        {
            if (bytes > end - stream.Position)
                throw InvalidModel();
        }

        private static InvalidDataException InvalidModel()
        {
            return new InvalidDataException("A textured model contains an invalid tag or count.");
        }
    }
}

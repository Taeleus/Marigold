using System;
using System.IO;

namespace Marigold
{
    // CPU color sampler for the HUD miniature. Read one small DDS mip, leaving the
    // original texture and the game's render resources alone.
    internal sealed class TexturedShipTexture
    {
        private const uint DdsMagic = 0x20534444;
        private const uint Dxt1 = 0x31545844;
        private const uint Dxt3 = 0x33545844;
        private const uint Dxt5 = 0x35545844;
        private const uint Dx10 = 0x30315844;

        private enum Format
        {
            Rgba, Bgra, Bc1, Bc2, Bc3, Bc7
        }

        private readonly int width, height;
        private readonly uint[] pixels;

        private TexturedShipTexture(int width, int height, uint[] pixels)
        {
            this.width = width;
            this.height = height;
            this.pixels = pixels;
        }

        public uint Sample(double u, double v)
        {
            int x = (int)((u - Math.Floor(u)) * width);
            int y = (int)((v - Math.Floor(v)) * height);
            return (uint)x < (uint)width && (uint)y < (uint)height ? pixels[y * width + x] : 0;
        }

        public static bool TryLoad(string assetPath, out TexturedShipTexture result, int maximumSide = 256)
        {
            result = null;
            if (string.IsNullOrEmpty(assetPath)
                || assetPath.StartsWith("\\\\", StringComparison.Ordinal)
                || assetPath.StartsWith("//", StringComparison.Ordinal)
                || !assetPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                if (!Path.IsPathRooted(assetPath)
                    || (Path.DirectorySeparatorChar == '\\' && (assetPath.Length < 3 || assetPath[1] != ':'
                        || (assetPath[2] != '\\' && assetPath[2] != '/'))))
                    return false;
                using (var reader = new BinaryReader(File.OpenRead(assetPath)))
                    result = Read(reader, maximumSide);
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            return result != null;
        }

        private static TexturedShipTexture Read(BinaryReader reader, int maximumSide)
        {
            if (reader.ReadUInt32() != DdsMagic || reader.ReadUInt32() != 124)
                return null;

            uint flags = reader.ReadUInt32();
            int height = reader.ReadInt32(), width = reader.ReadInt32();
            uint pitch = reader.ReadUInt32();
            uint depth = reader.ReadUInt32();
            int levels = Math.Max(1, reader.ReadInt32());
            if (width < 1 || height < 1 || width > 16384 || height > 16384 || levels > 15 || depth > 1)
                return null;

            reader.BaseStream.Position = 76;
            if (reader.ReadUInt32() != 32)
                return null;
            uint pixelFlags = reader.ReadUInt32();
            uint fourCc = reader.ReadUInt32(), bits = reader.ReadUInt32();
            uint red = reader.ReadUInt32(), green = reader.ReadUInt32(), blue = reader.ReadUInt32();
            reader.BaseStream.Position = 112;
            if ((reader.ReadUInt32() & 0x0020fe00) != 0) // Cubemap faces or a volume texture.
                return null;
            reader.BaseStream.Position = 128;

            Format format;
            if ((pixelFlags & 4) == 0)
            {
                if ((pixelFlags & 0x40) == 0 || bits != 32 || green != 0xff00)
                    return null;
                if (red == 0xff && blue == 0xff0000)
                    format = Format.Rgba;
                else if (red == 0xff0000 && blue == 0xff)
                    format = Format.Bgra;
                else
                    return null;
            }
            else if (fourCc == Dx10)
            {
                int dxgi = reader.ReadInt32();
                uint dimension = reader.ReadUInt32(), misc = reader.ReadUInt32(), arraySize = reader.ReadUInt32();
                reader.ReadUInt32();
                if (dimension != 3 || (misc & 4) != 0 || arraySize != 1)
                    return null;
                switch (dxgi)
                {
                    case 28:
                    case 29: format = Format.Rgba; break;
                    case 87:
                    case 88:
                    case 91:
                    case 93: format = Format.Bgra; break;
                    case 71:
                    case 72: format = Format.Bc1; break;
                    case 74:
                    case 75: format = Format.Bc2; break;
                    case 77:
                    case 78: format = Format.Bc3; break;
                    case 98:
                    case 99: format = Format.Bc7; break;
                    default: return null;
                }
            }
            else
            {
                switch (fourCc)
                {
                    case Dxt1: format = Format.Bc1; break;
                    case Dxt3: format = Format.Bc2; break;
                    case Dxt5: format = Format.Bc3; break;
                    default: return null;
                }
            }

            bool compressed = format >= Format.Bc1;
            int blockBytes = format == Format.Bc1 ? 8 : 16;
            // Legacy uncompressed DDS can specify row padding at its largest mip.
            int rowBytes = width * 4;
            if (!compressed && (flags & 8) != 0 && pitch != 0)
            {
                if (pitch < rowBytes || pitch > rowBytes + 256)
                    return null;
                rowBytes = (int)pitch;
            }
            for (int level = 0; level < levels; level++)
            {
                int byteCount = compressed
                    ? ((width + 3) / 4) * ((height + 3) / 4) * blockBytes
                    : rowBytes * height;
                if (byteCount > reader.BaseStream.Length - reader.BaseStream.Position)
                    return null;
                if (width <= maximumSide && height <= maximumSide)
                {
                    byte[] data = reader.ReadBytes(byteCount);
                    if (data.Length != byteCount)
                        return null;
                    uint[] pixels = compressed
                        ? DecodeBlocks(data, width, height, blockBytes, format)
                        : DecodePixels(data, width, height, rowBytes, format == Format.Bgra);
                    return new TexturedShipTexture(width, height, pixels);
                }

                reader.BaseStream.Position += byteCount;
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
                rowBytes = width * 4;
            }

            return null;
        }

        private static uint[] DecodePixels(byte[] data, int width, int height, int rowBytes, bool bgra)
        {
            var result = new uint[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int offset = y * rowBytes + x * 4;
                    result[y * width + x] = Rgb(data[offset + (bgra ? 2 : 0)], data[offset + 1], data[offset + (bgra ? 0 : 2)]);
                }
            return result;
        }

        private static uint[] DecodeBlocks(byte[] data, int width, int height, int blockBytes, Format format)
        {
            var result = new uint[width * height];
            var block = new uint[16];
            var palette = new uint[4];
            var endpoints = new int[24];
            var indices = new int[16];
            int offset = 0;
            for (int y = 0; y < height; y += 4)
                for (int x = 0; x < width; x += 4, offset += blockBytes)
                {
                    if (format == Format.Bc7)
                        DecodeBc7(data, offset, block, endpoints, indices);
                    else
                    {
                        // BC2/BC3 store alpha in the first eight bytes. In color-metal
                        // textures this is material data, not the silhouette opacity.
                        bool forceFourColors = format != Format.Bc1;
                        DecodeBc1(data, offset + (forceFourColors ? 8 : 0), block, palette, forceFourColors);
                    }
                    for (int row = 0; row < 4 && y + row < height; row++)
                        Array.Copy(block, row * 4, result, (y + row) * width + x, Math.Min(4, width - x));
                }
            return result;
        }

        private static void DecodeBc1(byte[] data, int offset, uint[] block, uint[] palette, bool forceFourColors)
        {
            int a = data[offset] | data[offset + 1] << 8;
            int b = data[offset + 2] | data[offset + 3] << 8;
            palette[0] = From565(a);
            palette[1] = From565(b);
            bool fourColors = forceFourColors || a > b;
            palette[2] = Mix(palette[0], palette[1], fourColors ? 2 : 1, fourColors ? 3 : 2);
            palette[3] = fourColors ? Mix(palette[0], palette[1], 1, 3) : 0;
            for (int i = 0; i < 16; i++)
                block[i] = palette[(data[offset + 4 + i / 4] >> ((i % 4) * 2)) & 3];
        }

        private static uint From565(int value)
        {
            return Rgb(Expand(value >> 11, 5), Expand((value >> 5) & 63, 6), Expand(value & 31, 5));
        }

        private static uint Mix(uint a, uint b, int aWeight, int divisor)
        {
            int bWeight = divisor - aWeight;
            return Rgb(
                ((int)(a >> 16) * aWeight + (int)(b >> 16) * bWeight) / divisor,
                ((int)((a >> 8) & 255) * aWeight + (int)((b >> 8) & 255) * bWeight) / divisor,
                ((int)(a & 255) * aWeight + (int)(b & 255) * bWeight) / divisor);
        }

        private static uint Rgb(int r, int g, int b)
        {
            return (uint)(r << 16 | g << 8 | b);
        }

        private static int Expand(int value, int bits)
        {
            int high = value << (8 - bits);
            return high | (high >> bits);
        }

        // BC7 decoding and partition tables adapted from bcdec (https://github.com/iOrange/bcdec).
        // Copyright (c) 2022 Sergii Kudlai
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy of
        // this software and associated documentation files (the "Software"), to deal in
        // the Software without restriction, including without limitation the rights to
        // use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
        // of the Software, and to permit persons to whom the Software is furnished to do
        // so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.

        private static readonly int[] ColorBits = { 4, 6, 5, 7, 5, 7, 7, 5 };
        private static readonly int[] AlphaBits = { 0, 0, 0, 0, 6, 8, 7, 5 };
        private static readonly int[] SubsetCount = { 3, 2, 3, 2, 1, 1, 1, 2 };
        private static readonly int[] IndexBits = { 3, 3, 2, 2, 2, 2, 4, 2 };
        private static readonly int[][] Weights =
        {
            null, null, new[] { 0, 21, 43, 64 }, new[] { 0, 9, 18, 27, 37, 46, 55, 64 },
            new[] { 0, 4, 9, 13, 17, 21, 26, 30, 34, 38, 43, 47, 51, 55, 60, 64 }
        };

        // For each partition, two bits per pixel store the subset and one bit stores
        // whether the pixel is its subset's anchor. Two-subset patterns come first.
        private static readonly uint[] Bc7Subsets =
        {
            0x50505050, 0x40404040, 0x54545454, 0x54505040, 0x50404000, 0x55545450, 0x55545040, 0x54504000,
            0x50400000, 0x55555450, 0x55544000, 0x54400000, 0x55555440, 0x55550000, 0x55555500, 0x55000000,
            0x55150100, 0x00004054, 0x15010000, 0x00405054, 0x00004050, 0x15050100, 0x05010000, 0x40505054,
            0x00404050, 0x05010100, 0x14141414, 0x05141450, 0x01155440, 0x00555500, 0x15014054, 0x05414150,
            0x44444444, 0x55005500, 0x11441144, 0x05055050, 0x05500550, 0x11114444, 0x41144114, 0x44111144,
            0x15055054, 0x01055040, 0x05041050, 0x05455150, 0x14414114, 0x50050550, 0x41411414, 0x00141400,
            0x00041504, 0x00105410, 0x10541000, 0x04150400, 0x50410514, 0x41051450, 0x05415014, 0x14054150,
            0x41050514, 0x41505014, 0x40011554, 0x54150140, 0x50505500, 0x00555050, 0x15151010, 0x54540404,
            0xAA685050, 0x6A5A5040, 0x5A5A4200, 0x5450A0A8, 0xA5A50000, 0xA0A05050, 0x5555A0A0, 0x5A5A5050,
            0xAA550000, 0xAA555500, 0xAAAA5500, 0x90909090, 0x94949494, 0xA4A4A4A4, 0xA9A59450, 0x2A0A4250,
            0xA5945040, 0x0A425054, 0xA5A5A500, 0x55A0A0A0, 0xA8A85454, 0x6A6A4040, 0xA4A45000, 0x1A1A0500,
            0x0050A4A4, 0xAAA59090, 0x14696914, 0x69691400, 0xA08585A0, 0xAA821414, 0x50A4A450, 0x6A5A0200,
            0xA9A58000, 0x5090A0A8, 0xA8A09050, 0x24242424, 0x00AA5500, 0x24924924, 0x24499224, 0x50A50A50,
            0x500AA550, 0xAAAA4444, 0x66660000, 0xA5A0A5A0, 0x50A050A0, 0x69286928, 0x44AAAA44, 0x66666600,
            0xAA444444, 0x54A854A8, 0x95809580, 0x96969600, 0xA85454A8, 0x80959580, 0xAA141414, 0x96960000,
            0xAAAA1414, 0xA05050A0, 0xA0A5A5A0, 0x96000000, 0x40804080, 0xA9A8A9A8, 0xAAAAAA44, 0x2A4A5254,
        };

        private static readonly ushort[] Bc7Anchors =
        {
            0x8001, 0x8001, 0x8001, 0x8001, 0x8001, 0x8001, 0x8001, 0x8001,
            0x8001, 0x8001, 0x8001, 0x8001, 0x8001, 0x8001, 0x8001, 0x8001,
            0x8001, 0x0005, 0x0101, 0x0005, 0x0005, 0x0101, 0x0101, 0x8001,
            0x0005, 0x0101, 0x0005, 0x0005, 0x0101, 0x0101, 0x0005, 0x0005,
            0x8001, 0x8001, 0x0041, 0x0101, 0x0005, 0x0101, 0x8001, 0x8001,
            0x0005, 0x0101, 0x0005, 0x0005, 0x0005, 0x8001, 0x8001, 0x0041,
            0x0041, 0x0005, 0x0041, 0x0101, 0x8001, 0x8001, 0x0005, 0x0005,
            0x8001, 0x8001, 0x8001, 0x8001, 0x8001, 0x0005, 0x0005, 0x8001,
            0x8009, 0x0109, 0x8101, 0x8009, 0x8101, 0x8009, 0x8009, 0x8101,
            0x8101, 0x8101, 0x8041, 0x8041, 0x8041, 0x8021, 0x8009, 0x0109,
            0x8009, 0x0109, 0x8101, 0x8009, 0x8009, 0x0109, 0x8041, 0x0501,
            0x0029, 0x8101, 0x0141, 0x0441, 0x8101, 0x8021, 0x8401, 0x8101,
            0x8101, 0x8009, 0x8009, 0x0421, 0x0441, 0x0501, 0x0301, 0x8401,
            0x8041, 0x8009, 0x8101, 0x8021, 0x8009, 0x8041, 0x8041, 0x8101,
            0x8009, 0x8009, 0x8021, 0x8021, 0x8021, 0x8101, 0x8021, 0x8401,
            0x8021, 0x8401, 0x8101, 0xA001, 0x8009, 0x9001, 0x8009, 0x0109,
        };

        private struct BitReader
        {
            private ulong low, high;

            public BitReader(byte[] data, int offset)
            {
                low = BitConverter.ToUInt64(data, offset);
                high = BitConverter.ToUInt64(data, offset + 8);
            }

            public int Read(int count)
            {
                int value = (int)(low & ((1UL << count) - 1));
                low = (low >> count) | (high << (64 - count));
                high >>= count;
                return value;
            }
        }

        private static void DecodeBc7(byte[] data, int offset, uint[] block, int[] endpoints, int[] indices)
        {
            var reader = new BitReader(data, offset);
            int mode = 0;
            while (mode < 8 && reader.Read(1) == 0)
                mode++;
            if (mode == 8)
            {
                Array.Clear(block, 0, block.Length);
                return;
            }

            int subsetCount = SubsetCount[mode];
            uint subsets = 0;
            int anchors = 1;
            if (subsetCount > 1)
            {
                int partition = reader.Read(mode == 0 ? 4 : 6) + (subsetCount - 2) * 64;
                subsets = Bc7Subsets[partition];
                anchors = Bc7Anchors[partition];
            }

            int rotation = mode == 4 || mode == 5 ? reader.Read(2) : 0;
            bool secondaryColor = mode == 4 && reader.Read(1) != 0;
            int rgbBits = ColorBits[mode], alphaBits = AlphaBits[mode];
            int endpointCount = subsetCount * 2;
            for (int channel = 0; channel < 3; channel++)
                for (int i = 0; i < endpointCount; i++)
                    endpoints[i * 4 + channel] = reader.Read(rgbBits);
            if (alphaBits > 0)
                for (int i = 0; i < endpointCount; i++)
                    endpoints[i * 4 + 3] = reader.Read(alphaBits);

            bool pBits = ((0xcb >> mode) & 1) != 0;
            int pBit = 0;
            for (int i = 0; i < endpointCount; i++)
            {
                if (pBits && (mode != 1 || (i & 1) == 0))
                    pBit = reader.Read(1);
                for (int channel = 0; channel < 4; channel++)
                {
                    int bits = channel == 3 ? alphaBits : rgbBits;
                    int index = i * 4 + channel;
                    if (bits == 0)
                        endpoints[index] = 255;
                    else if (pBits)
                        endpoints[index] = Expand(endpoints[index] * 2 + pBit, bits + 1);
                    else
                        endpoints[index] = Expand(endpoints[index], bits);
                }
            }

            int indexBits = IndexBits[mode];
            int secondaryBits = mode == 4 ? 3 : mode == 5 ? 2 : 0;
            for (int i = 0; i < 16; i++)
                indices[i] = reader.Read(indexBits - ((anchors >> i) & 1));

            for (int i = 0; i < 16; i++)
            {
                int first = (int)((subsets >> (i * 2)) & 3) * 8;
                int colorWeight = Weights[indexBits][indices[i]];
                int alphaWeight = colorWeight;
                if (secondaryBits > 0)
                {
                    int otherWeight = Weights[secondaryBits][reader.Read(secondaryBits - (i == 0 ? 1 : 0))];
                    if (secondaryColor)
                        colorWeight = otherWeight;
                    else
                        alphaWeight = otherWeight;
                }

                int red = Interpolate(endpoints, first, colorWeight);
                int green = Interpolate(endpoints, first + 1, colorWeight);
                int blue = Interpolate(endpoints, first + 2, colorWeight);
                // Modes 4 and 5 can store one RGB channel in the separate alpha stream.
                if (rotation != 0)
                {
                    int alpha = Interpolate(endpoints, first + 3, alphaWeight);
                    if (rotation == 1) red = alpha;
                    else if (rotation == 2) green = alpha;
                    else blue = alpha;
                }
                block[i] = Rgb(red, green, blue);
            }
        }

        private static int Interpolate(int[] endpoints, int first, int weight)
        {
            return (endpoints[first] * (64 - weight) + endpoints[first + 4] * weight + 32) >> 6;
        }
    }
}

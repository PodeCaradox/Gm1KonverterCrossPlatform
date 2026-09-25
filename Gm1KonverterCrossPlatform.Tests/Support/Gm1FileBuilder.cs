using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Tests.Support
{
    /// <summary>
    /// Builds synthetic but valid .gm1 files. The byte layout is written here independently of
    /// <see cref="Gm1File"/>, so the builder can be used to test reading and writing.
    /// <para>
    /// The images are encoded with the new encoders from images like the PNG import creates them
    /// (transparent pixels are <see cref="Argb1555.TransparentMarker"/>, all other pixels have the alpha bit set),
    /// so exporting and importing them again reproduces the same bytes.
    /// </para>
    /// </summary>
    internal sealed class Gm1FileBuilder
    {
        public const int HeaderFieldCount = Gm1FileHeader.ByteSize / sizeof(uint);
        public const int ImageCountField = 3;
        public const int DataTypeField = 5;
        public const int DataSizeField = 20;
        public const int TablesStart = Gm1FileHeader.ByteSize + Palette.ByteSize;
        public const byte GapFiller = 0xCD;

        private Gm1FileBuilder(Gm1DataType dataType)
        {
            DataType = dataType;
        }

        public Gm1DataType DataType { get; }

        /// <summary>The 22 header fields. Image count, data type and data size are set by <see cref="ToBytes"/>.</summary>
        public uint[] HeaderFields { get; } = new uint[HeaderFieldCount];

        public byte[] PaletteBytes { get; } = new byte[Palette.ByteSize];

        public List<Gm1Image> Images { get; } = new List<Gm1Image>();

        public static IEnumerable<object[]> AllDataTypes()
        {
            return Enum.GetValues(typeof(Gm1DataType)).Cast<Gm1DataType>().Select(type => new object[] { type });
        }

        /// <summary>
        /// Creates a file with <paramref name="itemCount"/> images (buildings for <see cref="Gm1DataType.TilesObject"/>),
        /// non-zero unknown header fields and a non-zero palette.
        /// </summary>
        public static Gm1FileBuilder Create(Gm1DataType dataType, int seed = 1, int itemCount = 5)
        {
            var random = new Random(seed * 7919 + (int)dataType);
            var builder = CreateEmpty(dataType, random);

            switch (dataType)
            {
                case Gm1DataType.Interface:
                case Gm1DataType.Font:
                    for (int i = 0; i < itemCount; i++)
                    {
                        var image = TestImages.Random(random, random.Next(1, 70), random.Next(1, 40));
                        builder.AddTgxImage(image, RandomHeader(random, image.Width, image.Height));
                    }

                    break;

                case Gm1DataType.TgxConstSize:
                    int width = random.Next(8, 60);
                    int height = random.Next(8, 40);
                    for (int i = 0; i < itemCount; i++)
                    {
                        var image = TestImages.Random(random, width, height);
                        builder.AddTgxImage(image, RandomHeader(random, width, height));
                    }

                    break;

                case Gm1DataType.Animations:
                    var indexer = new ColorTableIndexer(builder.CreatePalette(), 0);
                    var colors = builder.GetColorTable(0);
                    for (int i = 0; i < itemCount; i++)
                    {
                        var image = ToColorTableColors(TestImages.Random(random, random.Next(1, 60), random.Next(1, 40), colorCount: 8), colors);
                        builder.AddTgxImage(image, RandomHeader(random, image.Width, image.Height), indexer);
                    }

                    break;

                case Gm1DataType.TilesObject:
                    for (int i = 0; i < itemCount; i++)
                    {
                        builder.AddBuilding(random, random.Next(1, 5));
                    }

                    break;

                case Gm1DataType.NoCompression:
                case Gm1DataType.NoCompression1:
                    for (int i = 0; i < itemCount; i++)
                    {
                        var image = TestImages.Random(random, random.Next(1, 40), random.Next(1, 30));
                        builder.AddUncompressedImage(image, RandomHeader(random, image.Width, image.Height + dataType.HeightPadding()));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unknown data type.");
            }

            return builder;
        }

        /// <summary>Header and palette only. Animation files get 10 color tables with 256 different opaque colors each.</summary>
        public static Gm1FileBuilder CreateEmpty(Gm1DataType dataType, int seed = 1) => CreateEmpty(dataType, new Random(seed));

        public Palette CreatePalette() => Palette.Read(PaletteBytes);

        public ushort[] GetColorTable(int table)
        {
            var colors = new ushort[ColorTable.ColorCount];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = BinaryPrimitives.ReadUInt16LittleEndian(PaletteBytes.AsSpan(table * ColorTable.ByteSize + i * sizeof(ushort)));
            }

            return colors;
        }

        public void SetColor(int table, int index, ushort color)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(PaletteBytes.AsSpan(table * ColorTable.ByteSize + index * sizeof(ushort)), color);
        }

        public void AddTgxImage(Argb1555Image image, TgxImageHeader header, IPaletteIndexer? indexer = null)
        {
            header.Width = (ushort)image.Width;
            header.Height = (ushort)image.Height;
            Images.Add(new Gm1Image(header, TgxCodec.Encode(image, TgxEncoderOptions.ForGm1Image(DataType, header, indexer))));
        }

        public void AddUncompressedImage(Argb1555Image image, TgxImageHeader header)
        {
            header.Width = (ushort)image.Width;
            header.Height = (ushort)(image.Height + DataType.HeightPadding());
            Images.Add(new Gm1Image(header, NoCompressionCodec.Encode(image)));
        }

        /// <summary>
        /// Adds a random building. The parts are encoded twice (encode, render, encode), because only the
        /// second encoding is stable: the first one also stores pixels that the rendering cannot reproduce.
        /// </summary>
        public void AddBuilding(Random random, int diamondsPerRow)
        {
            int width = TileObjectCodec.GetImageWidth(diamondsPerRow);
            int height = diamondsPerRow * TileObjectCodec.DiamondHeight + random.Next(0, 80);
            var building = TestImages.Random(random, width, height, colorCount: 6, transparency: 0.4);

            var firstParts = TileObjectCodec.Encode(building);
            var rendered = AsImported(TileObjectCodec.Render(firstParts, TileObjectCodec.GetGroups(firstParts).Single()));
            foreach (var part in TileObjectCodec.Encode(rendered))
            {
                part.Header.AnimatedColor = (byte)random.Next(0, 3);
                Images.Add(part);
            }
        }

        /// <summary>
        /// Serializes the file.
        /// </summary>
        /// <param name="gapsBeforeImage">Number of unused bytes in the data block before the data of an image.</param>
        /// <param name="reverseDataOrder">Stores the data of the last image first.</param>
        public byte[] ToBytes(IReadOnlyDictionary<int, int>? gapsBeforeImage = null, bool reverseDataOrder = false)
        {
            int count = Images.Count;
            var offsets = new uint[count];
            var dataBlock = new List<byte>();
            var order = reverseDataOrder ? Enumerable.Range(0, count).Reverse() : Enumerable.Range(0, count);
            foreach (int i in order)
            {
                if (gapsBeforeImage != null && gapsBeforeImage.TryGetValue(i, out int gap))
                {
                    dataBlock.AddRange(Enumerable.Repeat(GapFiller, gap));
                }

                offsets[i] = (uint)dataBlock.Count;
                dataBlock.AddRange(Images[i].Data);
            }

            var fields = ExpectedHeaderFields(count, dataBlock.Count);
            var bytes = new byte[DataStart(count) + dataBlock.Count];
            for (int i = 0; i < HeaderFieldCount; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * sizeof(uint)), fields[i]);
            }

            PaletteBytes.CopyTo(bytes, Gm1FileHeader.ByteSize);
            for (int i = 0; i < count; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(OffsetEntry(i)), offsets[i]);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(SizeEntry(count, i)), (uint)Images[i].Data.Length);
                WriteImageHeader(Images[i].Header, bytes.AsSpan(ImageHeaderEntry(count, i)));
            }

            dataBlock.CopyTo(bytes, DataStart(count));
            return bytes;
        }

        /// <summary>The header fields as <see cref="ToBytes"/> writes them.</summary>
        public uint[] ExpectedHeaderFields(int imageCount, int dataSize)
        {
            var fields = (uint[])HeaderFields.Clone();
            fields[ImageCountField] = (uint)imageCount;
            fields[DataTypeField] = (uint)DataType;
            fields[DataSizeField] = (uint)dataSize;
            return fields;
        }

        public static int OffsetEntry(int index) => TablesStart + index * sizeof(uint);

        public static int SizeEntry(int imageCount, int index) => TablesStart + (imageCount + index) * sizeof(uint);

        public static int ImageHeaderEntry(int imageCount, int index) => TablesStart + imageCount * 2 * sizeof(uint) + index * TgxImageHeader.ByteSize;

        public static int DataStart(int imageCount) => TablesStart + imageCount * (2 * sizeof(uint) + TgxImageHeader.ByteSize);

        public static uint ReadUInt32(byte[] bytes, int position) => BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(position));

        public static void WriteUInt32(byte[] bytes, int position, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(position), value);

        /// <summary>The image header layout of the original implementation.</summary>
        public static void WriteImageHeader(TgxImageHeader header, Span<byte> destination)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination, header.Width);
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(2), header.Height);
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(4), header.OffsetX);
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(6), header.OffsetY);
            destination[8] = header.ImagePart;
            destination[9] = header.SubParts;
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(10), header.TileOffset);
            destination[12] = header.Direction;
            destination[13] = header.HorizontalOffsetOfImage;
            destination[14] = header.BuildingWidth;
            destination[15] = header.AnimatedColor;
        }

        /// <summary>A header with non-zero values in every field that the encoders do not set.</summary>
        public static TgxImageHeader RandomHeader(Random random, int width, int height)
        {
            return new TgxImageHeader
            {
                Width = (ushort)width,
                Height = (ushort)height,
                OffsetX = (ushort)random.Next(1, ushort.MaxValue),
                OffsetY = (ushort)random.Next(1, ushort.MaxValue),
                ImagePart = (byte)random.Next(1, 256),
                SubParts = (byte)random.Next(1, 256),
                TileOffset = (ushort)random.Next(1, ushort.MaxValue),
                Direction = (byte)random.Next(1, 256),
                HorizontalOffsetOfImage = (byte)random.Next(1, 256),
                BuildingWidth = (byte)random.Next(1, 256),
                AnimatedColor = (byte)random.Next(0, 3)
            };
        }

        /// <summary>Replaces every visible pixel with a color of the color table.</summary>
        public static Argb1555Image ToColorTableColors(Argb1555Image image, IReadOnlyList<ushort> colors)
        {
            var pixels = image.Pixels
                .Select(p => p == Argb1555.TransparentMarker ? p : colors[p % colors.Count])
                .ToArray();
            return new Argb1555Image(image.Width, image.Height, pixels);
        }

        /// <summary>A rendered image as the PNG import sees it: pixels without alpha bit become the transparent marker.</summary>
        public static Argb1555Image AsImported(Argb1555Image rendered)
        {
            var pixels = rendered.Pixels.Select(p => Argb1555.IsOpaque(p) ? p : Argb1555.TransparentMarker).ToArray();
            return new Argb1555Image(rendered.Width, rendered.Height, pixels);
        }

        /// <summary>256 different opaque colors.</summary>
        public static ushort[] DistinctOpaqueColors(Random random)
        {
            var colors = new HashSet<ushort>();
            while (colors.Count < ColorTable.ColorCount)
            {
                colors.Add(TestImages.Opaque(random.Next(0, 0x8000)));
            }

            return colors.ToArray();
        }

        private static Gm1FileBuilder CreateEmpty(Gm1DataType dataType, Random random)
        {
            var builder = new Gm1FileBuilder(dataType);
            for (int i = 0; i < HeaderFieldCount; i++)
            {
                // every other field uses the highest bit to catch sign problems
                builder.HeaderFields[i] = (uint)random.Next(1, int.MaxValue) | (i % 2 == 0 ? 0x8000_0000u : 0u);
            }

            if (dataType.UsesPalette())
            {
                for (int table = 0; table < Palette.ColorTableCount; table++)
                {
                    var colors = DistinctOpaqueColors(random);
                    for (int i = 0; i < colors.Length; i++)
                    {
                        builder.SetColor(table, i, colors[i]);
                    }
                }
            }
            else
            {
                random.NextBytes(builder.PaletteBytes);
            }

            return builder;
        }
    }
}

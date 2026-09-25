using System;
using System.Collections.Generic;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;
using LegacyColorTable = Gm1KonverterCrossPlatform.Tests.Legacy.ColorTable;
using LegacyDataType = Gm1KonverterCrossPlatform.Tests.Legacy.GM1FileHeader.DataType;
using LegacyDecoders = Gm1KonverterCrossPlatform.Tests.Legacy.LegacyDecoders;
using LegacyPalette = Gm1KonverterCrossPlatform.Tests.Legacy.Palette;
using LegacyUtility = Gm1KonverterCrossPlatform.Tests.Legacy.LegacyUtility;

namespace Gm1KonverterCrossPlatform.Tests.Codecs
{
    [Collection(Legacy.LegacyCollection.Name)]
    public class TgxCodecTests
    {
        public static IEnumerable<object[]> Seeds => TestImages.Seeds(40);

        public static IEnumerable<object[]> DataTypes()
        {
            yield return new object[] { Gm1DataType.Interface };
            yield return new object[] { Gm1DataType.Animations };
            yield return new object[] { Gm1DataType.TilesObject };
            yield return new object[] { Gm1DataType.Font };
            yield return new object[] { Gm1DataType.TgxConstSize };
        }

        [Theory]
        [MemberData(nameof(DataTypes))]
        public void Encode_ProducesSameBytesAsOriginalImplementation(Gm1DataType dataType)
        {
            var random = new Random((int)dataType);
            for (int i = 0; i < 60; i++)
            {
                var image = TestImages.Random(random, random.Next(1, 90), random.Next(1, 40));
                byte animatedColor = (byte)random.Next(0, 2);

                LegacyUtility.datatype = (LegacyDataType)dataType;
                var expected = LegacyUtility.ImgToGM1ByteArray(TestImages.ToList(image), image.Width, image.Height, animatedColor).ToArray();

                var header = new TgxImageHeader { AnimatedColor = animatedColor };
                var actual = TgxCodec.Encode(image, TgxEncoderOptions.ForGm1Image(dataType, header));

                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Encode_TgxFile_MatchesOriginalImplementationWithoutLoadedGm1File()
        {
            var random = new Random(7);
            var image = TestImages.Random(random, 64, 32);

            LegacyUtility.datatype = default;
            var expected = LegacyUtility.ImgToGM1ByteArray(TestImages.ToList(image), image.Width, image.Height, 1).ToArray();

            Assert.Equal(expected, TgxCodec.Encode(image, TgxEncoderOptions.ForTgxFile()));
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Decode_RestoresEveryVisiblePixel(int seed)
        {
            var random = new Random(seed);
            var image = TestImages.Random(random, random.Next(1, 120), random.Next(1, 60));

            var data = TgxCodec.Encode(image, new TgxEncoderOptions { EmptyRowMode = EmptyRowMode.TransparentRunUnlessRemainingRowsEmpty });
            var decoded = TgxCodec.Decode(data, image.Width, image.Height);

            for (int i = 0; i < image.Pixels.Length; i++)
            {
                ushort expected = image.Pixels[i] == Argb1555.TransparentMarker ? (ushort)0 : image.Pixels[i];
                Assert.Equal(expected, decoded.Pixels[i]);
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Decode_MatchesOriginalImplementation(int seed)
        {
            var random = new Random(seed);
            var image = TestImages.Random(random, random.Next(1, 120), random.Next(1, 60));
            var data = TgxCodec.Encode(image, new TgxEncoderOptions());

            var expected = LegacyDecoders.GM1ByteArrayToImg(data, image.Width, image.Height, null);
            var actual = TgxCodec.Decode(data, image.Width, image.Height).Pixels.Select(Argb1555.ToBgra8888).ToArray();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Decode_IgnoresAlphaBitOfStoredColors()
        {
            // stream of 2 pixels without alpha bit, as found in original game files
            byte[] data = { 0b000_00001, 0x1F, 0x00, 0xE0, 0x03, 0b100_00000 };

            var image = TgxCodec.Decode(data, 2, 1);

            Assert.Equal(new ushort[] { 0x801F, 0x83E0 }, image.Pixels);
        }

        [Fact]
        public void Decode_WithColorTable_UsesIndices()
        {
            var colors = new ushort[ColorTable.ColorCount];
            colors[3] = 0x8123;
            colors[255] = 0x0456;
            var table = new ColorTable(colors);
            byte[] data = { 0b000_00001, 3, 255, 0b010_00010, 3, 0b100_00000 };

            var image = TgxCodec.Decode(data, 5, 1, table);

            Assert.Equal(new ushort[] { 0x8123, 0x0456, 0x8123, 0x8123, 0x8123 }, image.Pixels);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Decode_NeverThrowsOnCorruptData(int seed)
        {
            var random = new Random(seed);
            var data = new byte[random.Next(0, 400)];
            random.NextBytes(data);

            var image = TgxCodec.Decode(data, random.Next(0, 20), random.Next(0, 20));

            Assert.NotNull(image);
        }

        [Fact]
        public void Decode_UnknownTokenType_ReadsOnePixelLikeOriginalDecoder()
        {
            byte[] data = { 0b011_00101, 0x1F, 0x80, 0b000_00000, 0xE0, 0x83 };

            var expected = LegacyDecoders.GM1ByteArrayToImg(data, 3, 1, null);
            var actual = TgxCodec.Decode(data, 3, 1).Pixels.Select(Argb1555.ToBgra8888).ToArray();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Decode_StopsAtTruncatedColor()
        {
            byte[] data = { 0b000_00011, 0x1F, 0x80, 0xE0 };

            var image = TgxCodec.Decode(data, 4, 1);

            Assert.Equal(new ushort[] { 0x801F, 0, 0, 0 }, image.Pixels);
        }

        [Fact]
        public void Encode_LongRunsAreSplitInto32PixelTokens()
        {
            var pixels = Enumerable.Repeat(TestImages.Opaque(5), 70).ToArray();
            var image = new Argb1555Image(70, 1, pixels);

            var data = TgxCodec.Encode(image, new TgxEncoderOptions());

            byte[] expected =
            {
                0b010_11111, 0x05, 0x80,
                0b010_11111, 0x05, 0x80,
                0b010_00101, 0x05, 0x80,
                0b100_00000
            };
            Assert.Equal(expected, data);
        }

        [Fact]
        public void Encode_UsesPaletteIndexer()
        {
            var colors = new ushort[ColorTable.ColorCount];
            colors[255] = TestImages.Opaque(1234);
            var palette = PaletteWith(colors);
            var image = new Argb1555Image(1, 1, new[] { TestImages.Opaque(1234) });

            var data = TgxCodec.Encode(image, new TgxEncoderOptions { PaletteIndexer = new ColorTableIndexer(palette) });

            Assert.Equal(new byte[] { 0b000_00000, 255, 0b100_00000 }, data);
        }

        [Fact]
        public void Encode_WithPalette_MatchesOriginalImplementationForUniqueColors()
        {
            var random = new Random(3);
            var colors = Enumerable.Range(0, ColorTable.ColorCount).Select(i => TestImages.Opaque(i * 97)).ToArray();
            var palette = PaletteWith(colors);
            var legacyPalette = new LegacyPalette { ColorTables = new[] { new LegacyColorTable { ColorList = colors } } };

            for (int i = 0; i < 30; i++)
            {
                var image = TestImages.Random(random, random.Next(1, 60), random.Next(1, 30));
                for (int p = 0; p < image.Pixels.Length; p++)
                {
                    if (image.Pixels[p] != Argb1555.TransparentMarker)
                    {
                        image.Pixels[p] = colors[image.Pixels[p] % 255];
                    }
                }

                LegacyUtility.datatype = LegacyDataType.Animations;
                var expected = LegacyUtility.ImgToGM1ByteArray(TestImages.ToList(image), image.Width, image.Height, 1, legacyPalette).ToArray();
                var actual = TgxCodec.Encode(image, new TgxEncoderOptions { PaletteIndexer = new ColorTableIndexer(palette) });

                Assert.Equal(expected, actual);
            }
        }

        private static Palette PaletteWith(ushort[] firstTable)
        {
            var palette = Palette.CreateEmpty();
            Array.Copy(firstTable, palette.ColorTables[0].Colors, ColorTable.ColorCount);
            return palette;
        }
    }
}

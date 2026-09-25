using System;
using System.Collections.Generic;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;
using LegacyDecoders = Gm1KonverterCrossPlatform.Tests.Legacy.LegacyDecoders;
using LegacyImage = Gm1KonverterCrossPlatform.Tests.Legacy.TGXImage;
using LegacyImageHeader = Gm1KonverterCrossPlatform.Tests.Legacy.TGXImageHeader;
using LegacyUtility = Gm1KonverterCrossPlatform.Tests.Legacy.LegacyUtility;

namespace Gm1KonverterCrossPlatform.Tests.Codecs
{
    [Collection(Legacy.LegacyCollection.Name)]
    public class TileObjectCodecTests
    {
        public static IEnumerable<object[]> Seeds => TestImages.Seeds(30);

        [Theory]
        [InlineData(1, 1)]
        [InlineData(4, 2)]
        [InlineData(9, 3)]
        [InlineData(16, 4)]
        [InlineData(100, 10)]
        [InlineData(2, 0)]
        [InlineData(0, 0)]
        public void GetDiamondCountPerRow_ReturnsSquareRoot(int parts, int expected)
        {
            Assert.Equal(expected, TileObjectCodec.GetDiamondCountPerRow(parts));
        }

        [Theory]
        [InlineData(30, 1)]
        [InlineData(62, 2)]
        [InlineData(478, 15)]
        [InlineData(510, 16)]
        [InlineData(574, 18)]
        [InlineData(60, 2)]
        public void GetDiamondCountPerRowFromImageWidth_HandlesExportedAndLegacyWidths(int width, int expected)
        {
            Assert.Equal(expected, TileObjectCodec.GetDiamondCountPerRowFromImageWidth(width));
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Encode_ProducesSamePartsAsOriginalImplementation(int seed)
        {
            var random = new Random(seed);
            var building = RandomBuilding(random, random.Next(1, 7));

            var expected = LegacyUtility.ConvertImgToTiles(TestImages.ToList(building), (ushort)building.Width, (ushort)building.Height, null);
            var actual = TileObjectCodec.Encode(building);

            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                AssertSameHeader(expected[i].Header, actual[i].Header);
                Assert.Equal(expected[i].ImgFileAsBytearray, actual[i].Data);
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Render_ProducesSameImageAsOriginalImplementation(int seed)
        {
            var random = new Random(seed);
            var parts = new List<Gm1Image>();
            for (int building = 0; building < 3; building++)
            {
                parts.AddRange(TileObjectCodec.Encode(RandomBuilding(random, random.Next(1, 6))));
            }

            var expected = LegacyDecoders.CreateTileImage(parts.Select(ToLegacy).ToList());
            var groups = TileObjectCodec.GetGroups(parts);

            Assert.Equal(expected.Count, groups.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                var actual = TileObjectCodec.Render(parts, groups[i]);

                Assert.Equal(expected[i].width, actual.Width);
                Assert.Equal(expected[i].height, actual.Height);

                // Ground tiles are drawn opaque now (like before the regression), the original drew them with the stored alpha bit.
                var expectedPixels = expected[i].result.Select(p => p == 0 ? 0 : p | 0xFF000000);
                Assert.Equal(expectedPixels, actual.Pixels.Select(Argb1555.ToBgra8888));
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RenderThenEncode_IsStable(int seed)
        {
            var random = new Random(seed);
            var parts = TileObjectCodec.Encode(RandomBuilding(random, random.Next(1, 6)));
            var group = TileObjectCodec.GetGroups(parts).Single();

            var rendered = ToImported(TileObjectCodec.Render(parts, group));
            var encodedAgain = TileObjectCodec.Encode(rendered);
            var renderedAgain = ToImported(TileObjectCodec.Render(encodedAgain, TileObjectCodec.GetGroups(encodedAgain).Single()));

            Assert.Equal(rendered.Pixels, renderedAgain.Pixels);
            Assert.Equal(encodedAgain.Select(p => p.Data), TileObjectCodec.Encode(renderedAgain).Select(p => p.Data));
        }

        [Theory]
        [InlineData(29)]
        [InlineData(510)]
        public void Encode_InvalidBuildingWidth_Throws(int width)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TileObjectCodec.Encode(new Argb1555Image(width, 40)));
        }

        [Fact]
        public void Encode_LargestBuilding_HasPartCountThatFitsIntoOneByte()
        {
            var building = new Argb1555Image(TileObjectCodec.GetImageWidth(TileObjectCodec.MaxDiamondsPerRow), 300);

            var parts = TileObjectCodec.Encode(building);

            Assert.Equal(225, parts.Count);
            Assert.All(parts, part => Assert.Equal(225, part.Header.SubParts));
        }

        [Fact]
        public void GetGroups_SplitsAtFirstPart()
        {
            var images = new[] { 0, 1, 2, 3, 0, 0, 1, 2, 3 }
                .Select(part => new Gm1Image(new TgxImageHeader { ImagePart = (byte)part }, new byte[512]))
                .ToList();

            var groups = TileObjectCodec.GetGroups(images);

            Assert.Equal(new[] { (0, 4), (4, 1), (5, 4) }, groups.Select(g => (g.FirstImageIndex, g.PartCount)));
        }

        [Fact]
        public void Render_CorruptPartsDoNotThrowOutOfRange()
        {
            var images = new List<Gm1Image>
            {
                new Gm1Image(new TgxImageHeader { SubParts = 1, TileOffset = 900, Direction = 3 }, new byte[] { 1, 2, 3 }),
            };
            images[0].Data = Enumerable.Repeat((byte)0b000_11111, 700).ToArray();

            var image = TileObjectCodec.Render(images, TileObjectCodec.GetGroups(images).Single());

            Assert.Equal(30, image.Width);
        }

        /// <summary>A building image as the PNG import creates it.</summary>
        private static Argb1555Image RandomBuilding(Random random, int diamondsPerRow)
        {
            int width = TileObjectCodec.GetImageWidth(diamondsPerRow);
            int height = diamondsPerRow * 16 + random.Next(0, 120);
            return TestImages.Random(random, width, height, colorCount: 6, transparency: 0.4);
        }

        /// <summary>Exported images are imported with 0x7FFF as transparent color.</summary>
        private static Argb1555Image ToImported(Argb1555Image rendered)
        {
            var pixels = rendered.Pixels.Select(p => Argb1555.IsOpaque(p) ? p : Argb1555.TransparentMarker).ToArray();
            return new Argb1555Image(rendered.Width, rendered.Height, pixels);
        }

        private static LegacyImage ToLegacy(Gm1Image image)
        {
            var header = image.Header;
            return new LegacyImage
            {
                ImgFileAsBytearray = image.Data,
                Header = new LegacyImageHeader
                {
                    Width = header.Width,
                    Height = header.Height,
                    OffsetX = header.OffsetX,
                    OffsetY = header.OffsetY,
                    ImagePart = header.ImagePart,
                    SubParts = header.SubParts,
                    TileOffset = header.TileOffset,
                    Direction = header.Direction,
                    HorizontalOffsetOfImage = header.HorizontalOffsetOfImage,
                    BuildingWidth = header.BuildingWidth,
                    AnimatedColor = header.AnimatedColor
                }
            };
        }

        private static void AssertSameHeader(LegacyImageHeader expected, TgxImageHeader actual)
        {
            Assert.Equal(
                (expected.Width, expected.Height, expected.OffsetX, expected.OffsetY, expected.ImagePart, expected.SubParts,
                 expected.TileOffset, expected.Direction, expected.HorizontalOffsetOfImage, expected.BuildingWidth, expected.AnimatedColor),
                (actual.Width, actual.Height, actual.OffsetX, actual.OffsetY, actual.ImagePart, actual.SubParts,
                 actual.TileOffset, actual.Direction, actual.HorizontalOffsetOfImage, actual.BuildingWidth, actual.AnimatedColor));
        }
    }
}

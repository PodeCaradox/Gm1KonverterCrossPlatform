using System;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Services
{
    public class Gm1DocumentTests
    {
        [Fact]
        public void ItemCount_BuildingFile_CountsBuildings()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.TilesObject, itemCount: 3));

            Assert.True(document.IsBuildingFile);
            Assert.Equal(3, document.ItemCount);
            Assert.True(document.File.Images.Count > 3);
            Assert.Equal(3, document.RenderItems().Count);
        }

        [Fact]
        public void ItemCount_OtherFiles_CountsImages()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Font, itemCount: 4));

            Assert.False(document.IsBuildingFile);
            Assert.Equal(4, document.ItemCount);
        }

        [Fact]
        public void GetFirstImageOfItem_BuildingFile_ReturnsFirstPartOfBuilding()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.TilesObject, seed: 2, itemCount: 3));
            var groups = TileObjectCodec.GetGroups(document.File.Images);

            for (int item = 0; item < groups.Count; item++)
            {
                var first = document.GetFirstImageOfItem(item);

                Assert.Same(document.File.Images[groups[item].FirstImageIndex], first);
                Assert.Equal(0, first.Header.ImagePart);
            }
        }

        [Fact]
        public void RenderItem_InvalidBuildingIndex_ThrowsArgumentOutOfRange()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.TilesObject, itemCount: 2));

            Assert.Throws<ArgumentOutOfRangeException>(() => document.RenderItem(2));
            Assert.Throws<ArgumentOutOfRangeException>(() => document.RenderItem(-1));
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(8u)]
        [InlineData(0xFFFF_FFFFu)]
        public void UnsupportedDataType_CanBeReadButNotRendered(uint dataType)
        {
            var bytes = Gm1FileBuilder.Create(Gm1DataType.Interface).ToBytes();
            Gm1FileBuilder.WriteUInt32(bytes, Gm1FileBuilder.DataTypeField * sizeof(uint), dataType);

            var document = new Gm1Document("unknown.gm1", Gm1File.Read(bytes));

            Assert.False(document.IsSupported);
            Assert.Throws<NotSupportedException>(() => document.RenderItems());
            Assert.Throws<NotSupportedException>(() => document.ReplaceItem(0, new Argb1555Image(1, 1)));
            Assert.Equal(bytes, document.ToBytes());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_WithoutFileName_Throws(string? fileName)
        {
            var file = Gm1File.Read(Gm1FileBuilder.Create(Gm1DataType.Interface).ToBytes());

            Assert.Throws<ArgumentException>(() => new Gm1Document(fileName!, file));
        }

        [Fact]
        public void ReplaceItem_UpdatesImageSizeAndHeaderTotals()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 3));
            var image = TestImages.Random(new Random(1), 77, 33);
            var header = document.File.Images[1].Header;
            var offsets = (header.OffsetX, header.OffsetY, header.TileOffset, header.AnimatedColor);

            document.ReplaceItem(1, image);

            Assert.Equal(77, header.Width);
            Assert.Equal(33, header.Height);
            Assert.Equal(offsets, (header.OffsetX, header.OffsetY, header.TileOffset, header.AnimatedColor));
            Assert.Equal((uint)document.File.Images.Sum(i => i.Data.Length), document.File.Header.DataSize);
            var rendered = document.RenderItem(1);
            Assert.Equal(image.Pixels.Select(p => p == Argb1555.TransparentMarker ? (ushort)0 : p), rendered.Pixels);
        }

        [Fact]
        public void ReplaceItem_NoCompression_StoresHeightWithPadding()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.NoCompression, itemCount: 2));
            var image = TestImages.Random(new Random(2), 12, 9);

            document.ReplaceItem(0, image);

            Assert.Equal(16, document.File.Images[0].Header.Height);
            Assert.Equal(12 * 9 * 2, document.File.Images[0].Data.Length);
            Assert.Equal(image.Pixels, document.RenderItem(0).Pixels);
        }

        [Fact]
        public void ReplaceItem_BuildingWithMoreParts_KeepsFollowingBuildings()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.TilesObject, seed: 3, itemCount: 3));
            var following = TileObjectCodec.GetGroups(document.File.Images).Skip(1)
                .Select(group => document.File.Images.Skip(group.FirstImageIndex).Take(group.PartCount).ToList())
                .ToList();
            var bigBuilding = TestImages.Random(new Random(4), TileObjectCodec.GetImageWidth(5), 100);

            document.ReplaceItem(0, bigBuilding);

            var groups = TileObjectCodec.GetGroups(document.File.Images);
            Assert.Equal(3, groups.Count);
            Assert.Equal(25, groups[0].PartCount);
            for (int i = 1; i < groups.Count; i++)
            {
                Assert.Equal(following[i - 1], document.File.Images.Skip(groups[i].FirstImageIndex).Take(groups[i].PartCount));
            }

            Assert.Equal((uint)document.File.Images.Count, document.File.Header.ImageCount);
        }

        [Fact]
        public void ReplaceItem_Building_KeepsAnimatedColorOfReplacedParts()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.TilesObject, seed: 4, itemCount: 1));
            for (int i = 0; i < document.File.Images.Count; i++)
            {
                document.File.Images[i].Header.AnimatedColor = (byte)(i + 1);
            }

            document.ReplaceItem(0, Gm1FileBuilder.AsImported(document.RenderItem(0)));

            Assert.Equal(Enumerable.Range(1, document.File.Images.Count).Select(i => (byte)i), document.File.Images.Select(i => i.Header.AnimatedColor));
        }

        [Fact]
        public void ReplaceItem_ImageTooLarge_ThrowsArgumentOutOfRange()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 2));

            Assert.Throws<ArgumentOutOfRangeException>(() => document.ReplaceItem(0, new Argb1555Image(ushort.MaxValue + 1, 1)));
        }

        [Fact]
        public void ReplaceItem_ImageTooLarge_KeepsItemUnchanged()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.NoCompression1, itemCount: 2);
            var original = builder.ToBytes();
            var document = new Gm1Document("test.gm1", Gm1File.Read(original));

            Assert.Throws<ArgumentOutOfRangeException>(() => document.ReplaceItem(0, new Argb1555Image(ushort.MaxValue + 1, 1)));

            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ReplaceItemWithColorTableImages_FileWithoutColorTables_ThrowsInvalidOperation()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Interface));

            Assert.Throws<InvalidOperationException>(() => document.ReplaceItemWithColorTableImages(0, new Argb1555Image?[] { new Argb1555Image(1, 1) }));
        }

        [Fact]
        public void ReplaceItemWithColorTableImages_WithoutFirstImage_ThrowsArgumentException()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Animations));

            Assert.Throws<ArgumentException>(() => document.ReplaceItemWithColorTableImages(0, new Argb1555Image?[] { null, new Argb1555Image(1, 1) }));
            Assert.Throws<ArgumentException>(() => document.ReplaceItemWithColorTableImages(0, Array.Empty<Argb1555Image?>()));
        }

        private static Gm1Document Open(Gm1FileBuilder builder) => new Gm1Document("test.gm1", Gm1File.Read(builder.ToBytes()));
    }
}

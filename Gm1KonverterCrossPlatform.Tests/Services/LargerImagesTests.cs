using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Services
{
    /// <summary>
    /// Imported images may be larger than the images they replace.
    /// </summary>
    public class LargerImagesTests : IDisposable
    {
        private const string FileName = "larger.gm1";

        private readonly TempDirectory temp = new TempDirectory();
        private readonly WorkFolder workFolder;

        public LargerImagesTests()
        {
            workFolder = new WorkFolder(temp.Combine("work"));
        }

        public static IEnumerable<object[]> ImageDataTypes()
        {
            return Enum.GetValues(typeof(Gm1DataType)).Cast<Gm1DataType>()
                .Where(type => type != Gm1DataType.TilesObject)
                .Select(type => new object[] { type });
        }

        public void Dispose() => temp.Dispose();

        [Theory]
        [MemberData(nameof(ImageDataTypes))]
        public void ImportImages_LargerImage_IsStoredWithNewSize(Gm1DataType dataType)
        {
            var document = Open(Gm1FileBuilder.Create(dataType, itemCount: 3).ToBytes());
            new Gm1Exporter(workFolder).ExportImages(document);
            var old = document.RenderItem(1);
            var larger = WithColorsOf(document, TestImages.Random(new Random(5), old.Width + 40, old.Height + 25));
            ImageFiles.SavePng(larger, workFolder.ImageFile(FileName, 2));

            new Gm1Importer(workFolder).ImportImages(document);
            var reopened = Reopen(document);

            Assert.Equal(larger.Width, reopened.RenderItem(1).Width);
            Assert.Equal(larger.Height, reopened.RenderItem(1).Height);
            Assert.Equal(Visible(larger), Visible(reopened.RenderItem(1)));
            Assert.Equal(document.RenderItem(0).Pixels, reopened.RenderItem(0).Pixels);
            Assert.Equal(document.RenderItem(2).Pixels, reopened.RenderItem(2).Pixels);
        }

        [Fact]
        public void ImportImages_LargerBuilding_GetsMorePartsAndKeepsOtherBuildings()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.TilesObject, itemCount: 3).ToBytes());
            new Gm1Exporter(workFolder).ExportImages(document);
            var others = new[] { document.RenderItem(0), document.RenderItem(2) };
            int oldParts = TileObjectCodec.GetGroups(document.File.Images)[1].PartCount;
            int diamonds = Math.Min(TileObjectCodec.GetDiamondCountPerRow(oldParts) + 2, TileObjectCodec.MaxDiamondsPerRow);
            var larger = TestImages.Random(new Random(6), TileObjectCodec.GetImageWidth(diamonds), diamonds * 16 + 120, transparency: 0);
            ImageFiles.SavePng(larger, workFolder.ImageFile(FileName, 2));

            new Gm1Importer(workFolder).ImportImages(document);
            var reopened = Reopen(document);

            var groups = TileObjectCodec.GetGroups(reopened.File.Images);
            Assert.Equal(3, groups.Count);
            Assert.Equal(diamonds * diamonds, groups[1].PartCount);
            Assert.Equal(larger.Width, reopened.RenderItem(1).Width);
            Assert.Equal(others[0].Pixels, reopened.RenderItem(0).Pixels);
            Assert.Equal(others[1].Pixels, reopened.RenderItem(2).Pixels);
        }

        [Fact]
        public void ImportTgxImage_LargerImage_IsStoredWithNewSize()
        {
            var original = TestImages.Random(new Random(7), 30, 20);
            var tgx = new TgxDocument("screen.tgx", new TgxFile(30, 20, TgxCodec.Encode(original, TgxEncoderOptions.ForTgxFile())));
            var transfer = new TgxImageTransfer(workFolder);
            var larger = TestImages.Random(new Random(8), 64, 48);
            ImageFiles.SavePng(larger, transfer.ImageFile(tgx));

            transfer.Import(tgx);
            var reopened = TgxFile.Read(tgx.ToBytes());

            Assert.Equal((64u, 48u), (reopened.Width, reopened.Height));
            Assert.Equal(Visible(larger), Visible(new TgxDocument("screen.tgx", reopened).Render()));
        }

        private Gm1Document Open(byte[] bytes) => Gm1Document.Load(temp.WriteFile(Path.Combine("gm", FileName), bytes));

        private static Gm1Document Reopen(Gm1Document document) => new Gm1Document(FileName, Gm1File.Read(document.ToBytes()));

        /// <summary>Animation colors must exist in the color table.</summary>
        private static Argb1555Image WithColorsOf(Gm1Document document, Argb1555Image image)
        {
            if (!document.HasColorTables)
            {
                return image;
            }

            var table = document.CurrentColorTable;
            for (int i = 0; i < image.Pixels.Length; i++)
            {
                if (image.Pixels[i] != Argb1555.TransparentMarker)
                {
                    image.Pixels[i] = table[image.Pixels[i] % ColorTable.ColorCount];
                }
            }

            return image;
        }

        private static ushort[] Visible(Argb1555Image image)
        {
            return image.Pixels.Select(p => Argb1555.IsOpaque(p) ? p : (ushort)0).ToArray();
        }
    }
}

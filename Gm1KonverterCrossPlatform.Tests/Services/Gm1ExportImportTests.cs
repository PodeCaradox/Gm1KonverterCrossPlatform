using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Layout;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Services
{
    /// <summary>
    /// Export to and import from a work folder, the way the user edits a .gm1 file.
    /// </summary>
    public class Gm1ExportImportTests : IDisposable
    {
        private const string FileName = "test_file.gm1";

        private readonly TempDirectory temp = new TempDirectory();
        private readonly WorkFolder workFolder;
        private readonly Gm1Exporter exporter;
        private readonly Gm1Importer importer;

        public Gm1ExportImportTests()
        {
            workFolder = new WorkFolder(temp.Combine("work"));
            exporter = new Gm1Exporter(workFolder);
            importer = new Gm1Importer(workFolder);
        }

        public static IEnumerable<object[]> DataTypes => Gm1FileBuilder.AllDataTypes();

        public void Dispose() => temp.Dispose();

        [Theory]
        [MemberData(nameof(DataTypes))]
        public void ExportImagesThenImport_Unchanged_KeepsFileBytes(Gm1DataType dataType)
        {
            var original = Gm1FileBuilder.Create(dataType).ToBytes();
            var document = Gm1Document.Load(temp.WriteFile(Path.Combine("gm", FileName), original));

            string folder = exporter.ExportImages(document);
            int imported = importer.ImportImages(document);

            Assert.Equal(workFolder.ImagesFolder(FileName), folder);
            Assert.Equal(document.ItemCount, imported);
            Assert.Equal(original, document.ToBytes());
        }

        [Theory]
        [MemberData(nameof(DataTypes))]
        public void ExportImages_WritesOnePngPerItem(Gm1DataType dataType)
        {
            var document = Open(Gm1FileBuilder.Create(dataType, itemCount: 3).ToBytes());

            exporter.ExportImages(document);

            var items = document.RenderItems();
            Assert.Equal(3, items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var png = ImageFiles.LoadPng(workFolder.ImageFile(FileName, i + 1));
                Assert.Equal(Gm1FileBuilder.AsImported(items[i]).Pixels, png.Pixels);
            }

            Assert.False(File.Exists(workFolder.ImageFile(FileName, 4)));
        }

        [Theory]
        [MemberData(nameof(DataTypes))]
        public void ExportBigImageThenImport_Unchanged_KeepsFileBytes(Gm1DataType dataType)
        {
            var original = Gm1FileBuilder.Create(dataType, seed: 2, itemCount: 8).ToBytes();
            var sizes = Open(original).RenderItems().Select(item => (item.Width, item.Height)).ToList();
            int widest = sizes.Max(size => size.Width);
            int totalWidth = sizes.Sum(size => size.Width);
            int wrappingWidth = totalWidth / 2;
            Assert.True(SpriteSheetLayout.Arrange(sizes, wrappingWidth).Cells.Select(cell => cell.Y).Distinct().Count() > 1, "rows must wrap");

            var maxWidths = new[] { 1, widest - 1, widest, widest + 1, wrappingWidth, totalWidth + 1 }.Where(width => width > 0).Distinct();
            foreach (int maxWidth in maxWidths)
            {
                var document = Open(original);

                string folder = exporter.ExportBigImage(document, maxWidth);
                var sheet = ImageFiles.LoadPng(workFolder.BigImageFile(FileName));
                importer.ImportBigImage(document);

                Assert.Equal(workFolder.BigImageFolder(FileName), folder);
                Assert.Equal(Math.Max(maxWidth, widest), sheet.Width);
                Assert.True(original.SequenceEqual(document.ToBytes()), $"max width {maxWidth} changed the file");
            }
        }

        [Fact]
        public void ImportBigImage_WithoutExport_ThrowsWorkflowException()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Interface).ToBytes());

            Assert.Throws<WorkflowException>(() => importer.ImportBigImage(document));
        }

        [Fact]
        public void ImportBigImage_SheetTooSmall_ThrowsWorkflowExceptionAndKeepsFile()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 6).ToBytes();
            var document = Open(original);
            exporter.ExportBigImage(document, 100);
            var sheet = ImageFiles.LoadPng(workFolder.BigImageFile(FileName));
            ImageFiles.SavePng(sheet.Crop(0, 0, sheet.Width, sheet.Height - 1), workFolder.BigImageFile(FileName));

            Assert.Throws<WorkflowException>(() => importer.ImportBigImage(document));
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void NoCompression_RepeatedExportAndImport_KeepsHeaderHeight()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.NoCompression);
            var original = builder.ToBytes();
            var heights = builder.Images.Select(image => image.Header.Height).ToList();
            var document = Open(original);

            for (int cycle = 0; cycle < 3; cycle++)
            {
                exporter.ExportImages(document);
                importer.ImportImages(document);
                Assert.Equal(heights, document.File.Images.Select(image => image.Header.Height));

                exporter.ExportBigImage(document, 64);
                importer.ImportBigImage(document);
                Assert.Equal(heights, document.File.Images.Select(image => image.Header.Height));
            }

            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void NoCompression_RenderedHeightIsHeaderHeightMinusSeven()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.NoCompression);
            var document = Open(builder.ToBytes());

            for (int i = 0; i < document.ItemCount; i++)
            {
                var header = document.File.Images[i].Header;
                var image = document.RenderItem(i);

                Assert.Equal(header.Width, image.Width);
                Assert.Equal(header.Height - 7, image.Height);
                Assert.Equal(image.Width * image.Height * 2, document.File.Images[i].Data.Length);
            }
        }

        [Fact]
        public void NoCompression1_RenderedHeightIsHeaderHeight()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.NoCompression1).ToBytes());

            for (int i = 0; i < document.ItemCount; i++)
            {
                Assert.Equal(document.File.Images[i].Header.Height, document.RenderItem(i).Height);
            }
        }

        [Fact]
        public void NoCompression_LegacyExportWithSevenTransparentRows_IsImportedWithoutGrowing()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.NoCompression).ToBytes();
            var document = Open(original);
            for (int i = 0; i < document.ItemCount; i++)
            {
                // version 5b1ade8 exported header.Height rows, the last 7 rows transparent
                var header = document.File.Images[i].Header;
                var legacy = new Argb1555Image(header.Width, header.Height);
                legacy.Draw(document.RenderItem(i), 0, 0);
                ImageFiles.SavePng(legacy, workFolder.ImageFile(FileName, i + 1));
            }

            importer.ImportImages(document);

            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void NoCompression_ImageWithVisiblePixelsInLastSevenRows_IsImportedWithItsFullHeight()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.NoCompression).ToBytes());
            var header = document.File.Images[0].Header;
            int oldHeight = header.Height;
            var image = new Argb1555Image(header.Width, header.Height);
            image.Draw(document.RenderItem(0), 0, 0);
            image[0, image.Height - 1] = 0x8123;
            ImageFiles.SavePng(image, workFolder.ImageFile(FileName, 1));

            importer.ImportImages(document);

            Assert.Equal(oldHeight + 7, document.File.Images[0].Header.Height);
            Assert.Equal(0x8123, document.RenderItem(0)[0, oldHeight - 1]);
        }

        [Theory]
        [InlineData(Gm1DataType.Interface)]
        [InlineData(Gm1DataType.Animations)]
        [InlineData(Gm1DataType.NoCompression)]
        [InlineData(Gm1DataType.TgxConstSize)]
        public void ImportImages_MissingImage_KeepsItsOriginalData(Gm1DataType dataType)
        {
            var original = Gm1FileBuilder.Create(dataType).ToBytes();
            var document = Open(original);
            exporter.ExportImages(document);
            File.Delete(workFolder.ImageFile(FileName, 2));
            var secondImageData = document.File.Images[1].Data;

            int imported = importer.ImportImages(document);

            Assert.Equal(document.ItemCount - 1, imported);
            Assert.Same(secondImageData, document.File.Images[1].Data);
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ImportImages_NoExportedImages_ThrowsWorkflowException()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Interface).ToBytes();
            var document = Open(original);

            Assert.Throws<WorkflowException>(() => importer.ImportImages(document));

            Directory.CreateDirectory(workFolder.ImagesFolder(FileName));
            File.WriteAllBytes(Path.Combine(workFolder.ImagesFolder(FileName), "Image0.png"), Array.Empty<byte>());
            Assert.Throws<WorkflowException>(() => importer.ImportImages(document));
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ImportImages_MissingBuildingImage_KeepsThatBuildingsParts()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.TilesObject, seed: 5, itemCount: 3).ToBytes();
            var document = Open(original);
            var secondBuilding = PartsOf(document, 1);
            exporter.ExportImages(document);
            File.Delete(workFolder.ImageFile(FileName, 2));

            int imported = importer.ImportImages(document);

            Assert.Equal(2, imported);
            Assert.Equal(3, document.ItemCount);
            Assert.Equal(secondBuilding, PartsOf(document, 1)); // same instances, not re-encoded
            Assert.Equal(original, document.ToBytes());
        }

        [Theory]
        [MemberData(nameof(DataTypes))]
        public void ImportImages_ChangedPixel_ChangesOnlyThatItem(Gm1DataType dataType)
        {
            var document = Open(Gm1FileBuilder.Create(dataType, seed: 3, itemCount: 4).ToBytes());
            var before = ItemData(document);
            exporter.ExportImages(document);

            string path = workFolder.ImageFile(FileName, 2);
            var image = ImageFiles.LoadPng(path);
            int pixel = Array.FindIndex(image.Pixels, p => p != Argb1555.TransparentMarker);
            Assert.True(pixel >= 0, "the image needs a visible pixel");
            ushort newColor = dataType == Gm1DataType.Animations
                ? document.CurrentColorTable.Colors.First(color => color != image.Pixels[pixel])
                : (ushort)(image.Pixels[pixel] ^ 0b00001_00001_00001);
            image.Pixels[pixel] = newColor;
            ImageFiles.SavePng(image, path);

            importer.ImportImages(document);

            var after = ItemData(document);
            Assert.NotEqual(before[1], after[1]);
            for (int i = 0; i < before.Count; i++)
            {
                if (i != 1)
                {
                    Assert.Equal(before[i], after[i]);
                }
            }

            var rendered = document.RenderItem(1);
            Assert.Equal((image.Width, image.Height), (rendered.Width, rendered.Height));
            Assert.Equal(newColor, rendered.Pixels[pixel]);
        }

        [Fact]
        public void ExportImagesThenImport_WithOtherColorTable_KeepsFileBytes()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Animations, seed: 6).ToBytes();
            var document = Open(original);
            document.ColorTableIndex = 7;

            exporter.ExportImages(document);
            importer.ImportImages(document);

            var png = ImageFiles.LoadPng(workFolder.ImageFile(FileName, 1));
            Assert.Equal(Gm1FileBuilder.AsImported(document.RenderItem(0, 7)).Pixels, png.Pixels);
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ImportImages_Animation_MapsColorsToIndicesOfCurrentColorTable()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Animations, seed: 4, itemCount: 3).ToBytes());
            document.ColorTableIndex = 3;
            exporter.ExportImages(document);
            string path = workFolder.ImageFile(FileName, 1);
            var image = ImageFiles.LoadPng(path);
            var table = document.File.Palette.ColorTables[3];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    image[x, y] = table[ExpectedIndex(x, y)];
                }
            }

            ImageFiles.SavePng(image, path);

            importer.ImportImages(document);

            var indices = DecodeIndices(document.File.Images[0]);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Assert.Equal(ExpectedIndex(x, y), indices[x, y]);
                }
            }

            static int ExpectedIndex(int x, int y) => (x * 7 + y * 31 + 11) % ColorTable.ColorCount;
        }

        [Fact]
        public void OriginalAnimation_ExportThenImport_KeepsFileBytes_WhenColorAppearsTwiceInFirstTable()
        {
            var builder = CreateAnimationWithDuplicateColor(seed: 1, allowDuplicatesAsNeighbours: false);
            var original = builder.ToBytes();
            var document = Open(original);
            AssertUsesBothDuplicateIndices(document);

            string folder = exporter.ExportOriginalAnimation(document);
            int imported = importer.ImportOriginalAnimation(document);

            Assert.Equal(workFolder.OriginalAnimationFolderRoot(FileName), folder);
            Assert.Equal(document.ItemCount, imported);
            Assert.Equal(original, document.ToBytes());
        }

        /// <summary>
        /// Row [5, 200, 200, 200, 17, 200, 5, 5, 5, 42] is imported as [200, 200, 200, 200, 17, 5, 5, 5, 5, 42]:
        /// the encoder measures runs on the color table 0 image (TgxCodec.cs MeasureColoredSegment), where 5 and 200
        /// have the same color, and writes one index for the whole run (WriteRepeatingPixels).
        /// </summary>
        [Fact]
        public void OriginalAnimation_ExportThenImport_KeepsFileBytes_WhenDuplicateColorsAreNeighbours()
        {
            var builder = Gm1FileBuilder.CreateEmpty(Gm1DataType.Animations, seed: 3);
            builder.SetColor(0, DuplicateIndex, builder.GetColorTable(0)[FirstIndex]);
            int[] row = { FirstIndex, DuplicateIndex, DuplicateIndex, DuplicateIndex, 17, DuplicateIndex, FirstIndex, FirstIndex, FirstIndex, 42 };
            var pixels = row.Select(index => (ushort)(0x8000 | index)).ToArray();
            builder.AddTgxImage(new Argb1555Image(row.Length, 1, pixels), new TgxImageHeader { AnimatedColor = 1 }, new IndexFromColorIndexer());
            var original = builder.ToBytes();
            var document = Open(original);

            exporter.ExportOriginalAnimation(document);
            importer.ImportOriginalAnimation(document);

            Assert.Equal(row, DecodeIndices(document.File.Images[0]).Pixels.Select(p => (int)p));
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ImportImages_ColorAppearingTwiceInFirstTable_UsesFirstIndex()
        {
            var builder = CreateAnimationWithDuplicateColor(seed: 2, allowDuplicatesAsNeighbours: false);
            var document = Open(builder.ToBytes());
            AssertUsesBothDuplicateIndices(document);

            exporter.ExportImages(document);
            importer.ImportImages(document);

            // without the renderings with the other color tables the index is ambiguous
            Assert.All(document.File.Images, image => Assert.DoesNotContain((ushort)DuplicateIndex, DecodeIndices(image).Pixels));
        }

        [Fact]
        public void ImportOriginalAnimation_WithoutExport_ThrowsWorkflowException()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes());

            Assert.Throws<WorkflowException>(() => importer.ImportOriginalAnimation(document));
        }

        [Fact]
        public void ColorTables_ExportThenImport_KeepsFileBytes()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes();
            var document = Open(original);

            string folder = exporter.ExportColorTables(document);
            int imported = importer.ImportColorTables(document);

            Assert.Equal(workFolder.ColorTablesFolder(FileName), folder);
            Assert.Equal(Palette.ColorTableCount, imported);
            Assert.All(Enumerable.Range(1, Palette.ColorTableCount), n => Assert.True(File.Exists(workFolder.ColorTableFile(FileName, n))));
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ImportColorTables_OnlySomeTables_ReplacesOnlyThose()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes());
            var before = document.File.Palette.ColorTables.Select(table => table.Copy()).ToArray();
            var changed = new ColorTable(Gm1FileBuilder.DistinctOpaqueColors(new Random(77)));
            Directory.CreateDirectory(workFolder.ColorTablesFolder(FileName));
            ImageFiles.SavePng(ColorTableImage.Render(changed), workFolder.ColorTableFile(FileName, 3));

            int imported = importer.ImportColorTables(document);

            Assert.Equal(1, imported);
            for (int table = 0; table < Palette.ColorTableCount; table++)
            {
                var expected = table == 2 ? changed : before[table];
                Assert.Equal(expected.Colors, document.File.Palette.ColorTables[table].Colors);
            }

            Assert.Equal(changed.Colors, Gm1File.Read(document.ToBytes()).Palette.ColorTables[2].Colors);
        }

        [Fact]
        public void ImportColorTables_WrongImageSize_ThrowsWorkflowException()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes());
            exporter.ExportColorTables(document);
            ImageFiles.SavePng(new Argb1555Image(100, 50), workFolder.ColorTableFile(FileName, 4));

            var exception = Assert.Throws<WorkflowException>(() => importer.ImportColorTables(document));

            Assert.IsType<ArgumentException>(exception.InnerException);
            Assert.Contains("ColorTable4.png", exception.Message);
        }

        [Fact]
        public void ImportColorTables_WithoutExport_ThrowsWorkflowException()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes());

            Assert.Throws<WorkflowException>(() => importer.ImportColorTables(document));
        }

        [Fact]
        public void ColorTableOperations_FileWithoutColorTables_ThrowInvalidOperationException()
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Interface).ToBytes());

            Assert.Throws<InvalidOperationException>(() => exporter.ExportColorTables(document));
            Assert.Throws<InvalidOperationException>(() => exporter.ExportOriginalAnimation(document));
            Assert.Throws<InvalidOperationException>(() => importer.ImportColorTables(document));
            Assert.Throws<InvalidOperationException>(() => importer.ImportOriginalAnimation(document));
        }

        [Fact]
        public void ColorTableIndex_ChangesRenderingButNotFileBytes()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes();
            var document = Open(original);
            var withFirstTable = document.RenderItems();

            document.ColorTableIndex = 4;
            var withFifthTable = document.RenderItems();

            Assert.NotEqual(withFirstTable.Select(image => image.Pixels), withFifthTable.Select(image => image.Pixels));
            for (int i = 0; i < document.ItemCount; i++)
            {
                var image = document.File.Images[i];
                var expected = TgxCodec.Decode(image.Data, image.Header.Width, image.Header.Height, document.File.Palette.ColorTables[4]);
                Assert.Equal(expected.Pixels, withFifthTable[i].Pixels);
                Assert.Equal(expected.Pixels, document.RenderItem(i).Pixels);
            }

            Assert.Same(document.File.Palette.ColorTables[4], document.CurrentColorTable);
            Assert.Equal(original, document.ToBytes());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(Palette.ColorTableCount)]
        public void ColorTableIndex_OutOfRange_Throws(int index)
        {
            var document = Open(Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes());

            Assert.Throws<ArgumentOutOfRangeException>(() => { document.ColorTableIndex = index; });
            Assert.Equal(0, document.ColorTableIndex);
        }

        private const int FirstIndex = 5;
        private const int DuplicateIndex = 200;

        private Gm1Document Open(byte[] bytes) => new Gm1Document(FileName, Gm1File.Read(bytes));

        /// <summary>
        /// An animation file whose color table 0 has the same color at index 5 and 200, while all other tables
        /// have different colors there. The images are encoded from color table indices, like the original files.
        /// </summary>
        private static Gm1FileBuilder CreateAnimationWithDuplicateColor(int seed, bool allowDuplicatesAsNeighbours)
        {
            var builder = Gm1FileBuilder.CreateEmpty(Gm1DataType.Animations, seed);
            builder.SetColor(0, DuplicateIndex, builder.GetColorTable(0)[FirstIndex]);

            var random = new Random(seed);
            int[] indices = { FirstIndex, DuplicateIndex, 0, 17, 42, 99, 255 };
            for (int n = 0; n < 5; n++)
            {
                int width = random.Next(3, 50);
                int height = random.Next(2, 25);
                var pixels = new ushort[width * height];
                for (int y = 0; y < height; y++)
                {
                    int previous = -1;
                    for (int x = 0; x < width; x++)
                    {
                        if (random.NextDouble() < 0.15)
                        {
                            pixels[y * width + x] = Argb1555.TransparentMarker;
                            previous = -1;
                            continue;
                        }

                        int index = previous;
                        if (previous < 0 || random.NextDouble() < 0.4)
                        {
                            do
                            {
                                index = indices[random.Next(indices.Length)];
                            }
                            while (!allowDuplicatesAsNeighbours && IsDuplicatePair(previous, index));
                        }

                        pixels[y * width + x] = (ushort)(0x8000 | index);
                        previous = index;
                    }
                }

                var header = Gm1FileBuilder.RandomHeader(random, width, height);
                builder.AddTgxImage(new Argb1555Image(width, height, pixels), header, new IndexFromColorIndexer());
            }

            return builder;
        }

        private static bool IsDuplicatePair(int first, int second)
        {
            return (first == FirstIndex && second == DuplicateIndex) || (first == DuplicateIndex && second == FirstIndex);
        }

        private static void AssertUsesBothDuplicateIndices(Gm1Document document)
        {
            var used = document.File.Images.SelectMany(image => DecodeIndices(image).Pixels).ToHashSet();
            Assert.Contains((ushort)FirstIndex, used);
            Assert.Contains((ushort)DuplicateIndex, used);
            Assert.Equal(document.File.Palette.ColorTables[0][FirstIndex], document.File.Palette.ColorTables[0][DuplicateIndex]);
            Assert.All(Enumerable.Range(1, Palette.ColorTableCount - 1), table =>
                Assert.NotEqual(document.File.Palette.ColorTables[table][FirstIndex], document.File.Palette.ColorTables[table][DuplicateIndex]));
        }

        /// <summary>The color table index of every pixel of an animation image, transparent pixels are 0xFFFF.</summary>
        private static Argb1555Image DecodeIndices(Gm1Image image)
        {
            var identity = new ColorTable(Enumerable.Range(0, ColorTable.ColorCount).Select(i => (ushort)(0x8000 | i)).ToArray());
            var decoded = TgxCodec.Decode(image.Data, image.Header.Width, image.Header.Height, identity);
            var indices = decoded.Pixels.Select(p => Argb1555.IsOpaque(p) ? (ushort)(p & 0xFF) : ushort.MaxValue).ToArray();
            return new Argb1555Image(decoded.Width, decoded.Height, indices);
        }

        /// <summary>The data of the images of every item (all parts of a building).</summary>
        private static List<List<byte[]>> ItemData(Gm1Document document)
        {
            if (!document.IsBuildingFile)
            {
                return document.File.Images.Select(image => new List<byte[]> { image.Data }).ToList();
            }

            return TileObjectCodec.GetGroups(document.File.Images)
                .Select(group => document.File.Images.Skip(group.FirstImageIndex).Take(group.PartCount).Select(image => image.Data).ToList())
                .ToList();
        }

        private static List<Gm1Image> PartsOf(Gm1Document document, int building)
        {
            var group = TileObjectCodec.GetGroups(document.File.Images)[building];
            return document.File.Images.Skip(group.FirstImageIndex).Take(group.PartCount).ToList();
        }

        /// <summary>Encodes images whose pixels are 0x8000 | color table index.</summary>
        private sealed class IndexFromColorIndexer : IPaletteIndexer
        {
            public byte GetIndex(int pixelIndex, ushort color) => (byte)color;
        }
    }
}

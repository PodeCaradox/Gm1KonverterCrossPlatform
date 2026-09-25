using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;
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
    /// A failing import must leave the document unchanged and report a message the user understands.
    /// </summary>
    public class ImportRobustnessTests : IDisposable
    {
        private const string FileName = "robust.gm1";

        private readonly TempDirectory temp = new TempDirectory();
        private readonly WorkFolder workFolder;

        public ImportRobustnessTests()
        {
            workFolder = new WorkFolder(temp.Combine("work"));
        }

        public void Dispose() => temp.Dispose();

        [Fact]
        public void ImportImages_CorruptPng_ThrowsWorkflowExceptionAndKeepsDocument()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 3).ToBytes();
            var document = Open(original);
            new Gm1Exporter(workFolder).ExportImages(document);
            ChangeFirstPixel(workFolder.ImageFile(FileName, 1));
            File.WriteAllBytes(workFolder.ImageFile(FileName, 2), new byte[] { 1, 2, 3 });

            var error = Assert.Throws<WorkflowException>(() => new Gm1Importer(workFolder).ImportImages(document));

            Assert.Contains("Image2.png", error.Message);
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ImportColorTables_OneTooSmall_KeepsAllTables()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Animations).ToBytes();
            var document = Open(original);
            new Gm1Exporter(workFolder).ExportColorTables(document);
            ImageFiles.SavePng(TestImages.Random(new Random(1), 5, 5), workFolder.ColorTableFile(FileName, 3));

            Assert.Throws<WorkflowException>(() => new Gm1Importer(workFolder).ImportColorTables(document));

            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ExportThenImportColorTables_KeepsColorsWithoutAlphaBit()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.Animations);
            var bytes = builder.ToBytes();

            // colors without alpha bit are exported as transparent pixels
            int firstColor = Gm1FileHeader.ByteSize;
            bytes[firstColor] = 0x34;
            bytes[firstColor + 1] = 0x12;
            var document = Open(bytes);

            new Gm1Exporter(workFolder).ExportColorTables(document);
            new Gm1Importer(workFolder).ImportColorTables(document);

            Assert.Equal(0x1234, document.File.Palette.ColorTables[0][0]);
            Assert.Equal(bytes, document.ToBytes());
        }

        [Fact]
        public void ReplaceItems_OneImageTooLarge_ReplacesNothing()
        {
            var original = Gm1FileBuilder.Create(Gm1DataType.Font, itemCount: 2).ToBytes();
            var document = Open(original);
            var small = new Argb1555Image(2, 2, Enumerable.Repeat((ushort)0x8001, 4).ToArray());
            var tooLarge = new Argb1555Image(ushort.MaxValue + 1, 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => document.ReplaceItems(new[] { (0, small), (1, tooLarge) }));

            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void ImportTgx_CorruptPng_ThrowsWorkflowException()
        {
            var tgx = new TgxDocument("screen.tgx", new TgxFile(1, 1, new byte[] { 0b100_00000 }));
            var transfer = new TgxImageTransfer(workFolder);
            transfer.Export(tgx);
            File.WriteAllText(transfer.ImageFile(tgx), "not a png");

            Assert.Throws<WorkflowException>(() => transfer.Import(tgx));
        }

        [Fact]
        public void ExecutableOffsetPatcher_SecondExecutableTooSmall_PatchesNeither()
        {
            var crusader = new byte[950_000];
            var extreme = new byte[1000];
            var patcher = ExecutableOffsetPatcher.FromBytes(crusader, extreme);

            Assert.Throws<InvalidDataException>(() => patcher.Write(0, new BuildingOffset(5, 6)));

            Assert.All(patcher.GetBytes(0), b => Assert.Equal(0, b));
        }

        [Fact]
        public void BuildingOffsetStore_ValueOutOfRange_ThrowsInvalidDataException()
        {
            Assert.Throws<InvalidDataException>(() => BuildingOffsetStore.Parse("{\"0\":{\"X\":1e20,\"Y\":0}}"));
        }

        private Gm1Document Open(byte[] bytes) => Gm1Document.Load(temp.WriteFile(Path.Combine("gm", FileName), bytes));

        private static void ChangeFirstPixel(string pngPath)
        {
            var image = ImageFiles.LoadPng(pngPath);
            image.Pixels[0] = TestImages.Opaque(image.Pixels[0] + 1);
            ImageFiles.SavePng(image, pngPath);
        }
    }
}

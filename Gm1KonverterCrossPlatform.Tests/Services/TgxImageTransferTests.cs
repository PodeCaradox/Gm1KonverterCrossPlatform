using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Tests.Files;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Services
{
    public class TgxImageTransferTests : IDisposable
    {
        private const string FileName = "interface_test.tgx";

        private readonly TempDirectory temp = new TempDirectory();
        private readonly WorkFolder workFolder;
        private readonly TgxImageTransfer transfer;

        public TgxImageTransferTests()
        {
            workFolder = new WorkFolder(temp.Combine("work"));
            transfer = new TgxImageTransfer(workFolder);
        }

        public void Dispose() => temp.Dispose();

        [Theory]
        [InlineData(1, 90, 45)]
        [InlineData(2, 1, 1)]
        [InlineData(3, 300, 3)]
        public void ExportThenImport_Unchanged_KeepsFileBytes(int seed, int width, int height)
        {
            var original = TgxFileTests.CreateTgxBytes(new Random(seed), width, height);
            var document = TgxDocument.Load(temp.WriteFile(Path.Combine("gfx", FileName), original));

            string folder = transfer.Export(document);
            transfer.Import(document);

            Assert.Equal(workFolder.FileFolder(FileName), folder);
            Assert.True(File.Exists(workFolder.TgxImageFile(FileName)));
            Assert.Equal(workFolder.TgxImageFile(FileName), transfer.ImageFile(document));
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void Import_WithoutExport_ThrowsWorkflowException()
        {
            var original = TgxFileTests.CreateTgxBytes(new Random(4), 20, 20);
            var document = TgxDocument.Load(temp.WriteFile(FileName, original));

            Assert.Throws<WorkflowException>(() => transfer.Import(document));
            Assert.Equal(original, document.ToBytes());
        }

        [Fact]
        public void Import_ImageWithOtherSize_ReplacesSizeAndPixels()
        {
            var document = TgxDocument.Load(temp.WriteFile(FileName, TgxFileTests.CreateTgxBytes(new Random(5), 20, 20)));
            var image = TestImages.Random(new Random(6), 41, 17);
            ImageFiles.SavePng(image, workFolder.TgxImageFile(FileName));

            transfer.Import(document);

            Assert.Equal(41u, document.File.Width);
            Assert.Equal(17u, document.File.Height);
            Assert.Equal(image.Pixels.Select(p => p == Argb1555.TransparentMarker ? (ushort)0 : p), document.Render().Pixels);
        }

        [Fact]
        public void Load_UsesFileNameWithoutDirectory()
        {
            var document = TgxDocument.Load(temp.WriteFile(Path.Combine("gfx", FileName), TgxFileTests.CreateTgxBytes(new Random(7), 2, 2)));

            Assert.Equal(FileName, document.FileName);
        }
    }
}

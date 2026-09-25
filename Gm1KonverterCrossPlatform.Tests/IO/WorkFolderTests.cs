using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.IO
{
    /// <summary>
    /// The names in the work folder are part of the user's workflow (and of folders created by older versions),
    /// so they must not change.
    /// </summary>
    public class WorkFolderTests
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "work");

        private readonly WorkFolder workFolder = new WorkFolder(Root);

        [Fact]
        public void FileFolder_IsNamedAfterGameFileWithoutExtension()
        {
            Assert.Equal(Path.Combine(Root, "anim_castle"), workFolder.FileFolder("anim_castle.gm1"));
        }

        [Fact]
        public void ImageFiles_UseOneBasedNumbers()
        {
            Assert.Equal(Path.Combine(Root, "anim_castle", "Images"), workFolder.ImagesFolder("anim_castle.gm1"));
            Assert.Equal(Path.Combine(Root, "anim_castle", "Images", "Image1.png"), workFolder.ImageFile("anim_castle.gm1", 1));
        }

        [Fact]
        public void BigImageFile_IsNamedAfterGameFile()
        {
            Assert.Equal(Path.Combine(Root, "anim_castle", "BigImage"), workFolder.BigImageFolder("anim_castle.gm1"));
            Assert.Equal(Path.Combine(Root, "anim_castle", "BigImage", "anim_castle.png"), workFolder.BigImageFile("anim_castle.gm1"));
        }

        [Fact]
        public void ColorTableFiles_UseOldFolderName()
        {
            Assert.Equal(Path.Combine(Root, "body_archer", "Colortables"), workFolder.ColorTablesFolder("body_archer.gm1"));
            Assert.Equal(Path.Combine(Root, "body_archer", "Colortables", "ColorTable10.png"), workFolder.ColorTableFile("body_archer.gm1", 10));
        }

        [Fact]
        public void OriginalAnimationFiles_KeepOldSpelling()
        {
            Assert.Equal(Path.Combine(Root, "body_archer", "OrginalAnimationPalette3"), workFolder.OriginalAnimationFolder("body_archer.gm1", 3));
            Assert.Equal(
                Path.Combine(Root, "body_archer", "OrginalAnimationPalette3", "OrginalAnimationImg12.png"),
                workFolder.OriginalAnimationFile("body_archer.gm1", 3, 12));
            Assert.Equal(Path.Combine(Root, "body_archer"), workFolder.OriginalAnimationFolderRoot("body_archer.gm1"));
        }

        [Fact]
        public void GifFile_UsesOldName()
        {
            Assert.Equal(Path.Combine(Root, "body_archer", "Gif", "ImageAsGif.gif"), workFolder.GifFile("body_archer.gm1"));
        }

        [Fact]
        public void BackupAndModdedFiles_KeepExtensionOfGameFile()
        {
            Assert.Equal(Path.Combine(Root, "anim_castle", "anim_castleSave.gm1"), workFolder.BackupFile("anim_castle.gm1"));
            Assert.Equal(Path.Combine(Root, "anim_castle", "anim_castleModded.gm1"), workFolder.ModdedFile("anim_castle.gm1"));
            Assert.Equal(Path.Combine(Root, "frontend", "frontendSave.tgx"), workFolder.BackupFile("frontend.tgx"));
        }

        [Fact]
        public void TgxImageFile_IsNamedAfterGameFile()
        {
            Assert.Equal(Path.Combine(Root, "frontend", "frontend.png"), workFolder.TgxImageFile("frontend.tgx"));
        }

        [Fact]
        public void OffsetsFile_IsInRoot()
        {
            Assert.Equal(Path.Combine(Root, "Offsets.json"), workFolder.OffsetsFile);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_WithoutRoot_Throws(string? root)
        {
            Assert.Throws<ArgumentException>(() => new WorkFolder(root!));
        }
    }
}

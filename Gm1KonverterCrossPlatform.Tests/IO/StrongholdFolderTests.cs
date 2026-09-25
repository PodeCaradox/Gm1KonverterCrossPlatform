using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.IO
{
    public class StrongholdFolderTests : IDisposable
    {
        private readonly TempDirectory temp = new TempDirectory();

        public void Dispose() => temp.Dispose();

        [Theory]
        [InlineData("C:\\Games\\Stronghold Crusader\\gm", "C:\\Games\\Stronghold Crusader")]
        [InlineData("C:\\Games\\Stronghold Crusader\\gm\\", "C:\\Games\\Stronghold Crusader")]
        [InlineData("C:\\Games\\Stronghold Crusader\\GM", "C:\\Games\\Stronghold Crusader")]
        [InlineData("/home/user/Stronghold/gm", "/home/user/Stronghold")]
        [InlineData("/home/user/Stronghold/gm/", "/home/user/Stronghold")]
        [InlineData("D:\\gm", "D:\\")]
        public void NormalizeRoot_GmSubFolder_ReturnsInstallationFolder(string path, string expected)
        {
            Assert.Equal(expected, StrongholdFolder.NormalizeRoot(path));
        }

        [Theory]
        [InlineData("C:\\gmods\\Stronghold")]
        [InlineData("C:\\Games\\Stronghold Crusader")]
        [InlineData("C:\\gm\\Stronghold")]
        [InlineData("C:\\Games\\Stronghold\\agm")]
        [InlineData("/home/user/Stronghold")]
        [InlineData("/home/user/gmods")]
        [InlineData("gm")]
        public void NormalizeRoot_OtherFolder_IsUnchanged(string path)
        {
            Assert.Equal(path, StrongholdFolder.NormalizeRoot(path));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeRoot_Empty_IsUnchanged(string? path)
        {
            Assert.Equal(path, StrongholdFolder.NormalizeRoot(path));
        }

        [Fact]
        public void Paths_PointIntoInstallationFolder()
        {
            var folder = new StrongholdFolder(temp.Path);

            Assert.Equal(Path.Combine(temp.Path, "gm"), folder.Gm1Folder);
            Assert.Equal(Path.Combine(temp.Path, "gfx"), folder.GfxFolder);
            Assert.Equal(Path.Combine(temp.Path, "gm", "anim_castle.gm1"), folder.Gm1File("anim_castle.gm1"));
            Assert.Equal(Path.Combine(temp.Path, "gfx", "frontend.tgx"), folder.TgxFile("frontend.tgx"));
            Assert.Equal(Path.Combine(temp.Path, "Stronghold Crusader.exe"), folder.CrusaderExecutablePath);
            Assert.Equal(Path.Combine(temp.Path, "Stronghold_Crusader_Extreme.exe"), folder.ExtremeExecutablePath);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_WithoutRoot_Throws(string? root)
        {
            Assert.Throws<ArgumentException>(() => new StrongholdFolder(root!));
        }

        [Fact]
        public void GetGm1FileNames_ReturnsSortedGm1FilesOnly()
        {
            foreach (string name in new[] { "tile_castle.gm1", "anim_castle.gm1", "Body_Archer.GM1", "readme.txt", "anim_castle.gm1.bak", "font.Gm1" })
            {
                temp.WriteFile(Path.Combine("gm", name), new byte[] { 1 });
            }

            Directory.CreateDirectory(temp.Combine("gm", "folder.gm1"));
            temp.WriteFile(Path.Combine("gfx", "other.gm1"), new byte[] { 1 });

            var names = new StrongholdFolder(temp.Path).GetGm1FileNames();

            Assert.Equal(new[] { "anim_castle.gm1", "Body_Archer.GM1", "font.Gm1", "tile_castle.gm1" }, names);
        }

        [Fact]
        public void GetTgxFileNames_ReturnsSortedTgxFilesOnly()
        {
            foreach (string name in new[] { "frontend_main.tgx", "Credits.TGX", "frontend_main.png" })
            {
                temp.WriteFile(Path.Combine("gfx", name), new byte[] { 1 });
            }

            var names = new StrongholdFolder(temp.Path).GetTgxFileNames();

            Assert.Equal(new[] { "Credits.TGX", "frontend_main.tgx" }, names);
        }

        [Fact]
        public void GetFileNames_MissingFolders_ReturnEmptyLists()
        {
            var folder = new StrongholdFolder(temp.Combine("does not exist"));

            Assert.Empty(folder.GetGm1FileNames());
            Assert.Empty(folder.GetTgxFileNames());
        }
    }
}

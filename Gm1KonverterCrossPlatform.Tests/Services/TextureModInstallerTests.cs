using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Core.Ucp;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Services
{
    public class TextureModInstallerTests : IDisposable
    {
        private const string FileName = "anim_castle.gm1";

        private static readonly byte[] OriginalContent = { 1, 2, 3, 4, 5 };
        private static readonly byte[] Modification = { 10, 20, 30 };
        private static readonly GameFile Castle = new GameFile(GameFolder.Gm, FileName);

        private readonly TempDirectory temp = new TempDirectory();
        private readonly StrongholdFolder stronghold;
        private readonly WorkFolder workFolder;
        private readonly string gameFile;
        private readonly UcpExtensionInfo modInfo = new UcpExtensionInfo("Textures", "1.0.0");

        public TextureModInstallerTests()
        {
            stronghold = new StrongholdFolder(temp.Combine("Stronghold"));
            workFolder = new WorkFolder(temp.Combine("work"));
            gameFile = temp.WriteFile(Path.Combine("Stronghold", "gm", FileName), OriginalContent);
        }

        public void Dispose() => temp.Dispose();

        [Fact]
        public void Install_WritesPluginAndLeavesGameFileUntouched()
        {
            var installer = CreateInstaller();

            installer.Install(Castle, Modification);

            Assert.Equal(OriginalContent, File.ReadAllBytes(gameFile));
            Assert.Equal(Modification, File.ReadAllBytes(temp.Combine("Stronghold", "ucp", "plugins", "Textures-1.0.0", "resources", "gm", FileName)));
            Assert.Equal(Modification, File.ReadAllBytes(workFolder.ModdedFile(FileName)));
            Assert.False(File.Exists(workFolder.BackupFile(FileName)));
        }

        [Fact]
        public void Install_WithoutWorkFolder_OnlyWritesPlugin()
        {
            var installer = new TextureModInstaller(stronghold, modInfo, workFolder: null);

            installer.Install(Castle, Modification);

            Assert.Equal(Modification, File.ReadAllBytes(installer.GetCurrentFile(Castle)));
            Assert.False(Directory.Exists(workFolder.Root));
        }

        [Fact]
        public void GetCurrentFile_PrefersReplacementOfPlugin()
        {
            var installer = CreateInstaller();
            Assert.Equal(gameFile, installer.GetCurrentFile(Castle));

            installer.Install(Castle, Modification);

            Assert.Equal(Modification, File.ReadAllBytes(installer.GetCurrentFile(Castle)));
        }

        [Fact]
        public void GetCurrentFile_Tgx_UsesGfxFolder()
        {
            Assert.Equal(stronghold.TgxFile("frame.tgx"), CreateInstaller().GetCurrentFile(new GameFile(GameFolder.Gfx, "frame.tgx")));
        }

        [Fact]
        public void Restore_RemovesFileFromPlugin()
        {
            var installer = CreateInstaller();
            installer.Install(Castle, Modification);
            Assert.True(installer.CanRestore(Castle));

            installer.Restore(Castle);

            Assert.False(installer.CanRestore(Castle));
            Assert.Equal(gameFile, installer.GetCurrentFile(Castle));
            Assert.Equal(OriginalContent, File.ReadAllBytes(gameFile));
        }

        [Fact]
        public void Restore_GameFileOverwrittenByOlderVersion_CopiesBackupBack()
        {
            // older versions overwrote the game file and kept the original as <name>Save.gm1
            File.WriteAllBytes(gameFile, Modification);
            temp.WriteFile(workFolder.BackupFile(FileName), OriginalContent);
            var installer = CreateInstaller();
            Assert.True(installer.CanRestore(Castle));

            installer.Restore(Castle);

            Assert.Equal(OriginalContent, File.ReadAllBytes(gameFile));
            Assert.False(File.Exists(workFolder.BackupFile(FileName)));
            Assert.False(installer.CanRestore(Castle));
        }

        [Fact]
        public void Restore_NothingToRestore_ThrowsWorkflowException()
        {
            var installer = CreateInstaller();

            Assert.False(installer.CanRestore(Castle));
            Assert.Throws<WorkflowException>(() => installer.Restore(Castle));
            Assert.Equal(OriginalContent, File.ReadAllBytes(gameFile));
        }

        private TextureModInstaller CreateInstaller() => new TextureModInstaller(stronghold, modInfo, workFolder);
    }
}

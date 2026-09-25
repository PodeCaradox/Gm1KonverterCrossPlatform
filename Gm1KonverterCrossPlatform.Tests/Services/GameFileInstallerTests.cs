using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Services
{
    public class GameFileInstallerTests : IDisposable
    {
        private const string FileName = "anim_castle.gm1";

        private static readonly byte[] OriginalContent = { 1, 2, 3, 4, 5 };
        private static readonly byte[] FirstModification = { 10, 20, 30 };
        private static readonly byte[] SecondModification = { 100, 200 };

        private readonly TempDirectory temp = new TempDirectory();
        private readonly WorkFolder workFolder;
        private readonly string gameFile;

        public GameFileInstallerTests()
        {
            workFolder = new WorkFolder(temp.Combine("work"));
            gameFile = temp.WriteFile(Path.Combine("Stronghold", "gm", FileName), OriginalContent);
        }

        private string BackupFile => workFolder.BackupFile(FileName);

        private string ModdedFile => workFolder.ModdedFile(FileName);

        public void Dispose() => temp.Dispose();

        [Fact]
        public void Install_FirstTime_CreatesBackupWithOriginalContent()
        {
            GameFileInstaller.Install(gameFile, FirstModification, BackupFile, ModdedFile);

            Assert.Equal(OriginalContent, File.ReadAllBytes(BackupFile));
        }

        [Fact]
        public void Install_SecondTime_DoesNotOverwriteBackup()
        {
            GameFileInstaller.Install(gameFile, FirstModification, BackupFile, ModdedFile);

            GameFileInstaller.Install(gameFile, SecondModification, BackupFile, ModdedFile);

            Assert.Equal(OriginalContent, File.ReadAllBytes(BackupFile));
        }

        [Fact]
        public void Install_WritesNewContentToGameFile()
        {
            GameFileInstaller.Install(gameFile, FirstModification, BackupFile, ModdedFile);
            Assert.Equal(FirstModification, File.ReadAllBytes(gameFile));

            GameFileInstaller.Install(gameFile, SecondModification, BackupFile, ModdedFile);
            Assert.Equal(SecondModification, File.ReadAllBytes(gameFile));
            Assert.False(File.Exists(gameFile + ".tmp"));
        }

        [Fact]
        public void Install_WritesModdedCopy()
        {
            GameFileInstaller.Install(gameFile, FirstModification, BackupFile, ModdedFile);
            Assert.Equal(FirstModification, File.ReadAllBytes(ModdedFile));

            GameFileInstaller.Install(gameFile, SecondModification, BackupFile, ModdedFile);
            Assert.Equal(SecondModification, File.ReadAllBytes(ModdedFile));
        }

        [Fact]
        public void Install_WithoutModdedCopy_WritesOnlyGameFileAndBackup()
        {
            GameFileInstaller.Install(gameFile, FirstModification, BackupFile);

            Assert.Equal(FirstModification, File.ReadAllBytes(gameFile));
            Assert.Equal(OriginalContent, File.ReadAllBytes(BackupFile));
            Assert.False(File.Exists(ModdedFile));
        }

        [Fact]
        public void Install_ExistingBackupFromEarlierSession_IsKept()
        {
            byte[] earlierBackup = { 42 };
            temp.WriteFile(Path.GetRelativePath(temp.Path, BackupFile), earlierBackup);

            GameFileInstaller.Install(gameFile, FirstModification, BackupFile, ModdedFile);

            Assert.Equal(earlierBackup, File.ReadAllBytes(BackupFile));
            Assert.Equal(FirstModification, File.ReadAllBytes(gameFile));
        }

        [Fact]
        public void Restore_CopiesBackupOverGameFile()
        {
            GameFileInstaller.Install(gameFile, FirstModification, BackupFile, ModdedFile);
            GameFileInstaller.Install(gameFile, SecondModification, BackupFile, ModdedFile);

            GameFileInstaller.Restore(BackupFile, gameFile);

            Assert.Equal(OriginalContent, File.ReadAllBytes(gameFile));
            Assert.Equal(OriginalContent, File.ReadAllBytes(BackupFile));
        }

        [Fact]
        public void Restore_WithoutBackup_ThrowsWorkflowExceptionAndKeepsGameFile()
        {
            Assert.Throws<WorkflowException>(() => GameFileInstaller.Restore(BackupFile, gameFile));

            Assert.Equal(OriginalContent, File.ReadAllBytes(gameFile));
        }
    }
}

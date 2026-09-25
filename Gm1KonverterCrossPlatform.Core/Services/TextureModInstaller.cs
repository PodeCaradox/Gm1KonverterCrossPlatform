using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Ucp;

namespace Gm1KonverterCrossPlatform.Core.Services
{
    /// <summary>
    /// Puts modified game files into a UCP3 plugin instead of overwriting them in the Stronghold folder.
    /// The original game files stay untouched; removing a file from the plugin restores the original.
    /// </summary>
    public sealed class TextureModInstaller
    {
        private readonly StrongholdFolder strongholdFolder;
        private readonly WorkFolder? workFolder;

        /// <param name="workFolder">Optional: receives a copy of each installed file and may hold backups of
        /// game files that older versions overwrote directly.</param>
        public TextureModInstaller(StrongholdFolder strongholdFolder, UcpExtensionInfo modInfo, WorkFolder? workFolder)
        {
            this.strongholdFolder = strongholdFolder ?? throw new ArgumentNullException(nameof(strongholdFolder));
            this.workFolder = workFolder;
            Ucp = new UcpFolder(strongholdFolder.Root);
            Plugin = UcpTexturePlugin.Open(Ucp, modInfo ?? throw new ArgumentNullException(nameof(modInfo)));
        }

        public UcpFolder Ucp { get; }

        public UcpTexturePlugin Plugin { get; }

        /// <summary>The file the game loads with the mod: the replacement of the plugin or the original game file.</summary>
        public string GetCurrentFile(GameFile file) => Plugin.FindFile(file) ?? GetGameFile(file);

        /// <summary>True if the plugin replaces the file or an older version left a backup of the original.</summary>
        public bool CanRestore(GameFile file) => Plugin.Contains(file) || LegacyBackupExists(file);

        /// <summary>Adds <paramref name="content"/> to the plugin.</summary>
        public void Install(GameFile file, byte[] content)
        {
            Plugin.AddFile(file, content);

            if (workFolder != null)
            {
                string moddedCopy = workFolder.ModdedFile(file.FileName);
                ImageFiles.EnsureDirectoryOf(moddedCopy);
                File.WriteAllBytes(moddedCopy, content);
            }
        }

        /// <summary>
        /// Removes the file from the plugin. If an older version of this program overwrote the game file
        /// directly, the saved original is copied back and the backup is deleted afterwards.
        /// </summary>
        /// <exception cref="WorkflowException">There is nothing to restore.</exception>
        public void Restore(GameFile file)
        {
            bool removed = Plugin.RemoveFile(file);

            if (LegacyBackupExists(file))
            {
                string backup = workFolder!.BackupFile(file.FileName);
                File.Copy(backup, GetGameFile(file), overwrite: true);
                File.Delete(backup);
            }
            else if (!removed)
            {
                throw new WorkflowException($"\"{file}\" is not part of the UCP mod \"{Plugin.Info.DisplayName}\".");
            }
        }

        private bool LegacyBackupExists(GameFile file) => workFolder != null && File.Exists(workFolder.BackupFile(file.FileName));

        private string GetGameFile(GameFile file)
        {
            switch (file.Folder)
            {
                case GameFolder.Gm: return strongholdFolder.Gm1File(file.FileName);
                case GameFolder.Gfx: return strongholdFolder.TgxFile(file.FileName);
                default: throw new ArgumentOutOfRangeException(nameof(file), file.Folder, null);
            }
        }
    }
}

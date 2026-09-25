using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;

namespace Gm1KonverterCrossPlatform.Core.Services
{
    /// <summary>
    /// Replaces game files with modified versions and restores the originals.
    /// </summary>
    public static class GameFileInstaller
    {
        /// <summary>
        /// Writes <paramref name="content"/> to the game file. The unmodified file is copied to
        /// <paramref name="backupFile"/> before the first modification and never overwritten afterwards.
        /// </summary>
        /// <param name="moddedCopy">Optional additional copy of the new content.</param>
        public static void Install(string gameFile, byte[] content, string backupFile, string? moddedCopy = null)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            if (!File.Exists(backupFile))
            {
                ImageFiles.EnsureDirectoryOf(backupFile);
                File.Copy(gameFile, backupFile);
            }

            WriteAllBytesSafely(gameFile, content);

            if (moddedCopy != null)
            {
                ImageFiles.EnsureDirectoryOf(moddedCopy);
                File.WriteAllBytes(moddedCopy, content);
            }
        }

        /// <summary>Copies the backup back over the game file.</summary>
        public static void Restore(string backupFile, string gameFile)
        {
            if (!File.Exists(backupFile))
            {
                throw new WorkflowException($"There is no saved original file \"{backupFile}\".");
            }

            File.Copy(backupFile, gameFile, overwrite: true);
        }

        /// <summary>Writes to a temporary file first so the game file is never left half written.</summary>
        private static void WriteAllBytesSafely(string path, byte[] content)
        {
            string temporaryFile = path + ".tmp";
            File.WriteAllBytes(temporaryFile, content);
            File.Move(temporaryFile, path, overwrite: true);
        }
    }
}

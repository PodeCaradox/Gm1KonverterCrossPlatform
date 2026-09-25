using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gm1KonverterCrossPlatform.Core.IO
{
    /// <summary>
    /// Paths inside the Stronghold (Crusader) installation folder.
    /// </summary>
    public sealed class StrongholdFolder
    {
        public const string Gm1FolderName = "gm";
        public const string GfxFolderName = "gfx";
        public const string CrusaderExecutable = "Stronghold Crusader.exe";
        public const string ExtremeExecutable = "Stronghold_Crusader_Extreme.exe";

        public StrongholdFolder(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("The Stronghold folder is not set.", nameof(root));
            Root = root;
        }

        public string Root { get; }

        public string Gm1Folder => Path.Combine(Root, Gm1FolderName);

        public string GfxFolder => Path.Combine(Root, GfxFolderName);

        public string Gm1File(string fileName) => Path.Combine(Gm1Folder, fileName);

        public string TgxFile(string fileName) => Path.Combine(GfxFolder, fileName);

        public string CrusaderExecutablePath => Path.Combine(Root, CrusaderExecutable);

        public string ExtremeExecutablePath => Path.Combine(Root, ExtremeExecutable);

        public IReadOnlyList<string> GetGm1FileNames() => GetFileNames(Gm1Folder, ".gm1");

        public IReadOnlyList<string> GetTgxFileNames() => GetFileNames(GfxFolder, ".tgx");

        /// <summary>
        /// Older versions stored the "gm" sub folder instead of the installation folder, and users sometimes
        /// select it by mistake. Returns the installation folder in both cases.
        /// </summary>
        public static string? NormalizeRoot(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            string trimmed = path!.TrimEnd('/', '\\');
            string lastSegment = trimmed.Split('/', '\\').Last();
            if (string.Equals(lastSegment, Gm1FolderName, StringComparison.OrdinalIgnoreCase))
            {
                string parent = trimmed.Substring(0, trimmed.Length - lastSegment.Length).TrimEnd('/', '\\');
                if (parent.EndsWith(":", StringComparison.Ordinal))
                {
                    parent += "\\";
                }

                if (parent.Length > 0)
                {
                    return parent;
                }
            }

            return path;
        }

        private static IReadOnlyList<string> GetFileNames(string directory, string extension)
        {
            if (!Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            return Directory.EnumerateFiles(directory)
                .Where(file => string.Equals(Path.GetExtension(file), extension, StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .OfType<string>()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

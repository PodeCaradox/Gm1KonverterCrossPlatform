using System;
using System.IO;
using System.Linq;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>The game folder a replaceable file belongs to.</summary>
    public enum GameFolder
    {
        /// <summary>"gm": .gm1 files.</summary>
        Gm,

        /// <summary>"gfx": .tgx files.</summary>
        Gfx,
    }

    /// <summary>A file the game loads, e.g. <c>gm\anim_castle.gm1</c>.</summary>
    public sealed record GameFile
    {
        /// <exception cref="ArgumentException"><paramref name="fileName"/> is not a plain file name.</exception>
        public GameFile(GameFolder folder, string fileName)
        {
            if (!IsValidFileName(fileName)) throw new ArgumentException($"\"{fileName}\" is not a file name.", nameof(fileName));

            Folder = folder;
            FileName = fileName;
        }

        public GameFolder Folder { get; }

        public string FileName { get; }

        /// <summary>"gm" or "gfx".</summary>
        public string FolderName => FolderNameOf(Folder);

        /// <summary>The path as the game opens it, e.g. <c>gm\anim_castle.gm1</c>. UCP compares it in lower case.</summary>
        public string GamePath => $"{FolderName}\\{FileName}".ToLowerInvariant();

        public static string FolderNameOf(GameFolder folder)
        {
            switch (folder)
            {
                case GameFolder.Gm: return "gm";
                case GameFolder.Gfx: return "gfx";
                default: throw new ArgumentOutOfRangeException(nameof(folder), folder, null);
            }
        }

        public bool Equals(GameFile? other)
        {
            return other != null && Folder == other.Folder && string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() => HashCode.Combine(Folder, StringComparer.OrdinalIgnoreCase.GetHashCode(FileName));

        public override string ToString() => $"{FolderName}/{FileName}";

        /// <summary>True for a plain file name without folder.</summary>
        public static bool IsValidFileName(string? fileName)
        {
            return !string.IsNullOrWhiteSpace(fileName)
                && fileName != "."
                && fileName != ".."
                && fileName.IndexOfAny(new[] { '/', '\\', ':' }) < 0
                && fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !fileName.Any(char.IsControl);
        }
    }
}

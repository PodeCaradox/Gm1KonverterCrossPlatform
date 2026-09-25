using System;
using System.IO;

namespace Gm1KonverterCrossPlatform.Core.IO
{
    /// <summary>
    /// Paths inside the user's work folder. Every game file gets a sub folder named like the file.
    /// The names are part of the user's workflow and must not change.
    /// </summary>
    public sealed class WorkFolder
    {
        public WorkFolder(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("The work folder is not set.", nameof(root));
            Root = root;
        }

        public string Root { get; }

        public string OffsetsFile => Path.Combine(Root, "Offsets.json");

        /// <summary>Folder of a game file, e.g. <c>work/anim_castle</c> for <c>anim_castle.gm1</c>.</summary>
        public string FileFolder(string gameFileName) => Path.Combine(Root, Path.GetFileNameWithoutExtension(gameFileName));

        public string ImagesFolder(string gameFileName) => Path.Combine(FileFolder(gameFileName), "Images");

        /// <param name="number">1 based image number.</param>
        public string ImageFile(string gameFileName, int number) => Path.Combine(ImagesFolder(gameFileName), $"Image{number}.png");

        public string BigImageFolder(string gameFileName) => Path.Combine(FileFolder(gameFileName), "BigImage");

        public string BigImageFile(string gameFileName) => Path.Combine(BigImageFolder(gameFileName), Path.GetFileNameWithoutExtension(gameFileName) + ".png");

        public string ColorTablesFolder(string gameFileName) => Path.Combine(FileFolder(gameFileName), "Colortables");

        /// <param name="number">1 based color table number.</param>
        public string ColorTableFile(string gameFileName, int number) => Path.Combine(ColorTablesFolder(gameFileName), $"ColorTable{number}.png");

        /// <param name="colorTableNumber">1 based color table number.</param>
        public string OriginalAnimationFolder(string gameFileName, int colorTableNumber)
        {
            return Path.Combine(FileFolder(gameFileName), $"OrginalAnimationPalette{colorTableNumber}");
        }

        public string OriginalAnimationFolderRoot(string gameFileName) => FileFolder(gameFileName);

        /// <param name="colorTableNumber">1 based color table number.</param>
        /// <param name="imageNumber">1 based image number.</param>
        public string OriginalAnimationFile(string gameFileName, int colorTableNumber, int imageNumber)
        {
            return Path.Combine(OriginalAnimationFolder(gameFileName, colorTableNumber), $"OrginalAnimationImg{imageNumber}.png");
        }

        public string GifFolder(string gameFileName) => Path.Combine(FileFolder(gameFileName), "Gif");

        public string GifFile(string gameFileName) => Path.Combine(GifFolder(gameFileName), "ImageAsGif.gif");

        /// <summary>Copy of the unmodified game file, created before the first modification.</summary>
        public string BackupFile(string gameFileName)
        {
            return Path.Combine(FileFolder(gameFileName), Path.GetFileNameWithoutExtension(gameFileName) + "Save" + Path.GetExtension(gameFileName));
        }

        /// <summary>Copy of the last modified game file.</summary>
        public string ModdedFile(string gameFileName)
        {
            return Path.Combine(FileFolder(gameFileName), Path.GetFileNameWithoutExtension(gameFileName) + "Modded" + Path.GetExtension(gameFileName));
        }

        /// <summary>Exported image of a standalone .tgx file.</summary>
        public string TgxImageFile(string gameFileName)
        {
            return Path.Combine(FileFolder(gameFileName), Path.GetFileNameWithoutExtension(gameFileName) + ".png");
        }
    }
}

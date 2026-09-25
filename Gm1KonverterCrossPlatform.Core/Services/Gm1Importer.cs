using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Layout;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Gm1KonverterCrossPlatform.Core.Services
{
    /// <summary>
    /// Imports edited images from the work folder into a .gm1 file. Images that do not exist in the
    /// work folder keep their current content. All files are read before the document is changed,
    /// so a broken file leaves the document unchanged.
    /// </summary>
    public sealed class Gm1Importer
    {
        private readonly WorkFolder workFolder;

        public Gm1Importer(WorkFolder workFolder)
        {
            this.workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
        }

        /// <summary>Imports <c>Images/Image{n}.png</c>.</summary>
        /// <returns>The number of imported images.</returns>
        public int ImportImages(Gm1Document document)
        {
            var replacements = new List<(int ItemIndex, Argb1555Image Image)>();
            int itemCount = document.ItemCount;
            for (int i = 0; i < itemCount; i++)
            {
                string path = workFolder.ImageFile(document.FileName, i + 1);
                if (!File.Exists(path))
                {
                    continue;
                }

                var image = LoadPng(path);
                if (!IsExportOfEmptyImage(document, i, image))
                {
                    replacements.Add((i, RemoveLegacyPadding(document, i, image)));
                }
            }

            if (replacements.Count == 0)
            {
                throw new WorkflowException($"No images found in \"{workFolder.ImagesFolder(document.FileName)}\". Please export the images first.");
            }

            ReplaceItems(document, replacements);
            return replacements.Count;
        }

        /// <summary>Imports the big image, using the same layout as the export.</summary>
        public void ImportBigImage(Gm1Document document)
        {
            string path = workFolder.BigImageFile(document.FileName);
            if (!File.Exists(path))
            {
                throw new WorkflowException($"\"{path}\" does not exist. Please export the big image first.");
            }

            var items = document.RenderItems();
            var replacements = new List<(int ItemIndex, Argb1555Image Image)>(items.Count);

            using (var sheet = LoadRgba(path))
            {
                var layout = SpriteSheetLayout.Arrange(items.Select(item => (item.Width, item.Height)).ToList(), sheet.Width);
                if (layout.Width > sheet.Width || layout.Height > sheet.Height)
                {
                    throw new WorkflowException(
                        $"\"{path}\" is {sheet.Width} x {sheet.Height} pixels but {layout.Width} x {layout.Height} pixels are needed. " +
                        "Please do not change the size of the exported image.");
                }

                for (int i = 0; i < layout.Cells.Count; i++)
                {
                    // Empty images have no area in the big image, there is nothing to import.
                    var cell = layout.Cells[i];
                    if (cell.Width > 0 && cell.Height > 0)
                    {
                        replacements.Add((i, ImageFiles.ToArgb1555(sheet, cell.X, cell.Y, cell.Width, cell.Height)));
                    }
                }
            }

            ReplaceItems(document, replacements);
        }

        /// <summary>Imports <c>Colortables/ColorTable{n}.png</c>.</summary>
        /// <returns>The number of imported color tables.</returns>
        public int ImportColorTables(Gm1Document document)
        {
            if (!document.HasColorTables) throw new InvalidOperationException("Only animation files have color tables.");

            var tables = document.File.Palette.ColorTables;
            var imported = new Dictionary<int, ColorTable>();
            for (int i = 0; i < tables.Length; i++)
            {
                string path = workFolder.ColorTableFile(document.FileName, i + 1);
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    imported[i] = ColorTableImage.Read(LoadPng(path), tables[i]);
                }
                catch (ArgumentException e)
                {
                    throw new WorkflowException($"\"{path}\": {e.Message}", e);
                }
            }

            if (imported.Count == 0)
            {
                throw new WorkflowException($"No color tables found in \"{workFolder.ColorTablesFolder(document.FileName)}\". Please export the color tables first.");
            }

            foreach (var table in imported)
            {
                tables[table.Key] = table.Value;
            }

            return imported.Count;
        }

        /// <summary>
        /// Imports images exported by <see cref="Gm1Exporter.ExportOriginalAnimation"/>. The versions of an image
        /// for the other color tables resolve colors that appear several times in a color table.
        /// </summary>
        /// <returns>The number of imported images.</returns>
        public int ImportOriginalAnimation(Gm1Document document)
        {
            if (!document.HasColorTables) throw new InvalidOperationException("Only animation files have color tables.");

            var replacements = new List<(int ItemIndex, Argb1555Image?[] ImagesPerColorTable)>();
            int itemCount = document.ItemCount;
            for (int i = 0; i < itemCount; i++)
            {
                string firstPath = workFolder.OriginalAnimationFile(document.FileName, 1, i + 1);
                if (!File.Exists(firstPath))
                {
                    continue;
                }

                var first = LoadPng(firstPath);
                var images = new Argb1555Image?[Palette.ColorTableCount];
                images[0] = first;
                for (int table = 1; table < Palette.ColorTableCount; table++)
                {
                    string path = workFolder.OriginalAnimationFile(document.FileName, table + 1, i + 1);
                    if (File.Exists(path))
                    {
                        var image = LoadPng(path);
                        images[table] = image.Width == first.Width && image.Height == first.Height ? image : null;
                    }
                }

                replacements.Add((i, images));
            }

            if (replacements.Count == 0)
            {
                throw new WorkflowException($"No images found in \"{workFolder.OriginalAnimationFolder(document.FileName, 1)}\". Please export the original animation first.");
            }

            try
            {
                foreach (var replacement in replacements)
                {
                    document.EnsureCanStore(replacement.ImagesPerColorTable[0]!);
                }
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new WorkflowException(e.Message, e);
            }

            foreach (var replacement in replacements)
            {
                document.ReplaceItemWithColorTableImages(replacement.ItemIndex, replacement.ImagesPerColorTable);
            }

            return replacements.Count;
        }

        /// <summary>Loads a PNG; unreadable files are reported with their path.</summary>
        internal static Argb1555Image LoadPng(string path)
        {
            try
            {
                return ImageFiles.LoadPng(path);
            }
            catch (Exception e) when (IsUnreadableImage(e))
            {
                throw new WorkflowException($"\"{path}\" could not be read: {e.Message}", e);
            }
        }

        private static Image<Rgba32> LoadRgba(string path)
        {
            try
            {
                return ImageFiles.LoadRgba(path);
            }
            catch (Exception e) when (IsUnreadableImage(e))
            {
                throw new WorkflowException($"\"{path}\" could not be read: {e.Message}", e);
            }
        }

        private static bool IsUnreadableImage(Exception e)
        {
            return e is ImageFormatException || e is NotSupportedException || e is IOException || e is UnauthorizedAccessException;
        }

        private static void ReplaceItems(Gm1Document document, List<(int ItemIndex, Argb1555Image Image)> replacements)
        {
            try
            {
                document.ReplaceItems(replacements);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new WorkflowException(e.Message, e);
            }
        }

        /// <summary>
        /// PNG files cannot be empty, so images without pixels are exported as one transparent pixel.
        /// </summary>
        private static bool IsExportOfEmptyImage(Gm1Document document, int itemIndex, Argb1555Image image)
        {
            if (document.IsBuildingFile || image.Width != 1 || image.Height != 1 || image.Pixels[0] != Argb1555.TransparentMarker)
            {
                return false;
            }

            var header = document.GetFirstImageOfItem(itemIndex).Header;
            return header.Width == 0 || header.Height <= document.DataType.HeightPadding();
        }

        /// <summary>
        /// Images of data type 5 exported by version 5b1ade8 and earlier had 7 additional transparent rows,
        /// importing them unchanged would make the image 7 rows higher each time.
        /// </summary>
        private static Argb1555Image RemoveLegacyPadding(Gm1Document document, int itemIndex, Argb1555Image image)
        {
            int padding = document.DataType.HeightPadding();
            if (padding == 0 || image.Height < padding)
            {
                return image;
            }

            var header = document.GetFirstImageOfItem(itemIndex).Header;
            if (image.Height != header.Height || !AreRowsTransparent(image, image.Height - padding, padding))
            {
                return image;
            }

            return image.Crop(0, 0, image.Width, image.Height - padding);
        }

        private static bool AreRowsTransparent(Argb1555Image image, int firstRow, int rowCount)
        {
            for (int y = firstRow; y < firstRow + rowCount; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (image[x, y] != Argb1555.TransparentMarker)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

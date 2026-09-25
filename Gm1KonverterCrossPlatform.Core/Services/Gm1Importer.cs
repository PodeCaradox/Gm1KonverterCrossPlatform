using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Layout;

namespace Gm1KonverterCrossPlatform.Core.Services
{
    /// <summary>
    /// Imports edited images from the work folder into a .gm1 file. Images that do not exist in the
    /// work folder keep their current content.
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
            int imported = 0;
            int itemCount = document.ItemCount;
            for (int i = 0; i < itemCount; i++)
            {
                string path = workFolder.ImageFile(document.FileName, i + 1);
                if (!File.Exists(path))
                {
                    continue;
                }

                var image = RemoveLegacyPadding(document, i, ImageFiles.LoadPng(path));
                document.ReplaceItem(i, image);
                imported++;
            }

            if (imported == 0)
            {
                throw new WorkflowException($"No images found in \"{workFolder.ImagesFolder(document.FileName)}\". Please export the images first.");
            }

            return imported;
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
            var images = new List<Argb1555Image>(items.Count);

            using (var sheet = ImageFiles.LoadRgba(path))
            {
                var layout = SpriteSheetLayout.Arrange(items.Select(item => (item.Width, item.Height)).ToList(), sheet.Width);
                if (layout.Width > sheet.Width || layout.Height > sheet.Height)
                {
                    throw new WorkflowException(
                        $"\"{path}\" is {sheet.Width} x {sheet.Height} pixels but {layout.Width} x {layout.Height} pixels are needed. " +
                        "Please do not change the size of the exported image.");
                }

                foreach (var cell in layout.Cells)
                {
                    images.Add(ImageFiles.ToArgb1555(sheet, cell.X, cell.Y, cell.Width, cell.Height));
                }
            }

            for (int i = 0; i < images.Count; i++)
            {
                document.ReplaceItem(i, images[i]);
            }
        }

        /// <summary>Imports <c>Colortables/ColorTable{n}.png</c>.</summary>
        /// <returns>The number of imported color tables.</returns>
        public int ImportColorTables(Gm1Document document)
        {
            if (!document.HasColorTables) throw new InvalidOperationException("Only animation files have color tables.");

            int imported = 0;
            for (int i = 0; i < Palette.ColorTableCount; i++)
            {
                string path = workFolder.ColorTableFile(document.FileName, i + 1);
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    document.File.Palette.ColorTables[i] = ColorTableImage.Read(ImageFiles.LoadPng(path));
                }
                catch (ArgumentException e)
                {
                    throw new WorkflowException($"\"{path}\": {e.Message}", e);
                }

                imported++;
            }

            if (imported == 0)
            {
                throw new WorkflowException($"No color tables found in \"{workFolder.ColorTablesFolder(document.FileName)}\". Please export the color tables first.");
            }

            return imported;
        }

        /// <summary>
        /// Imports images exported by <see cref="Gm1Exporter.ExportOriginalAnimation"/>. The versions of an image
        /// for the other color tables resolve colors that appear several times in a color table.
        /// </summary>
        /// <returns>The number of imported images.</returns>
        public int ImportOriginalAnimation(Gm1Document document)
        {
            if (!document.HasColorTables) throw new InvalidOperationException("Only animation files have color tables.");

            int imported = 0;
            int itemCount = document.ItemCount;
            for (int i = 0; i < itemCount; i++)
            {
                string firstPath = workFolder.OriginalAnimationFile(document.FileName, 1, i + 1);
                if (!File.Exists(firstPath))
                {
                    continue;
                }

                var images = new Argb1555Image?[Palette.ColorTableCount];
                images[0] = ImageFiles.LoadPng(firstPath);
                for (int table = 1; table < Palette.ColorTableCount; table++)
                {
                    string path = workFolder.OriginalAnimationFile(document.FileName, table + 1, i + 1);
                    if (File.Exists(path))
                    {
                        var image = ImageFiles.LoadPng(path);
                        images[table] = image.Width == images[0]!.Width && image.Height == images[0]!.Height ? image : null;
                    }
                }

                document.ReplaceItemWithColorTableImages(i, images);
                imported++;
            }

            if (imported == 0)
            {
                throw new WorkflowException($"No images found in \"{workFolder.OriginalAnimationFolder(document.FileName, 1)}\". Please export the original animation first.");
            }

            return imported;
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

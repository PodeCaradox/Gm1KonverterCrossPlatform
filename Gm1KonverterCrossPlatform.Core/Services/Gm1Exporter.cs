using System;
using System.Collections.Generic;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Layout;

namespace Gm1KonverterCrossPlatform.Core.Services
{
    /// <summary>
    /// Exports the images of a .gm1 file into the work folder.
    /// </summary>
    public sealed class Gm1Exporter
    {
        private readonly WorkFolder workFolder;

        public Gm1Exporter(WorkFolder workFolder)
        {
            this.workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
        }

        /// <summary>Saves every item as <c>Images/Image{n}.png</c>.</summary>
        /// <returns>The folder the images were written to.</returns>
        public string ExportImages(Gm1Document document)
        {
            var items = document.RenderItems();
            for (int i = 0; i < items.Count; i++)
            {
                ImageFiles.SavePng(items[i], workFolder.ImageFile(document.FileName, i + 1));
            }

            return workFolder.ImagesFolder(document.FileName);
        }

        /// <summary>Saves all items into one image, see <see cref="SpriteSheetLayout"/>.</summary>
        /// <returns>The folder the image was written to.</returns>
        public string ExportBigImage(Gm1Document document, int maxWidth)
        {
            var items = document.RenderItems();
            var layout = SpriteSheetLayout.Arrange(items.Select(item => (item.Width, item.Height)).ToList(), maxWidth);

            var sheet = new Argb1555Image(layout.Width, layout.Height);
            for (int i = 0; i < items.Count; i++)
            {
                sheet.Draw(items[i], layout.Cells[i].X, layout.Cells[i].Y);
            }

            ImageFiles.SavePng(sheet, workFolder.BigImageFile(document.FileName));
            return workFolder.BigImageFolder(document.FileName);
        }

        /// <summary>Saves the 10 color tables as <c>Colortables/ColorTable{n}.png</c>.</summary>
        public string ExportColorTables(Gm1Document document)
        {
            EnsureColorTables(document);

            var tables = document.File.Palette.ColorTables;
            for (int i = 0; i < tables.Length; i++)
            {
                ImageFiles.SavePng(ColorTableImage.Render(tables[i]), workFolder.ColorTableFile(document.FileName, i + 1));
            }

            return workFolder.ColorTablesFolder(document.FileName);
        }

        /// <summary>
        /// Saves every image once per color table (<c>OrginalAnimationPalette{t}/OrginalAnimationImg{n}.png</c>),
        /// which allows an exact re-import, see <see cref="Gm1Importer.ImportOriginalAnimation"/>.
        /// </summary>
        public string ExportOriginalAnimation(Gm1Document document)
        {
            EnsureColorTables(document);

            for (int table = 0; table < Palette.ColorTableCount; table++)
            {
                IReadOnlyList<Argb1555Image> items = document.RenderItems(table);
                for (int i = 0; i < items.Count; i++)
                {
                    ImageFiles.SavePng(items[i], workFolder.OriginalAnimationFile(document.FileName, table + 1, i + 1));
                }
            }

            return workFolder.OriginalAnimationFolderRoot(document.FileName);
        }

        /// <summary>Saves the given images as looping GIF animation.</summary>
        public string ExportGif(Gm1Document document, IReadOnlyList<Argb1555Image> frames, int delayMilliseconds)
        {
            GifExporter.Save(frames, delayMilliseconds, workFolder.GifFile(document.FileName));
            return workFolder.GifFolder(document.FileName);
        }

        private static void EnsureColorTables(Gm1Document document)
        {
            if (!document.HasColorTables)
            {
                throw new InvalidOperationException("Only animation files have color tables.");
            }
        }
    }
}

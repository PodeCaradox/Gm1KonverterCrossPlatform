using System;
using Gm1KonverterCrossPlatform.Core.Files;

namespace Gm1KonverterCrossPlatform.Core.Imaging
{
    /// <summary>
    /// Shows a color table as a grid of <see cref="Palette.ImageColumns"/> x <see cref="Palette.ImageRows"/> colored cells.
    /// </summary>
    public static class ColorTableImage
    {
        public static Argb1555Image Render(ColorTable colorTable, int cellSize = Palette.ImageCellSize)
        {
            if (colorTable == null) throw new ArgumentNullException(nameof(colorTable));
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));

            var image = new Argb1555Image(Palette.ImageColumns * cellSize, Palette.ImageRows * cellSize);
            for (int i = 0; i < ColorTable.ColorCount; i++)
            {
                int cellX = i % Palette.ImageColumns * cellSize;
                int cellY = i / Palette.ImageColumns * cellSize;
                for (int y = 0; y < cellSize; y++)
                {
                    for (int x = 0; x < cellSize; x++)
                    {
                        image[cellX + x, cellY + y] = colorTable[i];
                    }
                }
            }

            return image;
        }

        /// <summary>Reads the color of every cell (its top left pixel).</summary>
        /// <param name="current">
        /// The color table the image was exported from. Its colors without alpha bit are exported as transparent
        /// pixels; they are kept when the cell is still transparent, so an unchanged image imports unchanged.
        /// </param>
        /// <exception cref="ArgumentException">The image is too small.</exception>
        public static ColorTable Read(Argb1555Image image, ColorTable? current = null, int cellSize = Palette.ImageCellSize)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));

            int minimumWidth = (Palette.ImageColumns - 1) * cellSize + 1;
            int minimumHeight = (Palette.ImageRows - 1) * cellSize + 1;
            if (image.Width < minimumWidth || image.Height < minimumHeight)
            {
                throw new ArgumentException(
                    $"A color table image must be {Palette.ImageColumns * cellSize} x {Palette.ImageRows * cellSize} pixels, but it is {image.Width} x {image.Height}.",
                    nameof(image));
            }

            var colors = new ushort[ColorTable.ColorCount];
            for (int i = 0; i < ColorTable.ColorCount; i++)
            {
                ushort color = image[i % Palette.ImageColumns * cellSize, i / Palette.ImageColumns * cellSize];
                bool keepCurrent = current != null && !Argb1555.IsOpaque(color) && !Argb1555.IsOpaque(current[i]);
                colors[i] = keepCurrent ? current![i] : color;
            }

            return new ColorTable(colors);
        }

        /// <summary>Index of the color at a position in an image created by <see cref="Render"/>, -1 outside of the grid.</summary>
        public static int IndexAt(double x, double y, int cellSize)
        {
            if (cellSize <= 0) throw new ArgumentOutOfRangeException(nameof(cellSize));
            if (double.IsNaN(x) || double.IsNaN(y) || x < 0 || y < 0) return -1;

            int column = (int)x / cellSize;
            int row = (int)y / cellSize;
            if (column >= Palette.ImageColumns || row >= Palette.ImageRows) return -1;

            return row * Palette.ImageColumns + column;
        }
    }
}

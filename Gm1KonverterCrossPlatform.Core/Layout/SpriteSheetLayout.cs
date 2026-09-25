using System;
using System.Collections.Generic;

namespace Gm1KonverterCrossPlatform.Core.Layout
{
    /// <summary>
    /// Arranges images row by row into one big image ("big image" export and import).
    /// Export and import must use the same layout, so both use this class.
    /// </summary>
    public sealed class SpriteSheetLayout
    {
        private SpriteSheetLayout(int width, int height, IReadOnlyList<Cell> cells)
        {
            Width = width;
            Height = height;
            Cells = cells;
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>Position of every image, in input order.</summary>
        public IReadOnlyList<Cell> Cells { get; }

        /// <summary>
        /// Places the images from left to right. A new row starts when the next image would reach
        /// <paramref name="maxWidth"/>. The sheet is at least as wide as the widest image.
        /// </summary>
        public static SpriteSheetLayout Arrange(IReadOnlyList<(int Width, int Height)> sizes, int maxWidth)
        {
            if (sizes == null) throw new ArgumentNullException(nameof(sizes));
            if (maxWidth <= 0) throw new ArgumentOutOfRangeException(nameof(maxWidth), maxWidth, "The width must be greater than 0.");

            int sheetWidth = maxWidth;
            foreach (var size in sizes)
            {
                sheetWidth = Math.Max(sheetWidth, size.Width);
            }

            var cells = new List<Cell>(sizes.Count);
            int x = 0;
            int y = 0;
            int rowHeight = 0;

            foreach (var size in sizes)
            {
                bool rowHasImages = x > 0 || rowHeight > 0;
                if (rowHasImages && x + size.Width >= sheetWidth)
                {
                    y += rowHeight;
                    x = 0;
                    rowHeight = 0;
                }

                cells.Add(new Cell(x, y, size.Width, size.Height));
                x += size.Width;
                rowHeight = Math.Max(rowHeight, size.Height);
            }

            return new SpriteSheetLayout(sheetWidth, Math.Max(1, y + rowHeight), cells);
        }

        public readonly struct Cell
        {
            public Cell(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}

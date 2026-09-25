using System;

namespace Gm1KonverterCrossPlatform.Core.Imaging
{
    /// <summary>
    /// A bitmap with 16 bit ARGB1555 pixels stored row by row.
    /// Pixels that were never written are 0, which is a fully transparent color.
    /// </summary>
    public sealed class Argb1555Image
    {
        public Argb1555Image(int width, int height)
            : this(width, height, new ushort[CheckedPixelCount(width, height)])
        {
        }

        public Argb1555Image(int width, int height, ushort[] pixels)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));
            if (pixels.Length != CheckedPixelCount(width, height))
            {
                throw new ArgumentException($"Expected {width * height} pixels but got {pixels.Length}.", nameof(pixels));
            }

            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public int Width { get; }

        public int Height { get; }

        public ushort[] Pixels { get; }

        public ushort this[int x, int y]
        {
            get => Pixels[y * Width + x];
            set => Pixels[y * Width + x] = value;
        }

        public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        /// <summary>
        /// Copies a rectangle out of this image. Pixels outside of this image are filled with
        /// <paramref name="outsideColor"/>.
        /// </summary>
        public Argb1555Image Crop(int x, int y, int width, int height, ushort outsideColor = 0)
        {
            var result = new Argb1555Image(width, height);

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    int sourceX = x + column;
                    int sourceY = y + row;
                    result[column, row] = Contains(sourceX, sourceY) ? this[sourceX, sourceY] : outsideColor;
                }
            }

            return result;
        }

        /// <summary>Draws <paramref name="source"/> into this image, clipping everything outside.</summary>
        public void Draw(Argb1555Image source, int x, int y)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            for (int row = 0; row < source.Height; row++)
            {
                for (int column = 0; column < source.Width; column++)
                {
                    if (Contains(x + column, y + row))
                    {
                        this[x + column, y + row] = source[column, row];
                    }
                }
            }
        }

        private static int CheckedPixelCount(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), width, "Width must not be negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), height, "Height must not be negative.");
            return checked(width * height);
        }
    }
}

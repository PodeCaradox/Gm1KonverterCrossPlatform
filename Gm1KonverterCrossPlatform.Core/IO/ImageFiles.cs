using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Gm1KonverterCrossPlatform.Core.IO
{
    /// <summary>
    /// Reads and writes PNG files. Imported pixels with an alpha value of 0 become
    /// <see cref="Argb1555.TransparentMarker"/>, all other pixels are opaque.
    /// </summary>
    public static class ImageFiles
    {
        public static Argb1555Image LoadPng(string path)
        {
            using var image = Image.Load<Rgba32>(path);
            return ToArgb1555(image, 0, 0, image.Width, image.Height);
        }

        public static Image<Rgba32> LoadRgba(string path) => Image.Load<Rgba32>(path);

        /// <summary>Converts a rectangle of <paramref name="image"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The rectangle is not completely inside the image.</exception>
        public static Argb1555Image ToArgb1555(Image<Rgba32> image, int x, int y, int width, int height)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (x < 0 || y < 0 || width < 0 || height < 0 || x + width > image.Width || y + height > image.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(image),
                    $"The area {width} x {height} at ({x}, {y}) is outside of the image ({image.Width} x {image.Height}).");
            }

            var result = new Argb1555Image(width, height);
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    result[column, row] = ToArgb1555(image[x + column, y + row]);
                }
            }

            return result;
        }

        public static ushort ToArgb1555(Rgba32 pixel)
        {
            return pixel.A == 0
                ? Argb1555.TransparentMarker
                : Argb1555.Encode(pixel.R, pixel.G, pixel.B, byte.MaxValue);
        }

        public static Rgba32 ToRgba32(ushort color)
        {
            Argb1555.Decode(color, out byte r, out byte g, out byte b, out byte a);
            return a == 0 ? new Rgba32(0, 0, 0, 0) : new Rgba32(r, g, b, a);
        }

        public static Image<Rgba32> ToRgba(Argb1555Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var result = new Image<Rgba32>(Math.Max(1, image.Width), Math.Max(1, image.Height));
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    result[x, y] = ToRgba32(image[x, y]);
                }
            }

            return result;
        }

        /// <summary>Saves the image as PNG, creating the directory if necessary.</summary>
        public static void SavePng(Argb1555Image image, string path)
        {
            EnsureDirectoryOf(path);
            using var rgba = ToRgba(image);
            rgba.SaveAsPng(path);
        }

        internal static void EnsureDirectoryOf(string path)
        {
            string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}

using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    /// <summary>Creates Avalonia bitmaps for the preview.</summary>
    internal static class BitmapFactory
    {
        private static readonly Vector Dpi = new Vector(96, 96);

        public static WriteableBitmap Create(Argb1555Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            // Avalonia cannot create empty bitmaps.
            int width = Math.Max(1, image.Width);
            int height = Math.Max(1, image.Height);

            var bitmap = new WriteableBitmap(new PixelSize(width, height), Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (var buffer = bitmap.Lock())
            {
                var row = new int[width];
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        row[x] = unchecked((int)ToPremultipliedBgra(image[x, y]));
                    }

                    Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, width);
                }
            }

            return bitmap;
        }

        /// <summary>Alpha is either 0 or 255, so premultiplying only means clearing transparent pixels.</summary>
        private static uint ToPremultipliedBgra(ushort color)
        {
            return Argb1555.IsOpaque(color) ? Argb1555.ToBgra8888(color) : 0u;
        }
    }
}

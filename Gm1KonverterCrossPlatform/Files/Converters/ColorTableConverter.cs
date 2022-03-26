using System;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Media.Imaging;

namespace Gm1KonverterCrossPlatform.Files.Converters
{
	class ColorTableConverter
	{
        /// <summary>
        /// Convert ColorTable to image
        /// </summary>
        public static unsafe WriteableBitmap GetBitmap(ColorTable colorTable, int width, int height, int size)
        {
            if ((width * height) != ColorTable.ColorCount)
            {
                throw new ArgumentException($"Value of (width * height) must equal the total number of colors contained in colorTable, which is {ColorTable.ColorCount}.");
            }

            int bitmapWidth = width * size;
            int bitmapHeight = height * size;

            WriteableBitmap bitmap = new WriteableBitmap(
                new PixelSize(bitmapWidth, bitmapHeight),
                new Vector(96, 96),
                PixelFormat.Bgra8888, // Bgra8888 is device-native and much faster
                AlphaFormat.Premul);

            using (ILockedFramebuffer buffer = bitmap.Lock())
            {
                uint* pointer = (uint*)buffer.Address;

                for (int i = 0; i < ColorTable.ColorCount; i++)
                {
                    // position of color in bitmap
                    int y = i / width;
                    int x = i - (y * width);

                    y *= size;
                    x *= size;

                    // get converted color
                    uint colorBgra8888 = ColorConverter.Argb1555ToBgra8888(colorTable.ColorList[i]);

                    // write color to bitmap
                    for (int yy = 0; yy < size; yy++)
                    {
                        for (int xx = 0; xx < size; xx++)
                        {
                            pointer[((y + yy) * bitmapWidth) + (x + xx)] = colorBgra8888;
                        }
                    }
                }
            }

            return bitmap;
        }
    }
}

using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
	public class ColorTable
	{
        #region Public

        /// <summary>
        /// Color table is always 512 bytes in size, consisting of 256 2-byte colors.
        /// </summary>
        public const int ByteSize = 512;

        /// <summary>
        /// Color table always contains 256 colors.
        /// </summary>
        public const int ColorCount = 256;

        #endregion

        #region Variables

        private ushort[] colorList = new ushort[ColorCount];
        private WriteableBitmap bitmap;

        #endregion

        #region Construtor

        public ColorTable(byte[] byteArray)
        {
            for (int colorIndex = 0; colorIndex < ColorCount; colorIndex++)
            {
                colorList[colorIndex] = BitConverter.ToUInt16(byteArray, colorIndex * 2);
            }

            bitmap = GenerateBitmap(10);
        }

        public ColorTable(ushort[] ushortArray)
        {
            if (ushortArray.Length != ColorCount) {
                throw new ArgumentException($"Invalid input length ({ushortArray.Length}). The length must be {ColorCount}.");
            }

            colorList = ushortArray;

            bitmap = GenerateBitmap(10);
        }

        #endregion

        #region GetterSetter

        public ushort[] ColorList { get => colorList; set => colorList = value; }
        public WriteableBitmap Bitmap { get => bitmap; set => bitmap = value; }

        #endregion

        #region Methods

        /// <summary>
        /// Calculate the new byte array to save color table to a .gm1 file
        /// </summary>
        internal byte[] GetBytes()
        {
            List<byte> byteArray = new List<byte>();

            for (int i = 0; i < colorList.Length; i++)
            {
                byteArray.AddRange(BitConverter.GetBytes(colorList[i]));
            }

            return byteArray.ToArray();
        }

        /// <summary>
        /// Create new image from color list.
        /// </summary>
        /// <param name="scale">Scale the palette to desired size</param>
        unsafe private WriteableBitmap GenerateBitmap(int scale)
        {
            // layout used to draw color list to an image
            // total value of width * height must equal 256
            int width = 32;
            int height = 8;

            int bitmapWidth = width * scale;
            int bitmapHeight = height * scale;

            WriteableBitmap bitmap = new WriteableBitmap(
                new Avalonia.PixelSize(bitmapWidth, bitmapHeight),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888 // Bgra8888 is device-native and much faster
            );

            using (var buffer = bitmap.Lock())
            {
                for (int i = 0; i < 256; i++)
                {
                    // position of color in bitmap
                    int y = i / width;
                    int x = i - (y * width);

                    y *= scale;
                    x *= scale;

                    // get converted color
                    Utility.ReadColor(colorList[i], out byte r, out byte g, out byte b, out byte a);
                    uint colorByte = (uint)(b | (g << 8) | (r << 16) | (a << 24));

                    // write color to bitmap
                    for (int yy = 0; yy < scale; yy++)
                    {
                        for (int xx = 0; xx < scale; xx++)
                        {
                            var ptr = (uint*)buffer.Address;
                            ptr += (uint)(((y + yy) * bitmapWidth) + (x + xx));
                            *ptr = colorByte;
                        }
                    }
                }
            }

            return bitmap;
        }

        #endregion
    }
}

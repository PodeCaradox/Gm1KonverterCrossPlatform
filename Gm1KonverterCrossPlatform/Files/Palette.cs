using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
    public class Palette
    {
        /// <summary>
        /// The palette is always 5120 bytes in size.
        /// The palette consist of 10 colortables, each being 512 bytes in size
        /// and consisting of 256 2-byte colors.
        /// </summary>
        public const int ByteSize = 5120;

        /// <summary>
        /// A file always contains 10 colortables.
        /// </summary>
        public const int ColorTableCount = 10;

        public readonly static int pixelSize = 10;
        public readonly static ushort width = 32;
        public readonly static ushort height = 8;

        private ColorTable[] _colorTables = new ColorTable[ColorTableCount];
        private int _actualPalette = 0;
        private ushort[,] _arrayPaletten = new ushort[ColorTableCount, ColorTable.ColorCount];
        private WriteableBitmap[] _bitmaps = new WriteableBitmap[ColorTableCount];

        /// <summary>
        /// The palette consist of 10 colortables, each consisting of 256 colors, and is used in Animation files.
        /// </summary>
        /// <param name="byteArray">The palette byte array</param>
        public Palette(byte[] byteArray)
        {
            for (int i = 0; i < ColorTableCount; i++)
            {
                byte[] colorTableByteArray = new byte[ColorTable.ByteSize];
                Buffer.BlockCopy(byteArray, i * ColorTable.ByteSize, colorTableByteArray, 0, ColorTable.ByteSize);
                _colorTables[i] = new ColorTable(colorTableByteArray);

                for (int j = 0; j < ColorTable.ColorCount; j++)
                {
                    _arrayPaletten[i, j] = BitConverter.ToUInt16(byteArray, (i * ColorTable.ColorCount + j) * 2);
                }

                _bitmaps[i] = PalleteToImG(i, pixelSize);
            }
        }

        public ColorTable[] ColorTables { get => _colorTables; set => _colorTables = value; }
        public WriteableBitmap[] Bitmaps { get => _bitmaps; set => _bitmaps = value; }
        public ushort[,] ArrayPaletten { get => _arrayPaletten; set => _arrayPaletten = value; }
        public int ActualPalette { get => _actualPalette; set => _actualPalette = value; }
        public void SetPaleteUInt(int index, ushort[] array)
        {
            for (int i = 0; i < _arrayPaletten.GetLength(1); i++)
            {
                _arrayPaletten[index, i] = array[i];
            }
        }
        public WriteableBitmap GetBitmap(int index, int pixelsize)
        {
            return PalleteToImG(index, pixelsize);
        }

        /// <summary>
        /// Calculate the new ByteArray to save ColorTables
        /// </summary>
        internal byte[] GetBytes()
        {
            List<byte> byteArray = new List<byte>();
            for (int i = 0; i < _arrayPaletten.GetLength(0); i++)
            {
                for (int j = 0; j < _arrayPaletten.GetLength(1); j++)
                {
                    byteArray.AddRange(BitConverter.GetBytes(_arrayPaletten[i, j]));
                }
            }

            return byteArray.ToArray();
        }

        /// <summary>
        /// Calculate new Palette IMG with the new Pixelsize
        /// </summary>
        /// <param name="colorTable">Selected Palette 0-9</param>
        /// <param name="scale">Scale the palette to desired size</param>
        /// <returns></returns>
        private unsafe WriteableBitmap PalleteToImG(int colorTable, int scale)
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
                    Utility.ReadColor(_arrayPaletten[colorTable, i], out byte r, out byte g, out byte b, out byte a);
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
    }
}

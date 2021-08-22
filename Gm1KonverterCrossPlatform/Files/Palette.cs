﻿using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
    public class Palette
    {
        #region Public

        /// <summary>
        /// The palette is always 5120 bytes in size.
        /// The palette consist of 10 colortables, each being 512 bytes in size
        /// and consisting of 256 2-byte colors.
        /// </summary>
        public const int ByteSize = 5120;

        public readonly static int pixelSize = 10;
        public readonly static ushort width = 32;
        public readonly static ushort height = 8;

        #endregion
        
        #region Variables

        private int actualPalette = 0;
        private ushort[,] arrayPaletten = new ushort[10, 256];
        private WriteableBitmap[] bitmaps = new WriteableBitmap[10];

        #endregion

        #region Construtor
        /// <summary>
        /// The palette consist of 10 colortables, each consisting of 256 colors, and is used in Animation files.
        /// </summary>
        /// <param name="array">The GM1 File as byte Array</param>
        public Palette(byte[] array)
        {
            // palette is located immediately after header
            byte[] arrayPaletteByte = new byte[ByteSize];
            Buffer.BlockCopy(array, GM1FileHeader.ByteSize, arrayPaletteByte, 0, ByteSize);

            for (int i = 0; i < arrayPaletten.GetLength(0); i++)
            {
                for (int j = 0; j < arrayPaletten.GetLength(1); j++)
                {
                    this.arrayPaletten[i,j] = BitConverter.ToUInt16(arrayPaletteByte, (i*256+j) * 2);
                }
                bitmaps[i] = PalleteToImG(i, pixelSize);
            }
        }

        #endregion

        #region GetterSetter
        
        public WriteableBitmap[] Bitmaps { get => bitmaps; set => bitmaps = value; }
        public ushort[,] ArrayPaletten { get => arrayPaletten; set => arrayPaletten = value; }
        public int ActualPalette { get => actualPalette; set => actualPalette = value; }
        public bool PaletteChanged { get; internal set; } = false;
        public void SetPaleteUInt(int index, ushort[] array)
        {
            for (int i = 0; i < arrayPaletten.GetLength(1); i++)
            {
                arrayPaletten[index, i] = array[i];
            }
        }
        public WriteableBitmap GetBitmap(int index, int pixelsize)
        {
            return PalleteToImG(index, pixelsize);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Calculate the new ByteArray to save ColorTables
        /// </summary>
        internal byte[] GetBytes()
        {
            List<byte> newArray = new List<byte>();
            for (int i = 0; i < arrayPaletten.GetLength(0); i++)
            {
                for (int j = 0; j < arrayPaletten.GetLength(1); j++)
                {
                    newArray.AddRange(BitConverter.GetBytes(arrayPaletten[i, j]));
                }
            }

            return newArray.ToArray();
        }

        /// <summary>
        /// Calculate new Palette IMG with the new Pixelsize
        /// </summary>
        /// <param name="palette">Selected Palette 0-9</param>
        /// <param name="pixelSize">Make the Pallete pixelssize bigger for bigger IMGS</param>
        /// <returns></returns>
        private unsafe WriteableBitmap PalleteToImG(int palette, int pixelSize)
        {
            byte r, g, b, a;
            int height = 8 * pixelSize;
            int width = 32 * pixelSize;
            UInt32 colorByte = 0;
            WriteableBitmap bitmap = new WriteableBitmap(new Avalonia.PixelSize(width, height), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);// Bgra8888 is device-native and much faster.
            using (var buf = bitmap.Lock())
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (x % pixelSize==0)
                        {
                            var pos = y/pixelSize * width/ pixelSize + x/ pixelSize;
                            Utility.ReadColor(arrayPaletten[palette, pos], out r, out g, out b, out a);
                            colorByte = (UInt32)(b | (g << 8) | (r << 16) | (a << 24));
                        }
                        var ptr = (uint*)buf.Address;
                        ptr += (uint)((width * y) + x);
                        *ptr = colorByte;
                    }
                }
            }
            return bitmap;
        }

        //todo
        private void BitmapToColorTable(int index, List<uint> colors)
        {

        }

        #endregion
    }
}

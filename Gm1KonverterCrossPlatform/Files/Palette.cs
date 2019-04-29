using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Files.Gm1Converter
{
    class Palette
    {
        public readonly static int paletteSize = 5120;
        public readonly static int pixelSize = 10;
        public readonly static ushort width = 32;
        public readonly static ushort height = 8;
        #region Variables

        private int actualPalette = 0;
        private ushort[,] arrayPaletten = new ushort[10,256];
        private byte[] arrayPaletteByte = new byte[5120];
        private WriteableBitmap[] bitmaps = new WriteableBitmap[10];
        #endregion

        #region Construtor
        public Palette(byte[] array)
        {
            Buffer.BlockCopy(array, GM1FileHeader.fileHeaderSize, arrayPaletteByte, 0, paletteSize);
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
        
        public byte[] ArrayPaletteByte { get => arrayPaletteByte; set => arrayPaletteByte = value; }
        public WriteableBitmap[] Bitmaps { get => bitmaps; set => bitmaps = value; }
        public ushort[,] ArrayPaletten { get => arrayPaletten; set => arrayPaletten = value; }
        public int ActualPalette { get => actualPalette; set => actualPalette = value; }
        public bool PaletteChanged { get; internal set; } = false;

        public void SetPaleteUInt(int index,ushort[] array)
        {
            for (int i = 0; i < arrayPaletten.GetLength(1); i++)
            {
                arrayPaletten[index, i] = array[i];
            }
        }
        #endregion

        #region Methods

        internal void CalculateNewBytes()
        {
            List<byte> newArray = new List<byte>();
            for (int i = 0; i < arrayPaletten.GetLength(0); i++)
            {
                for (int j = 0; j < arrayPaletten.GetLength(1); j++)
                {
                    newArray.AddRange(BitConverter.GetBytes(arrayPaletten[i, j]));
                }
            }

            arrayPaletteByte = newArray.ToArray();
        }

        public WriteableBitmap GetBitmap(int index,int pixelsize)
        {
            return PalleteToImG(index,pixelsize);
        }

        private unsafe WriteableBitmap PalleteToImG(int palette,int pixelSize)
        {
                byte r, g, b, a;
                int height = 8 * pixelSize;
                int width = 32 * pixelSize;
                UInt32 colorByte=0;
                WriteableBitmap bitmap = new WriteableBitmap(new Avalonia.PixelSize(width, height), new Avalonia.Vector(100, 100), Avalonia.Platform.PixelFormat.Bgra8888);// Bgra8888 is device-native and much faster.
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

        private void BitmapToColorTable(int index,List<uint> colors)
        {

        }


        #endregion

    }
}

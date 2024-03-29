﻿using System;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.HelperClasses;

namespace Gm1KonverterCrossPlatform.Files
{
    class TilesImage : IDisposable
    {
        public static int Puffer = 500;

        private int width, height;
        private WriteableBitmap bmp;
        private int minusHeight = 9999999;
        private uint[] colors;

        public TilesImage(int width, int height)
        {
            this.width = width;
            this.height = height;
            colors = new uint[width * height];
        }

        public WriteableBitmap TileImage { get => bmp; set => bmp = value; }
        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }
        public int MinusHeight { get => minusHeight; set => minusHeight = value; }

        public void Dispose()
        {
            if (bmp != null)
            {
                bmp.Dispose();
            }
        }

        /// <summary>
        /// Add the Diamond Tile Img to the bigger Img
        /// </summary>
        /// <param name="imgFileAsByteArray">The Diamond byte array</param>
        /// <param name="xOffset">The xOffset in the bigger IMG</param>
        /// <param name="yOffset">The yOffset in the bigger IMG</param>
        internal void AddDiamondToImg(byte[] imgFileAsByteArray, int xOffset, int yOffset)
        {
            int[] array = {
                2, 6, 10, 14, 18, 22, 26, 30,
                30, 26, 22, 18, 14, 10, 6, 2
            };

            int bytePos = 0;

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < array[y]; x++)
                {
                    int pos = ((width * (y + yOffset)) + x + xOffset + 15 - array[y] / 2);
                    colors[pos] = Converters.ColorConverter.Argb1555ToBgra8888(BitConverter.ToUInt16(imgFileAsByteArray, bytePos));
                    bytePos += 2;
                }
            }
        }

        /// <summary>
        /// Add normal IMG to the bigger IMG
        /// </summary>
        /// <param name="imgFileAsBytearray"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        internal void AddImgTileOnTopToImg(byte[] imgFileAsBytearray, int offsetX, int offsetY)
        {
            uint x = 0;
            uint y = 0;

            ushort pixelColor;
            uint colorByte;

            for (int bytePos = 512; bytePos < imgFileAsBytearray.Length;)
            {
                byte token = imgFileAsBytearray[bytePos];
                byte tokentype = (byte)(token >> 5);
                byte length = (byte)((token & 31) + 1);

                bytePos++;

                switch (tokentype)
                {
                    case 0: //Stream-of-pixels 

                        for (byte i = 0; i < length; i++)
                        {
                            pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, bytePos);
                            bytePos += 2;

                            colorByte = Converters.ColorConverter.Argb1555ToBgra8888(pixelColor);

                            colors[(uint)((width * (y + offsetY)) + x + offsetX)] = colorByte;

                            x++;
                        }
                        break;

                    case 4: //Newline

                        y++;
                        x = 0;
                        break;

                    case 2: //Repeating pixels

                        pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, bytePos);
                        bytePos += 2;

                        colorByte = Converters.ColorConverter.Argb1555ToBgra8888(pixelColor);

                        for (byte i = 0; i < length; i++)
                        {
                            colors[(uint)((width * (y + offsetY)) + x + offsetX)] = colorByte;
                            x++;
                        }
                        break;

                    case 1: //Transparent-Pixel-String

                        x += length;

                        break;

                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Creates the new big IMG as Bitmap
        /// </summary>
        internal unsafe void CreateImagefromList()
        {
            Dispose();

            if (minusHeight == 9999999)
            {
                //is used for the correct height of the bitmap
                minusHeight = Puffer;
            }
            height = height - minusHeight;

            bmp = new WriteableBitmap(
                new Avalonia.PixelSize(width, height),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888, // Bgra8888 is device-native and much faster.
                Avalonia.Platform.AlphaFormat.Premul);

            using (var buffer = bmp.Lock())
            {
                uint* pointer = (uint*)buffer.Address;

                uint pos = 0;

                for (int i = width * minusHeight; i < colors.Length; i++)
                {
                    pointer[pos] = colors[i];
                    pos++;
                }
            }
        }
    }
}

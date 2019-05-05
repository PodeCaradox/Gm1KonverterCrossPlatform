using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gm1KonverterCrossPlatform.Files
{
    class TilesImage
    {

        #region Public

        public static int Puffer = 500;

        #endregion

        #region Variables

        private static int[] array = {
                2, 6, 10, 14, 18, 22, 26, 30,
                30, 26, 22, 18, 14, 10, 6, 2
            };
        private int width, height;
        private WriteableBitmap bmp;
        private int minusHeight = 9999999;
        private UInt32[] colors;

        #endregion
        
        #region Construtor

        public TilesImage(int width, int height)
        {
            this.width = width;
            this.height = height;
            colors = new UInt32[width * height];

        }

        #endregion

        #region GetterSetter

        public WriteableBitmap TileImage { get => bmp; set => bmp = value; }
        public int Width { get => width; set => width = value; }
        public int Height { get => height; set => height = value; }
        public int MinusHeight { get => minusHeight; set => minusHeight = value; }

        #endregion

        #region Methods

        /// <summary>
        /// Add the Diamond Tile Img to the bigger Img
        /// </summary>
        /// <param name="imgFileAsBytearray">The Diamond byte array</param>
        /// <param name="xOffset">The xOffset in the bigger IMG</param>
        /// <param name="yOffset">The yOffset in the bigger IMG</param>
        internal void AddDiamondToImg(byte[] imgFileAsBytearray, int xOffset, int yOffset)
        {
            int x = 0;
            int y = 0;
            int bytePos = 0;
            byte r, g, b, a;

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < array[i]; j++)
                {
                    UInt16 pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, (int)bytePos);
                    bytePos += 2;
                    Utility.ReadColor(pixelColor, out r, out g, out b, out a);
                    a = byte.MaxValue;
                    colors[(uint)((width * (y + yOffset)) + x + xOffset + 15 - array[i] / 2)] = (UInt32)(b | (g << 8) | (r << 16) | (a << 24));
                    x++;
                }
                x = 0;
                y++;
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
            byte r, g, b, a;


            for (uint bytePos = 512; bytePos < imgFileAsBytearray.Length;)
            {


                byte token = imgFileAsBytearray[bytePos];
                byte tokentype = (byte)(token >> 5);
                byte length = (byte)((token & 31) + 1);

                //transparent
                UInt32 colorByte = Utility.TransparentColorByte;

                bytePos++;
                byte index;

                ushort pixelColor;
                switch (tokentype)
                {
                    case 0://Stream-of-pixels 

                        for (byte i = 0; i < length; i++)
                        {

                            pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, (int)bytePos);
                            bytePos += 2;


                            Utility.ReadColor(pixelColor, out r, out g, out b, out a);
                            a = byte.MaxValue;

                            colors[(uint)((width * (y + offsetY)) + x + offsetX)] = (UInt32)(b | (g << 8) | (r << 16) | (a << 24));

                            x++;

                        }
                        break;
                    case 4://Newline

                        y++;
                        if (y > this.height) break;
                        x = 0;
                        break;
                    case 2://Repeating pixels 

                        pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, (int)bytePos);
                        bytePos += 2;


                        Utility.ReadColor(pixelColor, out r, out g, out b, out a);

                        a = byte.MaxValue;
                        colorByte = (uint)(b | (g << 8) | (r << 16) | (a << 24));

                        for (byte i = 0; i < length; i++)
                        {
                            colors[(uint)((width * (y + offsetY)) + x + offsetX)] = colorByte;
                            x++;
                        }
                        break;
                    case 1://Transparent-Pixel-String 
                        colorByte = Utility.TransparentColorByte;
                        for (byte i = 0; i < length; i++)
                        {
                            colors[(uint)((width * (y + offsetY)) + x + offsetX)] = colorByte;
                            x++;
                        }
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
            if (minusHeight == 9999999)
            {
                //is used for the correct height of the bitmap
                minusHeight = Puffer;
            }
            bmp = new WriteableBitmap(new Avalonia.PixelSize(width, height - minusHeight), new Avalonia.Vector(100, 100), Avalonia.Platform.PixelFormat.Bgra8888);// Bgra8888 is device-native and much faster.

            using (var buf = bmp.Lock())
            {
                uint zaehler = 0;
                for (int i = width * minusHeight; i < colors.Length; i++)
                {
                    var ptr = (uint*)buf.Address;
                    ptr += (uint)(zaehler);
                    *ptr = colors[i];
                    zaehler++;
                }
            }
        }

        #endregion

    }
}

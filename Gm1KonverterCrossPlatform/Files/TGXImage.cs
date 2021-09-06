using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;

namespace Files.Gm1Converter
{
    public class TGXImage
    {
        #region Variables

        // for tgx as standalone file
        private uint tgxwidth;
        private uint tgxheight;

        // for tgx as subimage in gm1 file
        private TGXImageHeader header;

        private uint offsetinByteArray;
        private uint sizeinByteArray;
        private byte[] imgFileAsBytearray;

        private WriteableBitmap bmp;

        #endregion

        #region Construtor

        public TGXImage()
        {

        }

        #endregion

        #region GetterSetter

        public UInt32 TgxWidth { get => tgxwidth; set => tgxwidth = value; }
        public UInt32 TgxHeight { get => tgxheight; set => tgxheight = value; }

        public TGXImageHeader Header { get => header; set => header = value; }

        public UInt32 OffsetinByteArray { get => offsetinByteArray; set => offsetinByteArray = value; }
        public UInt32 SizeinByteArray { get => sizeinByteArray; set => sizeinByteArray = value; }
        public byte[] ImgFileAsBytearray { get => imgFileAsBytearray; set => imgFileAsBytearray = value; }
        
        public WriteableBitmap Bitmap { get => bmp; set => bmp = value; }
        
        #endregion

        #region Methods

        /// <summary>
        /// Convert img byte array to IMG, use Pallete if not null
        /// </summary>
        /// <param name="palette">actual Pallete, Pallete is null if normal IMG</param>
        public unsafe void CreateImageFromByteArray(Palette palette, bool isTgxFile = false)
        {
            int width, height;
            if (!isTgxFile)
            {
                width = this.Header.Width;
                height = this.Header.Height;
            }
            else
            {
                width = (int)tgxwidth;
                height = (int)tgxheight;
            }

            bmp = new WriteableBitmap(new Avalonia.PixelSize(width, height), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);// Bgra8888 is device-native and much faster.

            using (var buf = bmp.Lock())
            {
                uint x = 0;
                uint y = 0;
                byte r, g, b, a;

                for (uint bytePos = 0; bytePos < imgFileAsBytearray.Length;)
                {
                    byte token = imgFileAsBytearray[bytePos];
                    byte tokentype = (byte)(token >> 5);
                    byte length = (byte)((token & 31) + 1);

                    //transparent
                    uint colorByte = Utility.TransparentColorByte;

                    bytePos++;
                    byte index;
                  
                    ushort pixelColor;
                    switch (tokentype)
                    {
                        case 0: //Stream-of-pixels 

                            for (byte i = 0; i < length; i++)
                            {
                               
                                if (palette != null)
                                {
                                    index = imgFileAsBytearray[bytePos];
                                    pixelColor = palette.ArrayPaletten[palette.ActualPalette, index];
                                    bytePos++;
                                }
                                else
                                {
                                    pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, (int)bytePos);
                                    bytePos += 2;
                                }
                           
                                Utility.ReadColor(pixelColor, out r, out g, out b, out a);
                                if (palette == null)
                                {
                                    a = 255;
                                }
                     
                                colorByte = (UInt32)(b | (g << 8) | (r << 16) | (a << 24));
                                var ptr = (uint*)buf.Address;
                                ptr += (uint)((width * y) + x);
                                *ptr = colorByte;
                               
                                x++;
                            }
                            break;
                        case 4: //Newline
                            colorByte = Utility.TransparentColorByte;
                            if (palette != null)
                            {
                                for (byte i = 0; i < width; i++)
                                {
                                    if (x >= width || y >= height)
                                        break;

                                    //Bgra8888

                                    var ptr = (uint*)buf.Address;
                                    ptr += (uint)((width * y) + x);
                                    *ptr = colorByte;
                                 
                                    x++;
                                }
                            }

                            y++;
                            if (y > height) break;
                            x = 0;
                            break;
                        case 2: //Repeating pixels 

                            if (palette != null)
                            {
                                index = imgFileAsBytearray[bytePos];
                                pixelColor = palette.ArrayPaletten[palette.ActualPalette, index];
                                bytePos++;
                            }
                            else
                            {
                                pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, (int)bytePos);
                                bytePos += 2;
                            }

                            Utility.ReadColor(pixelColor, out r, out g, out b, out a);
                            if (palette == null)
                            {
                                a = 255;
                            }
                            colorByte = (uint)(b | (g << 8) | (r << 16) | (a << 24));
                            for (byte i = 0; i < length; i++)
                            {
                                var ptr = (uint*)buf.Address;
                                ptr += (uint)((width * y) + x);
                                *ptr = colorByte;
                                
                                x++;
                            }
                            break;
                        case 1: //Transparent-Pixel-String 
                            colorByte = Utility.TransparentColorByte;
                            for (byte i = 0; i < length; i++)
                            {
                                var ptr = (uint*)buf.Address;
                                ptr += (uint)((width * y) + x);
                                *ptr = colorByte;
                                
                                x++;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        internal void ConvertImageWithPaletteToByteArray(List<ushort> colors, int width, int height, Palette palette, List<ushort>[] colorsImages = null)
        {
            var array = Utility.ImgToGM1ByteArray(colors, width, height, Header.AnimatedColor, palette, colorsImages);

            imgFileAsBytearray = array.ToArray();
        }

        internal unsafe void CreateNoComppressionImageFromByteArray(Palette palette,int offset)
        {
            //-7 because the images only height -7 long idk why
            bmp = new WriteableBitmap(
                new Avalonia.PixelSize(Header.Width, Header.Height - offset),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888 // Bgra8888 is device-native and much faster.
            );

            using (var buf = bmp.Lock())
            {
                byte r, g, b, a;
                uint x = 0;
                uint y = 0;
                for (int bytePos = 0; bytePos < imgFileAsBytearray.Length; bytePos+=2)
                {
                    Utility.ReadColor(BitConverter.ToUInt16(imgFileAsBytearray, bytePos), out r, out g, out b, out a);
                    var colorByte = (UInt32)(b | (g << 8) | (r << 16) | (a << 24));
                    var ptr = (uint*)buf.Address;
                    ptr += (uint)((Header.Width * y) + x);
                    *ptr = colorByte;
                    x++;
                    if (x == Header.Width)
                    {
                        y++;
                        x = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Convert imported Imgs without a Pallete to Byte array to safe new GM1 File
        /// </summary>
        /// <param name="colors">Color List of the new IMG</param>
        /// <param name="width">Width of the new IMG</param>
        /// <param name="height">Height of the new IMG</param>
        internal void ConvertImageWithoutPaletteToByteArray(List<ushort> colors, int width, int height)
        {
            var array = Utility.ImgToGM1ByteArray(colors, width, height, Header.AnimatedColor);
            //for (int i = 0; i < imgFileAsBytearray.Length; i++)
            //{
            //    if (imgFileAsBytearray[i]!= array[i])
            //    {

            //    }
            //}

            imgFileAsBytearray = array.ToArray();
        }

        internal void ConvertNoCommpressionImageToByteArray(List<ushort> list, int width, int height)
        {
            List<byte> newArray = new List<byte>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    newArray.AddRange(BitConverter.GetBytes(list[y * width + x]));
                }
            }
            imgFileAsBytearray = newArray.ToArray();
        }

        #endregion
    }
}

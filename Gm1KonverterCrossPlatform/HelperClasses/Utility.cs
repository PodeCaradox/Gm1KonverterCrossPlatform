using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using Color = System.Drawing.Color;
using Avalonia.Media.Imaging;
using Files.Gm1Converter;

namespace HelperClasses.Gm1Converter
{
    /// <summary>
    /// Reusable functions for many uses.
    /// </summary>
    public static class Utility
    {
        private static Color color = Color.Transparent;
        public static UInt32 TransparentColorByte = (UInt32)(color.B | (color.G << 8) | (color.R << 16) | (color.A << 24));

        #region Methods

        public static byte[] FileToByteArray(string fileName)
        {
            byte[] buff = null;
            FileStream fs = new FileStream(fileName,
                                           FileMode.Open,
                                           FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            long numBytes = new FileInfo(fileName).Length;
            buff = br.ReadBytes((int)numBytes);
            return buff;
        }

        public static void ByteArraytoFile(string fileName,byte[] array)
        {
            File.WriteAllBytes(fileName, array);
        }

        public unsafe static List<uint> ImgToColors(WriteableBitmap bmp,ushort width, ushort height)
        {
            List<uint> colors = new List<uint>();
            using (var buf = bmp.Lock())
            {
                byte r, g, b;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var ptr = (uint*)buf.Address;
                        ptr += (uint)((width * y) + x);
                        colors.Add(*ptr);
                    }
                }
            }

      
            return colors;
        }

        
      //todo Fehler falls 2 gleichfarbige und dan das letzte byte, siehe letztes bild
        /// <summary>
        /// Encoding back its not same as stronghold it do but it works so it is fine
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="oldarray"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        internal static byte[] ImgToGM1ByteArray(List<uint> colors, ushort width, ushort height,byte[] oldarray,Palette palette)
        {
            List<byte> array = new List<byte>();
            uint countSamePixel = 0;


            //3 bytes
            byte token = 0;
            // value 1-32
            byte length = 0;

            byte header = 0;
            bool transparentPixelString = false;
            bool repeatingPixels  = false;
            bool streamofpixels = false;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    transparentPixelString = false;
                    repeatingPixels = false;
                    streamofpixels = false;
                    int repeatedPixel = 0;
                     countSamePixel = 0;
                    var offset = i * width;
                    for (int z = j; z < width-1; z++)
                    {
                        if (offset + j >= 5200)
                        {

                        }
                        if (colors[z + offset] == TransparentColorByte)//newline or Transparent-Pixel-String
                        {
                            if (repeatingPixels|| streamofpixels)
                            {
                                break;
                            }

                            countSamePixel++;
                            transparentPixelString = true;
                        }else if (transparentPixelString)
                        {
                      
                            break;
                        }
                        else if (colors[z + offset] == colors[z + 1 + offset])//Repeating pixels 
                        {
                            if (streamofpixels && repeatedPixel >= 2)
                            {
                                countSamePixel-=2;
                                break;
                            }
                            repeatedPixel++;
                            countSamePixel++;
                            repeatingPixels = true;
                           
                        }else if (repeatingPixels)//only if more than 2 colors repeating to safe one byte
                        {
                            countSamePixel++;
                            if (repeatedPixel >= 2)
                            {
                               
                                break;
                            }
                            else
                            {
                                streamofpixels = true;
                                repeatingPixels = false;
                            }
                            
                        }
                        else if (colors[z + offset] != colors[z + 1 + offset])//Stream-of-pixels 
                        {
                            if (repeatingPixels || colors[z + 1 + offset] == TransparentColorByte)
                            {
                                if (colors[z + 1 + offset] == TransparentColorByte)
                                {
                                    countSamePixel++;
                                }
                                break;
                            }
                            countSamePixel++;
                            streamofpixels = true;
                        }
                        else if(streamofpixels)
                        {
                            break;
                        }


                    }
           

                    if (j + countSamePixel + 1 == width)//newline //+1 because width - 1 in loop
                    {
                        array.Add(0b1000_0000);
                        countSamePixel++;
                    }
                    else if(transparentPixelString)//Transparent-Pixel-String
                    {
                         header = 0b0010_0000;
                        var dummy = countSamePixel;
                        while (dummy / 32 > 0)
                        {
                            length = 0b0001_1111;
                            array.Add((byte)(header| length));
                            dummy -= 32;
                        }
                        if (dummy != 0)
                        {
                            length = (byte)(dummy - 1);//-1 because the test is pixel perfect in the loop and 0 == 1 in the encoding
                            array.Add((byte)(header | length));
                        }
                    }
                    else if (streamofpixels)
                    {
                        header = 0b0000_0000;
                        var dummy = countSamePixel;
                        int zaehler = 0;
                        while (dummy / 32 > 0)
                        {
                            length = 0b0001_1111;
                            array.Add((byte)(header | length));
                            for (int a = 0; a < 32; a++)
                            {
                                var color = colors[j + zaehler + offset];
                                var colorToFind = EncodeColorTo2Byte(color);
                                array.Add(FindColorInPalette(palette, colorToFind));
                                dummy--;
                                zaehler++;
                            }
                        }
                        if (dummy != 0)
                        {
                            length = (byte)(dummy - 1);//-1 because the test is pixel perfect in the loop and 0 == 1 in the encoding
                            array.Add((byte)(header | length));
                            for (int a = 0; a < dummy; a++)
                            {
                                var color = colors[j + zaehler + offset];
                                var colorToFind = EncodeColorTo2Byte(color);
                                array.Add(FindColorInPalette(palette, colorToFind));
                                zaehler++;
                            }
                        }



                    }
                    else if (repeatingPixels)
                    {
                        header = 0b0100_0000;
                        length = (byte)(countSamePixel - 1);//-1 because the test is pixel perfect in the loop and 0 == 1 in the encoding
                        array.Add((byte)(header | length));
                        var colorToFind = EncodeColorTo2Byte(colors[j + offset]);
                        array.Add(FindColorInPalette(palette, colorToFind));
                    }

                    j += (int)countSamePixel - 1;//-1 besauce loop +1

                }

            }

            test();

            return array.ToArray();
        }

        private static byte FindColorInPalette(Palette palette,UInt16 colorToFind)
        {
            byte offsetPalette = 0;
            for (byte colorPalette = 0; colorPalette < 255; colorPalette++)
            {
                if (palette.ArrayPalette[colorPalette] == colorToFind)
                {
                    offsetPalette = colorPalette;
                }
            }
            return offsetPalette;
        }

        private static void test()
        {
            //blue
            //0b0000_0000_0001_1111
            //green
            //0b0000_0011_1110_0000
            //red
            //0b0111_1100_0000_0000

            byte r, g, b;
            ReadColor(38087, out  r, out  g, out  b);
            var colorByte = (UInt32)(b | (g << 8) | (r << 16) | (255 << 24));

            var colorBytevergleich = EncodeColorTo2Byte(colorByte);


        }

        private static UInt16 EncodeColorTo2Byte(uint colorAsInt32)
        {
            byte[] arrray = { (byte)(204), (byte)196 };
            var testtt=BitConverter.ToUInt16(arrray, 0);
            var colors = BitConverter.GetBytes(colorAsInt32);

            UInt16 b = (UInt16)((colors[0] >> 3) & 0b0001_1111);
            UInt16 g = (UInt16)(((colors[1] >> 3) & 0b0001_1111) << 5);
            UInt16 r = (UInt16)(((colors[2] >> 3) & 0b0001_1111) << 10);
            UInt16 a = (UInt16)((colors[3] & 0b1000_0000)<<8);
            UInt16 color=(UInt16)(b | g | r | a);
            return color;
        }

        public static void ReadColor(UInt16 pixel, out byte r, out byte g, out byte b)
        {
            //blue
            //0b0000_0000_0001_1111
            //green
            //0b0000_0011_1110_0000
            //red
            //0b0111_1100_0000_0000
            //alpha
            //0b1000_0000_0000_0000

            byte a = (byte)((pixel >> 15) & 0b1);
            r = (byte)(((pixel >> 10) & 0b11111) << 3);
            g = (byte)(((pixel >> 5) & 0b11111) << 3);
            b = (byte)((pixel & 0b11111) << 3);
            var colorByte = (UInt32)(b | (g << 8) | (r << 16) | (255 << 24));
            //r = (byte)(r | (r >> 5));
            //g = (byte)(g | (g >> 5));
            //b = (byte)(b | (b >> 5));
        }

        #endregion


    }
}

using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using Color = System.Drawing.Color;
using Avalonia.Media.Imaging;
using Files.Gm1Converter;
using Image = SixLabors.ImageSharp.Image;
using Gm1KonverterCrossPlatform.HelperClasses.Views;
using SixLabors.ImageSharp.Formats;

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

        public unsafe static List<uint> ImgToColors(WriteableBitmap bmp,ushort width, ushort height, int pixelsize=1)
        {
            List<uint> colors = new List<uint>();
            using (var buf = bmp.Lock())
            {
                for (int y = 0; y < height; y += pixelsize)
                {
                    for (int x = 0; x < width; x += pixelsize)
                    {
                        var ptr = (uint*)buf.Address;
                        ptr += (uint)((width * y) + x);
                        colors.Add(*ptr);
                    }
                }
            }

      
            return colors;
        }

        public static List<UInt16> LoadImage(String filename, out int width, out int height,int animatedColor = 1, int pixelsize = 1)
        {
            width = 0;
            height = 0;
            List<UInt16> colors = new List<UInt16>();
            try
            {
                var image = Image.Load(filename);
                width = image.Width;
                height = image.Height;
                for (int i = 0; i < image.Height; i += pixelsize)
                {
                    for (int j = 0; j < image.Width; j += pixelsize) //Bgra8888
                    {
                        var pixel = image[j, i];
                        byte a = (animatedColor >= 1) ?  byte.MaxValue : byte.MinValue;
                        if (pixel.A == 0 && pixel.B==0 && pixel.G==0 && pixel.R==0)
                        {
                            colors.Add((animatedColor >= 1) ? (ushort)65535 : (ushort)32767);
                        }
                        else
                        {
                            colors.Add(EncodeColorTo2Byte((uint)(pixel.B | pixel.G << 8 | pixel.R << 16 | a << 24)));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, e.Message);
                messageBox.Show();
            }
            return colors;
        }


        internal static List<byte> ImgWithoutPaletteToGM1ByteArray(List<ushort> colors, int width, int height, byte[] imgFileAsBytearray,int animatedColor,int img)
        {
   

            int transparent = (animatedColor>=1) ? 65535 : 32767;
            List<byte> array = new List<byte>();
            byte length = 0;  // value 1-32  | 0 will be 1
            byte header = 0;   //3 bytes
            int countSamePixel = 0;
            bool newline = false;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width;)
                {
                    countSamePixel = 0;
                    //check for newline
                    for (int z = j; z < width; z++)
                    {
                        if(colors[i * width + z] == transparent)
                        {
                            newline = true;
                            countSamePixel++;
                        }
                        else
                        {
                            newline = false;
                            break;
                        }
                    }
                    if (newline == true && countSamePixel == width)
                    {
                        for (int z = 0; z < width; z++)//ist wahrscheinlich ein Fehler von Stronghold?
                        {
                            array.Add(0b0010_0000);
                        }
                        array.Add(0b1000_0000);
                        j = width;
                    }
                    else
                    {
                        header = 0b0010_0000;
                        var dummy = countSamePixel;
                        while (dummy / 32 > 0)
                        {
                            length = 0b0001_1111;
                            array.Add((byte)(header | length));
                            dummy -= 32;
                        }
                        if (dummy != 0)
                        {
                            length = (byte)(dummy - 1);//-1 because the test is pixel perfect in the loop and 0 == 1 in the encoding
                            array.Add((byte)(header | length));
                        }
                        j += countSamePixel;
                        countSamePixel = 0;
                        //Stream-of-pixels 
                        for (int z = j; z < width; z++)
                        {
                            if (colors[i * width + z] != transparent)  countSamePixel++;
                            else break;
                            
                        }

                        header = 0b0000_0000;
                        dummy = countSamePixel;
                        int zaehler = 0;
                        while (dummy / 32 > 0)
                        {
                            length = 0b0001_1111;
                            array.Add((byte)(header | length));
                            for (int a = 0; a < 32; a++)
                            {
                                var color = colors[j + zaehler + i * width];
                                array.AddRange(BitConverter.GetBytes(color));
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
                                var color = colors[j + zaehler + i * width];
                                array.AddRange(BitConverter.GetBytes(color));
                                zaehler++;
                            }
                        }

                        j += countSamePixel;
                        if (j == width)
                        {
                            array.Add(0b1000_0000);
                        }
                    }


                }
            }


            return array;
        }
        
        public static UInt16 EncodeColorTo2Byte(uint colorAsInt32)
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

        public static void ReadColor(UInt16 pixel, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((((pixel >> 15) & 0b0000_0001)==1)?255:0);
            r = (byte)(((pixel >> 10) & 0b11111) << 3);
            g = (byte)(((pixel >> 5) & 0b11111) << 3);
            b = (byte)((pixel & 0b11111) << 3);
            //r = (byte)(r | (r >> 5));
            //g = (byte)(g | (g >> 5));
            //b = (byte)(b | (b >> 5));
        }

        public static string[] GetFileNames(string path, string filter)
        {
            string[] files = Directory.GetFiles(path, filter);
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileName(files[i]);
            return files;
        }

        public static unsafe void CreateImageFromByteArray(WriteableBitmap bmp)
        {

        }

        public static int GetDiamondWidth(int anzParts)
        {
            int width = 0;
            int actualParts = 0;
            int corner = 1;
            while (true)
            {

                if (anzParts - actualParts - corner == 0)
                {
                    width = corner - corner/2;
                    break;
                }
                else if (anzParts - actualParts - corner < 0)
                {
                    //error
                    break;
                }
                actualParts += corner;
                corner += 2;

            }

            return width;
        }


        #endregion


    }
}

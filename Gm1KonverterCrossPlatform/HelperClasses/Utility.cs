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
using Avalonia;
using System.Linq;
using Avalonia.Controls;
using Gm1KonverterCrossPlatform.HelperClasses;

namespace HelperClasses.Gm1Converter
{
    /// <summary>
    /// Reusable functions for many uses.
    /// </summary>
    internal static class Utility
    {
        #region Public

        private static readonly Color color = Color.Transparent;
        internal static readonly UInt32 TransparentColorByte = (UInt32)(color.B | (color.G << 8) | (color.R << 16) | (color.A << 24));

        #endregion

        #region Methods

        /// <summary>
        /// Load the File as Bytearray
        /// </summary>
        /// <param name="fileName">The Filepath/Filenamee to load</param>
        /// <returns></returns>
        internal static byte[] FileToByteArray(string fileName)
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

        /// <summary>
        /// Save the bytearray as File
        /// </summary>
        /// <param name="fileName">The Filepath/Filenamee to save</param>
        /// <param name="array">The byte array to save</param>
        internal static void ByteArraytoFile(string fileName, byte[] array)
        {
            File.WriteAllBytes(fileName, array);
        }


        /// <summary>
        /// Load the IMG as Color 2byte list
        /// </summary>
        /// <param name="filename">The Filepath/Filenamee to load</param>
        /// <param name="width">The width from the IMG</param>
        /// <param name="height">The Height from the IMG</param>
        /// <param name="animatedColor">Needed if alpha is 0 or 1</param>
        /// <param name="pixelsize">Pixelsize of a pixel needed for Colortable</param>
        /// <returns></returns>
        internal static List<UInt16> LoadImage(String filename, out int width, out int height, int animatedColor = 1, int pixelsize = 1, uint type = 0)
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
                        byte a = (animatedColor >= 1 || ((GM1FileHeader.DataType)type) == GM1FileHeader.DataType.TilesObject) ? byte.MaxValue : byte.MinValue;
                        if (pixel.A == 0 && pixel.B == 0 && pixel.G == 0 && pixel.R == 0)
                        {
                            colors.Add((animatedColor >= 1 || ((GM1FileHeader.DataType)type) == GM1FileHeader.DataType.TilesObject) ? (ushort)65535 : (ushort)32767);
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


        /// <summary>
        /// Convert an IMG as Colorlist to ByteArray
        /// </summary>
        /// <param name="colors">The IMG as Color List</param>
        /// <param name="width">The width from the IMG</param>
        /// <param name="height">The Height from the IMG</param>
        /// <param name="animatedColor">Needed for Alpha is 1 or 0</param>
        /// <returns></returns>
        internal static List<byte> ImgWithoutPaletteToGM1ByteArray(List<ushort> colors, int width, int height, int animatedColor)
        {


            int transparent = (animatedColor >= 1) ? 65535 : 32767;
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
                        if (colors[i * width + z] == transparent)
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
                            if (colors[i * width + z] != transparent) countSamePixel++;
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

        private static int[] array = {
                2, 6, 10, 14, 18, 22, 26, 30,
                30, 26, 22, 18, 14, 10, 6, 2
            };
        static int before = 0;
        internal static List<TGXImage> ConvertImgToTiles(List<ushort> list, ushort width, ushort height, List<TGXImage> oldList)
        {
            List<TGXImage> newImageList = new List<TGXImage>();
            if (width==158&&height==359)
            {

            }
            //calculate Parts one part 16 x 30
            int partwidth = width / 30;//todo not exactly 30 width because 2 pixels between
            int totalTiles = partwidth;
            int dummy = 0;
            for (int i = 0; 0 != totalTiles; i++)
            {
                totalTiles--;
                dummy += totalTiles;
            }
            totalTiles = dummy * 2 + partwidth;
            int savedOffsetX = width / 2;
            int xOffset = savedOffsetX;
            int yOffset = height - 16;
            int partsPerLine = 1;
            int counter = 0;
            List<Byte> arrayByte;
            bool halfreached = false;
            for (int part = 0; part < totalTiles; part++)
            {

                counter++;
                int x = 0;
                int y = 0;
                arrayByte = new List<byte>();
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < array[i]; j++)
                    {
                        int number = ((width * (y + yOffset)) + x + xOffset - array[i] / 2);
                        var color = list[number];
                        //set to alpha
                        list[number] = 65535;
                        arrayByte.AddRange(BitConverter.GetBytes(color));
                        x++;
                    }
                    y++;
                    x = 0;
                }


                var newImage = new TGXImage();
                newImage.Direction = 0;
                newImage.Height = 16;
                newImage.Width = 30;
                newImage.SubParts = (byte)totalTiles;
                newImage.ImagePart = (byte)part;
                if (halfreached)
                {

                    //tileoffset=1st pixel from tile and than height
                    if (counter == 1)//left
                    {
                        if (part == totalTiles - 1)
                        {
                            newImage.BuildingWidth = 30;
                            newImage.Direction = 1;
                            int imageOnTopwidth = 30;
                            int imageOnTopheight = yOffset + 7;
                            int imageOnTopOffsetX = xOffset - 15;
                            List<ushort> colorListImgOnTop = GetColorList(list, imageOnTopwidth, imageOnTopheight, imageOnTopOffsetX, width);
                            var byteArrayImgonTop = ImgWithoutPaletteToGM1ByteArray(colorListImgOnTop, imageOnTopwidth, colorListImgOnTop.Count / imageOnTopwidth, 1);
                           
                            arrayByte.AddRange(byteArrayImgonTop);
                            newImage.TileOffset = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10 - 16 - 1);
                            newImage.Height = (ushort)imageOnTopheight;
                        }
                        else
                        {
                            newImage.BuildingWidth = 16;
                            newImage.Direction = 2;
                            int imageOnTopwidth = 16;
                            int imageOnTopheight = yOffset + 7;
                            int imageOnTopOffsetX = xOffset - 15;
                            List<ushort> colorListImgOnTop = GetColorList(list, imageOnTopwidth, imageOnTopheight, imageOnTopOffsetX, width);
                            var byteArrayImgonTop = ImgWithoutPaletteToGM1ByteArray(colorListImgOnTop, imageOnTopwidth, colorListImgOnTop.Count / imageOnTopwidth, 1);
                           
                            arrayByte.AddRange(byteArrayImgonTop);
                            newImage.TileOffset = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10 - 16 - 1);
                            newImage.Height = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10);
                        }
                    }
                    else if (counter == partsPerLine)//right
                    {

                        newImage.BuildingWidth = 16;
                        newImage.Direction = 3;
                        int imageOnTopwidth = 16;
                        int imageOnTopheight = yOffset + 7;
                        int imageOnTopOffsetX = xOffset;
                        List<ushort> colorListImgOnTop = GetColorList(list, imageOnTopwidth, imageOnTopheight, imageOnTopOffsetX - 1, width);
                        var byteArrayImgonTop = ImgWithoutPaletteToGM1ByteArray(colorListImgOnTop, imageOnTopwidth, colorListImgOnTop.Count / imageOnTopwidth, 1);
                       
                        arrayByte.AddRange(byteArrayImgonTop);
                        
                        newImage.Height = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10);
                        newImage.TileOffset = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10 - 16 - 1);
                        newImage.HorizontalOffsetOfImage = 14;


                    }
                }
                newImageList.Add(newImage);
                newImage.ImgFileAsBytearray = arrayByte.ToArray();
                xOffset += 32;

                if (counter == partsPerLine)
                {
                    yOffset -= 8;
                    counter = 0;
                    xOffset = savedOffsetX;
                    if (partsPerLine == partwidth - 1 && !halfreached)
                    {
                        halfreached = true;
                        partsPerLine += 2;
                        xOffset = -1;
                    }

                    if (!halfreached)
                    {
                        partsPerLine++;
                        xOffset -= 16;
                    }
                    else
                    {
                        xOffset += 16;
                        partsPerLine--;
                    }

                    savedOffsetX = xOffset;

                }



            }
            before += totalTiles;


            return newImageList;
        }

        private static List<ushort> GetColorList(List<ushort> list, int width, int height, int OffsetX, int orgWidth)
        {
            List<ushort> colorList = new List<ushort>();
            bool allTransparent = true;
            for (int y = 0; y < height; y++)
            {
                List<ushort> dummy = new List<ushort>();
                for (int x = OffsetX; x < OffsetX + width; x++)
                {

                    if (list[orgWidth * y + x] != 65535)
                    {

                        dummy.Add(list[orgWidth * y + x]);
                        allTransparent = false;
                    }
                    else
                    {
                        dummy.Add(list[0]);
                    }
                }
                if (!allTransparent) colorList.AddRange(dummy);
            }

            return colorList;
        }

        /// <summary>
        /// Convert Color 4 byte to 2 byte Color
        /// </summary>
        /// <param name="colorAsInt32">The Color to Convert</param>
        /// <returns></returns>
        internal static UInt16 EncodeColorTo2Byte(uint colorAsInt32)
        {
            byte[] arrray = { (byte)(204), (byte)196 };
            var testtt = BitConverter.ToUInt16(arrray, 0);
            var colors = BitConverter.GetBytes(colorAsInt32);

            UInt16 b = (UInt16)((colors[0] >> 3) & 0b0001_1111);
            UInt16 g = (UInt16)(((colors[1] >> 3) & 0b0001_1111) << 5);
            UInt16 r = (UInt16)(((colors[2] >> 3) & 0b0001_1111) << 10);
            UInt16 a = (UInt16)((colors[3] & 0b1000_0000) << 8);
            UInt16 color = (UInt16)(b | g | r | a);
            return color;
        }

        /// <summary>
        /// Convert 2byte Color to RGBA
        /// </summary>
        /// <param name="pixel">2 Byte Color to Convert</param>
        /// <param name="r">Red value</param>
        /// <param name="g">Green value</param>
        /// <param name="b">Blue value</param>
        /// <param name="a">Alpha value</param>
        internal static void ReadColor(UInt16 pixel, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((((pixel >> 15) & 0b0000_0001) == 1) ? 255 : 0);
            r = (byte)(((pixel >> 10) & 0b11111) << 3);
            g = (byte)(((pixel >> 5) & 0b11111) << 3);
            b = (byte)((pixel & 0b11111) << 3);

            //Round Color to full RGB white than 255 not below
            //r = (byte)(r | (r >> 5));
            //g = (byte)(g | (g >> 5));
            //b = (byte)(b | (b >> 5));
        }

        /// <summary>
        /// Get File names in an Path
        /// </summary>
        /// <param name="path">The Path to lookup</param>
        /// <param name="filter">The Filer, which files only</param>
        /// <returns></returns>
        internal static string[] GetFileNames(string path, string filter)
        {
            string[] files = Directory.GetFiles(path, filter,SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileName(files[i]);
            return files;
        }

        internal static string[] GetDirectoryNames(string path)
        {
            string[] files = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileName(files[i]);
            return files;
        }

        /// <summary>
        /// Calculate the width of a Image from the parts of Diamonds in it
        /// </summary>
        /// <param name="anzParts">The Number of Diamonds in the bigger IMG</param>
        /// <returns></returns>
        internal static int GetDiamondWidth(int anzParts)
        {
            int width = 0;
            int actualParts = 0;
            int corner = 1;
            while (true)
            {

                if (anzParts - actualParts - corner == 0)
                {
                    width = corner - corner / 2;
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

        public static void SelectCulture(UserConfig.Languages language)
        {
            var dictionaryList = Application.Current.Resources.MergedDictionaries;

            object dummy;
            var resourceDictionary = dictionaryList.FirstOrDefault(d => d.TryGetResource(language.ToString(),out dummy) == true);
            
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
        }

        public static String GetText(String key)
        {
            var dictionaryList = Application.Current.Resources.MergedDictionaries;
            object dummy=null;
            var resourceDictionary = dictionaryList.Last(d => d.TryGetResource(key, out dummy) == true);

            return dummy.ToString();
        }

    }
}

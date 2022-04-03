using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.Files;
using Gm1KonverterCrossPlatform.Files.Converters;
using Gm1KonverterCrossPlatform.Views;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    /// <summary>
    /// Reusable functions for many uses.
    /// </summary>
    internal static class Utility
    {
        public static GM1FileHeader.DataType datatype;

        internal static Image<Rgba32> LoadImageData(string filePath)
        {
            if (Logger.Loggeractiv) Logger.Log($"LoadImageData {filePath}");

            return Image.Load<Rgba32>(filePath);
        }

        /// <summary>
        /// Load the IMG as Color 2-byte list
        /// </summary>
        /// <param name="filename">The Filepath/Filename to load</param>
        /// <param name="width">The width from the IMG</param>
        /// <param name="height">The Height from the IMG</param>
        /// <param name="animatedColor">Needed if alpha is 0 or 1</param>
        /// <param name="pixelsize">Pixelsize of a pixel needed for Colortable</param>
        internal static List<UInt16> LoadImage(
            string filename,
            ref int width,
            ref int height,
            int animatedColor = 1,
            int pixelsize = 1,
            uint type = 0,
            int offsetx = 0,
            int offsety = 0)
        {
            if (Logger.Loggeractiv) Logger.Log($"LoadImage {filename}");

            Image<Rgba32> image = Image.Load<Rgba32>(filename);

            return LoadImage(image, ref width, ref height, animatedColor, pixelsize, type, offsetx, offsety);
        }

        /// <summary>
        /// Load the IMG as Color 2-byte list
        /// </summary>
        /// <param name="image">SixLabors.ImageSharp.Image data</param>
        /// <param name="width">The width from the IMG</param>
        /// <param name="height">The Height from the IMG</param>
        /// <param name="animatedColor">Needed if alpha is 0 or 1</param>
        /// <param name="pixelsize">Pixelsize of a pixel needed for Colortable</param>
        internal static List<ushort> LoadImage(
            Image<Rgba32> image,
            ref int width,
            ref int height,
            int animatedColor = 1,
            int pixelsize = 1,
            uint type = 0,
            int offsetx = 0,
            int offsety = 0)
        {
            if (Logger.Loggeractiv) Logger.Log("LoadImage from image data");

            List<ushort> colors = new List<ushort>();

            try
            {
                if (width == 0) width = image.Width;
                if (height == 0) height = image.Height;

                GM1FileHeader.DataType dataType = (GM1FileHeader.DataType)type;
                byte a = (animatedColor >= 1
                    || dataType == GM1FileHeader.DataType.TilesObject
                    || dataType == GM1FileHeader.DataType.Animations
                    || dataType == GM1FileHeader.DataType.TGXConstSize
                    || dataType == GM1FileHeader.DataType.NOCompression
                    || dataType == GM1FileHeader.DataType.NOCompression1
                    || dataType == GM1FileHeader.DataType.Interface) ? byte.MaxValue : byte.MinValue;

                for (int y = offsety; y < height + offsety; y += pixelsize)
                {
                    for (int x = offsetx; x < width + offsetx; x += pixelsize) //Bgra8888
                    {
                        Rgba32 pixel = image[x, y];

                        if (pixel.A == 0)
                        {
                            colors.Add(32767);
                        }
                        else
                        {
                            colors.Add(ColorConverter.EncodeArgb1555(pixel.R, pixel.G, pixel.B, a));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Logger.Loggeractiv) Logger.Log("Exception:\n" + e.Message);
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + e.Message);
                messageBox.Show();
            }

            return colors;
        }

        internal unsafe static WriteableBitmap LoadImageAsBitmap(
            String filename,
            ref int width,
            ref int height,
            int offsetx = 0,
            int offsety = 0)
        {
            if (Logger.Loggeractiv) Logger.Log($"LoadImageAsBitmap {filename}");

            Image<Rgba32> image = Image.Load<Rgba32>(filename);

            return LoadImageAsBitmap(image, ref width, ref height, offsetx, offsety);
        }

        internal unsafe static WriteableBitmap LoadImageAsBitmap(
            Image<Rgba32> image,
            ref int width,
            ref int height,
            int offsetx = 0,
            int offsety = 0)
        {
            if (Logger.Loggeractiv) Logger.Log("LoadImageAsBitmap from image data");

            if (width == 0) width = image.Width;
            if (height == 0) height = image.Height;

            WriteableBitmap bitmap = new WriteableBitmap(new PixelSize(width, height),new Vector(96,96),Avalonia.Platform.PixelFormat.Rgba8888);
            using (var bit = bitmap.Lock())
            {
                try
                {
                    int xBit = 0, yBit = 0;
                    for (int y = offsety; y < height + offsety; y++)
                    {
                        for (int x = offsetx; x < width + offsetx; x++) //Bgra8888
                        {
                            var pixel = image[x, y];

                            var ptr = (uint*)bit.Address;
                            ptr += (uint)((bitmap.PixelSize.Width * yBit) + xBit);
                            *ptr = pixel.PackedValue;
                            xBit++;
                        }
                        xBit = 0;
                        yBit++;
                    }
                }
                catch (Exception e)
                {
                    if (Logger.Loggeractiv) Logger.Log("Exception:\n" + e.Message);
                    MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + e.Message);
                    messageBox.Show();
                }
            }
               
            return bitmap;
        }

        /// <summary>
        /// Convert an IMG as Colorlist to ByteArray
        /// </summary>
        /// <param name="colors">The IMG as Color List</param>
        /// <param name="width">The width from the IMG</param>
        /// <param name="height">The Height from the IMG</param>
        /// <param name="animatedColor">Needed for Alpha is 1 or 0</param>
        /// <returns></returns>
        internal static List<byte> ImgToGM1ByteArray(List<ushort> colors, int width, int height, int animatedColor, Palette palette = null, List<ushort>[] paletteImages = null)
        {
            if (Logger.Loggeractiv) Logger.Log("ImgToGM1ByteArray");
            int transparent =  32767;
            ushort alpha = (animatedColor == 0) ? (ushort)0b1000_0000_0000_0000 : (ushort)0b0;
            List<byte> array = new List<byte>();
            byte length = 0; // value 1-32  | 0 will be 1
            byte header = 0; // 3 bytes
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
                  
                    //hole line transparent?
                    if (newline == true && countSamePixel == width)
                    {
                        //newline consist out of transparent pixelstrings only TGXConstSize, idk why(TiledObject empty on Top also not only newline)
                        if (datatype == GM1FileHeader.DataType.TGXConstSize || datatype == GM1FileHeader.DataType.TilesObject  || datatype == GM1FileHeader.DataType.Interface)
                        {
                            //TGXConstSize later only consists out of newlines?????
                            if (!CheckIfAllLinesUnderTransparent(colors, transparent, (i + 1) * width) || datatype == GM1FileHeader.DataType.TilesObject)
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
                                    length = (byte)(dummy - 1);//-1 because in the decoding +1
                                    array.Add((byte)(header | length));
                                }
                            }
                        }
                      
                        array.Add(0b1000_0000);
                       
                        j = width;
                        continue;
                    }
                    //pixels in the line
                    else
                    {
                        //transparent pixelstring?
                        var dummy = countSamePixel;
                        header = 0b0010_0000;
                        while (dummy / 32 > 0)
                        {
                            length = 0b0001_1111;
                            array.Add((byte)(header | length));
                            dummy -= 32;
                        }
                        if (dummy != 0)
                        {
                            length = (byte)(dummy - 1);//-1 because in the decoding +1
                            array.Add((byte)(header | length));
                        }
                        j += countSamePixel;
                        if (j == width)
                        {
                            array.Add(0b1000_0000);
                          
                            continue;
                        }
                        
                        countSamePixel = 1;
                        int repeatingPixel = 1;
                        //Stream-of-pixels or repeating pixels?
                        for (int z = j + 1; z < width; z++)
                        {
                            if (colors[i * width + z] != transparent && colors[i * width + z - 1] != colors[i * width + z])
                            {
                                if (repeatingPixel > 2)//only if more than 2 colors repeat
                                {
                                    break;
                                }
                                else if (repeatingPixel>1)
                                {
                                    countSamePixel += repeatingPixel - 1;
                                    repeatingPixel = 1;
                                }
                                
                                countSamePixel++;
                            }
                            else if (colors[i * width + z] != transparent && colors[i * width + z - 1] == colors[i * width + z])
                            {
                                repeatingPixel++;
                                if (countSamePixel > 1 && repeatingPixel > 2)
                                {
                                    countSamePixel--;
                                    repeatingPixel = 1;
                                    break;
                                }
                                if (z == width-1)
                                {
                                    countSamePixel++;
                                }
                            }
                            else
                            {
                                if (repeatingPixel < 3)
                                {
                                    countSamePixel += repeatingPixel - 1;
                                }
                                break;
                            }

                        }
                        if (repeatingPixel > 2)
                        {
                            countSamePixel = repeatingPixel;
                            header = 0b0100_0000;
                            dummy = countSamePixel;
                            
                            while (dummy / 32 > 0)
                            {
                                length = 0b0001_1111;
                                array.Add((byte)(header | length));
                              
                                var color = (ushort)(colors[j + i * width + 1] | alpha);
                                if (palette == null)
                                {
                                    array.AddRange(BitConverter.GetBytes(color));
                                }
                                else
                                {
                                    byte positioninColortable = FindColorPositionInPalette(color, j + i * width + 1, palette, paletteImages);
                                    array.Add(positioninColortable);
                                }

                                dummy -= 32;
                            }
                            if (dummy != 0)
                            {
                                length = (byte)(dummy - 1);//-1 because in the decoding +1
                                array.Add((byte)(header | length));
                               
                                var color = (ushort)(colors[j  + i * width + 1] | alpha);
                                if (palette==null)
                                {
                                    array.AddRange(BitConverter.GetBytes(color));
                                }
                                else
                                {
                                    byte positioninColortable = FindColorPositionInPalette(color, j + i * width + 1, palette, paletteImages);
                                    array.Add(positioninColortable);
                                }
                            }
                        }
                        else
                        {
                            header = 0b0000_0000;
                            dummy = countSamePixel;
                            int zaehler = 0;
                            while (dummy / 32 > 0)
                            {
                                length = 0b0001_1111;
                                array.Add((byte)(header | length));
                                for (int a = 0; a < 32; a++)
                                {
                                    var color = (ushort)(colors[j + zaehler + i * width] | alpha);
                                    if (palette == null)
                                    {
                                        array.AddRange(BitConverter.GetBytes(color));
                                    }
                                    else
                                    {
                                        byte positioninColortable = FindColorPositionInPalette(color, j + zaehler + i * width, palette, paletteImages);
                                        array.Add(positioninColortable);
                                    }

                                    dummy--;
                                    zaehler++;
                                }
                            }
                            if (dummy != 0)
                            {
                                length = (byte)(dummy - 1);//-1 because in the decoding +1
                                array.Add((byte)(header | length));
                                for (int a = 0; a < dummy; a++)
                                {
                                    var color = (ushort)(colors[j + zaehler + i * width] | alpha);
                                    if (palette == null)
                                    {
                                        array.AddRange(BitConverter.GetBytes(color));
                                    }
                                    else
                                    {
                                        byte positioninColortable = FindColorPositionInPalette(color, j + zaehler + i * width, palette, paletteImages);
                                        array.Add(positioninColortable);
                                    }

                                    zaehler++;
                                }
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

        internal static WriteableBitmap CreateBigImage(List<TilesImage> tilesImages, int BigImageSize)
        {
            if (Logger.Loggeractiv) Logger.Log("CreateBigImage");
            List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
            foreach (var item in tilesImages)
            {
                bitmaps.Add(item.TileImage);
            }
            return CreateBigImage(bitmaps, BigImageSize);
        }

        private static unsafe WriteableBitmap CreateBigImage(List<WriteableBitmap> bitmaps, int BigImageSize)
        {
            int maxwidth = BigImageSize;
            int maxheight = 0;
            int actualwidth = 0;
            int row = 0;

            List<int> heighrows = new List<int>();
            heighrows.Add(bitmaps[0].PixelSize.Height);
            foreach (var bitmap in bitmaps)
            {
                actualwidth += bitmap.PixelSize.Width;
                if (maxwidth <= actualwidth)
                {
                    heighrows.Add(bitmap.PixelSize.Height);
                    row++;
                    actualwidth = bitmap.PixelSize.Width;
                }

                if (heighrows[row] < bitmap.PixelSize.Height)
                {
                    heighrows[row] = bitmap.PixelSize.Height;
                }
            }

            foreach (var height in heighrows)
            {
                maxheight += height;
            }

            WriteableBitmap bigImage = new WriteableBitmap(new Avalonia.PixelSize(maxwidth, maxheight), new Avalonia.Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);// Bgra8888 is device-native and much faster.
            using (var buf = bigImage.Lock())
            {
                int xoffset = 0;
                int yoffset = 0;
                row = 0;
                actualwidth = 0;
                foreach (var bitmap in bitmaps)
                {
                    actualwidth += bitmap.PixelSize.Width;
                    if (maxwidth <= actualwidth)
                    {
                        actualwidth = bitmap.PixelSize.Width;
                        xoffset = 0;
                        yoffset += heighrows[row];
                        row++;
                    }

                    using (var bit = bitmap.Lock())
                    {
                        for (int y = 0; y < bitmap.PixelSize.Height; y++)
                        {
                            for (int x = 0; x < bitmap.PixelSize.Width; x++)
                            {
                                UInt32 colorByte;
                                var ptr = (uint*)bit.Address;
                                ptr += (uint)((bitmap.PixelSize.Width * y) + x);
                                colorByte = *ptr;

                                ptr = (uint*)buf.Address;
                                ptr += (uint)((maxwidth * (y+yoffset)) + x + xoffset);
                                *ptr = colorByte;
                            }
                        }
                    }

                    xoffset += bitmap.PixelSize.Width;
                }
            }
            return bigImage;
        }

        internal static WriteableBitmap CreateBigImage(List<TGXImage> imagesTGX, int BigImageWidth)
        {
            List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
            foreach (var item in imagesTGX)
            {
                bitmaps.Add(item.Bitmap);
            }
            return CreateBigImage(bitmaps, BigImageWidth);
        }

        private static byte FindColorPositionInPalette(ushort color, int position, Palette palette, List<ushort>[] paletteImages)
        {
            byte newPosition = 0;
            if (paletteImages == null)//not orginal Stronghold Files do not need to check all
            {
                for (byte i = 0; i < byte.MaxValue; i++)
                {
                    if (color == palette.ColorTables[0].ColorList[i])
                    {
                        newPosition = i;
                        break;
                    }
                }
            }
            else // Orginal ones
            {
                List<byte> positions = new List<byte>();
                for (byte i = 0; i < byte.MaxValue; i++)
                {
                    if (color == palette.ColorTables[0].ColorList[i])
                    {
                        positions.Add(i);
                    }
                }
                if (positions.Count > 1 || positions.Count == 0)
                {
                    for (int j = 0; j < 9; j++) //other 9 pictures check for only one position
                    {
                        List<byte> otherPositions = new List<byte>();
                        for (byte i = 0; i < byte.MaxValue; i++)
                        {
                            if (paletteImages[j][position] == palette.ColorTables[j+1].ColorList[i])
                            {
                                otherPositions.Add(i);
                            }
                        }
                        if (otherPositions.Count==1)
                        {
                            newPosition = otherPositions[0];
                            break;
                        }
                        else if (otherPositions.Count > 1)
                        {
                            newPosition = otherPositions[0];
                        }
                    }
                }
                else
                {
                    newPosition = positions[0];
                }
            }
            if (newPosition==0)
            {

            }
            return newPosition;
        }

        private static bool CheckIfAllLinesUnderTransparent(List<ushort> colors, int transparent, int offset)
        {
            for (int i = offset; i < colors.Count; i++)
            {
                if (colors[i] != transparent)
                {
                    return false;
                }
            }
            return true;
        }

        public static int YOffsetBefore { get; internal set; }
        public static int XOffsetBefore { get; internal set; }

        private static int biggestHeight = 0;
        internal static List<TGXImage> ConvertImgToTiles(List<ushort> list, ushort width, ushort height, List<TGXImage> oldList)
        {
            if (Logger.Loggeractiv) Logger.Log("ConvertImgToTiles");
            List<TGXImage> newImageList = new List<TGXImage>();

            //calculate Parts one part 16 x 30
            int partwidth = width / 30;//todo not exactly 30 width because 2 pixels between(can ignored because Church bigest tiledImage and work)
            int totalTiles = partwidth * partwidth;

            int savedOffsetX = width / 2;
            int xOffset = savedOffsetX;
            int yOffset = height - 16;
            int partsPerLine = 1;
            int counter = 0;

            List<byte> arrayByte;
            bool halfreached = false;
            datatype = GM1FileHeader.DataType.TilesObject;

            int[] array = {
                2, 6, 10, 14, 18, 22, 26, 30,
                30, 26, 22, 18, 14, 10, 6, 2
            };

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
                        arrayByte.AddRange(BitConverter.GetBytes(color));
                        x++;

                        list[number] = 32767;
                    }

                    y++;
                    x = 0;
                }

                var newImage = new TGXImage();
                newImage.Header = new TGXImageHeader();
                newImage.Header.Direction = 0;
                newImage.Header.Height = 16;
                newImage.Header.Width = 30;
                //newImage.OffsetX = (ushort)(xOffset + XOffsetBefore);
                //newImage.OffsetY = (ushort)(yOffset + YOffsetBefore);
                newImage.Header.SubParts = (byte)totalTiles;
                newImage.Header.ImagePart = (byte)part;

                if (totalTiles == 1) halfreached = true;

                if (halfreached)
                {
                    //tileoffset=1st pixel from tile and than height
                    if (counter == 1)//left
                    {
                        if (part == totalTiles - 1)
                        {
                            newImage.Header.BuildingWidth = 30;
                            newImage.Header.Direction = 1;
                            int imageOnTopwidth = 30;
                            int imageOnTopheight = yOffset + 7;
                            int imageOnTopOffsetX = xOffset - 15;
                            List<ushort> colorListImgOnTop = GetColorList(list, imageOnTopwidth, imageOnTopheight, imageOnTopOffsetX, width);
                           
                            if (colorListImgOnTop.Count != 0)
                            {
                                var byteArrayImgonTop = ImgToGM1ByteArray(colorListImgOnTop, imageOnTopwidth, colorListImgOnTop.Count / imageOnTopwidth, 1);
                                arrayByte.AddRange(byteArrayImgonTop);
                                newImage.Header.TileOffset = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10 - 16 - 1);
                                if (newImage.Header.TileOffset == ushort.MaxValue) newImage.Header.TileOffset = 0;
                                newImage.Header.Height = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 9);
                            }
                        }
                        else
                        {
                            newImage.Header.BuildingWidth = 16;
                            newImage.Header.Direction = 2;
                            int imageOnTopwidth = 16;
                            int imageOnTopheight = yOffset + 7;
                            int imageOnTopOffsetX = xOffset - 15;
                            List<ushort> colorListImgOnTop = GetColorList(list, imageOnTopwidth, imageOnTopheight, imageOnTopOffsetX, width);

                            if (colorListImgOnTop.Count != 0)
                            {
                                var byteArrayImgonTop = ImgToGM1ByteArray(colorListImgOnTop, imageOnTopwidth, colorListImgOnTop.Count / imageOnTopwidth, 1);
                                arrayByte.AddRange(byteArrayImgonTop);
                                newImage.Header.TileOffset = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10 - 16 - 1);
                                if (newImage.Header.TileOffset == ushort.MaxValue) newImage.Header.TileOffset = 0;
                                newImage.Header.Height = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 9);
                            }
                        }
                    }
                    else if (counter == partsPerLine)//right
                    {
                        newImage.Header.BuildingWidth = 16;
                        newImage.Header.Direction = 3;
                        int imageOnTopwidth = 16;
                        int imageOnTopheight = yOffset + 7;
                        int imageOnTopOffsetX = xOffset;
                        List<ushort> colorListImgOnTop = GetColorList(list, imageOnTopwidth, imageOnTopheight, imageOnTopOffsetX - 1, width);
                        
                        if (colorListImgOnTop.Count != 0)
                        {
                            var byteArrayImgonTop = ImgToGM1ByteArray(colorListImgOnTop, imageOnTopwidth, colorListImgOnTop.Count / imageOnTopwidth, 1);
                            arrayByte.AddRange(byteArrayImgonTop);
                            newImage.Header.Height = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 9);
                            newImage.Header.TileOffset = (ushort)(colorListImgOnTop.Count / imageOnTopwidth + 10 - 16 - 1);
                            if (newImage.Header.TileOffset == ushort.MaxValue) newImage.Header.TileOffset = 0;
                        }

                        newImage.Header.HorizontalOffsetOfImage = 14;
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

            XOffsetBefore += width;
            if (height > biggestHeight) biggestHeight = height;
            if (XOffsetBefore > 4000)
            {
                XOffsetBefore = 0;
                YOffsetBefore += biggestHeight;
            }

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
                    if (list[orgWidth * y + x] != 32767 || y > height - 8)
                    {
                        dummy.Add(list[orgWidth * y + x]);
                        allTransparent = false;
                    }
                    else
                    {
                        dummy.Add(32767);
                    }
                }
                if (!allTransparent) colorList.AddRange(dummy);
            }

            return colorList;
        }

        /// <summary>
        /// Convert 2-byte Color (A1R5G5B5) to RGBA
        /// </summary>
        /// <param name="pixel">2 Byte Color to Convert</param>
        /// <param name="r">Red value</param>
        /// <param name="g">Green value</param>
        /// <param name="b">Blue value</param>
        /// <param name="a">Alpha value</param>
        internal static void ReadColor(ushort pixel, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((((pixel >> 15) & 0b0000_0001) == 1) ? 255 : 0);
            r = (byte)(((pixel >> 10) & 0b11111) << 3);
            g = (byte)(((pixel >> 5) & 0b11111) << 3);
            b = (byte)((pixel & 0b11111) << 3);
        }

        /// <summary>
        /// Get File names in a directory
        /// </summary>
        /// <param name="directory">The Path to lookup</param>
        /// <param name="filter">The Filer, which files to select</param>
        internal static string[] GetFileNames(string directory, string filter)
        {
            string[] files = Directory.GetFiles(directory, filter, SearchOption.TopDirectoryOnly);
            
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileName(files[i]);

            return files;
        }

        internal static string[] GetDirectoryNames(string directory)
        {
            string[] files = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

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

        public static String GetText(String key)
        {
            var dictionaryList = Application.Current.Resources.MergedDictionaries;
            object dummy = null;
            var resourceDictionary = dictionaryList.Last(d => d.TryGetResource(key, out dummy) == true);

            return dummy.ToString();
        }
    }
}

// Copy of the decoding algorithms as they existed before the clean-code refactoring
// (commit 5b1ade8: HelperClasses/ImageConverter.cs, Files/TilesImage.cs, Files/DecodedFile.cs).
// Unsafe pointer writes into Avalonia bitmaps were replaced by writes into a uint[] (BGRA8888),
// everything else is unchanged. Used as reference for the refactored decoders.
using System;
using System.Collections.Generic;

#pragma warning disable
#nullable disable
namespace Gm1KonverterCrossPlatform.Tests.Legacy
{
    internal static class LegacyDecoders
    {
        internal static uint Argb1555ToBgra8888(ushort color)
        {
            byte a = (byte)((((color >> 15) & 0b0000_0001) == 1) ? 255 : 0);
            byte r = (byte)(((color >> 10) & 0b11111) << 3);
            byte g = (byte)(((color >> 5) & 0b11111) << 3);
            byte b = (byte)((color & 0b11111) << 3);
            return (uint)(b | g << 8 | r << 16 | a << 24);
        }

        /// <summary>ImageConverter.GM1ByteArrayToImg</summary>
        internal static uint[] GM1ByteArrayToImg(byte[] byteArray, int width, int height, ushort[] colorTable)
        {
            uint[] pointer = new uint[width * height];

            int pos = 0;
            int newLinePos = width;

            ushort colorArgb1555;
            uint colorBgra8888;

            for (int bytePos = 0; bytePos < byteArray.Length;)
            {
                byte token = byteArray[bytePos];
                int tokenType = token >> 5;
                int length = (token & 31) + 1;

                bytePos++;

                if (tokenType == 4)
                {
                    pos = newLinePos;
                    newLinePos += width;
                }
                else if (tokenType == 1)
                {
                    pos += length;
                }
                else
                {
                    int readLength = 1;
                    int writeLength = 1;

                    if (tokenType == 0)
                    {
                        readLength = length;
                    }
                    else if (tokenType == 2)
                    {
                        writeLength = length;
                    }

                    for (int i = 0; i < readLength; i++)
                    {
                        if (colorTable != null)
                        {
                            colorArgb1555 = colorTable[byteArray[bytePos]];
                            bytePos++;
                        }
                        else
                        {
                            colorArgb1555 = BitConverter.ToUInt16(byteArray, bytePos);
                            bytePos += 2;
                        }

                        colorBgra8888 = Argb1555ToBgra8888(colorArgb1555);

                        for (int j = 0; j < writeLength; j++)
                        {
                            pointer[pos] = colorBgra8888;
                            pos++;
                        }
                    }
                }
            }

            return pointer;
        }

        /// <summary>Gm1NoCompressionConverter.GetBitmap</summary>
        internal static uint[] NoCompressionToImg(byte[] byteArray, int width, int height)
        {
            uint[] pointer = new uint[width * height];
            int pos = 0;
            for (int bytePos = 0; bytePos < byteArray.Length; bytePos += 2)
            {
                pointer[pos] = Argb1555ToBgra8888(BitConverter.ToUInt16(byteArray, bytePos));
                pos++;
            }
            return pointer;
        }

        internal sealed class LegacyTilesImage
        {
            public static int Puffer = 500;

            public int width, height;
            public int minusHeight = 9999999;
            public uint[] colors;
            public uint[] result;

            public LegacyTilesImage(int width, int height)
            {
                this.width = width;
                this.height = height;
                colors = new uint[width * height];
            }

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
                        colors[pos] = Argb1555ToBgra8888(BitConverter.ToUInt16(imgFileAsByteArray, bytePos));
                        bytePos += 2;
                    }
                }
            }

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
                        case 0:
                            for (byte i = 0; i < length; i++)
                            {
                                pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, bytePos);
                                bytePos += 2;
                                colorByte = Argb1555ToBgra8888(pixelColor);
                                colors[(uint)((width * (y + offsetY)) + x + offsetX)] = colorByte;
                                x++;
                            }
                            break;
                        case 4:
                            y++;
                            x = 0;
                            break;
                        case 2:
                            pixelColor = BitConverter.ToUInt16(imgFileAsBytearray, bytePos);
                            bytePos += 2;
                            colorByte = Argb1555ToBgra8888(pixelColor);
                            for (byte i = 0; i < length; i++)
                            {
                                colors[(uint)((width * (y + offsetY)) + x + offsetX)] = colorByte;
                                x++;
                            }
                            break;
                        case 1:
                            x += length;
                            break;
                        default:
                            break;
                    }
                }
            }

            internal void CreateImagefromList()
            {
                if (minusHeight == 9999999)
                {
                    minusHeight = Puffer;
                }
                height = height - minusHeight;

                result = new uint[width * height];
                uint pos = 0;
                for (int i = width * minusHeight; i < colors.Length; i++)
                {
                    result[pos] = colors[i];
                    pos++;
                }
            }
        }

        /// <summary>DecodedFile.CreateTileImage (after offsets, sizes and headers were read).</summary>
        internal static List<LegacyTilesImage> CreateTileImage(List<TGXImage> _TGXImage)
        {
            var tilesImage = new List<LegacyTilesImage>();

            int offsetX = 0, offsetY = 0;
            int width = 0;
            int counter = -1;
            int itemsPerRow = 1;
            int actualItemsPerRow = 0;
            int safeoffset = 0;
            bool halfReached = false;
            int partsBefore = 0;

            for (int i = 0; i < _TGXImage.Count; i++)
            {
                if (_TGXImage[i].Header.ImagePart == 0)
                {
                    width = LegacyUtility.GetDiamondWidth(_TGXImage[i].Header.SubParts);

                    partsBefore += _TGXImage[i].Header.SubParts;

                    tilesImage.Add(new LegacyTilesImage(width * 30 + ((width - 1) * 2), width * 16 + _TGXImage[partsBefore - 1].Header.TileOffset + LegacyTilesImage.Puffer));
                    counter++;
                    itemsPerRow = 1;
                    actualItemsPerRow = 0;
                    offsetY = tilesImage[counter].height - 16;
                    offsetX = (width / 2) * 30 + (width - 1) - ((width % 2 == 0) ? 15 : 0);
                    safeoffset = offsetX;
                    halfReached = false;
                }

                if (_TGXImage[i].ImgFileAsBytearray.Length > 512)
                {
                    int right = 0;
                    if (_TGXImage[i].Header.Direction == 3)
                    {
                        right = 14;
                    }

                    tilesImage[counter].AddImgTileOnTopToImg(_TGXImage[i].ImgFileAsBytearray, offsetX + right, offsetY - _TGXImage[i].Header.TileOffset);

                    if (tilesImage[counter].minusHeight > offsetY - _TGXImage[i].Header.TileOffset)
                    {
                        tilesImage[counter].minusHeight = offsetY - _TGXImage[i].Header.TileOffset;
                    }
                }

                tilesImage[counter].AddDiamondToImg(_TGXImage[i].ImgFileAsBytearray, offsetX, offsetY);

                offsetX += 32;
                actualItemsPerRow++;
                if (actualItemsPerRow == itemsPerRow)
                {
                    offsetX = safeoffset;

                    offsetY -= 8;
                    if (itemsPerRow < width)
                    {
                        if (halfReached)
                        {
                            itemsPerRow--;
                            offsetX += 16;
                        }
                        else
                        {
                            itemsPerRow++;
                            offsetX -= 16;
                        }
                    }
                    else
                    {
                        itemsPerRow--;
                        offsetX += 16;
                        halfReached = true;
                    }

                    safeoffset = offsetX;
                    actualItemsPerRow = 0;
                }
            }

            foreach (var image in tilesImage)
            {
                image.CreateImagefromList();
            }

            return tilesImage;
        }
    }
}

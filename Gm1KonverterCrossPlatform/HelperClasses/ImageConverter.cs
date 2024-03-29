﻿using System;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.Files;
using Gm1KonverterCrossPlatform.Files.Converters;

/// <summary>
/// Convert game file components to WriteableBitmaps for visualisation in UI and export.
/// Convert WriteableBitmaps to game file components.
/// </summary>
namespace Gm1KonverterCrossPlatform.HelperClasses
{
	public static class ImageConverter
	{
        /// <summary>
        /// Convert GM1 byte array to image
        /// </summary>
        public static unsafe WriteableBitmap GM1ByteArrayToImg(byte[] byteArray, int width, int height, ColorTable colorTable)
        {
            // byteArray structure notes:
            // Byte array consists of 1-byte tokens, optionally followed by color information
            // The structure of token byte is tttlllll where t = tokenType and l = length
            // Lenght is zero by default and higher value indicates number of additional repeats
            // Line is always terminated by Newline token, even if there is continuation of non-transparent pixels from one line to another
            // Newline token length is always 0, there is always 1 token at the end of each line of pixels

            WriteableBitmap image = new WriteableBitmap(
                new PixelSize(width, height),
                new Vector(96, 96),
                PixelFormat.Bgra8888, // Bgra8888 is device-native and much faster
                AlphaFormat.Premul);

            using (ILockedFramebuffer buffer = image.Lock())
            {
                uint* pointer = (uint*)buffer.Address;

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

                    // Newline
                    if (tokenType == 4)
                    {
                        pos = newLinePos;
                        newLinePos += width;
                    }
                    // Repeating transparent pixel
                    else if (tokenType == 1)
                    {
                        pos += length;
                    }
                    else
                    {
                        int readLength = 1;
                        int writeLength = 1;

                        // Stream of pixels
                        if (tokenType == 0)
                        {
                            readLength = length;
                        }
                        // Repeating pixel
                        else if (tokenType == 2)
                        {
                            writeLength = length;
                        }

                        for (int i = 0; i < readLength; i++)
                        {
                            if (colorTable != null)
                            {
                                colorArgb1555 = colorTable.ColorList[byteArray[bytePos]];
                                bytePos++;
                            }
                            else
                            {
                                colorArgb1555 = BitConverter.ToUInt16(byteArray, bytePos);
                                bytePos += 2;
                            }

                            colorBgra8888 = ColorConverter.Argb1555ToBgra8888(colorArgb1555);

                            for (int j = 0; j < writeLength; j++)
                            {
                                pointer[pos] = colorBgra8888;
                                pos++;
                            }
                        }
                    }
                }
            }

            return image;
        }
    }
}

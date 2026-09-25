using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Core.Codecs
{
    /// <summary>
    /// Run length encoding used by .tgx files and most .gm1 image types.
    /// <para>
    /// The data is a sequence of 1 byte tokens <c>tttlllll</c> (t = token type, l = length - 1),
    /// optionally followed by color information: a 2 byte ARGB1555 color, or a 1 byte color table index
    /// for animation files. Every row of pixels ends with a newline token.
    /// </para>
    /// </summary>
    public static class TgxCodec
    {
        private const int MaxTokenLength = 32;
        private const int LengthMask = 0b0001_1111;

        private enum TokenType : byte
        {
            StreamOfPixels = 0b000,
            TransparentPixels = 0b001,
            RepeatingPixels = 0b010,
            Newline = 0b100
        }

        /// <summary>
        /// Decodes an image. Data that would be written outside of the image is ignored and truncated data
        /// stops the decoding, so corrupt files can never corrupt memory.
        /// </summary>
        /// <param name="colorTable">Color table for animation images, <c>null</c> for images with 2 byte colors.</param>
        public static Argb1555Image Decode(ReadOnlySpan<byte> data, int width, int height, ColorTable? colorTable = null)
        {
            var image = new Argb1555Image(width, height);
            var pixels = image.Pixels;

            int position = 0;
            int nextLineStart = width;
            int bytePosition = 0;

            while (bytePosition < data.Length)
            {
                byte token = data[bytePosition++];
                var tokenType = (TokenType)(token >> 5);
                int length = (token & LengthMask) + 1;

                switch (tokenType)
                {
                    case TokenType.Newline:
                        position = nextLineStart;
                        nextLineStart += width;
                        break;

                    case TokenType.TransparentPixels:
                        position += length;
                        break;

                    default:
                        // Stream or repeating pixels. Unknown token types are read as one pixel like the original decoder did.
                        int colorCount = tokenType == TokenType.StreamOfPixels ? length : 1;
                        int repetitions = tokenType == TokenType.RepeatingPixels ? length : 1;

                        for (int i = 0; i < colorCount; i++)
                        {
                            if (!TryReadColor(data, ref bytePosition, colorTable, out ushort color))
                            {
                                return image;
                            }

                            for (int j = 0; j < repetitions; j++)
                            {
                                if ((uint)position < (uint)pixels.Length)
                                {
                                    pixels[position] = color;
                                }

                                position++;
                            }
                        }

                        break;
                }
            }

            return image;
        }

        /// <summary>
        /// Encodes an image. Pixels with the value <see cref="Argb1555.TransparentMarker"/> are transparent.
        /// </summary>
        public static byte[] Encode(Argb1555Image image, TgxEncoderOptions options)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new Encoder(image, options).Encode();
        }

        private static bool TryReadColor(ReadOnlySpan<byte> data, ref int bytePosition, ColorTable? colorTable, out ushort color)
        {
            if (colorTable != null)
            {
                if (bytePosition >= data.Length)
                {
                    color = 0;
                    return false;
                }

                color = colorTable[data[bytePosition]];
                bytePosition++;
                return true;
            }

            if (bytePosition + sizeof(ushort) > data.Length)
            {
                color = 0;
                return false;
            }

            // Stronghold ignores the alpha bit of these pixels, only transparent tokens create transparency.
            color = (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(bytePosition)) | 0x8000);
            bytePosition += sizeof(ushort);
            return true;
        }

        /// <summary>
        /// The encoder produces byte identical output to the original implementation
        /// (Utility.ImgToGM1ByteArray), which Stronghold is known to accept. The only difference: for
        /// animation images runs are built from color table indices instead of colors, so neighbouring
        /// pixels with the same color but different indices keep their indices.
        /// </summary>
        private sealed class Encoder
        {
            private const ushort Transparent = Argb1555.TransparentMarker;

            private readonly ushort[] pixels;

            /// <summary>
            /// The values that are stored per pixel: colors, or color table indices for animation images.
            /// Runs are built from equal stored values, so pixels with the same color but different
            /// color table indices are never merged.
            /// </summary>
            private readonly ushort[] storedValues;

            private readonly int width;
            private readonly int height;
            private readonly TgxEncoderOptions options;
            private readonly ushort alpha;
            private readonly List<byte> output = new List<byte>();

            public Encoder(Argb1555Image image, TgxEncoderOptions options)
            {
                pixels = image.Pixels;
                width = image.Width;
                height = image.Height;
                this.options = options;
                alpha = options.ForceAlphaBit ? (ushort)0x8000 : (ushort)0;
                storedValues = CalculateStoredValues();
            }

            private ushort[] CalculateStoredValues()
            {
                var values = new ushort[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i] == Transparent)
                    {
                        continue;
                    }

                    ushort color = (ushort)(pixels[i] | alpha);
                    values[i] = options.PaletteIndexer != null ? options.PaletteIndexer.GetIndex(i, color) : color;
                }

                return values;
            }

            public byte[] Encode()
            {
                for (int y = 0; y < height; y++)
                {
                    EncodeRow(y);
                }

                return output.ToArray();
            }

            private void EncodeRow(int y)
            {
                if (width == 0)
                {
                    return;
                }

                int rowStart = y * width;

                if (CountTransparent(rowStart, 0) == width)
                {
                    if (WritesTransparentRunForEmptyRow(y))
                    {
                        WriteTransparentRun(width);
                    }

                    WriteNewline();
                    return;
                }

                int x = 0;
                while (x < width)
                {
                    int transparentPixels = CountTransparent(rowStart, x);
                    WriteTransparentRun(transparentPixels);
                    x += transparentPixels;

                    if (x == width)
                    {
                        WriteNewline();
                        break;
                    }

                    var segment = MeasureColoredSegment(rowStart, x);
                    if (segment.IsRepeating)
                    {
                        WriteRepeatingPixels(rowStart + x + 1, segment.Length);
                    }
                    else
                    {
                        WriteStreamOfPixels(rowStart + x, segment.Length);
                    }

                    x += segment.Length;
                    if (x == width)
                    {
                        WriteNewline();
                    }
                }
            }

            private bool WritesTransparentRunForEmptyRow(int y)
            {
                switch (options.EmptyRowMode)
                {
                    case EmptyRowMode.AlwaysTransparentRun:
                        return true;
                    case EmptyRowMode.TransparentRunUnlessRemainingRowsEmpty:
                        return !AreAllPixelsTransparentFrom((y + 1) * width);
                    default:
                        return false;
                }
            }

            private int CountTransparent(int rowStart, int x)
            {
                int count = 0;
                while (x + count < width && pixels[rowStart + x + count] == Transparent)
                {
                    count++;
                }

                return count;
            }

            private bool AreAllPixelsTransparentFrom(int index)
            {
                for (int i = index; i < pixels.Length; i++)
                {
                    if (pixels[i] != Transparent)
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Decides whether the colored pixels starting at <paramref name="x"/> are written as a run of one
            /// repeated value (3 or more equal pixels) or as a stream of different values, and how long it is.
            /// Pairs of equal pixels are kept inside a stream, a stream ends right before 3 equal pixels.
            /// </summary>
            private Segment MeasureColoredSegment(int rowStart, int x)
            {
                int streamLength = 1;
                int equalPixels = 1;

                for (int z = x + 1; z < width; z++)
                {
                    if (pixels[rowStart + z] == Transparent)
                    {
                        if (equalPixels < 3)
                        {
                            streamLength += equalPixels - 1;
                        }

                        break;
                    }

                    if (storedValues[rowStart + z] != storedValues[rowStart + z - 1])
                    {
                        if (equalPixels > 2)
                        {
                            break;
                        }

                        if (equalPixels > 1)
                        {
                            streamLength += equalPixels - 1;
                            equalPixels = 1;
                        }

                        streamLength++;
                    }
                    else
                    {
                        equalPixels++;
                        if (streamLength > 1 && equalPixels > 2)
                        {
                            streamLength--;
                            equalPixels = 1;
                            break;
                        }

                        if (z == width - 1)
                        {
                            streamLength++;
                        }
                    }
                }

                return equalPixels > 2
                    ? new Segment(equalPixels, isRepeating: true)
                    : new Segment(streamLength, isRepeating: false);
            }

            private void WriteTransparentRun(int length)
            {
                foreach (int chunk in SplitIntoTokenLengths(length))
                {
                    WriteToken(TokenType.TransparentPixels, chunk);
                }
            }

            private void WriteRepeatingPixels(int colorIndex, int length)
            {
                foreach (int chunk in SplitIntoTokenLengths(length))
                {
                    WriteToken(TokenType.RepeatingPixels, chunk);
                    WriteColor(colorIndex);
                }
            }

            private void WriteStreamOfPixels(int firstIndex, int length)
            {
                int index = firstIndex;
                foreach (int chunk in SplitIntoTokenLengths(length))
                {
                    WriteToken(TokenType.StreamOfPixels, chunk);
                    for (int i = 0; i < chunk; i++)
                    {
                        WriteColor(index++);
                    }
                }
            }

            private void WriteNewline() => WriteToken(TokenType.Newline, 1);

            private void WriteToken(TokenType type, int length)
            {
                output.Add((byte)((byte)type << 5 | (length - 1)));
            }

            private void WriteColor(int pixelIndex)
            {
                ushort value = storedValues[pixelIndex];
                output.Add((byte)value);

                // Colors take 2 bytes, color table indices 1 byte.
                if (options.PaletteIndexer == null)
                {
                    output.Add((byte)(value >> 8));
                }
            }

            private static IEnumerable<int> SplitIntoTokenLengths(int length)
            {
                while (length >= MaxTokenLength)
                {
                    yield return MaxTokenLength;
                    length -= MaxTokenLength;
                }

                if (length > 0)
                {
                    yield return length;
                }
            }
        }

        private readonly struct Segment
        {
            public Segment(int length, bool isRepeating)
            {
                Length = length;
                IsRepeating = isRepeating;
            }

            public int Length { get; }

            public bool IsRepeating { get; }
        }
    }
}

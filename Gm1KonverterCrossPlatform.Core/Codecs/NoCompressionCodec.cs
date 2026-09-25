using System;
using System.Buffers.Binary;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Core.Codecs
{
    /// <summary>
    /// Uncompressed images (GM1 data types 5 and 7): 2 bytes per pixel, row by row.
    /// </summary>
    public static class NoCompressionCodec
    {
        /// <summary>Decodes as many pixels as the data contains, surplus data is ignored.</summary>
        public static Argb1555Image Decode(ReadOnlySpan<byte> data, int width, int height)
        {
            var image = new Argb1555Image(width, height);
            int pixelCount = Math.Min(image.Pixels.Length, data.Length / sizeof(ushort));

            for (int i = 0; i < pixelCount; i++)
            {
                image.Pixels[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(i * sizeof(ushort)));
            }

            return image;
        }

        public static byte[] Encode(Argb1555Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            var data = new byte[image.Pixels.Length * sizeof(ushort)];
            for (int i = 0; i < image.Pixels.Length; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * sizeof(ushort)), image.Pixels[i]);
            }

            return data;
        }
    }
}

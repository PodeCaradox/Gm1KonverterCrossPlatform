using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;

namespace Gm1KonverterCrossPlatform.Files.Converters
{
	class Gm1NoCompressionConverter
	{
        internal static unsafe WriteableBitmap GetBitmap(byte[] byteArray, int width, int height)
        {
            WriteableBitmap bitmap = new WriteableBitmap(
                new Avalonia.PixelSize(width, height),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888, // Bgra8888 is device-native and much faster.
                Avalonia.Platform.AlphaFormat.Premul);

            using (var buffer = bitmap.Lock())
            {
                uint* pointer = (uint*)buffer.Address;

                int pos = 0;

                for (int bytePos = 0; bytePos < byteArray.Length; bytePos += 2)
                {
                    pointer[pos] = ColorConverter.Argb1555ToBgra8888(BitConverter.ToUInt16(byteArray, bytePos));
                    pos++;
                }
            }

            return bitmap;
        }

        internal static byte[] GetByteArray(List<ushort> list, int width, int height)
        {
            System.Diagnostics.Debug.WriteLine("no compression bytearray");
            int length = (width * height);

            byte[] byteArray = new byte[length * 2];

            for (int i = 0; i < length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(list[i]), 0, byteArray, i * 2, 2);
            }

            return byteArray;
        }
    }
}

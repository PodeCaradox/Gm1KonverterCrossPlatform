namespace Gm1KonverterCrossPlatform.HelperClasses
{
	internal static class ColorConverter
	{
        /// <summary>
        /// Convert 2-byte ARGB1555 Color to R8 G8 B8 A8 color components
        /// </summary>
        /// <param name="color">2-byte Color to Convert</param>
        /// <param name="r">Red value</param>
        /// <param name="g">Green value</param>
        /// <param name="b">Blue value</param>
        /// <param name="a">Alpha value</param>
        internal static void DecodeArgb1555(ushort color, out byte r, out byte g, out byte b, out byte a)
        {
            a = (byte)((((color >> 15) & 0b0000_0001) == 1) ? 255 : 0);
            r = (byte)(((color >> 10) & 0b11111) << 3);
            g = (byte)(((color >> 5) & 0b11111) << 3);
            b = (byte)((color & 0b11111) << 3);
        }

        /// <summary>
        /// Convert 2-byte Argb1555 Color to 4-byte Rgba8888 color
        /// </summary>
        /// <param name="color">2-byte Color to Convert</param>
        internal static uint Argb1555ToRgba8888(ushort color)
        {
            DecodeArgb1555(color, out byte r, out byte g, out byte b, out byte a);

            return (uint)(r | g << 8 | b << 16 | a << 24);
        }

        /// <summary>
        /// Convert 2-byte ARGB1555 Color to 4-byte BGRA8888 color
        /// </summary>
        /// <param name="color">2-byte Color to Convert</param>
        internal static uint Argb1555ToBgra8888(ushort color)
        {
            DecodeArgb1555(color, out byte r, out byte g, out byte b, out byte a);

            return (uint)(b | g << 8 | r << 16 | a << 24);
        }
    }
}

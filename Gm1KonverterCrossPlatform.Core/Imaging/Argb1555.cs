namespace Gm1KonverterCrossPlatform.Core.Imaging
{
    /// <summary>
    /// Conversions for the 16 bit ARGB1555 colors used by Stronghold (1 bit alpha, 5 bit per color channel).
    /// </summary>
    public static class Argb1555
    {
        /// <summary>
        /// Color value the encoder treats as "transparent pixel". Imported images use it for every pixel
        /// with an alpha value of zero.
        /// </summary>
        public const ushort TransparentMarker = 0x7FFF;

        private const ushort AlphaBit = 0x8000;
        private const int ChannelMask = 0b1_1111;

        /// <summary>True if the alpha bit is set.</summary>
        public static bool IsOpaque(ushort color) => (color & AlphaBit) != 0;

        /// <summary>The color without its alpha bit.</summary>
        public static ushort Rgb(ushort color) => (ushort)(color & ~AlphaBit);

        public static void Decode(ushort color, out byte r, out byte g, out byte b, out byte a)
        {
            a = IsOpaque(color) ? byte.MaxValue : byte.MinValue;
            r = (byte)(((color >> 10) & ChannelMask) << 3);
            g = (byte)(((color >> 5) & ChannelMask) << 3);
            b = (byte)((color & ChannelMask) << 3);
        }

        /// <summary>
        /// Encodes 8 bit channels. The lowest 3 bits of each channel are dropped,
        /// the alpha bit is set if the highest bit of <paramref name="a"/> is set.
        /// </summary>
        public static ushort Encode(byte r, byte g, byte b, byte a)
        {
            return (ushort)((a & 0b1000_0000) << 8
                | ((r >> 3) & ChannelMask) << 10
                | ((g >> 3) & ChannelMask) << 5
                | (b >> 3) & ChannelMask);
        }

        /// <summary>Converts to a 32 bit BGRA color (straight alpha, blue in the lowest byte).</summary>
        public static uint ToBgra8888(ushort color)
        {
            Decode(color, out byte r, out byte g, out byte b, out byte a);
            return (uint)(b | g << 8 | r << 16 | a << 24);
        }

        /// <summary>Converts to a 32 bit RGBA color (straight alpha, red in the lowest byte).</summary>
        public static uint ToRgba8888(ushort color)
        {
            Decode(color, out byte r, out byte g, out byte b, out byte a);
            return (uint)(r | g << 8 | b << 16 | a << 24);
        }

        /// <summary>Squared euclidean distance of the 5 bit channels, used to find the nearest palette color.</summary>
        public static int DistanceSquared(ushort first, ushort second)
        {
            int dr = ((first >> 10) & ChannelMask) - ((second >> 10) & ChannelMask);
            int dg = ((first >> 5) & ChannelMask) - ((second >> 5) & ChannelMask);
            int db = (first & ChannelMask) - (second & ChannelMask);
            return dr * dr + dg * dg + db * db;
        }
    }
}

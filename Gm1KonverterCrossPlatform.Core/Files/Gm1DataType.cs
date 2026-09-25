namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// Kind of images stored in a .gm1 file (header field 6).
    /// </summary>
    public enum Gm1DataType : uint
    {
        /// <summary>Interface items and some building animations, stored like TGX images.</summary>
        Interface = 1,

        /// <summary>Animations, stored like TGX images but with 1 byte palette indices instead of colors.</summary>
        Animations = 2,

        /// <summary>Buildings, stored as diamond shaped tiles with an optional TGX image on top.</summary>
        TilesObject = 3,

        /// <summary>Fonts, stored like TGX images.</summary>
        Font = 4,

        /// <summary>Walls, grass, stones and others. Uncompressed, 2 bytes per pixel, header height is 7 pixel higher than the data.</summary>
        NoCompression = 5,

        /// <summary>TGX images with constant size.</summary>
        TgxConstSize = 6,

        /// <summary>Uncompressed, 2 bytes per pixel.</summary>
        NoCompression1 = 7
    }

    public static class Gm1DataTypeExtensions
    {
        public static bool IsSupported(this Gm1DataType dataType)
        {
            return dataType >= Gm1DataType.Interface && dataType <= Gm1DataType.NoCompression1;
        }

        public static bool IsUncompressed(this Gm1DataType dataType)
        {
            return dataType == Gm1DataType.NoCompression || dataType == Gm1DataType.NoCompression1;
        }

        public static bool UsesPalette(this Gm1DataType dataType) => dataType == Gm1DataType.Animations;

        /// <summary>
        /// Number of rows the header height of an image is larger than the stored image data.
        /// </summary>
        public static int HeightPadding(this Gm1DataType dataType) => dataType == Gm1DataType.NoCompression ? 7 : 0;
    }
}

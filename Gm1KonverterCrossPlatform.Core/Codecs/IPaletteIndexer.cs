namespace Gm1KonverterCrossPlatform.Core.Codecs
{
    /// <summary>
    /// Maps the colors of an image to color table indices when encoding animation images.
    /// </summary>
    public interface IPaletteIndexer
    {
        /// <param name="pixelIndex">Index of the pixel in the image (row * width + column).</param>
        /// <param name="color">The color of the pixel.</param>
        byte GetIndex(int pixelIndex, ushort color);
    }
}

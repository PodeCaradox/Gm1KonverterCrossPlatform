using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.HelperClasses;

namespace Gm1KonverterCrossPlatform.ViewModels
{
    /// <summary>One image (or building) shown in the preview list.</summary>
    public sealed class ImagePreviewItem
    {
        public ImagePreviewItem(int index, Argb1555Image pixels, TgxImageHeader? header)
        {
            Index = index;
            Pixels = pixels;
            Header = header;
            Bitmap = BitmapFactory.Create(pixels);
        }

        /// <summary>Index of the image, or of the building in building files.</summary>
        public int Index { get; }

        public Argb1555Image Pixels { get; }

        /// <summary>Header of the image (first part for buildings), <c>null</c> for .tgx files.</summary>
        public TgxImageHeader? Header { get; }

        public WriteableBitmap Bitmap { get; }

        public double Width => Pixels.Width;

        public double Height => Pixels.Height;
    }
}

using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.HelperClasses;

namespace Gm1KonverterCrossPlatform.Files
{
    public class TGXImage
    {
        // for tgx as standalone file
        private uint tgxwidth;
        private uint tgxheight;

        // for tgx as subimage in gm1 file
        private TGXImageHeader header;

        private uint offsetinByteArray;
        private uint sizeinByteArray;
        private byte[] imgFileAsBytearray;

        private WriteableBitmap bmp;

        public TGXImage()
        {

        }

        public uint TgxWidth { get => tgxwidth; set => tgxwidth = value; }
        public uint TgxHeight { get => tgxheight; set => tgxheight = value; }

        public TGXImageHeader Header { get => header; set => header = value; }

        public uint OffsetinByteArray { get => offsetinByteArray; set => offsetinByteArray = value; }
        public uint SizeinByteArray { get => sizeinByteArray; set => sizeinByteArray = value; }
        public byte[] ImgFileAsBytearray { get => imgFileAsBytearray; set => imgFileAsBytearray = value; }
        
        public WriteableBitmap Bitmap { get => bmp; set => bmp = value; }

        internal void ConvertImageWithPaletteToByteArray(List<ushort> colors, int width, int height, Palette palette, List<ushort>[] colorsImages = null)
        {
            var array = Utility.ImgToGM1ByteArray(colors, width, height, Header.AnimatedColor, palette, colorsImages);

            imgFileAsBytearray = array.ToArray();
        }

        /// <summary>
        /// Convert imported Imgs without a Pallete to Byte array to safe new GM1 File
        /// </summary>
        /// <param name="colors">Color List of the new IMG</param>
        /// <param name="width">Width of the new IMG</param>
        /// <param name="height">Height of the new IMG</param>
        internal void ConvertImageWithoutPaletteToByteArray(List<ushort> colors, int width, int height)
        {
            var array = Utility.ImgToGM1ByteArray(colors, width, height, Header.AnimatedColor);

            imgFileAsBytearray = array.ToArray();
        }
    }
}

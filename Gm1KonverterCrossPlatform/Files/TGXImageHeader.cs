using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;

namespace Files.Gm1Converter
{
    public class TGXImageHeader
    {
        #region Public

        public const int ByteSize = 16;

        #endregion

        #region Variables

        private ushort width;
        private ushort height;
        private ushort offsetX;
        private ushort offsetY;
        private byte imagePart;
        private byte subParts;
        private ushort tileOffset;
        private byte direction;
        private byte horizontalOffsetOfImage;
        private byte buildingWidth;
        private byte animatedColor; // if alpha 1

        #endregion

        #region Construtor

        public TGXImageHeader(byte[] byteArray)
        {
            width = BitConverter.ToUInt16(byteArray, 0);
            height = BitConverter.ToUInt16(byteArray, 2);
            offsetX = BitConverter.ToUInt16(byteArray, 4);
            offsetY = BitConverter.ToUInt16(byteArray, 6);
            imagePart = byteArray[8];
            subParts = byteArray[9];
            tileOffset = BitConverter.ToUInt16(byteArray, 10);
            direction = byteArray[12];
            horizontalOffsetOfImage = byteArray[13];
            buildingWidth = byteArray[14];
            animatedColor = byteArray[15];
        }

        #endregion

        #region GetterSetter

        public ushort Width { get => width; set => width = value; }
        public ushort Height { get => height; set => height = value; }

        public ushort OffsetX { get => offsetX; set => offsetX = value; }
        public ushort OffsetY { get => offsetY; set => offsetY = value; }

        /// <summary>
        /// 0 denotes the start of a new collection
        /// </summary>
        public byte ImagePart { get => imagePart; set => imagePart = value; }

        /// <summary>
        /// Number of following parts in the collection
        /// </summary>
        public byte SubParts { get => subParts; set => subParts = value; }
        
        /// <summary>
        /// Vertical offset of the tile object on large surface
        /// </summary>
        public UInt16 TileOffset { get => tileOffset; set => tileOffset = value; }

        /// <summary>
        /// left, right, center... used for buildings only. 
        /// </summary>
        public byte Direction { get => direction; set => direction = value; }

        /// <summary>
        /// Initial horizontal offset of image. 
        /// </summary>
        public byte HorizontalOffsetOfImage { get => horizontalOffsetOfImage; set => horizontalOffsetOfImage = value; }

        /// <summary>
        /// Width of building part.
        /// </summary>
        public byte BuildingWidth { get => buildingWidth; set => buildingWidth = value; }

        /// <summary>
        /// Color, used for animated units only. 
        /// </summary>
        public byte AnimatedColor { get => animatedColor; set => animatedColor = value; }
        
        #endregion

        #region Methods

        /// <summary>
        /// Convert the Imageheader back to byte Array
        /// </summary>
        /// <returns></returns>
        internal byte[] GetBytes()
        {
            List<byte> array = new List<byte>();

            array.AddRange(BitConverter.GetBytes(width));
            array.AddRange(BitConverter.GetBytes(height));
            array.AddRange(BitConverter.GetBytes(offsetX));
            array.AddRange(BitConverter.GetBytes(offsetY));
            array.Add(imagePart);
            array.Add(subParts);
            array.AddRange(BitConverter.GetBytes(tileOffset));
            array.Add(direction);
            array.Add(horizontalOffsetOfImage);
            array.Add(buildingWidth);
            array.Add(animatedColor);

            return array.ToArray();
        }

        #endregion
    }
}

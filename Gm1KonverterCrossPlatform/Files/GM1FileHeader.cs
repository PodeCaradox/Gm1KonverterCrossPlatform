using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
    public class GM1FileHeader
    {

        #region Public

        public readonly static int fileHeaderSize = 88;

        /// <summary>
        ///  Data type is and ID that represents what kind of images are stored, they are as follows:
        ///1 – Interface items and some building animations.Images are stored similar to TGX images. 
        ///2 – Animations.
        ///3 – Buildings.Images are stored similar to TGX images but with a Tile object. 
        ///4 – Font.TGX format. 
        ///5 and 7 – Walls, grass, stones and other.No compression, stored with 2-bytes per pixel.
        /// </summary>
        public enum DataType : UInt32 { Interface = 1, Animations = 2, TilesObject = 3, Font = 4, NOCompression = 5, TGXConstSize = 6, NOCompression1 = 7 };

        #endregion
        
        #region Variables

        private String name;
        private UInt32 iUnknown1;
        private UInt32 iUnknown2;
        private UInt32 iUnknown3;
        private UInt32 iNumberOfPictureinFile;
        private UInt32 iUnknown4;
        private UInt32 iDataType;
        private UInt32[] iUnknown5 = new UInt32[14];

        internal byte[] GetBytes()
        {
            var bytearray = new List<byte>();
            bytearray.AddRange(BitConverter.GetBytes(iUnknown1));
            bytearray.AddRange(BitConverter.GetBytes(iUnknown2));
            bytearray.AddRange(BitConverter.GetBytes(iUnknown3));
            bytearray.AddRange(BitConverter.GetBytes(iNumberOfPictureinFile));
            bytearray.AddRange(BitConverter.GetBytes(iUnknown4));
            bytearray.AddRange(BitConverter.GetBytes(iDataType));
            for (int i = 0; i < iUnknown5.Length; i++)
            {
                bytearray.AddRange(BitConverter.GetBytes(this.iUnknown5[i]));
            }
            bytearray.AddRange(BitConverter.GetBytes(iDataSize));
            bytearray.AddRange(BitConverter.GetBytes(iUnknown6));

            return bytearray.ToArray();
        }

        private UInt32 iDataSize;
        private UInt32 iUnknown6;
        private UInt32[] size;

        #endregion

        #region Construtor

        /// <summary>
        /// The header has a length of 88-bytes, composed of unsigned 32-bit integers: 
        /// </summary>
        /// <param name="array"></param>
        public GM1FileHeader(byte[] array)
        {
            this.iUnknown1 = BitConverter.ToUInt32(array, 0);
            this.iUnknown2 = BitConverter.ToUInt32(array, 4);
            this.iUnknown3 = BitConverter.ToUInt32(array, 8);
            this.iNumberOfPictureinFile = BitConverter.ToUInt32(array, 12);
            this.iUnknown4 = BitConverter.ToUInt32(array, 16);
            this.iDataType = BitConverter.ToUInt32(array, 20);
            for (int i = 0; i < iUnknown5.Length; i++)
            {
                this.iUnknown5[i] = BitConverter.ToUInt32(array, 24 + i * 4);
            }
            this.size = new UInt32[2];
            this.size[0] = iUnknown5[6];
            this.size[1] = iUnknown5[7];

            this.iDataSize = BitConverter.ToUInt32(array, 80);
            this.iUnknown6 = BitConverter.ToUInt32(array, 84);
        }

        #endregion

        #region GetterSetter

        public UInt32 IUnknown1 { get => iUnknown1; }
        public UInt32 IUnknown2 { get => iUnknown2; }
        public UInt32 IUnknown3 { get => iUnknown3; }
        public UInt32 INumberOfPictureinFile { get => iNumberOfPictureinFile; set => iNumberOfPictureinFile = value; }
        public UInt32 IUnknown4 { get => iUnknown4; }
        public UInt32 IDataType { get => iDataType; }
        public UInt32[] IUnknown5 { get => iUnknown5; }
        public UInt32 IDataSize { get => iDataSize; set => iDataSize = value; }
        public UInt32 IUnknown6 { get => iUnknown6; }
        public UInt32[] Size { get => size; }
        public string Name { get => name; set => name = value; }

        #endregion

    }
}

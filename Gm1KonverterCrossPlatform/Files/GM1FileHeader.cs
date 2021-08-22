using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
    public class GM1FileHeader
    {
        #region Public Variables

        /// <summary>
        /// The header has a length of 88-bytes, composed of 22 unsigned 32-bit integers. 
        /// </summary>
        public const int ByteSize = 88;

        /// <summary>
        /// Data type is and ID that represents what kind of images are stored, they are as follows:
        /// <para>1 – Interface items and some building animations. Images are stored similar to TGX images.</para>
        /// <para>2 – Animations.</para>
        /// <para>3 – Buildings. Images are stored similar to TGX images but with a Tile object.</para>
        /// <para>4 – Font. TGX format.</para>
        /// <para>5 and 7 – Walls, grass, stones and other. No compression, stored with 2-bytes per pixel.</para>
        /// </summary>
        public enum DataType : uint { Interface = 1, Animations = 2, TilesObject = 3, Font = 4, NOCompression = 5, TGXConstSize = 6, NOCompression1 = 7 };

        #endregion
        
        #region Private Variables

        private string name;

        private uint iUnknown1;
        private uint iUnknown2;
        private uint iUnknown3;
        private uint iNumberOfPictureinFile;
        private uint iUnknown4;
        private uint iDataType;
        private uint[] iUnknown5 = new uint[14];
        private uint iDataSize;
        private uint iUnknown6;
        
        private uint[] size;

		#endregion

		#region Methods

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

        #endregion

        #region Construtor

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
            this.size = new uint[2];
            this.size[0] = iUnknown5[6];
            this.size[1] = iUnknown5[7];

            this.iDataSize = BitConverter.ToUInt32(array, 80);
            this.iUnknown6 = BitConverter.ToUInt32(array, 84);
        }

        #endregion

        #region GetterSetter

        public uint IUnknown1 { get => iUnknown1; }
        public uint IUnknown2 { get => iUnknown2; }
        public uint IUnknown3 { get => iUnknown3; }
        public uint INumberOfPictureinFile { get => iNumberOfPictureinFile; set => iNumberOfPictureinFile = value; }
        public uint IUnknown4 { get => iUnknown4; }
        public uint IDataType { get => iDataType; }
        public uint[] IUnknown5 { get => iUnknown5; }
        public uint IDataSize { get => iDataSize; set => iDataSize = value; }
        public uint IUnknown6 { get => iUnknown6; }
        public uint[] Size { get => size; }
        public string Name { get => name; set => name = value; }

        #endregion
    }
}

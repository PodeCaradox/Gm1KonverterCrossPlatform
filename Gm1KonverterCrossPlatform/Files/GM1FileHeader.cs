using System;
using System.Collections.Generic;

namespace Gm1KonverterCrossPlatform.Files
{
    public class GM1FileHeader
    {
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
        public enum DataType : uint {
            Interface = 1,
            Animations = 2,
            TilesObject = 3,
            Font = 4,
            NOCompression = 5,
            TGXConstSize = 6,
            NOCompression1 = 7
        };

        private string name;

        private uint unknownField1;
        private uint unknownField2;
        private uint unknownField3;
        private uint numberOfPicturesinFile;
        private uint unknownField5;
        private uint dataType;
        private uint unknownField7;
        private uint unknownField8;
        private uint unknownField9;
        private uint unknownField10;
        private uint unknownField11;
        private uint unknownField12;
        private uint width;
        private uint height;
        private uint unknownField15;
        private uint unknownField16;
        private uint unknownField17;
        private uint unknownField18;
        private uint originX;
        private uint originY;
        private uint dataSize;
        private uint unknownField22;

        public GM1FileHeader(byte[] byteArray)
        {
            unknownField1 = BitConverter.ToUInt32(byteArray, 0);
            unknownField2 = BitConverter.ToUInt32(byteArray, 4);
            unknownField3 = BitConverter.ToUInt32(byteArray, 8);
            numberOfPicturesinFile = BitConverter.ToUInt32(byteArray, 12);
            unknownField5 = BitConverter.ToUInt32(byteArray, 16);
            dataType = BitConverter.ToUInt32(byteArray, 20);
            unknownField7 = BitConverter.ToUInt32(byteArray, 24);
            unknownField8 = BitConverter.ToUInt32(byteArray, 28);
            unknownField9 = BitConverter.ToUInt32(byteArray, 32);
            unknownField10 = BitConverter.ToUInt32(byteArray, 36);
            unknownField11 = BitConverter.ToUInt32(byteArray, 40);
            unknownField12 = BitConverter.ToUInt32(byteArray, 44);
            width = BitConverter.ToUInt32(byteArray, 48);
            height = BitConverter.ToUInt32(byteArray, 52);
            unknownField15 = BitConverter.ToUInt32(byteArray, 56);
            unknownField16 = BitConverter.ToUInt32(byteArray, 60);
            unknownField17 = BitConverter.ToUInt32(byteArray, 64);
            unknownField18 = BitConverter.ToUInt32(byteArray, 68);
            originX = BitConverter.ToUInt32(byteArray, 72);
            originY = BitConverter.ToUInt32(byteArray, 76);
            dataSize = BitConverter.ToUInt32(byteArray, 80);
            unknownField22 = BitConverter.ToUInt32(byteArray, 84);
        }

        public string Name { get => name; set => name = value; }

        public uint UnknownField1 { get => unknownField1; set => unknownField1 = value; }
        public uint UnknownField2 { get => unknownField2; set => unknownField2 = value; }
        public uint UnknownField3 { get => unknownField3; set => unknownField3 = value; }
        public uint INumberOfPictureinFile { get => numberOfPicturesinFile; set => numberOfPicturesinFile = value; }
        public uint UnknownField5 { get => unknownField5; set => unknownField5 = value; }
        public uint IDataType { get => dataType; set => dataType = value; }
        public uint UnknownField7 { get => unknownField7; set => unknownField7 = value; }
        public uint UnknownField8 { get => unknownField8; set => unknownField8 = value; }
        public uint UnknownField9 { get => unknownField9; set => unknownField9 = value; }
        public uint UnknownField10 { get => unknownField10; set => unknownField10 = value; }
        public uint UnknownField11 { get => unknownField11; set => unknownField11 = value; }
        public uint UnknownField12 { get => unknownField12; set => unknownField12 = value; }
        public uint Width { get => width; set => width = value; }
        public uint Height { get => height; set => height = value; }
        public uint UnknownField15 { get => unknownField15; set => unknownField15 = value; }
        public uint UnknownField16 { get => unknownField16; set => unknownField16 = value; }
        public uint UnknownField17 { get => unknownField17; set => unknownField17 = value; }
        public uint UnknownField18 { get => unknownField18; set => unknownField18 = value; }
        public uint OriginX { get => originX; set => originX = value; }
        public uint OriginY { get => originY; set => originY = value; }
        public uint IDataSize { get => dataSize; set => dataSize = value; }
        public uint UnknownField22 { get => unknownField22; set => unknownField22 = value; }

        internal byte[] GetBytes()
        {
            List<byte> byteArray = new List<byte>();

            byteArray.AddRange(BitConverter.GetBytes(unknownField1));
            byteArray.AddRange(BitConverter.GetBytes(unknownField2));
            byteArray.AddRange(BitConverter.GetBytes(unknownField3));
            byteArray.AddRange(BitConverter.GetBytes(numberOfPicturesinFile));
            byteArray.AddRange(BitConverter.GetBytes(unknownField5));
            byteArray.AddRange(BitConverter.GetBytes(dataType));
            byteArray.AddRange(BitConverter.GetBytes(unknownField7));
            byteArray.AddRange(BitConverter.GetBytes(unknownField8));
            byteArray.AddRange(BitConverter.GetBytes(unknownField9));
            byteArray.AddRange(BitConverter.GetBytes(unknownField10));
            byteArray.AddRange(BitConverter.GetBytes(unknownField11));
            byteArray.AddRange(BitConverter.GetBytes(unknownField12));
            byteArray.AddRange(BitConverter.GetBytes(width));
            byteArray.AddRange(BitConverter.GetBytes(height));
            byteArray.AddRange(BitConverter.GetBytes(unknownField15));
            byteArray.AddRange(BitConverter.GetBytes(unknownField16));
            byteArray.AddRange(BitConverter.GetBytes(unknownField17));
            byteArray.AddRange(BitConverter.GetBytes(unknownField18));
            byteArray.AddRange(BitConverter.GetBytes(OriginX));
            byteArray.AddRange(BitConverter.GetBytes(OriginY));
            byteArray.AddRange(BitConverter.GetBytes(dataSize));
            byteArray.AddRange(BitConverter.GetBytes(unknownField22));

            return byteArray.ToArray();
        }
    }
}

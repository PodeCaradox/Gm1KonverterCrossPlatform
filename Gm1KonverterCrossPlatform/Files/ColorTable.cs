using System;
using System.Collections.Generic;

namespace Gm1KonverterCrossPlatform.Files
{
	public class ColorTable
	{
        /// <summary>
        /// Color table is always 512 bytes in size, consisting of 256 2-byte colors.
        /// </summary>
        public const int ByteSize = 512;

        /// <summary>
        /// Color table always contains 256 colors.
        /// </summary>
        public const int ColorCount = 256;

        private ushort[] _colorList;

        public ColorTable(byte[] byteArray)
        {
            _colorList = new ushort[ColorCount];

            for (int colorIndex = 0; colorIndex < ColorCount; colorIndex++)
            {
                _colorList[colorIndex] = BitConverter.ToUInt16(byteArray, colorIndex * 2);
            }
        }

        public ColorTable(ushort[] ushortArray)
        {
            if (ushortArray.Length != ColorCount) {
                throw new ArgumentException($"Invalid input length ({ushortArray.Length}). The length must be {ColorCount}.");
            }

            _colorList = ushortArray;
        }

        public ushort[] ColorList { get => _colorList; set => _colorList = value; }

        /// <summary>
        /// Calculate the new byte array to save color table to a .gm1 file
        /// </summary>
        public byte[] GetBytes()
        {
            List<byte> byteArray = new List<byte>();

            for (int i = 0; i < _colorList.Length; i++)
            {
                byteArray.AddRange(BitConverter.GetBytes(_colorList[i]));
            }

            return byteArray.ToArray();
        }

        /// <summary>
        /// Create a copy of this color table
        /// </summary>
        public ColorTable Copy()
        {
            return new ColorTable(_colorList);
        }
    }
}

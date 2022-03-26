using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using System;
using System.Collections.Generic;

namespace Files.Gm1Converter
{
	public class ColorTable
	{
        #region Public

        /// <summary>
        /// Color table is always 512 bytes in size, consisting of 256 2-byte colors.
        /// </summary>
        public const int ByteSize = 512;

        /// <summary>
        /// Color table always contains 256 colors.
        /// </summary>
        public const int ColorCount = 256;

        #endregion

        #region Variables

        private ushort[] colorList = new ushort[ColorCount];

        #endregion

        #region Construtor

        public ColorTable(byte[] byteArray)
        {
            for (int colorIndex = 0; colorIndex < ColorCount; colorIndex++)
            {
                colorList[colorIndex] = BitConverter.ToUInt16(byteArray, colorIndex * 2);
            }
        }

        public ColorTable(ushort[] ushortArray)
        {
            if (ushortArray.Length != ColorCount) {
                throw new ArgumentException($"Invalid input length ({ushortArray.Length}). The length must be {ColorCount}.");
            }

            colorList = ushortArray;
        }

        #endregion

        #region GetterSetter

        public ushort[] ColorList { get => colorList; set => colorList = value; }

        #endregion

        #region Methods

        /// <summary>
        /// Calculate the new byte array to save color table to a .gm1 file
        /// </summary>
        internal byte[] GetBytes()
        {
            List<byte> byteArray = new List<byte>();

            for (int i = 0; i < colorList.Length; i++)
            {
                byteArray.AddRange(BitConverter.GetBytes(colorList[i]));
            }

            return byteArray.ToArray();
        }

        /// <summary>
        /// Create a copy of this color table
        /// </summary>
        public ColorTable Copy()
        {
            return new ColorTable(colorList);
        }

        #endregion
    }
}

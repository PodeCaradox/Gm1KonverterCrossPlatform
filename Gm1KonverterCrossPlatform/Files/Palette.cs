using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using Gm1KonverterCrossPlatform.HelperClasses;

namespace Files.Gm1Converter
{
    public class Palette
    {
        /// <summary>
        /// The palette is always 5120 bytes in size.
        /// The palette consist of 10 colortables, each being 512 bytes in size
        /// and consisting of 256 2-byte colors.
        /// </summary>
        public const int ByteSize = 5120;

        /// <summary>
        /// A file always contains 10 colortables.
        /// </summary>
        public const int ColorTableCount = 10;

        public readonly static int pixelSize = 10;
        public readonly static ushort width = 32;
        public readonly static ushort height = 8;

        private ColorTable[] _colorTables = new ColorTable[ColorTableCount];
        private int _actualPalette = 0;

        /// <summary>
        /// The palette consist of 10 colortables, each consisting of 256 colors, and is used in Animation files.
        /// </summary>
        /// <param name="byteArray">The palette byte array</param>
        public Palette(byte[] byteArray)
        {
            for (int i = 0; i < ColorTableCount; i++)
            {
                byte[] colorTableByteArray = new byte[ColorTable.ByteSize];
                Buffer.BlockCopy(byteArray, i * ColorTable.ByteSize, colorTableByteArray, 0, ColorTable.ByteSize);
                _colorTables[i] = new ColorTable(colorTableByteArray);
            }
        }

        public ColorTable[] ColorTables { get => _colorTables; set => _colorTables = value; }
        public int ActualPalette { get => _actualPalette; set => _actualPalette = value; }

        /// <summary>
        /// Calculate the new ByteArray to save ColorTables
        /// </summary>
        internal byte[] GetBytes()
        {
            byte[] byteArray = new byte[ColorTable.ByteSize * ColorTableCount];

            for (int i = 0; i < ColorTableCount; i++)
            {
                Array.Copy(_colorTables[i].GetBytes(), byteArray, ColorTable.ByteSize);
            }

            return byteArray;
        }
    }
}

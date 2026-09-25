using System;

namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// The 5120 byte block after the GM1 header: 10 color tables with 256 colors each.
    /// Only animation files use it, but it is kept for every file type so that files are written back unchanged.
    /// </summary>
    public sealed class Palette
    {
        public const int ByteSize = ColorTableCount * ColorTable.ByteSize;

        public const int ColorTableCount = 10;

        /// <summary>Color tables are exported as images with 32 x 8 cells.</summary>
        public const int ImageColumns = 32;

        public const int ImageRows = 8;

        /// <summary>Size of one color cell in exported color table images.</summary>
        public const int ImageCellSize = 10;

        private Palette(ColorTable[] colorTables)
        {
            ColorTables = colorTables;
        }

        public ColorTable[] ColorTables { get; }

        public static Palette Read(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < ByteSize) throw new ArgumentException($"A palette needs {ByteSize} bytes.", nameof(bytes));

            var tables = new ColorTable[ColorTableCount];
            for (int i = 0; i < ColorTableCount; i++)
            {
                tables[i] = ColorTable.Read(bytes.Slice(i * ColorTable.ByteSize));
            }

            return new Palette(tables);
        }

        public static Palette CreateEmpty() => Read(new byte[ByteSize]);

        public void Write(Span<byte> destination)
        {
            for (int i = 0; i < ColorTableCount; i++)
            {
                ColorTables[i].Write(destination.Slice(i * ColorTable.ByteSize));
            }
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[ByteSize];
            Write(bytes);
            return bytes;
        }
    }
}

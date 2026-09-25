using System;
using System.Buffers.Binary;

namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// 256 ARGB1555 colors (512 bytes). Animation images store indices into a color table.
    /// </summary>
    public sealed class ColorTable
    {
        public const int ByteSize = ColorCount * sizeof(ushort);

        public const int ColorCount = 256;

        public ColorTable(ushort[] colors)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            if (colors.Length != ColorCount)
            {
                throw new ArgumentException($"Invalid input length ({colors.Length}). The length must be {ColorCount}.", nameof(colors));
            }

            Colors = colors;
        }

        public ushort[] Colors { get; }

        public ushort this[int index]
        {
            get => Colors[index];
            set => Colors[index] = value;
        }

        public static ColorTable Read(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < ByteSize) throw new ArgumentException($"A color table needs {ByteSize} bytes.", nameof(bytes));

            var colors = new ushort[ColorCount];
            for (int i = 0; i < ColorCount; i++)
            {
                colors[i] = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(i * sizeof(ushort)));
            }

            return new ColorTable(colors);
        }

        public void Write(Span<byte> destination)
        {
            if (destination.Length < ByteSize) throw new ArgumentException("Destination is too small.", nameof(destination));

            for (int i = 0; i < ColorCount; i++)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(i * sizeof(ushort)), Colors[i]);
            }
        }

        /// <summary>Creates an independent copy; changes to the copy do not affect this table.</summary>
        public ColorTable Copy() => new ColorTable((ushort[])Colors.Clone());
    }
}

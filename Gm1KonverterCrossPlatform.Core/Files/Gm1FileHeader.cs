using System;
using System.Buffers.Binary;

namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// The 88 byte header at the start of every .gm1 file, composed of 22 unsigned 32 bit integers.
    /// Unknown fields are kept so that a file can be written back unchanged.
    /// </summary>
    public sealed class Gm1FileHeader
    {
        public const int ByteSize = 88;

        private const int FieldCount = ByteSize / sizeof(uint);

        private readonly uint[] fields = new uint[FieldCount];

        private Gm1FileHeader()
        {
        }

        public uint UnknownField1 { get => fields[0]; set => fields[0] = value; }
        public uint UnknownField2 { get => fields[1]; set => fields[1] = value; }
        public uint UnknownField3 { get => fields[2]; set => fields[2] = value; }

        /// <summary>Number of images stored in the file.</summary>
        public uint ImageCount { get => fields[3]; set => fields[3] = value; }

        public uint UnknownField5 { get => fields[4]; set => fields[4] = value; }

        public Gm1DataType DataType { get => (Gm1DataType)fields[5]; set => fields[5] = (uint)value; }

        public uint UnknownField7 { get => fields[6]; set => fields[6] = value; }
        public uint UnknownField8 { get => fields[7]; set => fields[7] = value; }
        public uint UnknownField9 { get => fields[8]; set => fields[8] = value; }
        public uint UnknownField10 { get => fields[9]; set => fields[9] = value; }
        public uint UnknownField11 { get => fields[10]; set => fields[10] = value; }
        public uint UnknownField12 { get => fields[11]; set => fields[11] = value; }
        public uint Width { get => fields[12]; set => fields[12] = value; }
        public uint Height { get => fields[13]; set => fields[13] = value; }
        public uint UnknownField15 { get => fields[14]; set => fields[14] = value; }
        public uint UnknownField16 { get => fields[15]; set => fields[15] = value; }
        public uint UnknownField17 { get => fields[16]; set => fields[16] = value; }
        public uint UnknownField18 { get => fields[17]; set => fields[17] = value; }
        public uint OriginX { get => fields[18]; set => fields[18] = value; }
        public uint OriginY { get => fields[19]; set => fields[19] = value; }

        /// <summary>Size of the image data block in bytes.</summary>
        public uint DataSize { get => fields[20]; set => fields[20] = value; }

        public uint UnknownField22 { get => fields[21]; set => fields[21] = value; }

        public static Gm1FileHeader Read(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < ByteSize)
            {
                throw new ArgumentException($"A GM1 header needs {ByteSize} bytes but only {bytes.Length} are available.", nameof(bytes));
            }

            var header = new Gm1FileHeader();
            for (int i = 0; i < FieldCount; i++)
            {
                header.fields[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(i * sizeof(uint)));
            }

            return header;
        }

        public void Write(Span<byte> destination)
        {
            if (destination.Length < ByteSize) throw new ArgumentException("Destination is too small.", nameof(destination));

            for (int i = 0; i < FieldCount; i++)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(i * sizeof(uint)), fields[i]);
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

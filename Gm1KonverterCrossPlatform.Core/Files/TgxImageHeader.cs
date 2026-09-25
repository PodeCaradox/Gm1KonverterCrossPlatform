using System;
using System.Buffers.Binary;

namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// The 16 byte header stored for every image inside a .gm1 file.
    /// </summary>
    public sealed class TgxImageHeader
    {
        public const int ByteSize = 16;

        public ushort Width { get; set; }

        public ushort Height { get; set; }

        public ushort OffsetX { get; set; }

        public ushort OffsetY { get; set; }

        /// <summary>Index of the part inside a tile object, 0 starts a new building.</summary>
        public byte ImagePart { get; set; }

        /// <summary>Number of parts of the tile object this part belongs to.</summary>
        public byte SubParts { get; set; }

        /// <summary>Vertical offset of the image on top of a tile.</summary>
        public ushort TileOffset { get; set; }

        /// <summary>Left, right, center... used for buildings only.</summary>
        public byte Direction { get; set; }

        /// <summary>Initial horizontal offset of the image.</summary>
        public byte HorizontalOffsetOfImage { get; set; }

        /// <summary>Width of the building part.</summary>
        public byte BuildingWidth { get; set; }

        /// <summary>Color flag, used for animated units only. 0 forces the alpha bit of every color.</summary>
        public byte AnimatedColor { get; set; }

        public static TgxImageHeader Read(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < ByteSize) throw new ArgumentException($"An image header needs {ByteSize} bytes.", nameof(bytes));

            return new TgxImageHeader
            {
                Width = BinaryPrimitives.ReadUInt16LittleEndian(bytes),
                Height = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(2)),
                OffsetX = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(4)),
                OffsetY = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(6)),
                ImagePart = bytes[8],
                SubParts = bytes[9],
                TileOffset = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(10)),
                Direction = bytes[12],
                HorizontalOffsetOfImage = bytes[13],
                BuildingWidth = bytes[14],
                AnimatedColor = bytes[15]
            };
        }

        public void Write(Span<byte> destination)
        {
            if (destination.Length < ByteSize) throw new ArgumentException("Destination is too small.", nameof(destination));

            BinaryPrimitives.WriteUInt16LittleEndian(destination, Width);
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(2), Height);
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(4), OffsetX);
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(6), OffsetY);
            destination[8] = ImagePart;
            destination[9] = SubParts;
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(10), TileOffset);
            destination[12] = Direction;
            destination[13] = HorizontalOffsetOfImage;
            destination[14] = BuildingWidth;
            destination[15] = AnimatedColor;
        }

        public TgxImageHeader Copy() => (TgxImageHeader)MemberwiseClone();
    }
}

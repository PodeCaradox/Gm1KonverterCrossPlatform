using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// A .gm1 file. Layout:
    /// <list type="number">
    /// <item>header (88 bytes)</item>
    /// <item>palette (5120 bytes)</item>
    /// <item>offset of every image relative to the data block (4 bytes each)</item>
    /// <item>size of every image (4 bytes each)</item>
    /// <item>image headers (16 bytes each)</item>
    /// <item>data block with the encoded images</item>
    /// </list>
    /// </summary>
    public sealed class Gm1File
    {
        private const int OffsetEntrySize = sizeof(uint);
        private const int SizeEntrySize = sizeof(uint);
        private const int BytesPerImageInTables = OffsetEntrySize + SizeEntrySize + TgxImageHeader.ByteSize;

        private Gm1File(Gm1FileHeader header, Palette palette, List<Gm1Image> images)
        {
            Header = header;
            Palette = palette;
            Images = images;
        }

        public Gm1FileHeader Header { get; }

        public Palette Palette { get; }

        /// <summary>The images in file order. <see cref="ToBytes"/> updates the header to match this list.</summary>
        public List<Gm1Image> Images { get; }

        public Gm1DataType DataType => Header.DataType;

        /// <summary>
        /// Parses a .gm1 file.
        /// </summary>
        /// <exception cref="InvalidDataException">The data is not a valid .gm1 file.</exception>
        public static Gm1File Read(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            int tablesStart = Gm1FileHeader.ByteSize + Palette.ByteSize;
            if (bytes.Length < tablesStart)
            {
                throw new InvalidDataException($"The file is too small to be a GM1 file ({bytes.Length} bytes).");
            }

            var header = Gm1FileHeader.Read(bytes);
            var palette = Palette.Read(bytes.AsSpan(Gm1FileHeader.ByteSize));

            long imageCount = header.ImageCount;
            long dataStart = tablesStart + imageCount * BytesPerImageInTables;
            if (dataStart > bytes.Length)
            {
                throw new InvalidDataException($"The file claims to contain {imageCount} images but is too small for their tables.");
            }

            int count = (int)imageCount;
            int offsetsStart = tablesStart;
            int sizesStart = offsetsStart + count * OffsetEntrySize;
            int headersStart = sizesStart + count * SizeEntrySize;

            var images = new List<Gm1Image>(count);
            for (int i = 0; i < count; i++)
            {
                uint offset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offsetsStart + i * OffsetEntrySize));
                uint size = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sizesStart + i * SizeEntrySize));
                var imageHeader = TgxImageHeader.Read(bytes.AsSpan(headersStart + i * TgxImageHeader.ByteSize));

                long start = dataStart + offset;
                if (start + size > bytes.Length)
                {
                    throw new InvalidDataException($"Image {i + 1} lies outside of the file (offset {offset}, size {size}).");
                }

                byte[] data = bytes.AsSpan((int)start, (int)size).ToArray();
                images.Add(new Gm1Image(imageHeader, data));
            }

            return new Gm1File(header, palette, images);
        }

        /// <summary>
        /// Recalculates the image count and the data size in the header from <see cref="Images"/>.
        /// </summary>
        public void UpdateHeader()
        {
            long dataSize = 0;
            foreach (var image in Images)
            {
                dataSize += image.Data.Length;
            }

            Header.ImageCount = (uint)Images.Count;
            Header.DataSize = checked((uint)dataSize);
        }

        /// <summary>
        /// Serializes the file. The images are stored one after another in list order,
        /// offsets, sizes, image count and data size are recalculated.
        /// </summary>
        public byte[] ToBytes()
        {
            UpdateHeader();

            int count = Images.Count;
            int tablesStart = Gm1FileHeader.ByteSize + Palette.ByteSize;
            int offsetsStart = tablesStart;
            int sizesStart = offsetsStart + count * OffsetEntrySize;
            int headersStart = sizesStart + count * SizeEntrySize;
            int dataStart = tablesStart + count * BytesPerImageInTables;

            var bytes = new byte[checked(dataStart + (int)Header.DataSize)];
            Header.Write(bytes);
            Palette.Write(bytes.AsSpan(Gm1FileHeader.ByteSize));

            uint offset = 0;
            for (int i = 0; i < count; i++)
            {
                var image = Images[i];
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offsetsStart + i * OffsetEntrySize), offset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(sizesStart + i * SizeEntrySize), (uint)image.Data.Length);
                image.Header.Write(bytes.AsSpan(headersStart + i * TgxImageHeader.ByteSize));
                image.Data.CopyTo(bytes, dataStart + (int)offset);
                offset += (uint)image.Data.Length;
            }

            return bytes;
        }
    }
}

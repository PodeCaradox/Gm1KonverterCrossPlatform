using System;
using System.Buffers.Binary;
using System.IO;

namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// A standalone .tgx image: width and height as 32 bit integers followed by the RLE encoded pixels.
    /// </summary>
    public sealed class TgxFile
    {
        public const int HeaderSize = 2 * sizeof(uint);

        public TgxFile(uint width, uint height, byte[] data)
        {
            Width = width;
            Height = height;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public uint Width { get; set; }

        public uint Height { get; set; }

        public byte[] Data { get; set; }

        /// <exception cref="InvalidDataException">The data is not a valid .tgx file.</exception>
        public static TgxFile Read(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length < HeaderSize)
            {
                throw new InvalidDataException($"The file is too small to be a TGX file ({bytes.Length} bytes).");
            }

            uint width = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            uint height = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(sizeof(uint)));
            if (width > int.MaxValue || height > int.MaxValue || (ulong)width * height > int.MaxValue)
            {
                throw new InvalidDataException($"Invalid TGX image size {width} x {height}.");
            }

            return new TgxFile(width, height, bytes.AsSpan(HeaderSize).ToArray());
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[HeaderSize + Data.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, Width);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(sizeof(uint)), Height);
            Data.CopyTo(bytes, HeaderSize);
            return bytes;
        }
    }
}

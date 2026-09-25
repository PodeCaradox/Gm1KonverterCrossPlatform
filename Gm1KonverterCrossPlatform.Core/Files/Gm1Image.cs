using System;

namespace Gm1KonverterCrossPlatform.Core.Files
{
    /// <summary>
    /// One image stored in a .gm1 file: its header and its encoded data.
    /// </summary>
    public sealed class Gm1Image
    {
        public Gm1Image(TgxImageHeader header, byte[] data)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public TgxImageHeader Header { get; }

        /// <summary>Encoded image data, the format depends on the <see cref="Gm1DataType"/> of the file.</summary>
        public byte[] Data { get; set; }
    }
}

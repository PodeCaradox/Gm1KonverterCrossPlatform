using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Core.Documents
{
    /// <summary>An opened standalone .tgx file.</summary>
    public sealed class TgxDocument
    {
        public TgxDocument(string fileName, TgxFile file)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("A file name is required.", nameof(fileName));

            FileName = fileName;
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public string FileName { get; }

        public TgxFile File { get; }

        /// <exception cref="InvalidDataException">The file is not a valid .tgx file.</exception>
        public static TgxDocument Load(string path)
        {
            return new TgxDocument(Path.GetFileName(path), TgxFile.Read(System.IO.File.ReadAllBytes(path)));
        }

        public Argb1555Image Render() => TgxCodec.Decode(File.Data, (int)File.Width, (int)File.Height);

        public void Replace(Argb1555Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            File.Data = TgxCodec.Encode(image, TgxEncoderOptions.ForTgxFile());
            File.Width = (uint)image.Width;
            File.Height = (uint)image.Height;
        }

        public byte[] ToBytes() => File.ToBytes();
    }
}

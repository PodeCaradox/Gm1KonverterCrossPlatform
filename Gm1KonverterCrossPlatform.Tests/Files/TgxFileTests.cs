using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Files
{
    public class TgxFileTests
    {
        [Fact]
        public void ReadThenToBytes_ReturnsIdenticalBytes()
        {
            var bytes = CreateTgxBytes(new Random(5), 53, 21);

            var file = TgxFile.Read(bytes);

            Assert.Equal(bytes, file.ToBytes());
        }

        [Fact]
        public void Read_ReadsSizeAndData()
        {
            var bytes = CreateTgxBytes(new Random(6), 300, 2);

            var file = TgxFile.Read(bytes);

            Assert.Equal(300u, file.Width);
            Assert.Equal(2u, file.Height);
            Assert.Equal(bytes.Skip(TgxFile.HeaderSize), file.Data);
        }

        [Fact]
        public void Read_HeaderWithoutData_IsAccepted()
        {
            var bytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

            var file = TgxFile.Read(bytes);

            Assert.Equal(0u, file.Width);
            Assert.Equal(0u, file.Height);
            Assert.Empty(file.Data);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(TgxFile.HeaderSize - 1)]
        public void Read_ShorterThanHeader_ThrowsInvalidDataException(int length)
        {
            Assert.Throws<InvalidDataException>(() => TgxFile.Read(new byte[length]));
        }

        [Theory]
        [InlineData(0x8000_0000u, 1u)]
        [InlineData(1u, 0x8000_0000u)]
        [InlineData(uint.MaxValue, uint.MaxValue)]
        [InlineData(65536u, 65536u)]
        [InlineData(46341u, 46341u)]
        public void Read_InvalidSize_ThrowsInvalidDataException(uint width, uint height)
        {
            var bytes = new byte[TgxFile.HeaderSize + 4];
            Gm1FileBuilder.WriteUInt32(bytes, 0, width);
            Gm1FileBuilder.WriteUInt32(bytes, sizeof(uint), height);

            Assert.Throws<InvalidDataException>(() => TgxFile.Read(bytes));
        }

        [Fact]
        public void Read_LargestValidSize_IsAccepted()
        {
            var bytes = new byte[TgxFile.HeaderSize];
            Gm1FileBuilder.WriteUInt32(bytes, 0, 46340);
            Gm1FileBuilder.WriteUInt32(bytes, sizeof(uint), 46340);

            var file = TgxFile.Read(bytes);

            Assert.Equal(46340u, file.Width);
        }

        [Fact]
        public void ToBytes_WritesChangedSizeAndData()
        {
            var file = TgxFile.Read(CreateTgxBytes(new Random(7), 10, 10));
            file.Width = 0x01020304;
            file.Height = 7;
            file.Data = new byte[] { 9, 8 };

            var bytes = file.ToBytes();

            Assert.Equal(new byte[] { 4, 3, 2, 1, 7, 0, 0, 0, 9, 8 }, bytes);
        }

        internal static byte[] CreateTgxBytes(Random random, int width, int height)
        {
            var image = TestImages.Random(random, width, height);
            return new TgxFile((uint)width, (uint)height, TgxCodec.Encode(image, TgxEncoderOptions.ForTgxFile())).ToBytes();
        }
    }
}

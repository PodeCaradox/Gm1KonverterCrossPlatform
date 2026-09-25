using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Files
{
    public class Gm1FileTests
    {
        /// <summary>Every header property in file order.</summary>
        private static readonly (string Name, Func<Gm1FileHeader, uint> Get, Action<Gm1FileHeader, uint> Set)[] HeaderProperties =
        {
            (nameof(Gm1FileHeader.UnknownField1), h => h.UnknownField1, (h, v) => h.UnknownField1 = v),
            (nameof(Gm1FileHeader.UnknownField2), h => h.UnknownField2, (h, v) => h.UnknownField2 = v),
            (nameof(Gm1FileHeader.UnknownField3), h => h.UnknownField3, (h, v) => h.UnknownField3 = v),
            (nameof(Gm1FileHeader.ImageCount), h => h.ImageCount, (h, v) => h.ImageCount = v),
            (nameof(Gm1FileHeader.UnknownField5), h => h.UnknownField5, (h, v) => h.UnknownField5 = v),
            (nameof(Gm1FileHeader.DataType), h => (uint)h.DataType, (h, v) => h.DataType = (Gm1DataType)v),
            (nameof(Gm1FileHeader.UnknownField7), h => h.UnknownField7, (h, v) => h.UnknownField7 = v),
            (nameof(Gm1FileHeader.UnknownField8), h => h.UnknownField8, (h, v) => h.UnknownField8 = v),
            (nameof(Gm1FileHeader.UnknownField9), h => h.UnknownField9, (h, v) => h.UnknownField9 = v),
            (nameof(Gm1FileHeader.UnknownField10), h => h.UnknownField10, (h, v) => h.UnknownField10 = v),
            (nameof(Gm1FileHeader.UnknownField11), h => h.UnknownField11, (h, v) => h.UnknownField11 = v),
            (nameof(Gm1FileHeader.UnknownField12), h => h.UnknownField12, (h, v) => h.UnknownField12 = v),
            (nameof(Gm1FileHeader.Width), h => h.Width, (h, v) => h.Width = v),
            (nameof(Gm1FileHeader.Height), h => h.Height, (h, v) => h.Height = v),
            (nameof(Gm1FileHeader.UnknownField15), h => h.UnknownField15, (h, v) => h.UnknownField15 = v),
            (nameof(Gm1FileHeader.UnknownField16), h => h.UnknownField16, (h, v) => h.UnknownField16 = v),
            (nameof(Gm1FileHeader.UnknownField17), h => h.UnknownField17, (h, v) => h.UnknownField17 = v),
            (nameof(Gm1FileHeader.UnknownField18), h => h.UnknownField18, (h, v) => h.UnknownField18 = v),
            (nameof(Gm1FileHeader.OriginX), h => h.OriginX, (h, v) => h.OriginX = v),
            (nameof(Gm1FileHeader.OriginY), h => h.OriginY, (h, v) => h.OriginY = v),
            (nameof(Gm1FileHeader.DataSize), h => h.DataSize, (h, v) => h.DataSize = v),
            (nameof(Gm1FileHeader.UnknownField22), h => h.UnknownField22, (h, v) => h.UnknownField22 = v),
        };

        public static IEnumerable<object[]> DataTypes => Gm1FileBuilder.AllDataTypes();

        public static IEnumerable<object[]> NonAnimationDataTypes =>
            Gm1FileBuilder.AllDataTypes().Where(row => (Gm1DataType)row[0] != Gm1DataType.Animations);

        [Theory]
        [MemberData(nameof(DataTypes))]
        public void ReadThenToBytes_ReturnsIdenticalBytes(Gm1DataType dataType)
        {
            for (int seed = 1; seed <= 3; seed++)
            {
                var original = Gm1FileBuilder.Create(dataType, seed).ToBytes();

                var file = Gm1File.Read(original);

                Assert.Equal(dataType, file.DataType);
                Assert.Equal(original, file.ToBytes());
            }
        }

        [Fact]
        public void ReadThenToBytes_FileWithoutImages_ReturnsIdenticalBytes()
        {
            var original = Gm1FileBuilder.CreateEmpty(Gm1DataType.Interface).ToBytes();

            var file = Gm1File.Read(original);

            Assert.Empty(file.Images);
            Assert.Equal(Gm1FileBuilder.TablesStart, original.Length);
            Assert.Equal(original, file.ToBytes());
        }

        [Fact]
        public void Read_ReadsEveryHeaderFieldAtItsPosition()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.Font);
            var bytes = builder.ToBytes();
            var expected = builder.ExpectedHeaderFields(builder.Images.Count, builder.Images.Sum(i => i.Data.Length));

            var header = Gm1File.Read(bytes).Header;

            Assert.Equal(Gm1FileBuilder.HeaderFieldCount, HeaderProperties.Length);
            for (int i = 0; i < HeaderProperties.Length; i++)
            {
                Assert.True(expected[i] == HeaderProperties[i].Get(header), $"{HeaderProperties[i].Name} (field {i}) was {HeaderProperties[i].Get(header)}, expected {expected[i]}");
            }

            // unknown fields are not zero, otherwise the test would prove nothing
            Assert.All(expected, value => Assert.NotEqual(0u, value));
        }

        [Fact]
        public void ToBytes_WritesEveryHeaderFieldAtItsPosition()
        {
            var file = Gm1File.Read(Gm1FileBuilder.Create(Gm1DataType.Interface).ToBytes());
            var recalculated = new[] { Gm1FileBuilder.ImageCountField, Gm1FileBuilder.DataTypeField, Gm1FileBuilder.DataSizeField };
            for (int i = 0; i < HeaderProperties.Length; i++)
            {
                if (!recalculated.Contains(i))
                {
                    HeaderProperties[i].Set(file.Header, 0xA000_0000u + (uint)i * 0x0101);
                }
            }

            var bytes = file.ToBytes();

            for (int i = 0; i < HeaderProperties.Length; i++)
            {
                if (!recalculated.Contains(i))
                {
                    Assert.True(0xA000_0000u + (uint)i * 0x0101 == Gm1FileBuilder.ReadUInt32(bytes, i * sizeof(uint)), $"{HeaderProperties[i].Name} (field {i}) was not written at byte {i * sizeof(uint)}");
                }
            }

            Assert.Equal(Gm1DataType.Interface, Gm1File.Read(bytes).DataType);
        }

        [Fact]
        public void HeaderToBytes_MatchesHeaderPartOfFile()
        {
            var bytes = Gm1FileBuilder.Create(Gm1DataType.NoCompression).ToBytes();

            var header = Gm1FileHeader.Read(bytes);

            Assert.Equal(bytes.Take(Gm1FileHeader.ByteSize), header.ToBytes());
        }

        [Theory]
        [MemberData(nameof(DataTypes))]
        public void Read_ReadsImageHeadersAndData(Gm1DataType dataType)
        {
            var builder = Gm1FileBuilder.Create(dataType);

            var file = Gm1File.Read(builder.ToBytes());

            Assert.Equal(builder.Images.Count, file.Images.Count);
            for (int i = 0; i < builder.Images.Count; i++)
            {
                AssertSameHeader(builder.Images[i].Header, file.Images[i].Header);
                Assert.Equal(builder.Images[i].Data, file.Images[i].Data);
            }
        }

        [Theory]
        [MemberData(nameof(NonAnimationDataTypes))]
        public void ToBytes_KeepsPaletteBytesOfFilesWithoutColorTables(Gm1DataType dataType)
        {
            var builder = Gm1FileBuilder.Create(dataType);

            var bytes = Gm1File.Read(builder.ToBytes()).ToBytes();

            Assert.Contains(builder.PaletteBytes, b => b != 0);
            Assert.Equal(builder.PaletteBytes, bytes.Skip(Gm1FileHeader.ByteSize).Take(Palette.ByteSize));
        }

        [Fact]
        public void Read_ReadsColorTablesOfAnimationFile()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.Animations);

            var file = Gm1File.Read(builder.ToBytes());

            for (int table = 0; table < Palette.ColorTableCount; table++)
            {
                Assert.Equal(builder.GetColorTable(table), file.Palette.ColorTables[table].Colors);
            }
        }

        [Fact]
        public void ToBytes_StoresImagesContiguously_WhenOriginalDataHasGaps()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 4);
            var gaps = new Dictionary<int, int> { { 0, 5 }, { 2, 13 }, { 3, 1 } };
            var withGaps = builder.ToBytes(gaps);
            var original = Gm1File.Read(withGaps);

            var bytes = original.ToBytes();

            int count = builder.Images.Count;
            int dataSize = builder.Images.Sum(i => i.Data.Length);
            Assert.Equal(withGaps.Length - gaps.Values.Sum(), bytes.Length);
            Assert.Equal((uint)count, Gm1FileBuilder.ReadUInt32(bytes, Gm1FileBuilder.ImageCountField * sizeof(uint)));
            Assert.Equal((uint)dataSize, Gm1FileBuilder.ReadUInt32(bytes, Gm1FileBuilder.DataSizeField * sizeof(uint)));

            uint expectedOffset = 0;
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(expectedOffset, Gm1FileBuilder.ReadUInt32(bytes, Gm1FileBuilder.OffsetEntry(i)));
                Assert.Equal((uint)builder.Images[i].Data.Length, Gm1FileBuilder.ReadUInt32(bytes, Gm1FileBuilder.SizeEntry(count, i)));
                expectedOffset += (uint)builder.Images[i].Data.Length;
            }

            var reread = Gm1File.Read(bytes);
            Assert.Equal(original.Images.Select(i => i.Data), reread.Images.Select(i => i.Data));
            Assert.Equal(builder.Images.Select(i => i.Data), reread.Images.Select(i => i.Data));
        }

        [Fact]
        public void ToBytes_StoresImagesInListOrder_WhenOriginalDataIsReversed()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.NoCompression1, itemCount: 4);
            var reversed = builder.ToBytes(reverseDataOrder: true);

            var bytes = Gm1File.Read(reversed).ToBytes();

            Assert.NotEqual(reversed, bytes);
            Assert.Equal(builder.ToBytes(), bytes);
        }

        [Fact]
        public void ToBytes_RecalculatesImageCountAndDataSize_AfterImagesChanged()
        {
            var file = Gm1File.Read(Gm1FileBuilder.Create(Gm1DataType.Font, itemCount: 5).ToBytes());
            file.Images.RemoveAt(1);
            file.Images[0].Data = new byte[] { 0b100_00000 };
            file.Header.ImageCount = 999;
            file.Header.DataSize = 12345;

            var bytes = file.ToBytes();

            Assert.Equal(4u, file.Header.ImageCount);
            Assert.Equal((uint)file.Images.Sum(i => i.Data.Length), file.Header.DataSize);
            var reread = Gm1File.Read(bytes);
            Assert.Equal(4, reread.Images.Count);
            Assert.Equal(new byte[] { 0b100_00000 }, reread.Images[0].Data);
            Assert.Equal(Gm1FileBuilder.DataStart(4) + (int)file.Header.DataSize, bytes.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(Gm1FileHeader.ByteSize - 1)]
        [InlineData(Gm1FileHeader.ByteSize)]
        [InlineData(Gm1FileBuilder.TablesStart - 1)]
        public void Read_TooShortFile_ThrowsInvalidDataException(int length)
        {
            var bytes = Gm1FileBuilder.Create(Gm1DataType.Interface).ToBytes().Take(length).ToArray();

            Assert.Throws<InvalidDataException>(() => Gm1File.Read(bytes));
        }

        [Theory]
        [InlineData(6u)]
        [InlineData(1000u)]
        [InlineData(0x7FFF_FFFFu)]
        [InlineData(uint.MaxValue)]
        public void Read_ImageCountTooBigForFile_ThrowsInvalidDataException(uint imageCount)
        {
            // the file ends 10 bytes after the tables of 5 images, so the tables of 6 or more images do not fit
            var bytes = Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 5).ToBytes().Take(Gm1FileBuilder.DataStart(5) + 10).ToArray();
            Gm1FileBuilder.WriteUInt32(bytes, Gm1FileBuilder.ImageCountField * sizeof(uint), imageCount);

            Assert.Throws<InvalidDataException>(() => Gm1File.Read(bytes));
        }

        [Theory]
        [InlineData("offset + 1")]
        [InlineData("offset = 0x80000000")]
        [InlineData("offset = uint.MaxValue")]
        [InlineData("size + 1")]
        [InlineData("size = 0x80000000")]
        [InlineData("size = uint.MaxValue")]
        public void Read_ImageDataOutsideOfFile_ThrowsInvalidDataException(string change)
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 3);
            var bytes = builder.ToBytes();
            int last = builder.Images.Count - 1;
            int offsetEntry = Gm1FileBuilder.OffsetEntry(last);
            int sizeEntry = Gm1FileBuilder.SizeEntry(builder.Images.Count, last);
            switch (change)
            {
                case "offset + 1": Gm1FileBuilder.WriteUInt32(bytes, offsetEntry, Gm1FileBuilder.ReadUInt32(bytes, offsetEntry) + 1); break;
                case "offset = 0x80000000": Gm1FileBuilder.WriteUInt32(bytes, offsetEntry, 0x8000_0000u); break;
                case "offset = uint.MaxValue": Gm1FileBuilder.WriteUInt32(bytes, offsetEntry, uint.MaxValue); break;
                case "size + 1": Gm1FileBuilder.WriteUInt32(bytes, sizeEntry, Gm1FileBuilder.ReadUInt32(bytes, sizeEntry) + 1); break;
                case "size = 0x80000000": Gm1FileBuilder.WriteUInt32(bytes, sizeEntry, 0x8000_0000u); break;
                case "size = uint.MaxValue": Gm1FileBuilder.WriteUInt32(bytes, sizeEntry, uint.MaxValue); break;
                default: throw new ArgumentOutOfRangeException(nameof(change));
            }

            Assert.Throws<InvalidDataException>(() => Gm1File.Read(bytes));
        }

        [Fact]
        public void Read_ImageDataEndingExactlyAtEndOfFile_IsAccepted()
        {
            var builder = Gm1FileBuilder.Create(Gm1DataType.Interface, itemCount: 3);
            var bytes = builder.ToBytes();

            var file = Gm1File.Read(bytes);

            Assert.Equal(builder.Images[2].Data, file.Images[2].Data);
        }

        [Fact]
        public void Read_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Gm1File.Read(null!));
        }

        [Fact]
        public void TgxImageHeader_ReadThenWrite_KeepsEveryField()
        {
            var bytes = Enumerable.Range(1, TgxImageHeader.ByteSize).Select(i => (byte)(i * 17)).ToArray();

            var header = TgxImageHeader.Read(bytes);
            var written = new byte[TgxImageHeader.ByteSize];
            header.Write(written);

            Assert.Equal(bytes, written);
            var expected = new byte[TgxImageHeader.ByteSize];
            Gm1FileBuilder.WriteImageHeader(header, expected);
            Assert.Equal(expected, written);
        }

        [Fact]
        public void TgxImageHeader_Copy_IsIndependent()
        {
            var header = Gm1FileBuilder.RandomHeader(new Random(1), 10, 20);

            var copy = header.Copy();
            copy.Width = 99;
            copy.AnimatedColor = 42;

            AssertSameHeader(Gm1FileBuilder.RandomHeader(new Random(1), 10, 20), header);
            Assert.Equal(99, copy.Width);
        }

        [Fact]
        public void ColorTable_WrongNumberOfColors_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ColorTable(new ushort[255]));
            Assert.Throws<ArgumentException>(() => new ColorTable(new ushort[257]));
        }

        [Fact]
        public void ColorTable_Copy_IsIndependent()
        {
            var table = new ColorTable(Enumerable.Range(0, ColorTable.ColorCount).Select(i => (ushort)i).ToArray());

            var copy = table.Copy();
            copy[5] = 0xFFFF;

            Assert.Equal(5, table[5]);
            Assert.Equal(0xFFFF, copy[5]);
        }

        [Fact]
        public void Palette_ReadThenToBytes_ReturnsIdenticalBytes()
        {
            var bytes = new byte[Palette.ByteSize];
            new Random(3).NextBytes(bytes);

            var palette = Palette.Read(bytes);

            Assert.Equal(bytes, palette.ToBytes());
            Assert.Equal(Palette.ColorTableCount, palette.ColorTables.Length);
            Assert.Equal(bytes[ColorTable.ByteSize * 3 + 2] | bytes[ColorTable.ByteSize * 3 + 3] << 8, palette.ColorTables[3][1]);
        }

        private static void AssertSameHeader(TgxImageHeader expected, TgxImageHeader actual)
        {
            Assert.Equal(
                (expected.Width, expected.Height, expected.OffsetX, expected.OffsetY, expected.ImagePart, expected.SubParts,
                 expected.TileOffset, expected.Direction, expected.HorizontalOffsetOfImage, expected.BuildingWidth, expected.AnimatedColor),
                (actual.Width, actual.Height, actual.OffsetX, actual.OffsetY, actual.ImagePart, actual.SubParts,
                 actual.TileOffset, actual.Direction, actual.HorizontalOffsetOfImage, actual.BuildingWidth, actual.AnimatedColor));
        }
    }
}

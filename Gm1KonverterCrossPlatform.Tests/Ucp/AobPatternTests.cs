using System;
using Gm1KonverterCrossPlatform.Core.Ucp;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Ucp
{
    public class AobPatternTests
    {
        [Fact]
        public void Create_FormatsLikeUcp()
        {
            var pattern = AobPattern.Create(new byte[] { 0x8B, 0x4C, 0x24, 0x60, 0x0A }, i => i == 2 || i == 3, out int trimmed)!;

            Assert.Equal("8B 4C ? ? 0A", pattern.ToString());
            Assert.Equal(0, trimmed);
        }

        [Fact]
        public void Create_TrimsWildcardsAtBothEnds()
        {
            var pattern = AobPattern.Create(new byte[] { 1, 2, 3, 4, 5, 6 }, i => i < 2 || i == 5, out int trimmed)!;

            Assert.Equal("03 04 05", pattern.ToString());
            Assert.Equal(2, trimmed);
        }

        [Fact]
        public void Create_OnlyWildcards_ReturnsNull()
        {
            Assert.Null(AobPattern.Create(new byte[] { 1, 2 }, i => true, out _));
        }

        [Fact]
        public void Parse_ReadsUcpFormat()
        {
            var pattern = AobPattern.Parse("8B 4C ? ? 0a");

            Assert.Equal("8B 4C ? ? 0A", pattern.ToString());
            Assert.Equal(new[] { 1 }, pattern.FindAll(new byte[] { 0, 0x8B, 0x4C, 1, 2, 0x0A }, 5));
        }

        [Theory]
        [InlineData("8B XX")]
        [InlineData("8B 4")]
        [InlineData("8B ?? 4C")]
        public void Parse_InvalidToken_ThrowsFormatException(string text)
        {
            Assert.Throws<FormatException>(() => AobPattern.Parse(text));
        }

        [Theory]
        [InlineData("? 8B")]
        [InlineData("8B ?")]
        [InlineData("")]
        public void Parse_WildcardAtEndOrEmpty_ThrowsArgumentException(string text)
        {
            Assert.Throws<ArgumentException>(() => AobPattern.Parse(text));
        }

        [Fact]
        public void FindAll_RespectsWildcards()
        {
            var pattern = AobPattern.Create(new byte[] { 0xAA, 0, 0xBB }, i => i == 1, out _)!;
            byte[] data = { 0xAA, 1, 0xBB, 0xAA, 0xAA, 2, 0xBB, 0xAA, 3, 0xBC, 0xAA, 4 };

            Assert.Equal(new[] { 0, 4 }, pattern.FindAll(data, 10));
            Assert.Equal(new[] { 0 }, pattern.FindAll(data, 1));
        }

        [Fact]
        public void FindAll_MatchAtEnd_IsFound()
        {
            var pattern = AobPattern.Create(new byte[] { 7, 8 }, i => false, out _)!;

            Assert.Equal(new[] { 3 }, pattern.FindAll(new byte[] { 1, 2, 3, 7, 8 }, 5));
            Assert.Empty(pattern.FindAll(new byte[] { 1, 2, 3, 7 }, 5));
            Assert.Empty(pattern.FindAll(Array.Empty<byte>(), 5));
        }

        [Fact]
        public void FindAll_SameAsNaiveSearch()
        {
            var random = new Random(4);
            for (int run = 0; run < 200; run++)
            {
                var data = new byte[random.Next(0, 300)];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = (byte)random.Next(0, 3);
                }

                var source = new byte[random.Next(1, 6)];
                random.NextBytes(source);
                for (int i = 0; i < source.Length; i++)
                {
                    source[i] = (byte)(source[i] % 3);
                }

                bool[] wildcard = new bool[source.Length];
                for (int i = 1; i < source.Length - 1; i++)
                {
                    wildcard[i] = random.Next(0, 3) == 0;
                }

                var pattern = AobPattern.Create(source, i => wildcard[i], out _)!;
                var expected = new System.Collections.Generic.List<int>();
                for (int i = 0; i + source.Length <= data.Length; i++)
                {
                    bool match = true;
                    for (int j = 0; j < source.Length; j++)
                    {
                        match &= wildcard[j] || data[i + j] == source[j];
                    }

                    if (match)
                    {
                        expected.Add(i);
                    }
                }

                Assert.Equal(expected, pattern.FindAll(data, int.MaxValue));
            }
        }
    }
}

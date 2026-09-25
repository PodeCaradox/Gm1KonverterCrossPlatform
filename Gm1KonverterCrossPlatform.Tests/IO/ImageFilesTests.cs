using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Tests.Support;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.IO
{
    public class ImageFilesTests : IDisposable
    {
        private readonly TempDirectory temp = new TempDirectory();

        public void Dispose() => temp.Dispose();

        [Fact]
        public void SavePngThenLoadPng_KeepsEveryOpaqueColor()
        {
            var pixels = Enumerable.Range(0, 0x8000).Select(i => (ushort)(0x8000 | i)).ToArray();
            var image = new Argb1555Image(256, 128, pixels);
            string path = temp.Combine("colors.png");

            ImageFiles.SavePng(image, path);
            var loaded = ImageFiles.LoadPng(path);

            Assert.Equal(256, loaded.Width);
            Assert.Equal(128, loaded.Height);
            Assert.Equal(pixels, loaded.Pixels);
        }

        [Fact]
        public void SavePngThenLoadPng_PixelsWithoutAlphaBit_LoadAsTransparentMarker()
        {
            var image = new Argb1555Image(4, 1, new ushort[] { 0, Argb1555.TransparentMarker, 0x1234, 0x8000 });
            string path = temp.Combine("transparent.png");

            ImageFiles.SavePng(image, path);
            var loaded = ImageFiles.LoadPng(path);

            Assert.Equal(new ushort[] { Argb1555.TransparentMarker, Argb1555.TransparentMarker, Argb1555.TransparentMarker, 0x8000 }, loaded.Pixels);
        }

        [Fact]
        public void SavePng_WritesTransparentPixelsAsTransparentBlack()
        {
            string path = temp.Combine("black.png");

            ImageFiles.SavePng(new Argb1555Image(2, 1, new ushort[] { 0x7C00, 0xFC00 }), path);

            using var png = Image.Load<Rgba32>(path);
            Assert.Equal(new Rgba32(0, 0, 0, 0), png[0, 0]);
            Assert.Equal(new Rgba32(248, 0, 0, 255), png[1, 0]);
        }

        [Fact]
        public void LoadPng_QuantizesChannelsTo5BitsAndMakesVisiblePixelsOpaque()
        {
            string path = temp.Combine("external.png");
            using (var png = new Image<Rgba32>(4, 1))
            {
                png[0, 0] = new Rgba32(255, 7, 8, 255);
                png[1, 0] = new Rgba32(0, 0, 0, 1);
                png[2, 0] = new Rgba32(10, 20, 30, 0);
                png[3, 0] = new Rgba32(0x80, 0x40, 0x20, 128);
                png.SaveAsPng(path);
            }

            var loaded = ImageFiles.LoadPng(path);

            Assert.Equal(new ushort[] { 0x8000 | 31 << 10 | 0 << 5 | 1, 0x8000, Argb1555.TransparentMarker, 0x8000 | 16 << 10 | 8 << 5 | 4 }, loaded.Pixels);
        }

        [Fact]
        public void SavePng_CreatesMissingDirectories()
        {
            string path = temp.Combine("a", "b", "c", "image.png");

            ImageFiles.SavePng(new Argb1555Image(1, 1, new ushort[] { 0x8001 }), path);

            Assert.True(File.Exists(path));
        }

        [Fact]
        public void ToArgb1555_RegionInsideImage_CopiesPixels()
        {
            using var image = new Image<Rgba32>(10, 10);
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    image[x, y] = new Rgba32((byte)(x * 8), (byte)(y * 8), 0, 255);
                }
            }

            var region = ImageFiles.ToArgb1555(image, 2, 3, 8, 7);

            Assert.Equal((8, 7), (region.Width, region.Height));
            for (int y = 0; y < region.Height; y++)
            {
                for (int x = 0; x < region.Width; x++)
                {
                    Assert.Equal((ushort)(0x8000 | (x + 2) << 10 | (y + 3) << 5), region[x, y]);
                }
            }
        }

        [Theory]
        [InlineData(-1, 0, 1, 1)]
        [InlineData(0, -1, 1, 1)]
        [InlineData(0, 0, 11, 1)]
        [InlineData(0, 0, 1, 11)]
        [InlineData(5, 0, 6, 1)]
        [InlineData(0, 5, 1, 6)]
        [InlineData(10, 10, 1, 1)]
        [InlineData(0, 0, -1, 1)]
        [InlineData(0, 0, 1, -1)]
        [InlineData(int.MaxValue, 0, 1, 1)]
        public void ToArgb1555_RegionOutsideImage_ThrowsArgumentOutOfRange(int x, int y, int width, int height)
        {
            using var image = new Image<Rgba32>(10, 10);

            Assert.Throws<ArgumentOutOfRangeException>(() => ImageFiles.ToArgb1555(image, x, y, width, height));
        }

        [Fact]
        public void ToArgb1555_EmptyRegionAtEdge_IsAllowed()
        {
            using var image = new Image<Rgba32>(10, 10);

            var region = ImageFiles.ToArgb1555(image, 10, 10, 0, 0);

            Assert.Empty(region.Pixels);
        }
    }
}

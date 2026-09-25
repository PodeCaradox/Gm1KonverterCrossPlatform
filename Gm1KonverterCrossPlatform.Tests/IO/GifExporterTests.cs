using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Documents;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Tests.Support;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.IO
{
    public class GifExporterTests : IDisposable
    {
        private readonly TempDirectory temp = new TempDirectory();

        public void Dispose() => temp.Dispose();

        [Fact]
        public void Save_WritesOneFramePerImageOnCanvasOfLargestSize()
        {
            var frames = new[] { Filled(10, 20, 0xFC00), Filled(30, 5, 0x83E0), Filled(7, 7, 0x801F) };
            string path = temp.Combine("animation.gif");

            GifExporter.Save(frames, 100, path);

            using var gif = Image.Load<Rgba32>(path);
            Assert.Equal(3, gif.Frames.Count);
            Assert.Equal(30, gif.Width);
            Assert.Equal(20, gif.Height);
        }

        [Fact]
        public void Save_AlignsSmallerFramesToBottomRightCorner()
        {
            var frames = new[] { Filled(10, 10, 0xFC00), Filled(4, 3, 0x801F) };
            string path = temp.Combine("aligned.gif");

            GifExporter.Save(frames, 100, path);

            using var gif = Image.Load<Rgba32>(path);
            using var second = gif.Frames.CloneFrame(1);
            Assert.Equal(0, second[5, 6].A);
            Assert.Equal(0, second[9, 6].A);
            Assert.Equal(255, second[6, 7].A);
            Assert.Equal(255, second[9, 9].A);
            Assert.True(second[9, 9].B > 200 && second[9, 9].R < 50, "the second frame is blue");
        }

        [Theory]
        [InlineData(250, 25)]
        [InlineData(100, 10)]
        [InlineData(15, 1)]
        [InlineData(0, 1)]
        [InlineData(-10, 1)]
        public void Save_StoresDelayInHundredthsOfSecondsAndAtLeastOne(int delayMilliseconds, int expectedDelay)
        {
            string path = temp.Combine($"delay{delayMilliseconds}.gif");

            GifExporter.Save(new[] { Filled(2, 2, 0xFC00), Filled(2, 2, 0x83E0) }, delayMilliseconds, path);

            using var gif = Image.Load<Rgba32>(path);
            Assert.All(gif.Frames.Cast<ImageFrame<Rgba32>>(), frame => Assert.Equal(expectedDelay, frame.Metadata.GetGifMetadata().FrameDelay));
        }

        [Fact]
        public void Save_LoopsEndlessly()
        {
            string path = temp.Combine("loop.gif");

            GifExporter.Save(new[] { Filled(2, 2, 0xFC00), Filled(2, 2, 0x83E0) }, 100, path);

            using var gif = Image.Load<Rgba32>(path);
            Assert.Equal(0, gif.Metadata.GetGifMetadata().RepeatCount);
        }

        [Fact]
        public void Save_SingleFrame_Works()
        {
            string path = temp.Combine("single.gif");

            GifExporter.Save(new[] { Filled(3, 4, 0xFC00) }, 100, path);

            using var gif = Image.Load<Rgba32>(path);
            Assert.Equal(1, gif.Frames.Count);
            Assert.Equal((3, 4), (gif.Width, gif.Height));
        }

        [Fact]
        public void Save_NoFrames_ThrowsArgumentException()
        {
            string path = temp.Combine("empty.gif");

            Assert.Throws<ArgumentException>(() => GifExporter.Save(Array.Empty<Argb1555Image>(), 100, path));
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void ExportGif_WritesGifIntoWorkFolder()
        {
            var workFolder = new WorkFolder(temp.Combine("work"));
            var document = new Gm1Document("anim_test.gm1", Gm1File.Read(Gm1FileBuilder.Create(Gm1DataType.Animations, itemCount: 3).ToBytes()));
            var frames = document.RenderItems();

            string folder = new Gm1Exporter(workFolder).ExportGif(document, frames, 50);

            Assert.Equal(workFolder.GifFolder("anim_test.gm1"), folder);
            using var gif = Image.Load<Rgba32>(workFolder.GifFile("anim_test.gm1"));
            Assert.Equal(3, gif.Frames.Count);
            Assert.Equal(frames.Max(frame => frame.Width), gif.Width);
            Assert.Equal(frames.Max(frame => frame.Height), gif.Height);
        }

        private static Argb1555Image Filled(int width, int height, ushort color)
        {
            return new Argb1555Image(width, height, Enumerable.Repeat(color, width * height).ToArray());
        }
    }
}

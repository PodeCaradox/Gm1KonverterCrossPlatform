using System;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Files
{
    public class ColorTableImageTests
    {
        [Theory]
        [InlineData(1)]
        [InlineData(Palette.ImageCellSize)]
        [InlineData(20)]
        public void RenderThenRead_ReturnsSameColors(int cellSize)
        {
            var table = new ColorTable(Gm1FileBuilder.DistinctOpaqueColors(new Random(cellSize)));

            var image = ColorTableImage.Render(table, cellSize);
            var read = ColorTableImage.Read(image, cellSize);

            Assert.Equal(table.Colors, read.Colors);
        }

        [Fact]
        public void Render_Creates32By8GridOfFilledCells()
        {
            var table = new ColorTable(Enumerable.Range(0, ColorTable.ColorCount).Select(i => TestImages.Opaque(i * 100)).ToArray());

            var image = ColorTableImage.Render(table);

            Assert.Equal(320, image.Width);
            Assert.Equal(80, image.Height);
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Assert.Equal(table[y / 10 * 32 + x / 10], image[x, y]);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Render_InvalidCellSize_Throws(int cellSize)
        {
            var table = new ColorTable(new ushort[ColorTable.ColorCount]);

            Assert.Throws<ArgumentOutOfRangeException>(() => ColorTableImage.Render(table, cellSize));
        }

        [Theory]
        [InlineData(310, 80)]
        [InlineData(320, 70)]
        [InlineData(1, 1)]
        [InlineData(0, 0)]
        public void Read_TooSmallImage_ThrowsArgumentException(int width, int height)
        {
            var image = new Argb1555Image(width, height);

            Assert.Throws<ArgumentException>(() => ColorTableImage.Read(image));
        }

        [Fact]
        public void Read_SmallestImageContainingEveryCell_IsAccepted()
        {
            var table = new ColorTable(Gm1FileBuilder.DistinctOpaqueColors(new Random(2)));
            var image = ColorTableImage.Render(table).Crop(0, 0, 311, 71);

            var read = ColorTableImage.Read(image);

            Assert.Equal(table.Colors, read.Colors);
        }

        [Fact]
        public void Read_UsesTopLeftPixelOfEveryCell()
        {
            var table = new ColorTable(Gm1FileBuilder.DistinctOpaqueColors(new Random(4)));
            var image = ColorTableImage.Render(table);
            image[15, 25] = 0x8001; // inside cell 65, but not its top left pixel
            image[10, 20] = 0x8002; // top left pixel of cell 65

            var read = ColorTableImage.Read(image);

            Assert.Equal(0x8002, read[65]);
            Assert.Equal(table.Colors.Where((_, i) => i != 65), read.Colors.Where((_, i) => i != 65));
        }

        [Theory]
        [InlineData(0, 0, 10, 0)]
        [InlineData(9.9, 9.9, 10, 0)]
        [InlineData(10, 0, 10, 1)]
        [InlineData(15, 25, 10, 65)]
        [InlineData(319.9, 79.9, 10, 255)]
        [InlineData(320, 0, 10, -1)]
        [InlineData(0, 80, 10, -1)]
        [InlineData(-0.5, 5, 10, -1)]
        [InlineData(5, -0.1, 10, -1)]
        [InlineData(639, 159, 20, 255)]
        [InlineData(640, 0, 20, -1)]
        [InlineData(0, 160, 20, -1)]
        [InlineData(620, 140, 20, 255)]
        [InlineData(619.99, 139.99, 20, 222)]
        public void IndexAt_ReturnsCellIndexOrMinusOneOutsideOfGrid(double x, double y, int cellSize, int expected)
        {
            Assert.Equal(expected, ColorTableImage.IndexAt(x, y, cellSize));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Layout;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Layout
{
    public class SpriteSheetLayoutTests
    {
        public static IEnumerable<object[]> Seeds => TestImages.Seeds(40);

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Arrange_ImagesNarrowerThanMaxWidth_MatchesOriginalAlgorithm(int seed)
        {
            var random = new Random(seed);
            int maxWidth = random.Next(20, 400);
            var sizes = RandomSizes(random, random.Next(1, 40), maxWidth - 1);

            var layout = SpriteSheetLayout.Arrange(sizes, maxWidth);
            var expected = OriginalLayout(sizes, maxWidth);

            Assert.Equal(maxWidth, layout.Width);
            Assert.Equal(expected.Height, layout.Height);
            Assert.Equal(expected.Cells, layout.Cells.Select(cell => (cell.X, cell.Y)));
        }

        [Fact]
        public void Arrange_ImageReachingMaxWidth_StartsNewRow()
        {
            var sizes = new[] { (40, 5), (60, 7), (10, 3) };

            var layout = SpriteSheetLayout.Arrange(sizes, 100);

            // 40 + 60 reaches the max width, so the second image starts a new row (like the original)
            Assert.Equal(new[] { (0, 0), (0, 5), (60, 5) }, layout.Cells.Select(cell => (cell.X, cell.Y)));
            Assert.Equal(12, layout.Height);
        }

        [Fact]
        public void Arrange_FirstImageAsWideAsMaxWidth_StaysInFirstRow()
        {
            // the original started with an empty row here
            var layout = SpriteSheetLayout.Arrange(new[] { (100, 10), (30, 4) }, 100);

            Assert.Equal(new[] { (0, 0), (0, 10) }, layout.Cells.Select(cell => (cell.X, cell.Y)));
            Assert.Equal(14, layout.Height);
        }

        [Fact]
        public void Arrange_ImageWiderThanMaxWidth_WidensSheet()
        {
            var sizes = new[] { (30, 10), (250, 20), (40, 5) };

            var layout = SpriteSheetLayout.Arrange(sizes, 100);

            Assert.Equal(250, layout.Width);
            Assert.All(layout.Cells, cell => Assert.True(cell.X + cell.Width <= layout.Width));
        }

        [Fact]
        public void Arrange_EveryImageWiderThanMaxWidth_PlacesOneImagePerRow()
        {
            var sizes = new[] { (120, 10), (150, 20), (110, 5) };

            var layout = SpriteSheetLayout.Arrange(sizes, 100);

            Assert.Equal(150, layout.Width);
            Assert.Equal(new[] { (0, 0), (0, 10), (0, 30) }, layout.Cells.Select(cell => (cell.X, cell.Y)));
            Assert.Equal(35, layout.Height);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Arrange_CellsDoNotOverlapAndLieInsideSheet(int seed)
        {
            var random = new Random(seed);
            int maxWidth = random.Next(1, 300);
            var sizes = RandomSizes(random, random.Next(1, 50), 350);

            var layout = SpriteSheetLayout.Arrange(sizes, maxWidth);

            Assert.Equal(Math.Max(maxWidth, sizes.Max(size => size.Width)), layout.Width);
            Assert.Equal(sizes.Count, layout.Cells.Count);
            for (int i = 0; i < layout.Cells.Count; i++)
            {
                var cell = layout.Cells[i];
                Assert.Equal(sizes[i], (cell.Width, cell.Height));
                Assert.True(cell.X >= 0 && cell.Y >= 0 && cell.X + cell.Width <= layout.Width && cell.Y + cell.Height <= layout.Height, $"cell {i} lies outside of the sheet");
                for (int j = 0; j < i; j++)
                {
                    Assert.False(Overlap(cell, layout.Cells[j]), $"cells {j} and {i} overlap");
                }
            }
        }

        [Fact]
        public void Arrange_KeepsInputOrder()
        {
            var sizes = new[] { (10, 1), (20, 2), (30, 3), (40, 4) };

            var layout = SpriteSheetLayout.Arrange(sizes, 1000);

            Assert.Equal(new[] { 0, 10, 30, 60 }, layout.Cells.Select(cell => cell.X));
            Assert.All(layout.Cells, cell => Assert.Equal(0, cell.Y));
            Assert.Equal(4, layout.Height);
        }

        [Fact]
        public void Arrange_NoImages_ReturnsEmptySheetOfMaxWidth()
        {
            var layout = SpriteSheetLayout.Arrange(Array.Empty<(int, int)>(), 50);

            Assert.Empty(layout.Cells);
            Assert.Equal(50, layout.Width);
            Assert.Equal(1, layout.Height);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Arrange_MaxWidthNotPositive_Throws(int maxWidth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SpriteSheetLayout.Arrange(new[] { (10, 10) }, maxWidth));
        }

        [Fact]
        public void Arrange_SameInputWithSheetWidth_ReturnsSameLayout()
        {
            // the import arranges the images again using the width of the exported sheet
            var random = new Random(9);
            var sizes = RandomSizes(random, 30, 200);
            foreach (int maxWidth in new[] { 1, 150, 199, 200, 201, 700, 5000 })
            {
                var export = SpriteSheetLayout.Arrange(sizes, maxWidth);
                var import = SpriteSheetLayout.Arrange(sizes, export.Width);

                Assert.Equal((export.Width, export.Height), (import.Width, import.Height));
                Assert.Equal(export.Cells.Select(cell => (cell.X, cell.Y)), import.Cells.Select(cell => (cell.X, cell.Y)));
            }
        }

        /// <summary>
        /// The row wrapping rule of the original implementation (Utility.CreateBigImage), without its quirk of
        /// starting with an empty row when the first image is at least as wide as the maximum width.
        /// </summary>
        private static (List<(int X, int Y)> Cells, int Height) OriginalLayout(IReadOnlyList<(int Width, int Height)> sizes, int maxWidth)
        {
            var cells = new List<(int X, int Y)>();
            var rowHeights = new List<int> { 0 };
            int actualWidth = 0;
            int y = 0;

            foreach (var (width, height) in sizes)
            {
                actualWidth += width;
                bool firstRowIsEmpty = cells.Count == 0;
                if (maxWidth <= actualWidth && !firstRowIsEmpty)
                {
                    y += rowHeights[rowHeights.Count - 1];
                    rowHeights.Add(0);
                    actualWidth = width;
                }

                cells.Add((actualWidth - width, y));
                rowHeights[rowHeights.Count - 1] = Math.Max(rowHeights[rowHeights.Count - 1], height);
            }

            return (cells, Math.Max(1, rowHeights.Sum()));
        }

        private static List<(int Width, int Height)> RandomSizes(Random random, int count, int maxImageWidth)
        {
            return Enumerable.Range(0, count)
                .Select(_ => (random.Next(1, maxImageWidth + 1), random.Next(1, 100)))
                .ToList();
        }

        private static bool Overlap(SpriteSheetLayout.Cell first, SpriteSheetLayout.Cell second)
        {
            return first.X < second.X + second.Width && second.X < first.X + first.Width
                && first.Y < second.Y + second.Height && second.Y < first.Y + first.Height;
        }
    }
}

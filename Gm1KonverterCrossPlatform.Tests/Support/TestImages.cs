using System;
using System.Collections.Generic;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Tests.Support
{
    /// <summary>
    /// Creates images like the PNG import does: transparent pixels are <see cref="Argb1555.TransparentMarker"/>,
    /// all other pixels have the alpha bit set.
    /// </summary>
    internal static class TestImages
    {
        public static ushort Opaque(int value) => (ushort)(0x8000 | (value & 0x7FFF));

        public static Argb1555Image Random(Random random, int width, int height, int colorCount = 4, double transparency = 0.3, double runProbability = 0.5)
        {
            var colors = Enumerable.Range(0, colorCount).Select(_ => Opaque(random.Next(0, 0x7FFF))).ToArray();
            var pixels = new ushort[width * height];

            int i = 0;
            while (i < pixels.Length)
            {
                ushort color = random.NextDouble() < transparency
                    ? Argb1555.TransparentMarker
                    : colors[random.Next(colors.Length)];

                int length = random.NextDouble() < runProbability ? random.Next(1, 80) : 1;
                for (int j = 0; j < length && i < pixels.Length; j++)
                {
                    pixels[i++] = color;
                }
            }

            // make some rows fully transparent, including trailing rows
            for (int y = 0; y < height; y++)
            {
                if (random.NextDouble() < 0.15 || (y > height - 3 && random.NextDouble() < 0.5))
                {
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y * width + x] = Argb1555.TransparentMarker;
                    }
                }
            }

            return new Argb1555Image(width, height, pixels);
        }

        public static List<ushort> ToList(Argb1555Image image) => image.Pixels.ToList();

        public static IEnumerable<object[]> Seeds(int count) => Enumerable.Range(1, count).Select(seed => new object[] { seed });
    }
}

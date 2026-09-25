using System;
using System.Collections.Generic;
using System.Numerics;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Core.Codecs
{
    /// <summary>
    /// Finds the color table index for a color using one color table.
    /// <list type="number">
    /// <item>the first entry with exactly this color,</item>
    /// <item>the first entry with the same RGB value (alpha bit ignored),</item>
    /// <item>the same in the other color tables of the palette,</item>
    /// <item>the entry with the nearest color.</item>
    /// </list>
    /// </summary>
    public sealed class ColorTableIndexer : IPaletteIndexer
    {
        private readonly Palette palette;
        private readonly int preferredTable;
        private readonly Dictionary<ushort, byte> cache = new Dictionary<ushort, byte>();

        public ColorTableIndexer(Palette palette, int preferredTable = 0)
        {
            this.palette = palette ?? throw new ArgumentNullException(nameof(palette));
            if (preferredTable < 0 || preferredTable >= Palette.ColorTableCount)
            {
                throw new ArgumentOutOfRangeException(nameof(preferredTable));
            }

            this.preferredTable = preferredTable;
        }

        public byte GetIndex(int pixelIndex, ushort color)
        {
            if (!cache.TryGetValue(color, out byte index))
            {
                index = FindIndex(color);
                cache.Add(color, index);
            }

            return index;
        }

        private byte FindIndex(ushort color)
        {
            if (TryFindExact(palette.ColorTables[preferredTable], color, out byte index))
            {
                return index;
            }

            for (int table = 0; table < Palette.ColorTableCount; table++)
            {
                if (table != preferredTable && TryFindExact(palette.ColorTables[table], color, out index))
                {
                    return index;
                }
            }

            return FindNearest(palette.ColorTables[preferredTable], color);
        }

        private static bool TryFindExact(ColorTable table, ushort color, out byte index)
        {
            for (int i = 0; i < ColorTable.ColorCount; i++)
            {
                if (table[i] == color)
                {
                    index = (byte)i;
                    return true;
                }
            }

            ushort rgb = Argb1555.Rgb(color);
            for (int i = 0; i < ColorTable.ColorCount; i++)
            {
                if (Argb1555.Rgb(table[i]) == rgb)
                {
                    index = (byte)i;
                    return true;
                }
            }

            index = 0;
            return false;
        }

        private static byte FindNearest(ColorTable table, ushort color)
        {
            int bestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < ColorTable.ColorCount; i++)
            {
                int distance = Argb1555.DistanceSquared(table[i], color);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return (byte)bestIndex;
        }
    }

    /// <summary>
    /// Finds the color table index of a pixel using one rendering of the image per color table
    /// (as exported by "export original Stronghold animation"). A color can appear several times in one
    /// color table, e.g. neutral gray and a team color, but the index whose colors match the pixel in
    /// every color table is unambiguous.
    /// </summary>
    public sealed class MultiColorTableIndexer : IPaletteIndexer
    {
        private readonly IReadOnlyList<Argb1555Image?> imagesPerTable;
        private readonly Dictionary<ushort, IndexSet>[] indicesPerTable;
        private readonly ColorTableIndexer fallback;

        /// <param name="palette">The palette of the animation file.</param>
        /// <param name="imagesPerTable">
        /// The image rendered with color table 0, 1, ... Missing images (<c>null</c>) are skipped.
        /// </param>
        public MultiColorTableIndexer(Palette palette, IReadOnlyList<Argb1555Image?> imagesPerTable)
        {
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            this.imagesPerTable = imagesPerTable ?? throw new ArgumentNullException(nameof(imagesPerTable));
            if (imagesPerTable.Count > Palette.ColorTableCount)
            {
                throw new ArgumentException($"At most {Palette.ColorTableCount} images are allowed.", nameof(imagesPerTable));
            }

            fallback = new ColorTableIndexer(palette, 0);
            indicesPerTable = new Dictionary<ushort, IndexSet>[imagesPerTable.Count];
            for (int table = 0; table < imagesPerTable.Count; table++)
            {
                indicesPerTable[table] = BuildLookup(palette.ColorTables[table]);
            }
        }

        public byte GetIndex(int pixelIndex, ushort color)
        {
            var candidates = IndexSet.All;
            bool constrained = false;

            for (int table = 0; table < imagesPerTable.Count; table++)
            {
                var image = imagesPerTable[table];
                if (image == null || pixelIndex >= image.Pixels.Length)
                {
                    continue;
                }

                constrained = true;
                ushort rgb = Argb1555.Rgb(image.Pixels[pixelIndex]);
                candidates = indicesPerTable[table].TryGetValue(rgb, out var indices)
                    ? candidates.Intersect(indices)
                    : IndexSet.Empty;
            }

            return constrained && candidates.TryGetFirst(out byte index)
                ? index
                : fallback.GetIndex(pixelIndex, color);
        }

        private static Dictionary<ushort, IndexSet> BuildLookup(ColorTable table)
        {
            var lookup = new Dictionary<ushort, IndexSet>();
            for (int i = 0; i < ColorTable.ColorCount; i++)
            {
                ushort rgb = Argb1555.Rgb(table[i]);
                lookup.TryGetValue(rgb, out var indices);
                lookup[rgb] = indices.With(i);
            }

            return lookup;
        }

        /// <summary>A set of the color table indices 0 - 255.</summary>
        private readonly struct IndexSet
        {
            private readonly ulong bits0;
            private readonly ulong bits1;
            private readonly ulong bits2;
            private readonly ulong bits3;

            private IndexSet(ulong bits0, ulong bits1, ulong bits2, ulong bits3)
            {
                this.bits0 = bits0;
                this.bits1 = bits1;
                this.bits2 = bits2;
                this.bits3 = bits3;
            }

            public static IndexSet Empty => default;

            public static IndexSet All => new IndexSet(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue);

            public IndexSet With(int index)
            {
                ulong bit = 1UL << (index & 63);
                switch (index >> 6)
                {
                    case 0: return new IndexSet(bits0 | bit, bits1, bits2, bits3);
                    case 1: return new IndexSet(bits0, bits1 | bit, bits2, bits3);
                    case 2: return new IndexSet(bits0, bits1, bits2 | bit, bits3);
                    default: return new IndexSet(bits0, bits1, bits2, bits3 | bit);
                }
            }

            public IndexSet Intersect(IndexSet other)
            {
                return new IndexSet(bits0 & other.bits0, bits1 & other.bits1, bits2 & other.bits2, bits3 & other.bits3);
            }

            public bool TryGetFirst(out byte index)
            {
                ulong[] words = { bits0, bits1, bits2, bits3 };
                for (int word = 0; word < words.Length; word++)
                {
                    if (words[word] != 0)
                    {
                        index = (byte)(word * 64 + BitOperations.TrailingZeroCount(words[word]));
                        return true;
                    }
                }

                index = 0;
                return false;
            }
        }
    }
}

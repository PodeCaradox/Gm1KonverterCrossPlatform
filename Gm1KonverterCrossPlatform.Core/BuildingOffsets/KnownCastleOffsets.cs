using System.Collections.Generic;
using Gm1KonverterCrossPlatform.Core.Ucp;

namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// Byte patterns and original values of the castle building offsets, taken from Stronghold Crusader.exe
    /// (MD5 bc6524aff1831afdf4ec07267720bb6f) and Stronghold_Crusader_Extreme.exe (MD5 e45051a5e777ca09bf40862db16cc4c3).
    /// </summary>
    /// <remarks>
    /// The code around the offsets is the same in both games, but the embedded memory addresses differ and the
    /// offsets of Crusader are 912 bytes earlier. Each pattern has the offset values and every byte that differs
    /// between the two executables as wildcards, so it finds the offset in both games, and it occurs only once in
    /// each of them. The original values are the same in both games.
    /// </remarks>
    public static class KnownCastleOffsets
    {
        private static readonly Dictionary<int, KnownOffset> OffsetsByFirstImage = new Dictionary<int, KnownOffset>
        {
            { 0, new KnownOffset("2E 03 00 00 8B 85 ? ? F9 00 3B C2 74 18 8D 96 ? ? ? ? 52 8D 4F ? 51 50 6A 36 B9 90 ? ? ? E8 ? ? ? FF 8B 85", relativeX: 23, relativeY: 16, new BuildingOffset(-48, -194)) },
            { 1, new KnownOffset("4C 02 00 00 8B 85 ? ? F9 00 3B C2 74 18 8D 96 ? ? ? ? 52 8D 4F ? 51 50 6A 36 B9 90 ? ? ? E8 ? ? F6 FF 8B 85", relativeX: 23, relativeY: 16, new BuildingOffset(-77, -246)) },
            { 2, new KnownOffset("97 01 00 00 8B 85 ? ? F9 00 3B C2 74 18 8D 96 ? ? ? ? 52 8D 4F ? 51 50 6A 36 B9 90 ? ? ? E8 ? ? F6 FF 8B 85", relativeX: 23, relativeY: 16, new BuildingOffset(-76, -260)) },
            { 3, new KnownOffset("BD 02 00 00 8B 85 ? ? F9 00 3B C2 74 18 8D 96 ? ? ? ? 52 8D 4F ? 51 50 6A 36 B9 90 ? ? ? E8 ? ? F6 FF 8B 85", relativeX: 23, relativeY: 16, new BuildingOffset(-63, -225)) },
            { 12, new KnownOffset("B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 18 8D 8E ? ? ? ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 15 8D 4E ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 15 8D 4E ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 18 8D", relativeX: 60, relativeY: 56, new BuildingOffset(3, -91)) },
            { 13, new KnownOffset("6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 15 8D 4E ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 18", relativeX: 28, relativeY: 24, new BuildingOffset(-51, -91)) },
            { 23, new KnownOffset("00 00 8D 8E ? ? ? ? 51 8D 57 ? 52 50 6A 36", relativeX: 11, relativeY: 4, new BuildingOffset(-96, -143)) },
            { 43, new KnownOffset("CC 05 00 00 8B 85 ? ? F9 00 3B C2 74 18 8D 8E ? ? ? ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85", relativeX: 23, relativeY: 16, new BuildingOffset(-7, -193)) },
            { 44, new KnownOffset("CC 05 00 00 8B 85 ? ? F9 00 3B C2 74 18 8D 8E ? ? ? ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 18 8D 8E ? ? ? ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9 00 85 C0 74 15 8D 4E ? 51 8D 57 ? 52 50 6A 36 B9 90 ? ? ? E8 ? ? F7 FF 8B 85 ? ? F9", relativeX: 57, relativeY: 50, new BuildingOffset(-96, -193)) },
            { 121, new KnownOffset("51 50 6A 36 B9 90 ? ? ? E8 ? ? F6 FF 8B 85 ? ? F9 00 85 C0 74 18 8D 96 ? ? ? ? 52 8D 4F ? 51 50 6A 36 B9 90 ? ? ? E8 ? ? F6 FF 8B 85 ? ? F9 00 85 C0 0F 84 B6 01", relativeX: 33, relativeY: 26, new BuildingOffset(-77, -315)) },
            { 123, new KnownOffset("4D 03 00 00 8D 96 ? ? ? ? 52 8D 4F ? 51 50 6A 36 E9 30", relativeX: 13, relativeY: 6, new BuildingOffset(-34, -379)) },
            { 124, new KnownOffset("7D 03 00 00 8B 85 ? ? F9 00 3B C2 74 18 8D 96 ? ? ? ? 52 8D 4F ? 51 50 6A 36 B9 90 ? ? ? E8 ? ? ? FF 8B 85", relativeX: 23, relativeY: 16, new BuildingOffset(-33, -320)) },
        };

        /// <summary>The known pattern of <paramref name="group"/>, if there is one.</summary>
        public static bool TryGet(OffsetGroup group, out KnownOffset known)
        {
            return OffsetsByFirstImage.TryGetValue(group.ImageIndices[0], out known!);
        }
    }

    /// <summary>A pattern that finds an offset in both games, and its original value.</summary>
    public sealed class KnownOffset
    {
        public KnownOffset(string pattern, int relativeX, int relativeY, BuildingOffset original)
        {
            Pattern = AobPattern.Parse(pattern);
            RelativeX = relativeX;
            RelativeY = relativeY;
            Original = original;
        }

        public AobPattern Pattern { get; }

        /// <summary>Position of the x byte relative to the pattern start.</summary>
        public int RelativeX { get; }

        /// <summary>Position of the y value relative to the pattern start.</summary>
        public int RelativeY { get; }

        /// <summary>The value in the unmodified games.</summary>
        public BuildingOffset Original { get; }
    }
}

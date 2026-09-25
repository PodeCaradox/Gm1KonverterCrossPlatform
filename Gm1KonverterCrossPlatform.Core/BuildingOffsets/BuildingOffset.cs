namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// Drawing offset of a castle building image (anim_castle.gm1). Stored in Offsets.json as
    /// <c>{"X": 1, "Y": 2}</c>, compatible with files written by older versions.
    /// </summary>
    public readonly struct BuildingOffset
    {
        public BuildingOffset(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public override string ToString() => $"{X}, {Y}";
    }
}

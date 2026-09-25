namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// Where changed building offsets are applied: the UCP3 module <see cref="Ucp.UcpOffsetModule"/>.
    /// </summary>
    public interface IBuildingOffsetTarget
    {
        /// <summary>True if the offset of this image can be changed.</summary>
        bool Supports(int imageIndex);

        bool TryRead(int imageIndex, out BuildingOffset offset);

        void Write(int imageIndex, BuildingOffset offset);
    }
}

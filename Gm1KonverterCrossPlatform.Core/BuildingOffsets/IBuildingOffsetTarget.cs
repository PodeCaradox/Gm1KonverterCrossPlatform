namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// Where changed building offsets are applied. Currently the Stronghold executables are patched
    /// (<see cref="ExecutableOffsetPatcher"/>); a UCP module can implement this interface later.
    /// </summary>
    public interface IBuildingOffsetTarget
    {
        /// <summary>True if the offset of this image can be changed.</summary>
        bool Supports(int imageIndex);

        bool TryRead(int imageIndex, out BuildingOffset offset);

        void Write(int imageIndex, BuildingOffset offset);
    }
}

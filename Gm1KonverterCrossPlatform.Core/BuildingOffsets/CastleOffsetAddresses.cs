using System;
using System.Collections.Generic;

namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// Where the drawing offsets of the castle building images are stored in Stronghold_Crusader_Extreme.exe.
    /// In Stronghold Crusader.exe they are <see cref="CrusaderAddressShift"/> bytes earlier.
    /// </summary>
    public static class CastleOffsetAddresses
    {
        public const int CrusaderAddressShift = -912;

        private static readonly Dictionary<int, OffsetAddress> AddressesByImageIndex = new Dictionary<int, OffsetAddress>
        {
            { 0, new OffsetAddress(939615, 939608) },
            { 1, new OffsetAddress(939841, 939834) },
            { 2, new OffsetAddress(940022, 940015) },
            { 3, new OffsetAddress(939728, 939721) },
            { 12, new OffsetAddress(939000, 938996, singleByteY: true) },
            { 13, new OffsetAddress(939031, 939027, singleByteY: true) },
            { 23, new OffsetAddress(938858, 938851) },
            { 43, new OffsetAddress(938935, 938928) },
            { 44, new OffsetAddress(938969, 938962) },
            { 121, new OffsetAddress(939943, 939936) },
            { 122, new OffsetAddress(939943, 939936) },
            { 123, new OffsetAddress(939574, 939567) },
            { 124, new OffsetAddress(939536, 939529) },
            { 125, new OffsetAddress(939574, 939567) }
        };

        /// <summary>Only images of this file have offsets in the executable.</summary>
        public static bool AppliesTo(string gm1FileName)
        {
            return gm1FileName != null && gm1FileName.IndexOf("anim_castle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryGet(int imageIndex, out OffsetAddress address) => AddressesByImageIndex.TryGetValue(imageIndex, out address);
    }

    public readonly struct OffsetAddress
    {
        public OffsetAddress(int x, int y, bool singleByteY = false)
        {
            X = x;
            Y = y;
            SingleByteY = singleByteY;
        }

        /// <summary>Address of the signed 1 byte x offset.</summary>
        public int X { get; }

        /// <summary>Address of the y offset, a 4 byte integer or a signed byte (<see cref="SingleByteY"/>).</summary>
        public int Y { get; }

        public bool SingleByteY { get; }
    }
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>Image indices that have an offset in the executable, sorted.</summary>
        public static IReadOnlyList<int> ImageIndices { get; } = AddressesByImageIndex.Keys.OrderBy(index => index).ToList();

        /// <summary>
        /// Every offset address with the images that use it, sorted by the first image. Some images share
        /// one offset (121 and 122, 123 and 125).
        /// </summary>
        public static IReadOnlyList<OffsetGroup> Groups { get; } = AddressesByImageIndex
            .GroupBy(entry => entry.Value, entry => entry.Key)
            .Select(group => new OffsetGroup(group.Key, group.OrderBy(index => index).ToList()))
            .OrderBy(group => group.ImageIndices[0])
            .ToList();

        /// <summary>
        /// Every byte of every offset (Extreme addresses). These bytes differ between installations, e.g.
        /// after older versions of this program patched the executable.
        /// </summary>
        public static IReadOnlyCollection<int> VariableAddresses { get; } =
            new HashSet<int>(AddressesByImageIndex.Values.SelectMany(address => address.Bytes));

        /// <summary>The group of <paramref name="address"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The address is not a castle offset.</exception>
        public static OffsetGroup GroupOf(OffsetAddress address)
        {
            return Groups.FirstOrDefault(group => group.Address.Equals(address))
                ?? throw new ArgumentOutOfRangeException(nameof(address), "This is not a castle offset address.");
        }

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

        public int YLength => SingleByteY ? 1 : sizeof(int);

        /// <summary>First address of the x and y bytes.</summary>
        public int Start => Math.Min(X, Y);

        /// <summary>Address after the last x or y byte.</summary>
        public int End => Math.Max(X + 1, Y + YLength);

        /// <summary>The addresses of all x and y bytes.</summary>
        public IEnumerable<int> Bytes => new[] { X }.Concat(Enumerable.Range(Y, YLength));

        /// <summary>
        /// The bytes to write for <paramref name="offset"/>: x as signed byte, y as little endian integer or
        /// signed byte. Signed bytes are clamped to -128..127.
        /// </summary>
        public IReadOnlyList<OffsetWrite> GetWrites(BuildingOffset offset)
        {
            var y = new byte[YLength];
            if (SingleByteY)
            {
                y[0] = unchecked((byte)ClampToSByte(offset.Y));
            }
            else
            {
                BinaryPrimitives.WriteInt32LittleEndian(y, offset.Y);
            }

            return new[]
            {
                new OffsetWrite(X, new[] { unchecked((byte)ClampToSByte(offset.X)) }),
                new OffsetWrite(Y, y),
            };
        }

        /// <summary>Reads the offset from the bytes starting at <see cref="Start"/>.</summary>
        public BuildingOffset Read(ReadOnlySpan<byte> bytesFromStart)
        {
            int x = unchecked((sbyte)bytesFromStart[X - Start]);
            int y = SingleByteY
                ? unchecked((sbyte)bytesFromStart[Y - Start])
                : BinaryPrimitives.ReadInt32LittleEndian(bytesFromStart.Slice(Y - Start));
            return new BuildingOffset(x, y);
        }

        private static sbyte ClampToSByte(int value) => (sbyte)Math.Max(sbyte.MinValue, Math.Min(sbyte.MaxValue, value));
    }

    /// <summary>An offset address and the images drawn with it.</summary>
    public sealed class OffsetGroup
    {
        public OffsetGroup(OffsetAddress address, IReadOnlyList<int> imageIndices)
        {
            Address = address;
            ImageIndices = imageIndices;
        }

        public OffsetAddress Address { get; }

        /// <summary>Sorted, at least one.</summary>
        public IReadOnlyList<int> ImageIndices { get; }

        /// <summary>A stable identifier, e.g. "image121".</summary>
        public string Name => "image" + ImageIndices[0];
    }

    /// <summary>Bytes to write at an address.</summary>
    public sealed class OffsetWrite
    {
        public OffsetWrite(int address, byte[] bytes)
        {
            Address = address;
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        public int Address { get; }

        public IReadOnlyList<byte> Bytes { get; }
    }
}

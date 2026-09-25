using System;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;

namespace Gm1KonverterCrossPlatform.Tests.Support
{
    /// <summary>Stand-ins for Stronghold Crusader.exe and Stronghold_Crusader_Extreme.exe.</summary>
    internal static class FakeExecutables
    {
        /// <summary>Big enough for every offset address, the real executables are about 2 MB.</summary>
        public const int Size = 1_000_000;

        /// <summary>Random bytes like machine code: short byte sequences are already unique. Same content for the same seed.</summary>
        public static byte[] Content(int seed = 1, int size = Size)
        {
            var bytes = new byte[size];
            new Random(seed).NextBytes(bytes);
            return bytes;
        }

        public static StrongholdExecutable Crusader(int seed = 1) => StrongholdExecutable.Crusader(Content(seed));

        public static StrongholdExecutable Extreme(int seed = 1) => StrongholdExecutable.Extreme(Content(seed));

        /// <summary>An Extreme executable with the same code as <paramref name="crusader"/>, 912 bytes later (like the real ones).</summary>
        public static StrongholdExecutable ExtremeOf(StrongholdExecutable crusader)
        {
            int shift = -CastleOffsetAddresses.CrusaderAddressShift;
            var bytes = Content(seed: 99, size: crusader.Bytes.Length + shift);
            Array.Copy(crusader.Bytes, 0, bytes, shift, crusader.Bytes.Length);
            return StrongholdExecutable.Extreme(bytes);
        }

        /// <summary>Patches the offset into the file like older versions of the program did.</summary>
        public static void Apply(StrongholdExecutable executable, int imageIndex, BuildingOffset offset)
        {
            if (!CastleOffsetAddresses.TryGet(imageIndex, out var address))
            {
                throw new ArgumentOutOfRangeException(nameof(imageIndex));
            }

            foreach (var write in address.GetWrites(offset))
            {
                for (int i = 0; i < write.Bytes.Count; i++)
                {
                    executable.Bytes[write.Address + executable.AddressShift + i] = write.Bytes[i];
                }
            }
        }
    }
}

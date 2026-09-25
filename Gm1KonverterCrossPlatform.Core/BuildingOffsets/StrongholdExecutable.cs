using System;
using System.Collections.Generic;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;

namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// The content of Stronghold Crusader.exe or Stronghold_Crusader_Extreme.exe. Offsets are addressed like
    /// in the Extreme executable (<see cref="CastleOffsetAddresses"/>) and shifted by <see cref="AddressShift"/>.
    /// </summary>
    public sealed class StrongholdExecutable
    {
        public StrongholdExecutable(string name, byte[] bytes, int addressShift)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            AddressShift = addressShift;
        }

        /// <summary>File name, e.g. "Stronghold Crusader.exe".</summary>
        public string Name { get; }

        public byte[] Bytes { get; }

        /// <summary>Added to Extreme addresses to get the address in this file.</summary>
        public int AddressShift { get; }

        /// <summary>The executables that exist in <paramref name="folder"/>, Stronghold Crusader.exe first.</summary>
        public static IReadOnlyList<StrongholdExecutable> LoadAll(StrongholdFolder folder)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));

            var executables = new List<StrongholdExecutable>();
            if (File.Exists(folder.CrusaderExecutablePath))
            {
                executables.Add(new StrongholdExecutable(StrongholdFolder.CrusaderExecutable, File.ReadAllBytes(folder.CrusaderExecutablePath), CastleOffsetAddresses.CrusaderAddressShift));
            }

            if (File.Exists(folder.ExtremeExecutablePath))
            {
                executables.Add(new StrongholdExecutable(StrongholdFolder.ExtremeExecutable, File.ReadAllBytes(folder.ExtremeExecutablePath), 0));
            }

            return executables;
        }

        public static StrongholdExecutable Crusader(byte[] bytes) => new StrongholdExecutable(StrongholdFolder.CrusaderExecutable, bytes, CastleOffsetAddresses.CrusaderAddressShift);

        public static StrongholdExecutable Extreme(byte[] bytes) => new StrongholdExecutable(StrongholdFolder.ExtremeExecutable, bytes, 0);

        /// <summary>True if every byte of the offset lies inside the file.</summary>
        public bool Contains(OffsetAddress address) => Contains(address.Start, address.End - address.Start);

        /// <summary>True if <paramref name="length"/> bytes at the Extreme address lie inside the file.</summary>
        public bool Contains(int address, int length)
        {
            long start = (long)address + AddressShift;
            return start >= 0 && length >= 0 && start + length <= Bytes.Length;
        }

        public bool TryRead(OffsetAddress address, out BuildingOffset offset)
        {
            if (!Contains(address))
            {
                offset = default;
                return false;
            }

            offset = address.Read(Bytes.AsSpan(address.Start + AddressShift));
            return true;
        }
    }
}

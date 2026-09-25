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

        /// <summary>
        /// Added to Extreme addresses to get the address in this file. For Stronghold Crusader.exe this is the
        /// known difference of -912 bytes; <see cref="Ucp.UcpOffsetModule"/> measures it with the known patterns.
        /// </summary>
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
        public bool Contains(OffsetAddress address) => Contains(address, AddressShift);

        /// <summary>True if every byte of the offset lies inside the file when shifted by <paramref name="shift"/>.</summary>
        public bool Contains(OffsetAddress address, int shift) => Contains(address.Start, address.End - address.Start, shift);

        /// <summary>True if <paramref name="length"/> bytes at the Extreme address lie inside the file.</summary>
        public bool Contains(int address, int length) => Contains(address, length, AddressShift);

        /// <summary>True if <paramref name="length"/> bytes at the Extreme address + <paramref name="shift"/> lie inside the file.</summary>
        public bool Contains(int address, int length, int shift)
        {
            long start = (long)address + shift;
            return start >= 0 && length >= 0 && start + length <= Bytes.Length;
        }

        public bool TryRead(OffsetAddress address, out BuildingOffset offset) => TryRead(address, AddressShift, out offset);

        /// <summary>Reads the offset at the Extreme address + <paramref name="shift"/>.</summary>
        public bool TryRead(OffsetAddress address, int shift, out BuildingOffset offset)
        {
            if (!Contains(address, shift))
            {
                offset = default;
                return false;
            }

            offset = address.Read(Bytes.AsSpan(address.Start + shift));
            return true;
        }
    }
}

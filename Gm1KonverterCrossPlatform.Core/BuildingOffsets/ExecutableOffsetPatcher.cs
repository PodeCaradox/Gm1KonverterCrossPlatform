using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.IO;

namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// Patches the building offsets directly into Stronghold Crusader.exe and Stronghold_Crusader_Extreme.exe.
    /// Only executables that exist are patched.
    /// </summary>
    public sealed class ExecutableOffsetPatcher : IBuildingOffsetTarget
    {
        private readonly IReadOnlyList<Executable> executables;

        private ExecutableOffsetPatcher(IReadOnlyList<Executable> executables)
        {
            this.executables = executables;
        }

        public bool HasExecutables => executables.Count > 0;

        public static ExecutableOffsetPatcher Load(StrongholdFolder folder)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));

            var executables = new List<Executable>();
            if (File.Exists(folder.CrusaderExecutablePath))
            {
                executables.Add(new Executable(folder.CrusaderExecutablePath, File.ReadAllBytes(folder.CrusaderExecutablePath), CastleOffsetAddresses.CrusaderAddressShift));
            }

            if (File.Exists(folder.ExtremeExecutablePath))
            {
                executables.Add(new Executable(folder.ExtremeExecutablePath, File.ReadAllBytes(folder.ExtremeExecutablePath), 0));
            }

            return new ExecutableOffsetPatcher(executables);
        }

        /// <summary>For tests: patches the given executable images in memory.</summary>
        internal static ExecutableOffsetPatcher FromBytes(byte[]? crusader, byte[]? extreme)
        {
            var executables = new List<Executable>();
            if (crusader != null) executables.Add(new Executable(string.Empty, crusader, CastleOffsetAddresses.CrusaderAddressShift));
            if (extreme != null) executables.Add(new Executable(string.Empty, extreme, 0));
            return new ExecutableOffsetPatcher(executables);
        }

        public bool Supports(int imageIndex) => HasExecutables && CastleOffsetAddresses.TryGet(imageIndex, out _);

        /// <summary>Reads the offset from the first executable (Stronghold Crusader.exe if it exists).</summary>
        public bool TryRead(int imageIndex, out BuildingOffset offset)
        {
            offset = default;
            return CastleOffsetAddresses.TryGet(imageIndex, out var address)
                && executables.Count > 0
                && executables[0].TryRead(address, out offset);
        }

        /// <summary>Changes the offset in memory, call <see cref="Save"/> to write the executables.</summary>
        /// <exception cref="InvalidDataException">The executable does not contain the expected address (unknown version).</exception>
        public void Write(int imageIndex, BuildingOffset offset)
        {
            if (!CastleOffsetAddresses.TryGet(imageIndex, out var address))
            {
                throw new ArgumentOutOfRangeException(nameof(imageIndex), imageIndex, "This image has no offset in the executable.");
            }

            if (!HasExecutables)
            {
                throw new InvalidOperationException("No Stronghold executable was found.");
            }

            foreach (var executable in executables)
            {
                executable.Write(address, offset);
            }
        }

        public void Save()
        {
            foreach (var executable in executables.Where(e => e.Path.Length > 0))
            {
                File.WriteAllBytes(executable.Path, executable.Bytes);
            }
        }

        internal byte[] GetBytes(int index) => executables[index].Bytes;

        private sealed class Executable
        {
            private readonly int addressShift;

            public Executable(string path, byte[] bytes, int addressShift)
            {
                Path = path;
                Bytes = bytes;
                this.addressShift = addressShift;
            }

            public string Path { get; }

            public byte[] Bytes { get; }

            public bool TryRead(OffsetAddress address, out BuildingOffset offset)
            {
                int x = address.X + addressShift;
                int y = address.Y + addressShift;
                int yLength = address.SingleByteY ? 1 : sizeof(int);
                if (!Contains(x, 1) || !Contains(y, yLength))
                {
                    offset = default;
                    return false;
                }

                int yValue = address.SingleByteY
                    ? unchecked((sbyte)Bytes[y])
                    : BinaryPrimitives.ReadInt32LittleEndian(Bytes.AsSpan(y));
                offset = new BuildingOffset(unchecked((sbyte)Bytes[x]), yValue);
                return true;
            }

            public void Write(OffsetAddress address, BuildingOffset offset)
            {
                int x = address.X + addressShift;
                int y = address.Y + addressShift;
                int yLength = address.SingleByteY ? 1 : sizeof(int);
                if (!Contains(x, 1) || !Contains(y, yLength))
                {
                    throw new InvalidDataException($"\"{Path}\" is too small for the offset addresses, this version of Stronghold is not supported.");
                }

                Bytes[x] = unchecked((byte)ClampToSByte(offset.X));
                if (address.SingleByteY)
                {
                    Bytes[y] = unchecked((byte)ClampToSByte(offset.Y));
                }
                else
                {
                    BinaryPrimitives.WriteInt32LittleEndian(Bytes.AsSpan(y), offset.Y);
                }
            }

            private bool Contains(int address, int length) => address >= 0 && address + length <= Bytes.Length;

            private static sbyte ClampToSByte(int value) => (sbyte)Math.Max(sbyte.MinValue, Math.Min(sbyte.MaxValue, value));
        }
    }
}

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;
using Gm1KonverterCrossPlatform.Core.IO;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.BuildingOffsets
{
    public class ExecutableOffsetPatcherTests
    {
        /// <summary>Big enough for every address, the executables are about 2 MB.</summary>
        private const int ExecutableSize = 1_000_000;

        private const int CrusaderShift = 912;

        /// <summary>Image index, address of X and address of Y in Stronghold_Crusader_Extreme.exe (from the original implementation).</summary>
        public static IEnumerable<object[]> Int32Addresses => new[]
        {
            new object[] { 0, 939615, 939608 },
            new object[] { 1, 939841, 939834 },
            new object[] { 2, 940022, 940015 },
            new object[] { 3, 939728, 939721 },
            new object[] { 23, 938858, 938851 },
            new object[] { 43, 938935, 938928 },
            new object[] { 44, 938969, 938962 },
            new object[] { 121, 939943, 939936 },
            new object[] { 122, 939943, 939936 },
            new object[] { 123, 939574, 939567 },
            new object[] { 124, 939536, 939529 },
            new object[] { 125, 939574, 939567 },
        };

        public static IEnumerable<object[]> SingleByteAddresses => new[]
        {
            new object[] { 12, 939000, 938996 },
            new object[] { 13, 939031, 939027 },
        };

        [Theory]
        [MemberData(nameof(Int32Addresses))]
        public void Write_Extreme_WritesSignedByteXAndInt32YAtDocumentedAddresses(int imageIndex, int xAddress, int yAddress)
        {
            var extreme = FakeExecutable();
            var patcher = ExecutableOffsetPatcher.FromBytes(null, extreme);

            patcher.Write(imageIndex, new BuildingOffset(-3, -123456));

            Assert.Equal(unchecked((byte)(sbyte)-3), extreme[xAddress]);
            Assert.Equal(-123456, BinaryPrimitives.ReadInt32LittleEndian(extreme.AsSpan(yAddress)));
            AssertOnlyChanged(extreme, xAddress, 1, yAddress, 4);
        }

        [Theory]
        [MemberData(nameof(Int32Addresses))]
        public void Write_Crusader_UsesAddresses912BytesEarlier(int imageIndex, int xAddress, int yAddress)
        {
            var crusader = FakeExecutable();
            var patcher = ExecutableOffsetPatcher.FromBytes(crusader, null);

            patcher.Write(imageIndex, new BuildingOffset(100, 70000));

            Assert.Equal(100, crusader[xAddress - CrusaderShift]);
            Assert.Equal(70000, BinaryPrimitives.ReadInt32LittleEndian(crusader.AsSpan(yAddress - CrusaderShift)));
            AssertOnlyChanged(crusader, xAddress - CrusaderShift, 1, yAddress - CrusaderShift, 4);
        }

        [Theory]
        [MemberData(nameof(SingleByteAddresses))]
        public void Write_Image12And13_WritesYAsSignedByte(int imageIndex, int xAddress, int yAddress)
        {
            var crusader = FakeExecutable();
            var extreme = FakeExecutable();
            var patcher = ExecutableOffsetPatcher.FromBytes(crusader, extreme);

            patcher.Write(imageIndex, new BuildingOffset(5, -7));

            Assert.Equal(5, extreme[xAddress]);
            Assert.Equal(unchecked((byte)(sbyte)-7), extreme[yAddress]);
            AssertOnlyChanged(extreme, xAddress, 1, yAddress, 1);
            Assert.Equal(5, crusader[xAddress - CrusaderShift]);
            Assert.Equal(unchecked((byte)(sbyte)-7), crusader[yAddress - CrusaderShift]);
            AssertOnlyChanged(crusader, xAddress - CrusaderShift, 1, yAddress - CrusaderShift, 1);
        }

        [Fact]
        public void Write_BothExecutables_PatchesBoth()
        {
            var crusader = FakeExecutable();
            var extreme = FakeExecutable();
            var patcher = ExecutableOffsetPatcher.FromBytes(crusader, extreme);

            patcher.Write(0, new BuildingOffset(-1, 42));

            Assert.Equal(0xFF, crusader[939615 - CrusaderShift]);
            Assert.Equal(42, BinaryPrimitives.ReadInt32LittleEndian(crusader.AsSpan(939608 - CrusaderShift)));
            Assert.Equal(0xFF, extreme[939615]);
            Assert.Equal(42, BinaryPrimitives.ReadInt32LittleEndian(extreme.AsSpan(939608)));
            Assert.Same(crusader, patcher.GetBytes(0));
            Assert.Same(extreme, patcher.GetBytes(1));
        }

        [Theory]
        [InlineData(200, 127)]
        [InlineData(128, 127)]
        [InlineData(127, 127)]
        [InlineData(-128, -128)]
        [InlineData(-129, -128)]
        [InlineData(-500, -128)]
        public void Write_ClampsSignedByteValues(int value, int expected)
        {
            var extreme = FakeExecutable();
            var patcher = ExecutableOffsetPatcher.FromBytes(null, extreme);

            patcher.Write(0, new BuildingOffset(value, value));
            patcher.Write(12, new BuildingOffset(value, value));

            Assert.Equal(expected, unchecked((sbyte)extreme[939615]));
            Assert.Equal(value, BinaryPrimitives.ReadInt32LittleEndian(extreme.AsSpan(939608)));
            Assert.Equal(expected, unchecked((sbyte)extreme[939000]));
            Assert.Equal(expected, unchecked((sbyte)extreme[938996]));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Write_ExecutableTooSmall_ThrowsInvalidDataException(bool crusader)
        {
            // the Y address fits, the X address (7 bytes later) does not
            int size = crusader ? 939612 - CrusaderShift : 939612;
            var small = new byte[size];
            var patcher = crusader ? ExecutableOffsetPatcher.FromBytes(small, null) : ExecutableOffsetPatcher.FromBytes(null, small);

            Assert.Throws<InvalidDataException>(() => patcher.Write(0, new BuildingOffset(1, 1)));
            Assert.All(small, b => Assert.Equal(0, b));
        }

        [Fact]
        public void Write_AddressAtLastByte_IsAccepted()
        {
            // X of image 2 (940022) is the highest address
            var extreme = new byte[940023];
            var patcher = ExecutableOffsetPatcher.FromBytes(null, extreme);

            patcher.Write(2, new BuildingOffset(1, 2));

            Assert.Equal(1, extreme[940022]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        [InlineData(126)]
        public void Write_UnknownImageIndex_ThrowsArgumentOutOfRange(int imageIndex)
        {
            var patcher = ExecutableOffsetPatcher.FromBytes(FakeExecutable(), FakeExecutable());

            Assert.Throws<ArgumentOutOfRangeException>(() => patcher.Write(imageIndex, new BuildingOffset(1, 1)));
        }

        [Fact]
        public void Write_NoExecutables_ThrowsInvalidOperation()
        {
            var patcher = ExecutableOffsetPatcher.FromBytes(null, null);

            Assert.Throws<InvalidOperationException>(() => patcher.Write(0, new BuildingOffset(1, 1)));
        }

        [Theory]
        [InlineData(0, -5, 123456)]
        [InlineData(12, -5, 7)]
        [InlineData(124, 127, int.MinValue)]
        public void TryRead_ReturnsWrittenOffset(int imageIndex, int x, int y)
        {
            var patcher = ExecutableOffsetPatcher.FromBytes(FakeExecutable(), FakeExecutable());
            patcher.Write(imageIndex, new BuildingOffset(x, y));

            Assert.True(patcher.TryRead(imageIndex, out var offset));

            Assert.Equal((x, y), (offset.X, offset.Y));
        }

        [Fact]
        public void TryRead_ReadsCrusaderExecutableFirst()
        {
            var crusader = FakeExecutable();
            var extreme = FakeExecutable();
            crusader[939615 - CrusaderShift] = 11;
            extreme[939615] = 22;
            var patcher = ExecutableOffsetPatcher.FromBytes(crusader, extreme);

            Assert.True(patcher.TryRead(0, out var offset));

            Assert.Equal(11, offset.X);
        }

        [Fact]
        public void TryRead_OnlyExtremeExecutable_ReadsExtremeAddresses()
        {
            var extreme = FakeExecutable();
            extreme[939615] = 22;
            var patcher = ExecutableOffsetPatcher.FromBytes(null, extreme);

            Assert.True(patcher.TryRead(0, out var offset));

            Assert.Equal(22, offset.X);
        }

        [Fact]
        public void TryRead_UnknownIndexOrTooSmallExecutable_ReturnsFalse()
        {
            Assert.False(ExecutableOffsetPatcher.FromBytes(FakeExecutable(), null).TryRead(5, out _));
            Assert.False(ExecutableOffsetPatcher.FromBytes(new byte[100], null).TryRead(0, out _));
            Assert.False(ExecutableOffsetPatcher.FromBytes(null, null).TryRead(0, out _));
        }

        [Fact]
        public void Supports_KnownImagesOnlyWhenAnExecutableExists()
        {
            var patcher = ExecutableOffsetPatcher.FromBytes(FakeExecutable(), null);

            Assert.True(patcher.Supports(0));
            Assert.True(patcher.Supports(125));
            Assert.False(patcher.Supports(5));
            Assert.False(ExecutableOffsetPatcher.FromBytes(null, null).Supports(0));
        }

        [Theory]
        [InlineData("anim_castle.gm1", true)]
        [InlineData("ANIM_CASTLE.GM1", true)]
        [InlineData("anim_castle_modded.gm1", true)]
        [InlineData("tile_castle.gm1", false)]
        public void CastleOffsetAddresses_AppliesToCastleAnimationFile(string fileName, bool expected)
        {
            Assert.Equal(expected, CastleOffsetAddresses.AppliesTo(fileName));
        }

        [Fact]
        public void LoadAndSave_PatchesExecutablesInStrongholdFolder()
        {
            using var temp = new TempDirectory();
            var folder = new StrongholdFolder(temp.Path);
            File.WriteAllBytes(folder.CrusaderExecutablePath, FakeExecutable());
            File.WriteAllBytes(folder.ExtremeExecutablePath, FakeExecutable());
            var patcher = ExecutableOffsetPatcher.Load(folder);

            patcher.Write(1, new BuildingOffset(-8, 999));
            patcher.Save();

            Assert.True(patcher.HasExecutables);
            var crusader = File.ReadAllBytes(folder.CrusaderExecutablePath);
            var extreme = File.ReadAllBytes(folder.ExtremeExecutablePath);
            Assert.Equal(unchecked((byte)(sbyte)-8), crusader[939841 - CrusaderShift]);
            Assert.Equal(999, BinaryPrimitives.ReadInt32LittleEndian(crusader.AsSpan(939834 - CrusaderShift)));
            Assert.Equal(unchecked((byte)(sbyte)-8), extreme[939841]);
            Assert.Equal(999, BinaryPrimitives.ReadInt32LittleEndian(extreme.AsSpan(939834)));
        }

        [Fact]
        public void Load_OnlyCrusaderExecutable_PatchesOnlyIt()
        {
            using var temp = new TempDirectory();
            var folder = new StrongholdFolder(temp.Path);
            File.WriteAllBytes(folder.CrusaderExecutablePath, FakeExecutable());
            var patcher = ExecutableOffsetPatcher.Load(folder);

            patcher.Write(0, new BuildingOffset(3, 4));
            patcher.Save();

            Assert.Equal(3, File.ReadAllBytes(folder.CrusaderExecutablePath)[939615 - CrusaderShift]);
            Assert.False(File.Exists(folder.ExtremeExecutablePath));
        }

        [Fact]
        public void Load_NoExecutables_HasNoExecutables()
        {
            using var temp = new TempDirectory();

            var patcher = ExecutableOffsetPatcher.Load(new StrongholdFolder(temp.Path));

            Assert.False(patcher.HasExecutables);
            Assert.False(patcher.Supports(0));
        }

        /// <summary>Filled with a pattern so that unintended changes are visible.</summary>
        private static byte[] FakeExecutable()
        {
            var bytes = new byte[ExecutableSize];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(i * 31 + 7);
            }

            return bytes;
        }

        private static void AssertOnlyChanged(byte[] bytes, int xAddress, int xLength, int yAddress, int yLength)
        {
            var original = FakeExecutable();
            for (int i = 0; i < bytes.Length; i++)
            {
                bool expectedChange = (i >= xAddress && i < xAddress + xLength) || (i >= yAddress && i < yAddress + yLength);
                if (!expectedChange && bytes[i] != original[i])
                {
                    Assert.Fail($"Byte {i} changed unexpectedly.");
                }
            }
        }
    }
}

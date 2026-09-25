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
    /// <summary>
    /// Addresses and encoding of the castle building offsets, as the original implementation patched them
    /// into the executables.
    /// </summary>
    public class OffsetAddressTests
    {
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
        public void GetWrites_Extreme_WritesSignedByteXAndInt32YAtDocumentedAddresses(int imageIndex, int xAddress, int yAddress)
        {
            var extreme = FakeExecutables.Extreme();

            FakeExecutables.Apply(extreme, imageIndex, new BuildingOffset(-3, -123456));

            Assert.Equal(unchecked((byte)(sbyte)-3), extreme.Bytes[xAddress]);
            Assert.Equal(-123456, BinaryPrimitives.ReadInt32LittleEndian(extreme.Bytes.AsSpan(yAddress)));
            AssertOnlyChanged(extreme.Bytes, xAddress, 1, yAddress, 4);
        }

        [Theory]
        [MemberData(nameof(Int32Addresses))]
        public void GetWrites_Crusader_UsesAddresses912BytesEarlier(int imageIndex, int xAddress, int yAddress)
        {
            var crusader = FakeExecutables.Crusader();

            FakeExecutables.Apply(crusader, imageIndex, new BuildingOffset(100, 70000));

            Assert.Equal(100, crusader.Bytes[xAddress - CrusaderShift]);
            Assert.Equal(70000, BinaryPrimitives.ReadInt32LittleEndian(crusader.Bytes.AsSpan(yAddress - CrusaderShift)));
            AssertOnlyChanged(crusader.Bytes, xAddress - CrusaderShift, 1, yAddress - CrusaderShift, 4);
        }

        [Theory]
        [MemberData(nameof(SingleByteAddresses))]
        public void GetWrites_Image12And13_WritesYAsSignedByte(int imageIndex, int xAddress, int yAddress)
        {
            var extreme = FakeExecutables.Extreme();

            FakeExecutables.Apply(extreme, imageIndex, new BuildingOffset(5, -7));

            Assert.Equal(5, extreme.Bytes[xAddress]);
            Assert.Equal(unchecked((byte)(sbyte)-7), extreme.Bytes[yAddress]);
            AssertOnlyChanged(extreme.Bytes, xAddress, 1, yAddress, 1);
        }

        [Theory]
        [InlineData(200, 127)]
        [InlineData(128, 127)]
        [InlineData(127, 127)]
        [InlineData(-128, -128)]
        [InlineData(-129, -128)]
        [InlineData(-500, -128)]
        public void GetWrites_ClampsSignedByteValues(int value, int expected)
        {
            var extreme = FakeExecutables.Extreme();

            FakeExecutables.Apply(extreme, 0, new BuildingOffset(value, value));
            FakeExecutables.Apply(extreme, 12, new BuildingOffset(value, value));

            Assert.Equal(expected, unchecked((sbyte)extreme.Bytes[939615]));
            Assert.Equal(value, BinaryPrimitives.ReadInt32LittleEndian(extreme.Bytes.AsSpan(939608)));
            Assert.Equal(expected, unchecked((sbyte)extreme.Bytes[939000]));
            Assert.Equal(expected, unchecked((sbyte)extreme.Bytes[938996]));
        }

        [Theory]
        [InlineData(0, 939608, 939616)]
        [InlineData(12, 938996, 939001)]
        [InlineData(2, 940015, 940023)]
        public void StartAndEnd_CoverXAndY(int imageIndex, int start, int end)
        {
            Assert.True(CastleOffsetAddresses.TryGet(imageIndex, out var address));

            Assert.Equal((start, end), (address.Start, address.End));
        }

        [Fact]
        public void VariableAddresses_ContainEveryOffsetByte()
        {
            Assert.Contains(939615, CastleOffsetAddresses.VariableAddresses);
            Assert.Contains(939611, CastleOffsetAddresses.VariableAddresses);
            Assert.Contains(938996, CastleOffsetAddresses.VariableAddresses);
            Assert.DoesNotContain(938997, CastleOffsetAddresses.VariableAddresses);
            Assert.DoesNotContain(939612, CastleOffsetAddresses.VariableAddresses);
        }

        [Theory]
        [InlineData(0, -5, 123456)]
        [InlineData(12, -5, 7)]
        [InlineData(124, 127, int.MinValue)]
        public void TryRead_ReturnsWrittenOffset(int imageIndex, int x, int y)
        {
            var crusader = FakeExecutables.Crusader();
            FakeExecutables.Apply(crusader, imageIndex, new BuildingOffset(x, y));
            CastleOffsetAddresses.TryGet(imageIndex, out var address);

            Assert.True(crusader.TryRead(address, out var offset));

            Assert.Equal((x, y), (offset.X, offset.Y));
        }

        [Fact]
        public void TryRead_TooSmallExecutable_ReturnsFalse()
        {
            CastleOffsetAddresses.TryGet(0, out var address);

            // the Y address fits, the X address (7 bytes later) does not
            Assert.False(StrongholdExecutable.Extreme(new byte[939612]).TryRead(address, out _));
            Assert.False(StrongholdExecutable.Crusader(new byte[939612 - CrusaderShift]).TryRead(address, out _));
            Assert.True(StrongholdExecutable.Extreme(new byte[939616]).TryRead(address, out _));
        }

        [Theory]
        [InlineData("anim_castle.gm1", true)]
        [InlineData("ANIM_CASTLE.GM1", true)]
        [InlineData("anim_castle_modded.gm1", true)]
        [InlineData("tile_castle.gm1", false)]
        public void AppliesTo_CastleAnimationFile(string fileName, bool expected)
        {
            Assert.Equal(expected, CastleOffsetAddresses.AppliesTo(fileName));
        }

        [Fact]
        public void LoadAll_ReadsExistingExecutablesCrusaderFirst()
        {
            using var temp = new TempDirectory();
            var folder = new StrongholdFolder(temp.Path);
            Assert.Empty(StrongholdExecutable.LoadAll(folder));

            File.WriteAllBytes(folder.ExtremeExecutablePath, new byte[] { 2 });
            File.WriteAllBytes(folder.CrusaderExecutablePath, new byte[] { 1 });
            var executables = StrongholdExecutable.LoadAll(folder);

            Assert.Equal(new[] { StrongholdFolder.CrusaderExecutable, StrongholdFolder.ExtremeExecutable }, new[] { executables[0].Name, executables[1].Name });
            Assert.Equal((-CrusaderShift, 0), (executables[0].AddressShift, executables[1].AddressShift));
            Assert.Equal(new byte[] { 1 }, executables[0].Bytes);
        }

        private static void AssertOnlyChanged(byte[] bytes, int xAddress, int xLength, int yAddress, int yLength)
        {
            var original = FakeExecutables.Extreme().Bytes;
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

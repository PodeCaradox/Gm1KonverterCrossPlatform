using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;
using Gm1KonverterCrossPlatform.Core.Ucp;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Ucp
{
    public class UcpOffsetModuleTests : IDisposable
    {
        private readonly TempDirectory temp = new TempDirectory();
        private readonly UcpFolder ucp;
        private readonly UcpExtensionInfo info = new UcpExtensionInfo("My-Castle-Offsets", "1.0.0", "My Castle (Offsets)", "Pode");

        public UcpOffsetModuleTests()
        {
            ucp = new UcpFolder(temp.Path);
        }

        public void Dispose() => temp.Dispose();

        [Fact]
        public void Patches_ChangeTheSameBytesAsPatchingTheExecutable()
        {
            var crusader = FakeExecutables.Crusader(seed: 1);
            var extreme = FakeExecutables.Extreme(seed: 2);
            var module = Open(crusader, extreme);
            var offsets = RandomOffsets(new Random(5));

            foreach (var game in new[] { crusader, extreme })
            {
                var expected = Copy(game);
                var actual = Copy(game);
                foreach (var entry in offsets)
                {
                    FakeExecutables.Apply(expected, entry.Key, entry.Value);
                }

                RunLikeInitScript(module, actual, offsets);

                Assert.Equal(expected.Bytes, actual.Bytes);
            }
        }

        [Fact]
        public void Patches_ExecutablesPatchedByOlderVersion_AreFoundAsWell()
        {
            // the module is created from executables that an older version patched ...
            var crusader = FakeExecutables.Crusader();
            var extreme = FakeExecutables.ExtremeOf(crusader);
            foreach (var entry in RandomOffsets(new Random(1)))
            {
                FakeExecutables.Apply(crusader, entry.Key, entry.Value);
                FakeExecutables.Apply(extreme, entry.Key, entry.Value);
            }

            var module = Open(crusader, extreme);
            var offsets = RandomOffsets(new Random(2));

            // ... and the game runs with other offsets in memory
            var game = Copy(extreme);
            foreach (var entry in RandomOffsets(new Random(3)))
            {
                FakeExecutables.Apply(game, entry.Key, entry.Value);
            }

            var expected = Copy(game);
            foreach (var entry in offsets)
            {
                FakeExecutables.Apply(expected, entry.Key, entry.Value);
            }

            RunLikeInitScript(module, game, offsets);

            Assert.Equal(expected.Bytes, game.Bytes);
        }

        [Fact]
        public void CreatePatches_SameCodeInBothExecutables_OnePatternFitsBoth()
        {
            var crusader = FakeExecutables.Crusader();
            var module = Open(crusader, FakeExecutables.ExtremeOf(crusader));

            foreach (int imageIndex in CastleOffsetAddresses.ImageIndices)
            {
                Assert.Single(Patches(module, imageIndex));
            }
        }

        [Fact]
        public void CreatePatches_DifferentExecutables_OnePatternEach()
        {
            var module = Open(FakeExecutables.Crusader(seed: 1), FakeExecutables.Extreme(seed: 2));

            Assert.Equal(2, Patches(module, 0).Count);
        }

        [Fact]
        public void CreatePatches_PatternsAreShortForUniqueCode()
        {
            var module = Open(FakeExecutables.Extreme());

            var patch = Patches(module, 0).Single();

            // 8 bytes offset (y, 3 bytes code, x) and 8 bytes context on both sides
            Assert.Equal(24, patch.Pattern.Length);
            Assert.Equal("? ? ? ?", string.Join(" ", patch.Pattern.ToString().Split(' ').Skip(8).Take(4)));
            Assert.Equal(new[] { 15, 8 }, patch.Writes.Select(write => write.Address));
        }

        [Fact]
        public void CreatePatches_RepeatedCode_ExtendsPatternUntilUnique()
        {
            var extreme = FakeExecutables.Extreme();
            CastleOffsetAddresses.TryGet(0, out var address);

            // the same code 30 bytes around the offset exists a second time
            Array.Copy(extreme.Bytes, address.Start - 30, extreme.Bytes, 100_000, address.End - address.Start + 60);
            var module = Open(extreme);

            var patch = Patches(module, 0).Single();

            Assert.True(patch.Pattern.Length > 60, patch.Pattern.ToString());
            Assert.Equal(new[] { address.Start - patch.Writes.Last().Address }, patch.Pattern.FindAll(extreme.Bytes, 2));
        }

        [Fact]
        public void CreatePatches_PatternOfOneExecutableMatchesOtherAtWrongPlace_IsNotUsedForIt()
        {
            var crusader = FakeExecutables.Crusader(seed: 1);
            var extreme = FakeExecutables.Extreme(seed: 2);
            CastleOffsetAddresses.TryGet(0, out var address);

            // Crusader's code around the offset appears in Extreme at an unrelated place
            Array.Copy(crusader.Bytes, address.Start + crusader.AddressShift - 100, extreme.Bytes, 200_000, 208);
            var module = Open(crusader, extreme);
            var offsets = new Dictionary<int, BuildingOffset> { { 0, new BuildingOffset(7, 8) } };

            var game = Copy(extreme);
            var expected = Copy(extreme);
            FakeExecutables.Apply(expected, 0, offsets[0]);
            RunLikeInitScript(module, game, offsets);

            Assert.Equal(expected.Bytes, game.Bytes);
        }

        [Fact]
        public void CreatePatches_CodeOfOneExecutableFoundInOtherOnly_ThrowsInvalidDataException()
        {
            var crusader = FakeExecutables.Crusader(seed: 1);
            var extreme = FakeExecutables.Extreme(seed: 2);
            CastleOffsetAddresses.TryGet(0, out var address);

            // longer than every pattern: the game could not tell the places apart
            Array.Copy(crusader.Bytes, address.Start + crusader.AddressShift - 600, extreme.Bytes, 200_000, 1300);
            var module = Open(crusader, extreme);

            Assert.Throws<InvalidDataException>(() => module.Write(0, new BuildingOffset(1, 2)));
        }

        [Fact]
        public void CreatePatches_NoUniquePattern_ThrowsInvalidDataException()
        {
            var module = Open(StrongholdExecutable.Extreme(new byte[FakeExecutables.Size]));

            Assert.Throws<InvalidDataException>(() => module.Write(0, new BuildingOffset(1, 2)));
            Assert.False(module.Exists);
        }

        [Fact]
        public void Write_ExecutableTooSmall_WritesNothing()
        {
            var module = Open(FakeExecutables.Crusader(), StrongholdExecutable.Extreme(new byte[939612]));

            Assert.Throws<InvalidDataException>(() => module.Write(0, new BuildingOffset(5, 6)));

            Assert.False(module.Exists);
        }

        [Fact]
        public void WriteAll_WritesModuleFiles()
        {
            var crusader = FakeExecutables.Crusader();
            var module = Open(crusader, FakeExecutables.ExtremeOf(crusader));

            module.WriteAll(new Dictionary<int, BuildingOffset> { { 12, new BuildingOffset(-5, 7) }, { 0, new BuildingOffset(3, 40) } });

            Assert.Equal(temp.Combine("ucp", "modules", "My-Castle-Offsets-1.0.0"), module.Folder);
            Assert.Equal(
                "name: My-Castle-Offsets\n" +
                "display-name: \"My Castle (Offsets)\"\n" +
                "author: \"Pode\"\n" +
                "version: 1.0.0\n" +
                "dependencies:\n" +
                "  framework: ^3.0.0\n" +
                "meta:\n" +
                "  version: 1.0.0\n",
                File.ReadAllText(Path.Combine(module.Folder, "definition.yml")));

            string script = File.ReadAllText(Path.Combine(module.Folder, "init.lua"));
            Assert.Contains("    image = 0,\n", script);
            Assert.Contains("    image = 12,\n", script);
            Assert.True(script.IndexOf("image = 0,", StringComparison.Ordinal) < script.IndexOf("image = 12,", StringComparison.Ordinal));
            Assert.Contains("writes = { { 15, { 0x03 } }, { 8, { 0x28, 0x00, 0x00, 0x00 } } }", script);
            Assert.Contains("core.scanForAOB(variant.pattern)", script);
            Assert.Contains("core.writeCodeBytes(address + write[1], write[2])", script);
            Assert.Contains("log(WARNING, \"[My-Castle-Offsets]: offset of image \" .. patch.image", script);

            Assert.Equal(new[] { 0, 12 }, module.Offsets.Keys);
            Assert.Contains("| 12 | -5 | 7 |", File.ReadAllText(Path.Combine(module.Folder, "locale", "description-en.md")));
        }

        [Fact]
        public void CreateInitScript_UnknownImageInOffsetsFile_IsSkipped()
        {
            var module = Open(FakeExecutables.Extreme());

            string script = module.CreateInitScript(new Dictionary<int, BuildingOffset> { { 5, new BuildingOffset(1, 2) }, { 0, new BuildingOffset(3, 4) } });

            Assert.Contains("image = 0,", script);
            Assert.DoesNotContain("image = 5,", script);
        }

        [Fact]
        public void Write_KeepsOffsetsWrittenBefore()
        {
            var module = Open(FakeExecutables.Extreme());

            module.Write(0, new BuildingOffset(1, 2));
            module.Write(1, new BuildingOffset(3, 4));
            module.Write(0, new BuildingOffset(5, 6));

            Assert.Equal(new[] { (0, 5, 6), (1, 3, 4) }, module.Offsets.Select(entry => (entry.Key, entry.Value.X, entry.Value.Y)));
        }

        [Fact]
        public void TryRead_PrefersModuleOffsetOverExecutable()
        {
            var extreme = FakeExecutables.Extreme();
            FakeExecutables.Apply(extreme, 0, new BuildingOffset(11, 12));
            FakeExecutables.Apply(extreme, 1, new BuildingOffset(21, 22));
            var module = Open(extreme);

            module.Write(0, new BuildingOffset(-1, -2));

            Assert.True(module.TryRead(0, out var fromModule));
            Assert.True(module.TryRead(1, out var fromExecutable));
            Assert.Equal((-1, -2), (fromModule.X, fromModule.Y));
            Assert.Equal((21, 22), (fromExecutable.X, fromExecutable.Y));
            Assert.False(module.TryRead(5, out _));
        }

        [Fact]
        public void TryRead_ReadsCrusaderExecutableFirst()
        {
            var crusader = FakeExecutables.Crusader();
            var extreme = FakeExecutables.Extreme();
            FakeExecutables.Apply(crusader, 0, new BuildingOffset(11, 0));
            FakeExecutables.Apply(extreme, 0, new BuildingOffset(22, 0));

            Assert.True(Open(crusader, extreme).TryRead(0, out var offset));

            Assert.Equal(11, offset.X);
        }

        [Fact]
        public void Supports_KnownImagesOnlyWhenAnExecutableExists()
        {
            var module = Open(FakeExecutables.Crusader());

            Assert.True(module.Supports(0));
            Assert.True(module.Supports(125));
            Assert.False(module.Supports(5));
            Assert.False(Open().Supports(0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        [InlineData(126)]
        public void Write_UnknownImageIndex_ThrowsArgumentOutOfRange(int imageIndex)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Open(FakeExecutables.Extreme()).Write(imageIndex, new BuildingOffset(1, 1)));
        }

        [Fact]
        public void Write_NoExecutables_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => Open().Write(0, new BuildingOffset(1, 1)));
        }

        [Fact]
        public void Open_ModuleWithOtherVersion_MovesItToNewVersion()
        {
            var extreme = FakeExecutables.Extreme();
            Open(extreme).Write(0, new BuildingOffset(1, 2));

            var module = UcpOffsetModule.Open(ucp, new UcpExtensionInfo("My-Castle-Offsets", "2.0.0"), new[] { extreme });

            Assert.Equal(new[] { "My-Castle-Offsets-2.0.0" }, Directory.GetDirectories(ucp.ModulesFolder).Select(Path.GetFileName));
            Assert.True(module.TryRead(0, out var offset));
            Assert.Equal((1, 2), (offset.X, offset.Y));
            Assert.Contains("version: 2.0.0", File.ReadAllText(Path.Combine(module.Folder, "definition.yml")));
        }

        [Fact]
        public void InfoFor_DerivesModuleFromTextureMod()
        {
            var moduleInfo = UcpOffsetModule.InfoFor(new UcpExtensionInfo("My-Castle", "1.2.3", "My Castle", "Pode"));

            Assert.Equal(("My-Castle-Offsets", "1.2.3", "My Castle (Offsets)", "Pode"), (moduleInfo.Name, moduleInfo.Version, moduleInfo.DisplayName, moduleInfo.Author));
        }

        private UcpOffsetModule Open(params StrongholdExecutable[] executables) => UcpOffsetModule.Open(ucp, info, executables);

        private static IReadOnlyList<OffsetPatch> Patches(UcpOffsetModule module, int imageIndex)
        {
            CastleOffsetAddresses.TryGet(imageIndex, out var address);
            return module.CreatePatches(address, new BuildingOffset(3, 40));
        }

        private static Dictionary<int, BuildingOffset> RandomOffsets(Random random)
        {
            return CastleOffsetAddresses.ImageIndices.ToDictionary(index => index, _ => new BuildingOffset(random.Next(-128, 128), random.Next(-100_000, 100_000)));
        }

        private static StrongholdExecutable Copy(StrongholdExecutable executable)
        {
            return new StrongholdExecutable(executable.Name, (byte[])executable.Bytes.Clone(), executable.AddressShift);
        }

        /// <summary>
        /// Does what the generated init.lua does in the game: the first pattern that is found is used,
        /// its bytes are written relative to the first match.
        /// </summary>
        private static void RunLikeInitScript(UcpOffsetModule module, StrongholdExecutable game, IReadOnlyDictionary<int, BuildingOffset> offsets)
        {
            foreach (var entry in offsets.OrderBy(entry => entry.Key))
            {
                CastleOffsetAddresses.TryGet(entry.Key, out var address);
                foreach (var patch in module.CreatePatches(address, entry.Value))
                {
                    var matches = patch.Pattern.FindAll(game.Bytes, 1);
                    if (matches.Count == 0)
                    {
                        continue;
                    }

                    foreach (var write in patch.Writes)
                    {
                        for (int i = 0; i < write.Bytes.Count; i++)
                        {
                            game.Bytes[matches[0] + write.Address + i] = write.Bytes[i];
                        }
                    }

                    break;
                }
            }
        }
    }
}

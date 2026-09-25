using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;
using Gm1KonverterCrossPlatform.Core.IO;
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
        public void Patches_SetOffsets_ChangeTheSameBytesAsPatchingTheExecutable()
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
        public void Patches_OffsetsNotSetAndNotChangedInGui_KeepTheValueOfTheGame()
        {
            var extreme = FakeExecutables.Extreme();
            var module = Open(extreme);
            var offsets = new Dictionary<int, BuildingOffset> { { 0, new BuildingOffset(5, 6) } };

            // the GUI saves every option with its start value
            var config = module.CreateEntries(offsets).ToDictionary(entry => entry.Name, entry => (X: (int?)entry.Start.X, Y: (int?)entry.Start.Y));
            var game = FakeExecutables.Extreme(seed: 1);
            FakeExecutables.Apply(game, 1, new BuildingOffset(99, 999));
            var expected = Copy(game);
            FakeExecutables.Apply(expected, 0, new BuildingOffset(5, 6));

            RunLikeInitScript(module, game, offsets, config);

            Assert.Equal(expected.Bytes, game.Bytes);
        }

        [Fact]
        public void Patches_ValueChangedInGui_WritesOnlyThatValue()
        {
            var extreme = FakeExecutables.Extreme();
            var module = Open(extreme);
            var offsets = new Dictionary<int, BuildingOffset> { { 0, new BuildingOffset(5, 6) } };
            var config = module.CreateEntries(offsets).ToDictionary(entry => entry.Name, entry => (X: (int?)entry.Start.X, Y: (int?)entry.Start.Y));
            config["image0"] = (-7, 6);
            config["image3"] = (config["image3"].X, -300);
            config["image13"] = (200, config["image13"].Y);

            var game = Copy(extreme);
            var expected = Copy(extreme);
            FakeExecutables.Apply(expected, 0, new BuildingOffset(-7, 6));
            CastleOffsetAddresses.TryGet(3, out var image3);
            CastleOffsetAddresses.TryGet(13, out var image13);
            ApplyOnly(expected, image3, new BuildingOffset(0, -300), x: false);
            ApplyOnly(expected, image13, new BuildingOffset(200, 0), x: true);

            RunLikeInitScript(module, game, offsets, config);

            Assert.Equal(expected.Bytes, game.Bytes);
        }

        [Fact]
        public void CreatePatches_SameCodeInBothExecutables_OneLocalPatternFitsBoth()
        {
            var crusader = FakeExecutables.Crusader();
            var module = Open(crusader, FakeExecutables.ExtremeOf(crusader));

            foreach (var group in CastleOffsetAddresses.Groups)
            {
                Assert.Single(LocalPatches(module, group));
            }
        }

        [Fact]
        public void CreatePatches_DifferentExecutables_OneLocalPatternEach()
        {
            var module = Open(FakeExecutables.Crusader(seed: 1), FakeExecutables.Extreme(seed: 2));

            Assert.Equal(2, LocalPatches(module, GroupOf(0)).Count);
        }

        [Fact]
        public void CreatePatches_KnownPatternComesFirst()
        {
            var module = Open(FakeExecutables.Extreme());
            KnownCastleOffsets.TryGet(GroupOf(0), out var known);

            var patches = module.CreatePatches(GroupOf(0));

            Assert.Equal(known.Pattern.ToString(), patches[0].Pattern.ToString());
            Assert.Equal((known.RelativeX, known.RelativeY), (patches[0].RelativeX, patches[0].RelativeY));
        }

        [Fact]
        public void CreatePatches_PatternsAreShortForUniqueCode()
        {
            var module = Open(FakeExecutables.Extreme());

            var patch = LocalPatches(module, GroupOf(0)).Single();

            // 8 bytes offset (y, 3 bytes code, x) and 8 bytes context on both sides
            Assert.Equal(24, patch.Pattern.Length);
            Assert.Equal("? ? ? ?", string.Join(" ", patch.Pattern.ToString().Split(' ').Skip(8).Take(4)));
            Assert.Equal((15, 8), (patch.RelativeX, patch.RelativeY));
        }

        [Fact]
        public void CreatePatches_RepeatedCode_ExtendsPatternUntilUnique()
        {
            var extreme = FakeExecutables.Extreme();
            CastleOffsetAddresses.TryGet(0, out var address);

            // the same code 30 bytes around the offset exists a second time
            Array.Copy(extreme.Bytes, address.Start - 30, extreme.Bytes, 100_000, address.End - address.Start + 60);
            var module = Open(extreme);

            var patch = LocalPatches(module, GroupOf(0)).Single();

            Assert.True(patch.Pattern.Length > 60, patch.Pattern.ToString());
            Assert.Equal(new[] { patch.StartAddress }, patch.Pattern.FindAll(extreme.Bytes, 2));
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
        public void Write_CodeOfOneExecutableFoundInOtherOnly_ThrowsInvalidDataException()
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
        public void Write_NoUniquePatternInLocalExecutable_ThrowsInvalidDataException()
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

            Assert.Equal(temp.Combine("ucp", "modules", "My-Castle-Offsets-1.0.0.zip"), module.ZipPath);
            Assert.Equal(
                new[] { "definition.yml", "init.lua", "locale/description-en.md", "offsets.json", "options.yml" },
                EntryNames(module));
            Assert.Equal(
                "name: My-Castle-Offsets\n" +
                "display-name: \"My Castle (Offsets)\"\n" +
                "author: \"Pode\"\n" +
                "version: 1.0.0\n" +
                "type: module\n" +
                "description: \"Castle building offsets, adjustable in the UCP3 GUI\"\n" +
                "dependencies:\n" +
                "  framework: ^3.0.0\n" +
                "meta:\n" +
                "  version: 1.0.0\n",
                ReadEntry(module, "definition.yml"));

            string script = ReadEntry(module, "init.lua");
            Assert.Contains("    name = \"image0\",\n    images = \"0\",\n    start = { x = 3, y = 40 },\n    fixed = true,\n    singleByteY = false,\n", script);
            Assert.Contains("    name = \"image12\",\n    images = \"12\",\n    start = { x = -5, y = 7 },\n    fixed = true,\n    singleByteY = true,\n", script);
            Assert.Contains("    name = \"image121\",\n    images = \"121, 122\",\n", script);
            Assert.Contains("fixed = false", script);
            Assert.Contains("core.scanForAOB(variant.pattern)", script);
            Assert.Contains("core.writeCodeBytes(address + variant.x, bytesOf(clamp(x, -128, 127), 1))", script);
            Assert.Contains("log(WARNING, \"[My-Castle-Offsets]: offset of image \" .. offset.images", script);

            string options = ReadEntry(module, "options.yml");
            Assert.StartsWith("specification-version: 1.0.0\noptions:\n  - display: GroupBox\n", options);
            Assert.Contains("    category: [\"My Castle (Offsets)\"]\n", options);
            Assert.Contains("          - url: My-Castle-Offsets.image0.x\n", options);
            Assert.Contains("              value: 40\n              min: -2147483648\n              max: 2147483647\n", options);
            Assert.Contains("          - url: My-Castle-Offsets.image12.y\n", options);
            Assert.Equal(CastleOffsetAddresses.Groups.Count * 2, options.Split("display: Number").Length - 1);

            Assert.Equal(new[] { 0, 12 }, module.Offsets.Keys);
            Assert.Contains("| 12 | -5 | 7 | set in the Gm1 Konverter |", ReadEntry(module, "locale/description-en.md"));
        }

        [Fact]
        public void CreateEntries_OneEntryPerAddress_LastSetImageWins()
        {
            var module = Open(FakeExecutables.Extreme());

            var entries = module.CreateEntries(new Dictionary<int, BuildingOffset>
            {
                { 121, new BuildingOffset(1, 1) },
                { 122, new BuildingOffset(2, 2) },
                { 5, new BuildingOffset(3, 3) }, // no castle offset, e.g. from an edited file
            });

            Assert.Equal(12, entries.Count);
            var shared = entries.Single(entry => entry.Name == "image121");
            Assert.Equal("121, 122", shared.Images);
            Assert.True(shared.Fixed);
            Assert.Equal((2, 2), (shared.Start.X, shared.Start.Y));
            Assert.Equal(1, entries.Count(entry => entry.Fixed));
        }

        [Fact]
        public void WithoutExecutables_KnownPatternsAndOriginalValuesAreUsed()
        {
            var module = Open();
            KnownCastleOffsets.TryGet(GroupOf(1), out var known);

            module.Write(0, new BuildingOffset(5, 6));

            Assert.True(module.Supports(0));
            Assert.True(module.TryRead(1, out var original));
            Assert.Equal(known.Original, original);
            var entries = module.CreateEntries(module.Offsets);
            Assert.Equal(12, entries.Count);
            Assert.All(entries, entry => Assert.Single(entry.Patches));
        }

        [Fact]
        public void KnownPatterns_FindOffsetsInBothGamesAtTheirOwnAddresses()
        {
            // like the real games: the same code, other embedded addresses, Crusader 912 bytes earlier
            var (crusader, extreme) = GamesWithKnownCode(crusaderShift: CastleOffsetAddresses.CrusaderAddressShift);
            var module = Open(crusader, extreme);

            foreach (var group in CastleOffsetAddresses.Groups)
            {
                KnownCastleOffsets.TryGet(group, out var known);
                var patch = module.CreatePatches(group).Single();

                Assert.Equal(known.Pattern.ToString(), patch.Pattern.ToString());
                Assert.True(module.TryRead(group.ImageIndices[0], out var offset));
                Assert.Equal(known.Original, offset);
            }
        }

        [Fact]
        public void KnownPatterns_CrusaderAtAnotherShift_IsMeasured()
        {
            var (crusader, extreme) = GamesWithKnownCode(crusaderShift: -1000);
            var module = Open(crusader, extreme);

            foreach (var group in CastleOffsetAddresses.Groups)
            {
                Assert.Equal(-1000, module.ShiftOf(crusader, group.Address));
                Assert.Equal(0, module.ShiftOf(extreme, group.Address));
            }

            var offsets = new Dictionary<int, BuildingOffset> { { 0, new BuildingOffset(9, 99) } };
            var game = Copy(crusader);
            var expected = Copy(crusader);
            CastleOffsetAddresses.TryGet(0, out var address);
            foreach (var write in address.GetWrites(offsets[0]))
            {
                write.Bytes.ToArray().CopyTo(expected.Bytes, write.Address - 1000);
            }

            RunLikeInitScript(module, game, offsets);

            Assert.Equal(expected.Bytes, game.Bytes);
        }

        [Fact]
        public void KnownPatterns_AreConsistentWithTheAddressTable()
        {
            foreach (var group in CastleOffsetAddresses.Groups)
            {
                Assert.True(KnownCastleOffsets.TryGet(group, out var known), group.Name);
                var tokens = known.Pattern.ToString().Split(' ');

                Assert.Equal(group.Address.Y - group.Address.X, known.RelativeY - known.RelativeX);
                Assert.Equal("?", tokens[known.RelativeX]);
                Assert.All(Enumerable.Range(known.RelativeY, group.Address.YLength), i => Assert.Equal("?", tokens[i]));
                Assert.True(tokens.Count(token => token != "?") >= 10, known.Pattern.ToString());
            }
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
        public void Supports_EveryCastleOffset()
        {
            var module = Open();

            Assert.True(module.Supports(0));
            Assert.True(module.Supports(125));
            Assert.False(module.Supports(5));
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
        public void Open_ModuleWithOtherVersion_MovesItToNewVersion()
        {
            var extreme = FakeExecutables.Extreme();
            Open(extreme).Write(0, new BuildingOffset(1, 2));

            var module = UcpOffsetModule.Open(ucp, new UcpExtensionInfo("My-Castle-Offsets", "2.0.0"), new[] { extreme });

            Assert.Equal(new[] { "My-Castle-Offsets-2.0.0.zip" }, Directory.GetFileSystemEntries(ucp.ModulesFolder).Select(Path.GetFileName));
            Assert.True(module.TryRead(0, out var offset));
            Assert.Equal((1, 2), (offset.X, offset.Y));
            Assert.Contains("version: 2.0.0", ReadEntry(module, "definition.yml"));
        }

        [Fact]
        public void Open_FolderOfEarlierVersion_IsReplacedByZipWithTheSameOffsets()
        {
            // earlier versions of this program wrote the module as folder, which the UCP3 GUI does not list
            string folder = temp.Combine("ucp", "modules", "My-Castle-Offsets-1.0.0");
            temp.WriteFile(Path.Combine(folder, "init.lua"), System.Text.Encoding.UTF8.GetBytes("-- Generated by Gm1 Konverter. ...\n"));
            temp.WriteFile(Path.Combine(folder, "offsets.json"), System.Text.Encoding.UTF8.GetBytes("{\"0\":{\"X\":4.0,\"Y\":5.0}}"));

            var module = Open(FakeExecutables.Extreme());

            Assert.False(Directory.Exists(folder));
            Assert.True(module.Exists);
            Assert.Equal((4, 5), (module.Offsets[0].X, module.Offsets[0].Y));
        }

        [Fact]
        public void Open_FolderNotWrittenByThisProgram_IsKept()
        {
            string folder = temp.Combine("ucp", "modules", "My-Castle-Offsets-1.0.0");
            temp.WriteFile(Path.Combine(folder, "init.lua"), System.Text.Encoding.UTF8.GetBytes("return {}"));
            temp.WriteFile(Path.Combine(folder, "offsets.json"), System.Text.Encoding.UTF8.GetBytes("{}"));

            var module = Open(FakeExecutables.Extreme());

            Assert.True(Directory.Exists(folder));
            Assert.False(module.Exists);
        }

        [Fact]
        public void Write_LeavesOnlyTheZipInModulesFolder()
        {
            var module = Open(FakeExecutables.Extreme());

            module.Write(0, new BuildingOffset(1, 2));
            module.Write(1, new BuildingOffset(3, 4));

            Assert.Equal(new[] { "My-Castle-Offsets-1.0.0.zip" }, Directory.GetFileSystemEntries(ucp.ModulesFolder).Select(Path.GetFileName));
        }

        [Fact]
        public void InfoFor_DerivesModuleFromTextureMod()
        {
            var moduleInfo = UcpOffsetModule.InfoFor(new UcpExtensionInfo("My-Castle", "1.2.3", "My Castle", "Pode"));

            Assert.Equal(("My-Castle-Offsets", "1.2.3", "My Castle (Offsets)", "Pode"), (moduleInfo.Name, moduleInfo.Version, moduleInfo.DisplayName, moduleInfo.Author));
        }

        private UcpOffsetModule Open(params StrongholdExecutable[] executables) => UcpOffsetModule.Open(ucp, info, executables);

        private static string ReadEntry(UcpOffsetModule module, string name)
        {
            using var zip = ZipFile.OpenRead(module.ZipPath);
            using var reader = new StreamReader(zip.GetEntry(name)!.Open());
            return reader.ReadToEnd();
        }

        private static IReadOnlyList<string> EntryNames(UcpOffsetModule module)
        {
            using var zip = ZipFile.OpenRead(module.ZipPath);
            return zip.Entries.Select(entry => entry.FullName).OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        private static OffsetGroup GroupOf(int imageIndex)
        {
            CastleOffsetAddresses.TryGet(imageIndex, out var address);
            return CastleOffsetAddresses.GroupOf(address);
        }

        /// <summary>The patterns created from the local executables, without the known pattern.</summary>
        private static IReadOnlyList<OffsetPatch> LocalPatches(UcpOffsetModule module, OffsetGroup group)
        {
            KnownCastleOffsets.TryGet(group, out var known);
            return module.CreatePatches(group).Where(patch => patch.Pattern.ToString() != known.Pattern.ToString()).ToList();
        }

        /// <summary>
        /// Random executables that contain the known patterns like the real games: fixed bytes equal, wildcards
        /// (embedded addresses) different, offsets with their original values.
        /// </summary>
        private static (StrongholdExecutable Crusader, StrongholdExecutable Extreme) GamesWithKnownCode(int crusaderShift)
        {
            var crusader = new StrongholdExecutable(StrongholdFolder.CrusaderExecutable, FakeExecutables.Content(seed: 1), crusaderShift);
            var extreme = FakeExecutables.Extreme(seed: 2);
            var random = new Random(3);
            foreach (var group in CastleOffsetAddresses.Groups)
            {
                KnownCastleOffsets.TryGet(group, out var known);
                int start = group.Address.X - known.RelativeX;
                var tokens = known.Pattern.ToString().Split(' ');
                for (int i = 0; i < tokens.Length; i++)
                {
                    bool wildcard = tokens[i] == "?";
                    extreme.Bytes[start + i] = wildcard ? (byte)random.Next(256) : Convert.ToByte(tokens[i], 16);
                    crusader.Bytes[start + crusaderShift + i] = wildcard ? (byte)random.Next(256) : Convert.ToByte(tokens[i], 16);
                }
            }

            foreach (var group in CastleOffsetAddresses.Groups)
            {
                KnownCastleOffsets.TryGet(group, out var known);
                foreach (var write in group.Address.GetWrites(known.Original))
                {
                    write.Bytes.ToArray().CopyTo(extreme.Bytes, write.Address);
                    write.Bytes.ToArray().CopyTo(crusader.Bytes, write.Address + crusaderShift);
                }
            }

            return (crusader, extreme);
        }

        private static Dictionary<int, BuildingOffset> RandomOffsets(Random random)
        {
            // one image per address, images sharing an address would overwrite each other
            return CastleOffsetAddresses.Groups.ToDictionary(
                group => group.ImageIndices[0],
                _ => new BuildingOffset(random.Next(-128, 128), random.Next(-100_000, 100_000)));
        }

        private static StrongholdExecutable Copy(StrongholdExecutable executable)
        {
            return new StrongholdExecutable(executable.Name, (byte[])executable.Bytes.Clone(), executable.AddressShift);
        }

        private static void ApplyOnly(StrongholdExecutable executable, OffsetAddress address, BuildingOffset offset, bool x)
        {
            foreach (var write in address.GetWrites(offset).Where(write => (write.Address == address.X) == x))
            {
                write.Bytes.ToArray().CopyTo(executable.Bytes, write.Address + executable.AddressShift);
            }
        }

        /// <summary>
        /// Does what the generated init.lua does in the game: a value is written if it was set in this program or
        /// changed in the GUI (<paramref name="config"/>); the first variant that is found is used.
        /// </summary>
        private static void RunLikeInitScript(
            UcpOffsetModule module,
            StrongholdExecutable game,
            IReadOnlyDictionary<int, BuildingOffset> offsets,
            IReadOnlyDictionary<string, (int? X, int? Y)>? config = null)
        {
            foreach (var entry in module.CreateEntries(offsets))
            {
                var configured = config != null && config.TryGetValue(entry.Name, out var values) ? values : (null, null);
                int? x = ValueToWrite(configured.X, entry.Start.X, entry.Fixed);
                int? y = ValueToWrite(configured.Y, entry.Start.Y, entry.Fixed);
                if (x == null && y == null)
                {
                    continue;
                }

                foreach (var patch in entry.Patches)
                {
                    var matches = patch.Pattern.FindAll(game.Bytes, 1);
                    if (matches.Count == 0)
                    {
                        continue;
                    }

                    var value = new BuildingOffset(x ?? 0, y ?? 0);
                    foreach (var write in entry.Address.GetWrites(value))
                    {
                        bool isX = write.Address == entry.Address.X;
                        if ((isX && x == null) || (!isX && y == null))
                        {
                            continue;
                        }

                        int relative = isX ? patch.RelativeX : patch.RelativeY;
                        write.Bytes.ToArray().CopyTo(game.Bytes, matches[0] + relative);
                    }

                    break;
                }
            }
        }

        private static int? ValueToWrite(int? configured, int start, bool isFixed)
        {
            if (configured != null && configured != start)
            {
                return configured;
            }

            return isFixed ? start : (int?)null;
        }
    }
}

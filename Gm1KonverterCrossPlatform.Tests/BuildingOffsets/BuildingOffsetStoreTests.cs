using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.BuildingOffsets;
using Gm1KonverterCrossPlatform.Tests.Support;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.BuildingOffsets
{
    public class BuildingOffsetStoreTests : IDisposable
    {
        /// <summary>Written by older versions (serialized Avalonia points).</summary>
        private const string OldFormat = "{\"12\":{\"X\":-5.0,\"Y\":7.0,\"IsDefault\":false},\"0\":{\"X\":3.0,\"Y\":123456.0,\"IsDefault\":false}}";

        private readonly TempDirectory temp = new TempDirectory();
        private readonly string path;

        public BuildingOffsetStoreTests()
        {
            path = temp.Combine("work", "Offsets.json");
        }

        public void Dispose() => temp.Dispose();

        [Fact]
        public void Parse_OldFormat_ReadsEveryOffset()
        {
            var offsets = BuildingOffsetStore.Parse(OldFormat);

            Assert.Equal(2, offsets.Count);
            Assert.Equal((-5, 7), (offsets[12].X, offsets[12].Y));
            Assert.Equal((3, 123456), (offsets[0].X, offsets[0].Y));
        }

        [Fact]
        public void Parse_RoundsFractionalValues()
        {
            var offsets = BuildingOffsetStore.Parse("{\"1\":{\"X\":2.6,\"Y\":-3.7}}");

            Assert.Equal((3, -4), (offsets[1].X, offsets[1].Y));
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("{}")]
        public void Parse_EmptyContent_ReturnsNoOffsets(string json)
        {
            Assert.Empty(BuildingOffsetStore.Parse(json));
        }

        [Fact]
        public void Load_OldFormatFile_ReadsEveryOffsetSortedByImage()
        {
            temp.WriteFile(Path.Combine("work", "Offsets.json"), System.Text.Encoding.UTF8.GetBytes(OldFormat));

            var store = BuildingOffsetStore.Load(path);

            Assert.Equal(new[] { 0, 12 }, store.Offsets.Keys);
            Assert.Equal((-5, 7), (store.Offsets[12].X, store.Offsets[12].Y));
        }

        [Fact]
        public void Load_MissingFile_ReturnsEmptyStore()
        {
            var store = BuildingOffsetStore.Load(path);

            Assert.Empty(store.Offsets);
            Assert.False(File.Exists(path));
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("{")]
        [InlineData("[1, 2]")]
        [InlineData("{\"abc\":{\"X\":1,\"Y\":2}}")]
        [InlineData("{\"1\":{\"X\":\"left\",\"Y\":2}}")]
        public void Load_InvalidJson_ThrowsInvalidDataException(string json)
        {
            File.WriteAllText(temp.Combine("Offsets.json"), json);

            Assert.Throws<InvalidDataException>(() => BuildingOffsetStore.Load(temp.Combine("Offsets.json")));
        }

        [Fact]
        public void Set_PersistsOffset()
        {
            var store = BuildingOffsetStore.Load(path);

            store.Set(43, new BuildingOffset(-12, 3456));

            var reloaded = BuildingOffsetStore.Load(path);
            Assert.Equal((-12, 3456), (reloaded.Offsets[43].X, reloaded.Offsets[43].Y));
            Assert.Equal((-12, 3456), (store.Offsets[43].X, store.Offsets[43].Y));
        }

        [Fact]
        public void Set_KeepsExistingEntriesOfFile()
        {
            temp.WriteFile(Path.Combine("work", "Offsets.json"), System.Text.Encoding.UTF8.GetBytes(OldFormat));
            var store = BuildingOffsetStore.Load(path);

            store.Set(1, new BuildingOffset(9, 10));
            store.Set(2, new BuildingOffset(11, 12));

            var reloaded = BuildingOffsetStore.Load(path);
            Assert.Equal(new[] { 0, 1, 2, 12 }, reloaded.Offsets.Keys);
            Assert.Equal((3, 123456), (reloaded.Offsets[0].X, reloaded.Offsets[0].Y));
            Assert.Equal((-5, 7), (reloaded.Offsets[12].X, reloaded.Offsets[12].Y));
            Assert.Equal((9, 10), (reloaded.Offsets[1].X, reloaded.Offsets[1].Y));
            Assert.Equal((11, 12), (reloaded.Offsets[2].X, reloaded.Offsets[2].Y));
        }

        [Fact]
        public void Set_SameImageAgain_ReplacesOffset()
        {
            var store = BuildingOffsetStore.Load(path);

            store.Set(3, new BuildingOffset(1, 1));
            store.Set(3, new BuildingOffset(2, 2));

            var reloaded = BuildingOffsetStore.Load(path);
            Assert.Single(reloaded.Offsets);
            Assert.Equal((2, 2), (reloaded.Offsets[3].X, reloaded.Offsets[3].Y));
        }

        [Fact]
        public void Set_WritesFormatOlderVersionsCanRead()
        {
            var store = BuildingOffsetStore.Load(path);

            store.Set(12, new BuildingOffset(-5, 7));

            // older versions deserialize Dictionary<int, Avalonia.Point>, which needs numeric X and Y
            var json = JObject.Parse(File.ReadAllText(path));
            var entry = Assert.IsType<JObject>(json["12"]);
            Assert.Equal(new[] { "X", "Y" }, entry.Properties().Select(p => p.Name));
            Assert.Equal(-5.0, (double)entry["X"]!);
            Assert.Equal(7.0, (double)entry["Y"]!);
        }
    }
}

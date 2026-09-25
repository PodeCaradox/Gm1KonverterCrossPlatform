using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Settings;
using Gm1KonverterCrossPlatform.Tests.Support;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Settings
{
    public class UserConfigStoreTests : IDisposable
    {
        private readonly TempDirectory temp = new TempDirectory();
        private readonly UserConfigStore store;

        public UserConfigStoreTests()
        {
            store = new UserConfigStore(temp.Combine("config"));
        }

        public void Dispose() => temp.Dispose();

        [Fact]
        public void FilePath_UsesOldFileName()
        {
            Assert.Equal(temp.Combine("config", "UserConfig.txt"), store.FilePath);
        }

        [Fact]
        public void Load_MissingFile_ReturnsDefaults()
        {
            var config = store.Load();

            AssertDefaults(config);
            Assert.False(File.Exists(store.FilePath));
        }

        [Fact]
        public void Load_NullCrusaderPath_ReturnsConfig()
        {
            WriteConfig("{\"Language\":1,\"ColorTheme\":1,\"CrusaderPath\":null,\"WorkFolderPath\":\"C:\\\\work\",\"OpenFolderAfterExport\":true,\"ActivateLogger\":false}");

            var config = store.Load();

            Assert.Null(config.CrusaderPath);
            Assert.Equal(Language.Deutsch, config.Language);
            Assert.Equal("C:\\work", config.WorkFolderPath);
        }

        [Fact]
        public void Load_ConfigOfOlderVersion_ReadsEveryValue()
        {
            WriteConfig("{\"Language\":2,\"ColorTheme\":1,\"CrusaderPath\":\"D:\\\\Games\\\\Stronghold Crusader\",\"WorkFolderPath\":\"D:\\\\Work\",\"OpenFolderAfterExport\":true,\"ActivateLogger\":true}");

            var config = store.Load();

            Assert.Equal(Language.Русский, config.Language);
            Assert.Equal(ColorTheme.Dark, config.ColorTheme);
            Assert.Equal("D:\\Games\\Stronghold Crusader", config.CrusaderPath);
            Assert.Equal("D:\\Work", config.WorkFolderPath);
            Assert.True(config.OpenFolderAfterExport);
            Assert.True(config.ActivateLogger);
        }

        [Theory]
        [InlineData("C:\\\\Games\\\\Stronghold Crusader\\\\gm", "C:\\Games\\Stronghold Crusader")]
        [InlineData("C:\\\\Games\\\\Stronghold Crusader\\\\gm\\\\", "C:\\Games\\Stronghold Crusader")]
        [InlineData("/home/user/Stronghold/gm", "/home/user/Stronghold")]
        [InlineData("C:\\\\gmods\\\\Stronghold", "C:\\gmods\\Stronghold")]
        [InlineData("C:\\\\Games\\\\Stronghold Crusader", "C:\\Games\\Stronghold Crusader")]
        public void Load_LegacyGmFolder_IsNormalizedToInstallationFolder(string jsonPath, string expected)
        {
            WriteConfig("{\"CrusaderPath\":\"" + jsonPath + "\"}");

            var config = store.Load();

            Assert.Equal(expected, config.CrusaderPath);
        }

        [Theory]
        [InlineData("{")]
        [InlineData("not json")]
        [InlineData("[]")]
        [InlineData("{\"Language\":\"Klingon\"}")]
        [InlineData("{\"OpenFolderAfterExport\":\"maybe\"}")]
        public void Load_CorruptFile_ReturnsDefaults(string content)
        {
            WriteConfig(content);

            var config = store.Load();

            AssertDefaults(config);
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        public void Load_EmptyFile_ReturnsDefaults(string content)
        {
            WriteConfig(content);

            AssertDefaults(store.Load());
        }

        [Fact]
        public void Load_UnknownEnumValues_FallBackToDefaults()
        {
            WriteConfig("{\"Language\":42,\"ColorTheme\":-1,\"WorkFolderPath\":\"/work\"}");

            var config = store.Load();

            Assert.Equal(Language.English, config.Language);
            Assert.Equal(ColorTheme.Light, config.ColorTheme);
            Assert.Equal("/work", config.WorkFolderPath);
        }

        [Fact]
        public void SaveThenLoad_ReturnsSameValues()
        {
            var config = new UserConfig
            {
                Language = Language.Русский,
                ColorTheme = ColorTheme.Dark,
                CrusaderPath = "/games/Stronghold Crusader",
                WorkFolderPath = "/home/user/Stronghold work",
                OpenFolderAfterExport = true,
                ActivateLogger = true
            };

            store.Save(config);
            var loaded = store.Load();

            Assert.Equal(
                (config.Language, config.ColorTheme, config.CrusaderPath, config.WorkFolderPath, config.OpenFolderAfterExport, config.ActivateLogger),
                (loaded.Language, loaded.ColorTheme, loaded.CrusaderPath, loaded.WorkFolderPath, loaded.OpenFolderAfterExport, loaded.ActivateLogger));
            Assert.False(File.Exists(store.FilePath + ".tmp"));
        }

        [Fact]
        public void Save_OverwritesExistingFile()
        {
            store.Save(new UserConfig { WorkFolderPath = "/first" });

            store.Save(new UserConfig { WorkFolderPath = "/second" });

            Assert.Equal("/second", store.Load().WorkFolderPath);
        }

        [Fact]
        public void Save_UsesPropertyNamesAndNumericEnumsOfOlderVersions()
        {
            store.Save(new UserConfig { Language = Language.Русский, ColorTheme = ColorTheme.Dark, CrusaderPath = null, WorkFolderPath = "/work" });

            var json = JObject.Parse(File.ReadAllText(store.FilePath));

            Assert.Equal(
                new[] { "Language", "ColorTheme", "CrusaderPath", "WorkFolderPath", "OpenFolderAfterExport", "ActivateLogger" },
                json.Properties().Select(p => p.Name));
            Assert.Equal(JTokenType.Integer, json["Language"]!.Type);
            Assert.Equal(2, (int)json["Language"]!);
            Assert.Equal(JTokenType.Integer, json["ColorTheme"]!.Type);
            Assert.Equal(1, (int)json["ColorTheme"]!);
            Assert.Equal(JTokenType.Null, json["CrusaderPath"]!.Type);
            Assert.Equal(JTokenType.Boolean, json["OpenFolderAfterExport"]!.Type);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Constructor_WithoutDirectory_Throws(string? directory)
        {
            Assert.Throws<ArgumentException>(() => new UserConfigStore(directory!));
        }

        private void WriteConfig(string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(store.FilePath)!);
            File.WriteAllText(store.FilePath, content);
        }

        private static void AssertDefaults(UserConfig config)
        {
            Assert.Equal(Language.English, config.Language);
            Assert.Equal(ColorTheme.Light, config.ColorTheme);
            Assert.Null(config.CrusaderPath);
            Assert.Null(config.WorkFolderPath);
            Assert.False(config.OpenFolderAfterExport);
            Assert.False(config.ActivateLogger);
        }
    }
}

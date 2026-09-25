using System;
using System.IO;
using System.Linq;
using Gm1KonverterCrossPlatform.Core.Ucp;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Ucp
{
    public class UcpTexturePluginTests : IDisposable
    {
        private static readonly GameFile Castle = new GameFile(GameFolder.Gm, "anim_castle.gm1");
        private static readonly GameFile Frame = new GameFile(GameFolder.Gfx, "frame_stone.tgx");

        private readonly TempDirectory temp = new TempDirectory();
        private readonly UcpFolder ucp;
        private readonly UcpExtensionInfo info = new UcpExtensionInfo("My-Castle", "1.0.0", "My Castle", "Pode");

        public UcpTexturePluginTests()
        {
            ucp = new UcpFolder(temp.Path);
        }

        public void Dispose() => temp.Dispose();

        [Fact]
        public void Open_WithoutFiles_WritesNothing()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);

            Assert.Empty(plugin.Files);
            Assert.False(plugin.Exists);
            Assert.False(Directory.Exists(ucp.Root));
        }

        [Fact]
        public void Open_ExistingPlugin_DoesNotRewriteFiles()
        {
            var first = UcpTexturePlugin.Open(ucp, info);
            first.AddFile(Castle, new byte[] { 1 });
            string definition = Path.Combine(first.Folder, "definition.yml");
            File.WriteAllText(definition, "edited");

            UcpTexturePlugin.Open(ucp, new UcpExtensionInfo("My-Castle", "1.0.0", "Other", "Other"));

            Assert.Equal("edited", File.ReadAllText(definition));
        }

        [Fact]
        public void AddFile_WritesResourceAndPluginFiles()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);

            plugin.AddFile(Castle, new byte[] { 1, 2, 3 });

            string folder = temp.Combine("ucp", "plugins", "My-Castle-1.0.0");
            Assert.Equal(folder, plugin.Folder);
            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(folder, "resources", "gm", "anim_castle.gm1")));
            Assert.True(File.Exists(Path.Combine(folder, "definition.yml")));
            Assert.True(File.Exists(Path.Combine(folder, "init.lua")));
            Assert.True(File.Exists(Path.Combine(folder, "locale", "description-en.md")));
            Assert.Equal(new[] { Castle }, plugin.Files);
        }

        [Fact]
        public void CreateDefinition_DescribesPluginWithFilesDependency()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);

            string expected =
                "name: My-Castle\n" +
                "display-name: \"My Castle\"\n" +
                "author: \"Pode\"\n" +
                "version: 1.0.0\n" +
                "dependencies:\n" +
                "  framework: ^3.0.0\n" +
                "  frontend: ^1.0.0\n" +
                "  files: ^1.0.0\n" +
                "meta:\n" +
                "  version: 1.0.0\n";
            Assert.Equal(expected, plugin.CreateDefinition());
        }

        [Fact]
        public void CreateDefinition_EscapesUserText()
        {
            var plugin = UcpTexturePlugin.Open(ucp, new UcpExtensionInfo("x", "1.0.0", "Say \"hi\" \\o/", "line\nbreak"));

            string definition = plugin.CreateDefinition();

            Assert.Contains("display-name: \"Say \\\"hi\\\" \\\\o/\"\n", definition);
            Assert.Contains("author: \"linebreak\"\n", definition);
        }

        [Fact]
        public void CreateInitScript_OverridesEveryFileWithAliasPath()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);
            plugin.AddFile(Frame, new byte[] { 1 });
            plugin.AddFile(Castle, new byte[] { 2 });

            string script = File.ReadAllText(Path.Combine(plugin.Folder, "init.lua"));

            Assert.Contains(
                "local replacements = {\n" +
                "  { \"gfx\\\\frame_stone.tgx\", \"ucp/plugins/My-Castle/resources/gfx/frame_stone.tgx\" },\n" +
                "  { \"gm\\\\anim_castle.gm1\", \"ucp/plugins/My-Castle/resources/gm/anim_castle.gm1\" },\n" +
                "}\n",
                script);
            Assert.Contains("modules.files:overrideFileWith(replacement[1], replacement[2])", script);
            Assert.Contains("enable = function(self, config)", script);
            Assert.Contains("disable = function(self, config)", script);
        }

        [Fact]
        public void GamePath_IsLowerCaseLikeUcpLookup()
        {
            Assert.Equal("gm\\anim_castle.gm1", new GameFile(GameFolder.Gm, "Anim_Castle.GM1").GamePath);
            Assert.Equal("gfx\\frame.tgx", new GameFile(GameFolder.Gfx, "frame.tgx").GamePath);
        }

        [Fact]
        public void AddFile_SameFileWithOtherCase_ReplacesContent()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);
            plugin.AddFile(Castle, new byte[] { 1 });

            plugin.AddFile(new GameFile(GameFolder.Gm, "ANIM_CASTLE.gm1"), new byte[] { 9, 9 });

            Assert.Single(plugin.Files);
            Assert.Equal(new byte[] { 9, 9 }, File.ReadAllBytes(plugin.FindFile(Castle)!));
        }

        [Fact]
        public void AddFile_LeavesNoTemporaryFiles()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);

            plugin.AddFile(Castle, new byte[] { 1 });
            plugin.AddFile(Castle, new byte[] { 2 });

            Assert.Empty(Directory.EnumerateFiles(plugin.Folder, "*.tmp", SearchOption.AllDirectories));
        }

        [Fact]
        public void AddFile_CreatesOnlyValidExtensionFoldersForUcp()
        {
            UcpTexturePlugin.Open(ucp, info).AddFile(Castle, new byte[] { 1 });

            // UCP3 refuses to start if any folder in ucp/plugins is not named <name>-<version>.
            Assert.Equal(new[] { "My-Castle-1.0.0" }, Directory.GetDirectories(ucp.PluginsFolder).Select(Path.GetFileName));
        }

        [Fact]
        public void RemoveFile_RemovesOverrideButKeepsPlugin()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);
            plugin.AddFile(Castle, new byte[] { 1 });
            plugin.AddFile(Frame, new byte[] { 2 });

            Assert.True(plugin.RemoveFile(Castle));

            Assert.Equal(new[] { Frame }, plugin.Files);
            Assert.DoesNotContain("anim_castle", File.ReadAllText(Path.Combine(plugin.Folder, "init.lua")));
            Assert.True(plugin.RemoveFile(Frame));
            Assert.True(File.Exists(Path.Combine(plugin.Folder, "definition.yml")));
            Assert.Contains("local replacements = {\n}\n", File.ReadAllText(Path.Combine(plugin.Folder, "init.lua")));
        }

        [Fact]
        public void RemoveFile_NotContained_ReturnsFalse()
        {
            Assert.False(UcpTexturePlugin.Open(ucp, info).RemoveFile(Castle));
        }

        [Fact]
        public void Open_ExistingPluginWithOtherVersion_MovesFilesToNewVersion()
        {
            UcpTexturePlugin.Open(ucp, info).AddFile(Castle, new byte[] { 7 });
            var newVersion = new UcpExtensionInfo("My-Castle", "1.1.0", "My Castle", "Pode");

            var plugin = UcpTexturePlugin.Open(ucp, newVersion);

            Assert.Equal(new[] { "My-Castle-1.1.0" }, Directory.GetDirectories(ucp.PluginsFolder).Select(Path.GetFileName));
            Assert.Equal(new byte[] { 7 }, File.ReadAllBytes(plugin.FindFile(Castle)!));
            Assert.Contains("version: 1.1.0\n", File.ReadAllText(Path.Combine(plugin.Folder, "definition.yml")));
        }

        [Fact]
        public void Open_OtherPluginWithSimilarName_IsNotTouched()
        {
            UcpTexturePlugin.Open(ucp, new UcpExtensionInfo("My-Castle-Walls", "1.0.0")).AddFile(Castle, new byte[] { 1 });

            var plugin = UcpTexturePlugin.Open(ucp, info);

            Assert.Empty(plugin.Files);
            Assert.True(Directory.Exists(Path.Combine(ucp.PluginsFolder, "My-Castle-Walls-1.0.0")));
        }

        [Fact]
        public void UpdateDefinition_ChangedDisplayName_IsWritten()
        {
            UcpTexturePlugin.Open(ucp, info).AddFile(Castle, new byte[] { 1 });
            var plugin = UcpTexturePlugin.Open(ucp, new UcpExtensionInfo("My-Castle", "1.0.0", "Renamed", "Someone"));

            plugin.UpdateDefinition();

            string definition = File.ReadAllText(Path.Combine(plugin.Folder, "definition.yml"));
            Assert.Contains("display-name: \"Renamed\"", definition);
            Assert.Contains("author: \"Someone\"", definition);
        }

        [Fact]
        public void CreateDescription_ListsReplacedFiles()
        {
            var plugin = UcpTexturePlugin.Open(ucp, info);
            plugin.AddFile(Castle, new byte[] { 1 });

            string description = plugin.CreateDescription();

            Assert.StartsWith("# My Castle\n", description);
            Assert.Contains("- `gm/anim_castle.gm1`\n", description);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("..")]
        [InlineData("../anim_castle.gm1")]
        [InlineData("gm/anim_castle.gm1")]
        [InlineData("gm\\anim_castle.gm1")]
        [InlineData("c:anim_castle.gm1")]
        public void GameFile_RejectsPaths(string fileName)
        {
            Assert.Throws<ArgumentException>(() => new GameFile(GameFolder.Gm, fileName));
        }

        [Fact]
        public void UcpFolder_IsInstalled_DetectsUcp3()
        {
            Assert.False(ucp.IsInstalled);

            temp.WriteFile(Path.Combine("ucp", "ucp-version.yml"), new byte[] { 1 });

            Assert.True(ucp.IsInstalled);
        }
    }
}

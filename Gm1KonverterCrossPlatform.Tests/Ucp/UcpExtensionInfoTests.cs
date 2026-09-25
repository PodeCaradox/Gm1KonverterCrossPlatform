using System;
using System.Text.RegularExpressions;
using Gm1KonverterCrossPlatform.Core.Ucp;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Ucp
{
    public class UcpExtensionInfoTests
    {
        [Theory]
        [InlineData("Meine Texturen!", "Meine-Texturen")]
        [InlineData("Größere Burg", "Groessere-Burg")]
        [InlineData("a__b..c", "a-b-c")]
        [InlineData("  -castle- ", "castle")]
        [InlineData("v2 castle 3", "v2-castle-3")]
        [InlineData("", UcpExtensionInfo.DefaultName)]
        [InlineData(null, UcpExtensionInfo.DefaultName)]
        [InlineData("---", UcpExtensionInfo.DefaultName)]
        [InlineData("Крепость", UcpExtensionInfo.DefaultName)]
        public void ToExtensionName_KeepsOnlyCharactersUcpAccepts(string? text, string expected)
        {
            string name = UcpExtensionInfo.ToExtensionName(text);

            Assert.Equal(expected, name);
            Assert.True(UcpExtensionInfo.IsValidName(name));
        }

        [Theory]
        [InlineData("1.0.0", true)]
        [InlineData("12.3.45", true)]
        [InlineData(" 1.0.0 ", true)]
        [InlineData("1.0", false)]
        [InlineData("v1.0.0", false)]
        [InlineData("1.0.0-beta", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidVersion_AcceptsOnlyMajorMinorPatch(string? version, bool expected)
        {
            Assert.Equal(expected, UcpExtensionInfo.IsValidVersion(version));
        }

        [Theory]
        [InlineData("a_b")]
        [InlineData("a.b")]
        [InlineData("-a")]
        [InlineData("a-")]
        [InlineData("a--b")]
        [InlineData("")]
        public void Constructor_InvalidName_Throws(string name)
        {
            Assert.Throws<ArgumentException>(() => new UcpExtensionInfo(name, "1.0.0"));
        }

        [Fact]
        public void Constructor_InvalidVersion_Throws()
        {
            Assert.Throws<ArgumentException>(() => new UcpExtensionInfo("castle", "1"));
        }

        [Fact]
        public void FromUserInput_InvalidVersion_UsesDefaultVersion()
        {
            var info = UcpExtensionInfo.FromUserInput("My Castle", " Pode ", "latest");

            Assert.Equal("My-Castle", info.Name);
            Assert.Equal("My Castle", info.DisplayName);
            Assert.Equal("Pode", info.Author);
            Assert.Equal(UcpExtensionInfo.DefaultVersion, info.Version);
        }

        [Fact]
        public void FromUserInput_Empty_UsesDefaults()
        {
            var info = UcpExtensionInfo.FromUserInput(null, null, null);

            Assert.Equal(UcpExtensionInfo.DefaultName, info.Name);
            Assert.Equal(UcpExtensionInfo.DefaultName, info.DisplayName);
            Assert.Equal(string.Empty, info.Author);
            Assert.Equal("Gm1Konverter-Textures-1.0.0", info.FolderName);
        }

        [Theory]
        [InlineData("castle", "1.0.0")]
        [InlineData("castle-2", "1.0.0")]
        [InlineData("Gm1Konverter-Textures", "10.20.30")]
        [InlineData("a1-2-3", "4.5.6")]
        public void FolderName_IsParsedBackByUcp(string name, string version)
        {
            var info = new UcpExtensionInfo(name, version);

            var (parsedName, parsedVersion) = ParseLikeUcp(info.FolderName);

            Assert.Equal(name, parsedName);
            Assert.Equal(version, parsedVersion);
        }

        [Fact]
        public void FolderName_OfAnyUserInput_IsParsedBackByUcp()
        {
            var random = new Random(3);
            const string characters = "aZ09 -_.!äß/\\\"'Ж";
            for (int i = 0; i < 500; i++)
            {
                var text = new char[random.Next(0, 20)];
                for (int c = 0; c < text.Length; c++)
                {
                    text[c] = characters[random.Next(characters.Length)];
                }

                var info = UcpExtensionInfo.FromUserInput(new string(text), null, $"{random.Next(0, 20)}.{random.Next(0, 20)}.{random.Next(0, 200)}");

                Assert.Equal((info.Name, info.Version), ParseLikeUcp(info.FolderName));
            }
        }

        /// <summary>
        /// Port of <c>utils.parseExtensionsFolder</c> (UCP3 content/ucp/code/config/utils.lua):
        /// the version is <c>(-[0-9\.]+)$</c>, the name is <c>([a-zA-Z0-9-]+)$</c> of the rest.
        /// </summary>
        private static (string Name, string Version) ParseLikeUcp(string folderName)
        {
            var version = Regex.Match(folderName, @"-[0-9\\.]+$");
            Assert.True(version.Success, folderName);

            string rest = folderName.Substring(0, version.Index);
            var name = Regex.Match(rest, "[a-zA-Z0-9-]+$");
            Assert.True(name.Success && name.Index == 0, folderName);

            return (name.Value, version.Value.Substring(1));
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>Files every generated UCP3 extension (plugin or module) consists of.</summary>
    internal static class ExtensionFiles
    {
        public const string DefinitionFileName = "definition.yml";
        public const string InitFileName = "init.lua";
        public const string DescriptionFile = "locale/description-en.md";

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        /// <summary>The definition.yml read by UCP3 and its GUI.</summary>
        /// <param name="type">"plugin" or "module", must match the folder the extension is in.</param>
        public static string CreateDefinition(UcpExtensionInfo info, string type, string description, IEnumerable<KeyValuePair<string, string>> dependencies)
        {
            var yaml = new StringBuilder();
            yaml.Append("name: ").Append(info.Name).Append('\n');
            yaml.Append("display-name: ").Append(ScriptText.YamlString(info.DisplayName)).Append('\n');
            yaml.Append("author: ").Append(ScriptText.YamlString(info.Author)).Append('\n');
            yaml.Append("version: ").Append(info.Version).Append('\n');
            yaml.Append("type: ").Append(type).Append('\n');
            yaml.Append("description: ").Append(ScriptText.YamlString(description)).Append('\n');
            yaml.Append("dependencies:\n");
            foreach (var dependency in dependencies)
            {
                yaml.Append("  ").Append(dependency.Key).Append(": ").Append(dependency.Value).Append('\n');
            }

            yaml.Append("meta:\n");
            yaml.Append("  version: 1.0.0\n");
            return yaml.ToString();
        }

        /// <summary>Writes definition.yml, init.lua and the English description into <paramref name="folder"/>.</summary>
        public static void Write(string folder, string definition, string initScript, string description)
        {
            Directory.CreateDirectory(folder);
            WriteText(Path.Combine(folder, DefinitionFileName), definition);
            WriteText(Path.Combine(folder, InitFileName), initScript);

            string descriptionPath = Path.Combine(folder, DescriptionFile);
            Directory.CreateDirectory(Path.GetDirectoryName(descriptionPath)!);
            WriteText(descriptionPath, description);
        }

        /// <summary>
        /// If the extension folder does not exist but exactly one other version does, that folder is renamed,
        /// so a new version keeps the content of the old one.
        /// </summary>
        /// <returns>True if a folder was renamed.</returns>
        public static bool MoveOtherVersion(string folder, IReadOnlyList<string> foldersOfAllVersions)
        {
            if (Directory.Exists(folder) || foldersOfAllVersions.Count != 1)
            {
                return false;
            }

            Directory.Move(foldersOfAllVersions[0], folder);
            return true;
        }

        public static void WriteText(string path, string text) => WriteBytes(path, Utf8WithoutBom.GetBytes(text));

        /// <summary>Writes to a temporary file first so the game never reads a half written file.</summary>
        public static void WriteBytes(string path, byte[] content)
        {
            string temporaryFile = path + ".tmp";
            File.WriteAllBytes(temporaryFile, content);
            File.Move(temporaryFile, path, overwrite: true);
        }
    }
}

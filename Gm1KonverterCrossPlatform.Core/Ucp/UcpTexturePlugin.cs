using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>
    /// A UCP3 plugin that replaces game files (.gm1, .tgx) instead of overwriting them in the Stronghold
    /// folder. The replacements live in <c>resources/gm</c> and <c>resources/gfx</c>; the generated
    /// <c>init.lua</c> registers each of them with the "files" module of UCP3.
    /// </summary>
    /// <remarks>
    /// The plugin folder is never deleted, not even when the last file is removed: UCP3 refuses to start
    /// the game if an activated plugin is missing.
    /// </remarks>
    public sealed class UcpTexturePlugin
    {
        public const string DefinitionFileName = "definition.yml";
        public const string InitFileName = "init.lua";
        public const string ResourcesFolderName = "resources";

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private UcpTexturePlugin(UcpExtensionInfo info, string folder)
        {
            Info = info;
            Folder = folder;
        }

        public UcpExtensionInfo Info { get; }

        /// <summary>The plugin folder <c>ucp/plugins/&lt;name&gt;-&lt;version&gt;</c>.</summary>
        public string Folder { get; }

        /// <summary>
        /// Opens the plugin <paramref name="info"/> in <paramref name="ucp"/>. If the plugin only exists with
        /// another version, that folder is renamed to the new version so no replacement gets lost.
        /// Otherwise nothing is written until a file is added.
        /// </summary>
        public static UcpTexturePlugin Open(UcpFolder ucp, UcpExtensionInfo info)
        {
            if (ucp == null) throw new ArgumentNullException(nameof(ucp));
            if (info == null) throw new ArgumentNullException(nameof(info));

            var plugin = new UcpTexturePlugin(info, ucp.PluginFolder(info));
            if (!Directory.Exists(plugin.Folder))
            {
                var otherVersions = ucp.FindPluginFolders(info.Name);
                if (otherVersions.Count == 1)
                {
                    Directory.Move(otherVersions[0], plugin.Folder);
                    plugin.UpdateDefinition();
                }
            }

            return plugin;
        }

        /// <summary>True once a file was added.</summary>
        public bool Exists => Directory.Exists(Folder);

        /// <summary>The replaced game files, sorted by path.</summary>
        public IReadOnlyList<GameFile> Files
        {
            get
            {
                var files = new List<GameFile>();
                foreach (GameFolder gameFolder in Enum.GetValues(typeof(GameFolder)))
                {
                    string directory = ResourceFolder(gameFolder);
                    if (!Directory.Exists(directory))
                    {
                        continue;
                    }

                    files.AddRange(Directory.EnumerateFiles(directory)
                        .Select(Path.GetFileName)
                        .OfType<string>()
                        .Where(name => GameFile.IsValidFileName(name) && !name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                        .Select(name => new GameFile(gameFolder, name)));
                }

                return files.OrderBy(file => file.GamePath, StringComparer.Ordinal).ToList();
            }
        }

        public bool Contains(GameFile file) => FindFile(file) != null;

        /// <summary>The full path of the replacement for <paramref name="file"/>, or null.</summary>
        public string? FindFile(GameFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            string directory = ResourceFolder(file.Folder);
            if (!Directory.Exists(directory))
            {
                return null;
            }

            return Directory.EnumerateFiles(directory)
                .FirstOrDefault(path => string.Equals(Path.GetFileName(path), file.FileName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Adds or updates the replacement for <paramref name="file"/>.</summary>
        public void AddFile(GameFile file, byte[] content)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (content == null) throw new ArgumentNullException(nameof(content));

            string target = FindFile(file) ?? Path.Combine(ResourceFolder(file.Folder), file.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            WriteSafely(target, content);
            UpdateDefinition();
        }

        /// <summary>Removes the replacement, so the game loads its original file again.</summary>
        /// <returns>False if the plugin did not contain the file.</returns>
        public bool RemoveFile(GameFile file)
        {
            string? path = FindFile(file);
            if (path == null)
            {
                return false;
            }

            File.Delete(path);
            UpdateDefinition();
            return true;
        }

        /// <summary>The generated definition.yml.</summary>
        public string CreateDefinition()
        {
            var yaml = new StringBuilder();
            yaml.Append("name: ").Append(Info.Name).Append('\n');
            yaml.Append("display-name: ").Append(ScriptText.YamlString(Info.DisplayName)).Append('\n');
            yaml.Append("author: ").Append(ScriptText.YamlString(Info.Author)).Append('\n');
            yaml.Append("version: ").Append(Info.Version).Append('\n');
            yaml.Append("dependencies:\n");
            yaml.Append("  framework: ^3.0.0\n");
            yaml.Append("  frontend: ^1.0.0\n");
            yaml.Append("  files: ^1.0.0\n");
            yaml.Append("meta:\n");
            yaml.Append("  version: 1.0.0\n");
            return yaml.ToString();
        }

        /// <summary>The generated init.lua, which registers every replacement with the "files" module.</summary>
        public string CreateInitScript()
        {
            var lua = new StringBuilder();
            lua.Append("-- Generated by Gm1 Konverter. The file is rewritten whenever a file is added or removed.\n");
            lua.Append("-- Each entry replaces the game file (left) with the file of this plugin (right).\n");
            lua.Append("local replacements = {\n");
            foreach (var file in Files)
            {
                string pluginPath = $"{UcpFolder.FolderName}/{UcpFolder.PluginsFolderName}/{Info.Name}/{ResourcesFolderName}/{file.FolderName}/{file.FileName}";
                lua.Append("  { ").Append(ScriptText.LuaString(file.GamePath)).Append(", ").Append(ScriptText.LuaString(pluginPath)).Append(" },\n");
            }

            lua.Append("}\n");
            lua.Append('\n');
            lua.Append("return {\n");
            lua.Append("  enable = function(self, config)\n");
            lua.Append("    for _, replacement in ipairs(replacements) do\n");
            lua.Append("      modules.files:overrideFileWith(replacement[1], replacement[2])\n");
            lua.Append("    end\n");
            lua.Append("  end,\n");
            lua.Append('\n');
            lua.Append("  disable = function(self, config)\n");
            lua.Append("  end,\n");
            lua.Append("}\n");
            return lua.ToString();
        }

        /// <summary>The generated English description shown in the UCP3 GUI.</summary>
        public string CreateDescription()
        {
            var markdown = new StringBuilder();
            markdown.Append("# ").Append(ScriptText.SingleLine(Info.DisplayName)).Append('\n');
            markdown.Append('\n');
            markdown.Append("Textures created with the Gm1 Konverter.\n");
            markdown.Append('\n');
            markdown.Append("Replaced files:\n");
            markdown.Append('\n');
            foreach (var file in Files)
            {
                markdown.Append("- `").Append(ScriptText.SingleLine(file.ToString()).Replace("`", "'")).Append("`\n");
            }

            return markdown.ToString();
        }

        private string ResourceFolder(GameFolder folder) => Path.Combine(Folder, ResourcesFolderName, GameFile.FolderNameOf(folder));

        /// <summary>
        /// Rewrites definition.yml, init.lua and the description, e.g. after the name or author changed.
        /// Creates the plugin if it does not exist yet.
        /// </summary>
        public void UpdateDefinition()
        {
            Directory.CreateDirectory(Folder);
            WriteSafely(Path.Combine(Folder, DefinitionFileName), Utf8WithoutBom.GetBytes(CreateDefinition()));
            WriteSafely(Path.Combine(Folder, InitFileName), Utf8WithoutBom.GetBytes(CreateInitScript()));

            string locale = Path.Combine(Folder, "locale");
            Directory.CreateDirectory(locale);
            WriteSafely(Path.Combine(locale, "description-en.md"), Utf8WithoutBom.GetBytes(CreateDescription()));
        }

        /// <summary>Writes to a temporary file first so the game never reads a half written file.</summary>
        private static void WriteSafely(string path, byte[] content)
        {
            string temporaryFile = path + ".tmp";
            File.WriteAllBytes(temporaryFile, content);
            File.Move(temporaryFile, path, overwrite: true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>
    /// The "ucp" folder of the Unofficial Crusader Patch 3 inside the Stronghold installation folder.
    /// </summary>
    public sealed class UcpFolder
    {
        public const string FolderName = "ucp";
        public const string PluginsFolderName = "plugins";
        public const string ModulesFolderName = "modules";

        private const string VersionFile = "ucp-version.yml";
        private const string CodeFolderName = "code";

        public UcpFolder(string strongholdRoot)
        {
            if (string.IsNullOrWhiteSpace(strongholdRoot)) throw new ArgumentException("The Stronghold folder is not set.", nameof(strongholdRoot));
            Root = Path.Combine(strongholdRoot, FolderName);
        }

        public string Root { get; }

        /// <summary>True if UCP3 is installed. Extensions are written in any case and work once it is installed.</summary>
        public bool IsInstalled => File.Exists(Path.Combine(Root, VersionFile)) || Directory.Exists(Path.Combine(Root, CodeFolderName));

        public string PluginsFolder => Path.Combine(Root, PluginsFolderName);

        public string ModulesFolder => Path.Combine(Root, ModulesFolderName);

        public string PluginFolder(UcpExtensionInfo info) => Path.Combine(PluginsFolder, info.FolderName);

        public string ModuleFolder(UcpExtensionInfo info) => Path.Combine(ModulesFolder, info.FolderName);

        /// <summary>A module packed as zip, the form the UCP3 GUI lists (it shows module folders only for developer builds).</summary>
        public string ModuleZip(UcpExtensionInfo info) => Path.Combine(ModulesFolder, info.FolderName + ".zip");

        /// <summary>Folders of the plugin <paramref name="name"/> in any version.</summary>
        public IReadOnlyList<string> FindPluginFolders(string name) => FindExtensionFolders(PluginsFolder, name);

        /// <summary>Folders of the module <paramref name="name"/> in any version.</summary>
        public IReadOnlyList<string> FindModuleFolders(string name) => FindExtensionFolders(ModulesFolder, name);

        /// <summary>Zip files of the module <paramref name="name"/> in any version.</summary>
        public IReadOnlyList<string> FindModuleZips(string name)
        {
            if (!Directory.Exists(ModulesFolder))
            {
                return Array.Empty<string>();
            }

            string prefix = name + "-";
            return Directory.EnumerateFiles(ModulesFolder, "*.zip")
                .Where(file =>
                {
                    string baseName = Path.GetFileNameWithoutExtension(file);
                    return baseName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && UcpExtensionInfo.IsValidVersion(baseName.Substring(prefix.Length));
                })
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<string> FindExtensionFolders(string parent, string name)
        {
            if (!Directory.Exists(parent))
            {
                return Array.Empty<string>();
            }

            string prefix = name + "-";
            return Directory.EnumerateDirectories(parent)
                .Where(folder =>
                {
                    string folderName = Path.GetFileName(folder);
                    return folderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && UcpExtensionInfo.IsValidVersion(folderName.Substring(prefix.Length));
                })
                .OrderBy(folder => folder, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}

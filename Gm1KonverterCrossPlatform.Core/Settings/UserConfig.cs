namespace Gm1KonverterCrossPlatform.Core.Settings
{
    /// <summary>
    /// The user's settings. Stored as JSON by <see cref="UserConfigStore"/>; the property names are
    /// part of the file format.
    /// </summary>
    public sealed class UserConfig
    {
        public Language Language { get; set; } = Language.English;

        public ColorTheme ColorTheme { get; set; } = ColorTheme.Light;

        /// <summary>The Stronghold (Crusader) installation folder.</summary>
        public string? CrusaderPath { get; set; }

        public string? WorkFolderPath { get; set; }

        public bool OpenFolderAfterExport { get; set; }

        public bool ActivateLogger { get; set; }

        /// <summary>Display name of the UCP3 plugin that receives the modified files.</summary>
        public string? UcpModName { get; set; }

        public string? UcpModAuthor { get; set; }

        /// <summary>Version of the UCP3 plugin, e.g. "1.0.0".</summary>
        public string? UcpModVersion { get; set; }
    }
}

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
    }
}

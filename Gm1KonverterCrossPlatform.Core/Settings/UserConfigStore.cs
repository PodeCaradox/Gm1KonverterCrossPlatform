using System;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;
using Newtonsoft.Json;

namespace Gm1KonverterCrossPlatform.Core.Settings
{
    /// <summary>Loads and saves the <see cref="UserConfig"/>.</summary>
    public sealed class UserConfigStore
    {
        public const string DefaultFileName = "UserConfig.txt";

        public UserConfigStore(string directory, string fileName = DefaultFileName)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A directory is required.", nameof(directory));
            FilePath = Path.Combine(directory, fileName);
        }

        public string FilePath { get; }

        /// <summary>
        /// Loads the config. A missing or unreadable file results in the default config, so the program always starts.
        /// </summary>
        public UserConfig Load()
        {
            UserConfig? config = null;
            try
            {
                if (File.Exists(FilePath))
                {
                    config = JsonConvert.DeserializeObject<UserConfig>(File.ReadAllText(FilePath));
                }
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
            {
                config = null;
            }

            config ??= new UserConfig();
            config.CrusaderPath = StrongholdFolder.NormalizeRoot(config.CrusaderPath);
            if (!Enum.IsDefined(typeof(Language), config.Language)) config.Language = Language.English;
            if (!Enum.IsDefined(typeof(ColorTheme), config.ColorTheme)) config.ColorTheme = ColorTheme.Light;
            return config;
        }

        public void Save(UserConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            ImageFiles.EnsureDirectoryOf(FilePath);
            string temporaryFile = FilePath + ".tmp";
            File.WriteAllText(temporaryFile, JsonConvert.SerializeObject(config, Formatting.Indented));
            File.Move(temporaryFile, FilePath, overwrite: true);
        }
    }
}

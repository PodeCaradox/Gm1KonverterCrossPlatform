using System.IO;
using Newtonsoft.Json;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    public class UserConfig
    {
        private readonly string path = Config.AppDataPath;
        private readonly string fileName = "UserConfig.txt";

        private Languages.Language language = Languages.DefaultLanguage;
        private ColorThemes.ColorTheme colorTheme = ColorThemes.DefaultColorTheme;
        private string crusaderPath;
        private string workFolderPath;
        private bool openFolderAfterExport;
        private bool activateLogger;

        public Languages.Language Language
        {
            get => language;
            set
            {
                language = value;
                SaveData();
            }
        }

        public ColorThemes.ColorTheme ColorTheme
        {
            get => colorTheme;
            set
            {
                colorTheme = value;
                SaveData();
            }
        }

        public string CrusaderPath
        {
            get => crusaderPath;
            set
            {
                crusaderPath = value;
                SaveData();
            }
        }

        public string WorkFolderPath
        {
            get => workFolderPath;
            set
            {
                workFolderPath = value;
                SaveData();
            }
        }

        public bool OpenFolderAfterExport
        {
            get => openFolderAfterExport;
            set
            {
                openFolderAfterExport = value;
                SaveData();
            }
        }

        public bool ActivateLogger
        {
            get => activateLogger;
            set
            {
                Logger.Loggeractiv = value;
                activateLogger = value;
                SaveData();
            }
        }

        /// <summary>
        /// Load the UserConfig from a file.
        /// </summary>
        public void LoadData()
        {
            string filePath = Path.Combine(path, fileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                UserConfig obj = JsonConvert.DeserializeObject<UserConfig>(json);

                language = obj.Language;
                colorTheme = obj.colorTheme;
                crusaderPath = obj.CrusaderPath.Replace("\\gm",string.Empty);
                workFolderPath = obj.WorkFolderPath;
                openFolderAfterExport = obj.OpenFolderAfterExport;
                activateLogger = obj.ActivateLogger;
            }
        }

        /// <summary>
        /// Save the UserConfig to a file.
        /// </summary>
        private void SaveData()
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filePath = Path.Combine(path, fileName);
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}

using Newtonsoft.Json;
using System;
using System.IO;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    public class UserConfig
    {
        #region Variables
        private Languages.Language language = Languages.DefaultLanguage;
        private readonly string path;
        private readonly string fileName = "UserConfig.txt";
        private string crusaderPath;
        private string workFolderPath;
        private bool openFolderAfterExport;
        private bool activateLogger;
        #endregion

        #region Construtor

        public UserConfig()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Combine(appData, "Gm1ConverterCrossPlatform");
        }

        #endregion
        
        #region GetterSetter

        public string CrusaderPath
        {
            get => crusaderPath;
            set
            {
                crusaderPath = value;
                SaveData();
            }
        }

        public Languages.Language Language
        {
            get => language;
            set
            {
                language = value;
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

        #endregion

        #region Methods

        /// <summary>
        /// Load the UserConfig from a file.
        /// </summary>
        public void LoadData()
        {
            string filePath = Path.Combine(path, fileName);
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var obj = JsonConvert.DeserializeObject<UserConfig>(json);
                crusaderPath = obj.CrusaderPath;
                workFolderPath = obj.WorkFolderPath;
                openFolderAfterExport = obj.OpenFolderAfterExport;
                activateLogger = obj.ActivateLogger;
                language = obj.Language;
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

        #endregion
    }
}

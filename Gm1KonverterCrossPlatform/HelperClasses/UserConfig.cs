using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    public class UserConfig
    {
        #region Variables

        private String path;
        private String crusaderPath;
        private String workFolderPath;
        private bool openFolderAfterExport;
        private bool activateLogger;
        #endregion

        #region Construtor

        public UserConfig()
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Gm1ConverterCrossPlatform";
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
        /// Load the Userconfig
        /// </summary>
        public void LoadData()
        {
            if (Directory.Exists(path))
            {
                var json = File.ReadAllText(path + "\\UserConfig.txt");
                var obj = JsonConvert.DeserializeObject<UserConfig>(json);
                crusaderPath = obj.CrusaderPath;
                workFolderPath = obj.WorkFolderPath;
                openFolderAfterExport = obj.OpenFolderAfterExport;
                activateLogger = obj.ActivateLogger;
            }
            else
            {
                Directory.CreateDirectory(path);
            }

        }
        /// <summary>
        /// Save the Userconfig
        /// </summary>
        private void SaveData()
        {
            String json = JsonConvert.SerializeObject(this);
            File.WriteAllText(path + "\\UserConfig.txt", json);
        }

        #endregion



    }
}

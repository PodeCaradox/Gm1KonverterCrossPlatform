using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    public class UserConfig
    {
        public UserConfig()
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Gm1ConverterCrossPlatform";




        }

        public void LoadData()
        {
            if (Directory.Exists(path))
            {
                var json = File.ReadAllText(path + "\\UserConfig.txt");
                var obj = JsonConvert.DeserializeObject<UserConfig>(json);
                CrusaderPath = obj.CrusaderPath;
                WorkFolderPath = obj.WorkFolderPath;
                OpenFolderAfterExport = obj.OpenFolderAfterExport;
            }
            else
            {
                Directory.CreateDirectory(path);
            }
           
        }

        private void SafeData()
        {
            String json = JsonConvert.SerializeObject(this);
            File.WriteAllText(path + "\\UserConfig.txt", json);
        }

        private String path;
        private String crusaderPath;
        private String workFolderPath;
        private bool openFolderAfterExport;

        public string CrusaderPath {
            get => crusaderPath;
            set {
                crusaderPath = value;
                SafeData();
            }
        }
        public string WorkFolderPath {
            get => workFolderPath;
            set {

                workFolderPath = value;
                SafeData();
            }
        }

        public bool OpenFolderAfterExport {
            get => openFolderAfterExport;
            set {
                openFolderAfterExport = value;
                SafeData();
            }
        }
    }
}

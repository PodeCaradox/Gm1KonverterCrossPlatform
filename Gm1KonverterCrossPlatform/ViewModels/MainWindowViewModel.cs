
using ReactiveUI;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Files.Gm1Converter;
using Avalonia.Media.Imaging;
using HelperClasses.Gm1Converter;
using Gm1KonverterCrossPlatform.HelperClasses.Views;
using System.IO;
using System;
using Gm1KonverterCrossPlatform.HelperClasses;
using Avalonia;
using System.Linq;

namespace Gm1KonverterCrossPlatform.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        #region Variables

        private UserConfig userConfig;

        private String colorAsText = "";
        public String ColorAsText
        {

            get => colorAsText;
            set
            {
                this.RaiseAndSetIfChanged(ref colorAsText, value);


            }
        }

        
        private UserConfig.Languages actualLanguage = UserConfig.Languages.English;
        public UserConfig.Languages ActualLanguage
        {

            get => actualLanguage;
            set {
                this.RaiseAndSetIfChanged(ref actualLanguage, value);
                Utility.SelectCulture(value);
                ChangeLanguages();
                userConfig.Language = value;
            }
        }

        private void ChangeLanguages()
        {
            if (File == null) return; 
            Filetype = Utility.GetText("Datatype") + ((GM1FileHeader.DataType)File.FileHeader.IDataType);
           if(File.Palette!=null) ActualPalette = Utility.GetText("Palette") + (File.Palette.ActualPalette + 1);


        }

        private UserConfig.Languages[] languages = new UserConfig.Languages[]{ UserConfig.Languages.Deutsch, UserConfig.Languages.English, UserConfig.Languages.Русский };
        public UserConfig.Languages[] Languages
        {

            get => languages;
            set => this.RaiseAndSetIfChanged(ref languages, value);
        }

     
        private String filetype = "Datatype: ";
        public String Filetype
        {

            get => filetype;
            set => this.RaiseAndSetIfChanged(ref filetype, value);
        }

        private int red = 0;
        public int Red
        {

            get => red;
            set {
                if (value > 255)
                {
                    value = 255;
                }
                else if (value < 0)
                {
                    value = 0;
                }
                this.RaiseAndSetIfChanged(ref red, value);
                ColorAsText = "#" + 255.ToString("X2") + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
            }
        }

        private int blue = 0;
        public int Blue
        {

            get => blue;
            set
            {
                if (value>255)
                {
                    value = 255;
                }
                else if (value < 0)
                {
                    value = 0;
                }

                this.RaiseAndSetIfChanged(ref blue, value);
                ColorAsText = "#" + 255.ToString("X2") + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
            }
        }

        private int green = 0;
        public int Green
        {

            get => green;
            set
            {
                if (value > 255)
                {
                    value = 255;
                }
                else if (value < 0)
                {
                    value = 0;
                }
                this.RaiseAndSetIfChanged(ref green, value);
                ColorAsText = "#" + 255.ToString("X2") + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
            }
        }

      
        private String actualPalette = Utility.GetText("Palette")+"1";
        public String ActualPalette
        {

            get => actualPalette;
            set => this.RaiseAndSetIfChanged(ref actualPalette, value);
        }

        

        internal String[] workfolderFiles;
        internal String[] WorkfolderFiles
        {
            get => workfolderFiles;
            set => this.RaiseAndSetIfChanged(ref workfolderFiles, value);
        }

        internal String[] strongholdFiles;
        internal String[] StrongholdFiles
        {
            get => strongholdFiles;
            set => this.RaiseAndSetIfChanged(ref strongholdFiles, value);
        }

        private bool buttonsEnabled = false;
        public bool ButtonsEnabled
        {

            get => buttonsEnabled;
            set => this.RaiseAndSetIfChanged(ref buttonsEnabled, value);
        }

        private bool openFolderAfterExport = false;
        public bool OpenFolderAfterExport
        {

            get => openFolderAfterExport;
            set
            {
                this.RaiseAndSetIfChanged(ref openFolderAfterExport, value);
                userConfig.OpenFolderAfterExport = value;
            }
        }

        private bool loggerActiv = false;
        public bool LoggerActiv
        {

            get => loggerActiv;
            set {
                this.RaiseAndSetIfChanged(ref loggerActiv, value);
                userConfig.ActivateLogger = value;

            }
        }

        private bool replaceWithSaveFile = false;
        public bool ReplaceWithSaveFile
        {

            get => replaceWithSaveFile;
            set => this.RaiseAndSetIfChanged(ref replaceWithSaveFile, value);
        }

        private bool colorButtonsEnabled = false;
        public bool ColorButtonsEnabled
        {

            get => colorButtonsEnabled;
            set => this.RaiseAndSetIfChanged(ref colorButtonsEnabled, value);
        }
        private bool importButtonEnabled = false;
        public bool ImportButtonEnabled
        {

            get => importButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref importButtonEnabled, value);
        }

        private bool decodeButtonEnabled = false;
        public bool DecodeButtonEnabled
        {

            get => decodeButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref decodeButtonEnabled, value);
        }

        internal WriteableBitmap actuellColorTableChangeColorWindow;
        internal WriteableBitmap ActuellColorTableChangeColorWindow
        {
            get => actuellColorTableChangeColorWindow;
            set => this.RaiseAndSetIfChanged(ref actuellColorTableChangeColorWindow, value);
        }

        internal WriteableBitmap actuellColorTable;
        internal WriteableBitmap ActuellColorTable
        {
            get => actuellColorTable;
            set => this.RaiseAndSetIfChanged(ref actuellColorTable, value);
        }

        internal ObservableCollection<Image> images = new ObservableCollection<Image>();

        internal ObservableCollection<Image> TGXImages
        {
            get => images;
            set => this.RaiseAndSetIfChanged(ref images, value);
        }
        internal DecodedFile File { get; set; }
        public UserConfig UserConfig { get => userConfig; set => userConfig = value; }


        #endregion

        #region Methods

        /// <summary>
        /// Load the GM1 Files from the CrusaderPath
        /// </summary>
        internal void LoadStrongholdFiles()
        {
            if (!String.IsNullOrEmpty(userConfig.CrusaderPath))
            {
                StrongholdFiles = Utility.GetFileNames(userConfig.CrusaderPath, "*.gm1");

            }

        }

        internal void LoadWorkfolderFiles()
        {
            if (!String.IsNullOrEmpty(userConfig.WorkFolderPath))
            {
                WorkfolderFiles = Utility.GetDirectoryNames(userConfig.WorkFolderPath);

            }

        }


        /// <summary>
        /// Decode the GM1 File to IMGS and Headers
        /// </summary>
        /// <param name="fileName">The Filepath/Filename to decode</param>
        /// <param name="window">actual avalonia window for error Text</param>
        /// <returns></returns>
        internal bool DecodeData(string fileName, Window window)
        {
          
            //Convert Selected file
            
                 File = new DecodedFile();
                if (!File.DecodeGm1File(Utility.FileToByteArray(userConfig.CrusaderPath+"\\"+ fileName), fileName))
                {
                    MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, (GM1FileHeader.DataType)File.FileHeader.IDataType + Utility.GetText("TilesarenotSupportedyet"));
                    messageBox.ShowDialog(window);
                    return false;
                }
                
                if((GM1FileHeader.DataType)File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject)
                {
                    ShowTileImgToWindow();
                }
                else
                {
                    ShowTGXImgToWindow();
                }
                

            return true;


          
        }

        /// <summary>
        /// Show the Imgs to the main Window
        /// </summary>
        private void ShowTileImgToWindow()
        {
            TGXImages = new ObservableCollection<Image>();
            for (int j = 0; j < File.TilesImages.Count; j++)
            {
                var bitmap = File.TilesImages[j].TileImage;

                Image image = new Image();
                image.MaxHeight = File.TilesImages[j].Height;
                image.MaxWidth = File.TilesImages[j].Width;
                image.Source = bitmap;
                TGXImages.Add(image);

            }
        }

        /// <summary>
        /// Show the Tile Imgs to the main Window
        /// </summary>
        private void ShowTGXImgToWindow()
        {
            TGXImages = new ObservableCollection<Image>();
            for (int j = 0; j < File.FileHeader.INumberOfPictureinFile; j++)
            {
                var bitmap = File.ImagesTGX[j].Bitmap;

                Image image = new Image();
                image.MaxHeight = File.ImagesTGX[j].Height;
                image.MaxWidth = File.ImagesTGX[j].Width;
                image.Source = bitmap;
                TGXImages.Add(image);

            }
            if (File.Palette!=null)
            {
                ActuellColorTable = File.Palette.Bitmaps[File.Palette.ActualPalette];
            }
           
        }

        /// <summary>
        /// Changes the actual Paletteimg
        /// </summary>
        /// <param name="number"></param>
        internal void ChangePalette(int number)
        {
            if (number>0)
            {
                    if (File.Palette.ActualPalette+ number>9)
                    {
                    File.Palette.ActualPalette = 0;
                    }
                    else
                    {
                    File.Palette.ActualPalette += number;
                    }
            }
            else
            {
                    if (File.Palette.ActualPalette + number < 0)
                    {
                    File.Palette.ActualPalette = 9;
                    }
                    else
                    {
                    File.Palette.ActualPalette += number;
                    }
            }
            ActualPalette = Utility.GetText("Palette")  + (File.Palette.ActualPalette+1);
            File.DecodeGm1File(File.FileArray, File.FileHeader.Name);
            ShowTGXImgToWindow();
        }

        /// <summary>
        /// Generate the Palette with the IMGS new, maybe after Import from new Colortables
        /// </summary>
        internal void GeneratePaletteAndImgNew()
        {
            File.DecodeGm1File(File.FileArray, File.FileHeader.Name);
            ShowTGXImgToWindow();
        }

        #endregion

    }
}

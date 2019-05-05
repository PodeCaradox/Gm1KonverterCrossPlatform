
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

namespace Gm1KonverterCrossPlatform.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private UserConfig userConfig;

        private String colorAsText = "";
        public String ColorAsText
        {

            get => colorAsText;
            set {
                this.RaiseAndSetIfChanged(ref colorAsText, value);


            }
        }

        private String yOffset = "";
        public String YOffset
        {

            get => yOffset;
            set => this.RaiseAndSetIfChanged(ref yOffset, value);
        }

        private String xOffset = "";
        public String XOffset
        {

            get => xOffset;
            set => this.RaiseAndSetIfChanged(ref xOffset, value);
        }


        private String actualPalette = "Palette 1";
        public String ActualPalette
        {

            get => actualPalette;
            set => this.RaiseAndSetIfChanged(ref actualPalette, value);
        }



     

        internal void LoadStrongholdFiles()
        {
            if (!String.IsNullOrEmpty( userConfig.CrusaderPath))
            {
                StrongholdFiles = Utility.GetFileNames(userConfig.CrusaderPath,"*.gm1");
          
            }
                
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
            set {
                this.RaiseAndSetIfChanged(ref openFolderAfterExport, value);
                userConfig.OpenFolderAfterExport = value;
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

        #region Methods

        internal bool DecodeData(string fileName, Window window)
        {
          
            //Convert Selected file
            
                 File = new DecodedFile();
                if (!File.DecodeGm1File(Utility.FileToByteArray(userConfig.CrusaderPath+"\\"+ fileName), fileName))
                {
                    MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Only Animation Tiles are Supported yet. \nError from " + fileName);
                    messageBox.ShowDialog(window);
                    return false;
                }
                
                if((GM1FileHeader.DataType)File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject)
                {
                    File.CreateTileImage();
                    ShowTileImgToWindow();
                }
                else
                {
                    ShowTGXImgToWindow();
                }
                

            return true;


          
        }

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
            ActualPalette = "Palette " + (File.Palette.ActualPalette+1);
            File.DecodeGm1File(File.FileArray, File.FileHeader.Name);
            ShowTGXImgToWindow();
        }

        internal void GeneratePaletteAndImgNew()
        {
            File.DecodeGm1File(File.FileArray, File.FileHeader.Name);
            ShowTGXImgToWindow();
        }

        #endregion

    }
}

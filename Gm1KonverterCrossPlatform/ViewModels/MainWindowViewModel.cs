
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

namespace Gm1KonverterCrossPlatform.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        

        private String colorAsText = "";
        public String ColorAsText
        {

            get => colorAsText;
            set {
                this.RaiseAndSetIfChanged(ref colorAsText, value);


            }
        }

        private String actualPalette = "Actual Palette 1";
        public String ActualPalette
        {

            get => actualPalette;
            set => this.RaiseAndSetIfChanged(ref actualPalette, value);
        }

        private bool convertButtonEnabled = false;
        public bool ConvertButtonEnabled {

            get => convertButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref convertButtonEnabled, value);
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
        internal List<DecodedFile> Files { get; set; } = new List<DecodedFile>();

        #region Methods

        internal void DecodeData(string[] pathToFiles, Window window)
        {
            Files = new List<DecodedFile>();
            //Convert Selected file
            foreach (var file in pathToFiles)
            {
                var decodedFile = new DecodedFile();
                if (!decodedFile.DecodeGm1File(Utility.FileToByteArray(file), System.IO.Path.GetFileName(file)))
                {
                    MessageBox messageBox = new MessageBox(MessageBox.MessageTyp.Info, "Only Animation Tiles are Supported yet. \nError from " + System.IO.Path.GetFileName(file));
                    messageBox.ShowDialog(window);
                    return;
                }

                Files.Add(decodedFile);
            }

            for (int i = 0; i < Files.Count; i++)
            {
                if (!Directory.Exists(Files[i].FileHeader.Name))
                {
                    Directory.CreateDirectory(Files[i].FileHeader.Name);
                }

                //Palette
                //for (int j = 0; j < Files[i].Palette.Bitmaps.Length; j++)
                //{
                //    Files[i].Palette.Bitmaps[j].Save(Files[i].FileHeader.Name + "/Palette" + j + ".png"); ;
                //}

                ShowImgToWindow();
              
            }


          
        }

        private void ShowImgToWindow()
        {
            TGXImages = new ObservableCollection<Image>();
            for (int j = 0; j < Files[0].FileHeader.INumberOfPictureinFile; j++)
            {
                var bitmap = Files[0].Images[j].bmp;

                Image image = new Image();
                image.MaxHeight = Files[0].Images[j].Height;
                image.MaxWidth = Files[0].Images[j].Width;
                image.Source = bitmap;
                TGXImages.Add(image);
                //safe later is in progress
                //bitmap.Save(Files[i].FileHeader.Name + "/"+ Files[i].Palette.ActualPalette+ "Bild" + j + ".png");

            }
            ActuellColorTable = Files[0].Palette.Bitmaps[Files[0].Palette.ActualPalette];
        }

        internal void ChangePalette(int number)
        {
            if (number>0)
            {
                    if (Files[0].Palette.ActualPalette+ number>9)
                    {
                    Files[0].Palette.ActualPalette = 0;
                    }
                    else
                    {
                    Files[0].Palette.ActualPalette += number;
                    }
            }
            else
            {
                    if (Files[0].Palette.ActualPalette + number < 0)
                    {
                    Files[0].Palette.ActualPalette = 9;
                    }
                    else
                    {
                    Files[0].Palette.ActualPalette += number;
                    }
            }
            ActualPalette = "Actual Palette " + (Files[0].Palette.ActualPalette+1);
            Files[0].DecodeGm1File(Files[0].FileArray, Files[0].FileHeader.Name);
            ShowImgToWindow();
        }

        internal void GeneratePaletteAndImgNew()
        {
            Files[0].DecodeGm1File(Files[0].FileArray, Files[0].FileHeader.Name);
            ShowImgToWindow();
        }

        #endregion

    }
}

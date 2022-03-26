using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ReactiveUI;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.Files;
using Gm1KonverterCrossPlatform.Files.Converters;
using Gm1KonverterCrossPlatform.Views;
using Gm1KonverterCrossPlatform.HelperClasses;
using Newtonsoft.Json;

namespace Gm1KonverterCrossPlatform.ViewModels
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private UserConfig userConfig;
        public UserConfig UserConfig { get => userConfig; set => userConfig = value; }

        private Languages.Language actualLanguage;
        public Languages.Language ActualLanguage
        {
            get => actualLanguage;
            set {
                this.RaiseAndSetIfChanged(ref actualLanguage, value);
                HelperClasses.Languages.SelectLanguage(value);
                userConfig.Language = value;
            }
        }

        private Languages.Language[] languages = new Languages.Language[] { HelperClasses.Languages.Language.Deutsch, HelperClasses.Languages.Language.English, HelperClasses.Languages.Language.Русский };
        public Languages.Language[] Languages
        {
            get => languages;
            set => this.RaiseAndSetIfChanged(ref languages, value);
        }

        private ColorThemes.ColorTheme actualColorTheme;

        private ColorThemes.ColorTheme[] colorThemes = new ColorThemes.ColorTheme[] { HelperClasses.ColorThemes.ColorTheme.Light, HelperClasses.ColorThemes.ColorTheme.Dark };
        public ColorThemes.ColorTheme[] ColorThemes
        {
            get => colorThemes;
            set => this.RaiseAndSetIfChanged(ref colorThemes, value);
        }
        public ColorThemes.ColorTheme ActualColorTheme
        {
            get => actualColorTheme;
            set
            {
                this.RaiseAndSetIfChanged(ref actualColorTheme, value);
                HelperClasses.ColorThemes.SelectColorTheme(value);
                userConfig.ColorTheme = value;
            }
        }

        private GM1FileHeader.DataType filetype;
        public GM1FileHeader.DataType Filetype
        {
            get => filetype;
            set => this.RaiseAndSetIfChanged(ref filetype, value);
        }

        private Image gifImage;
        public Image GIFImage
        {
            get => gifImage;
            set => this.RaiseAndSetIfChanged(ref gifImage, value);
        }

        private int delay = 100;
        public int Delay
        {
            get => delay;
            set => this.RaiseAndSetIfChanged(ref delay, value);
        }
        
        private int actualPalette = 1;
        public int ActualPalette
        {
            get => actualPalette;
            set => this.RaiseAndSetIfChanged(ref actualPalette, value);
        }

        internal string[] workfolderFiles;
        internal string[] WorkfolderFiles
        {
            get => workfolderFiles;
            set => this.RaiseAndSetIfChanged(ref workfolderFiles, value);
        }

        internal string[] strongholdFiles;
        internal string[] StrongholdFiles
        {
            get => strongholdFiles;
            set => this.RaiseAndSetIfChanged(ref strongholdFiles, value);
        }

        internal string[] gfxFiles;
        internal string[] GfxFiles
        {
            get => gfxFiles;
            set => this.RaiseAndSetIfChanged(ref gfxFiles, value);
        }

        private bool buttonsEnabled = false;
        public bool ButtonsEnabled
        {
            get => buttonsEnabled;
            set => this.RaiseAndSetIfChanged(ref buttonsEnabled, value);
        }

        private bool offsetExpanderVisible = false;
        public bool OffsetExpanderVisible
        {
            get => offsetExpanderVisible;
            set => this.RaiseAndSetIfChanged(ref offsetExpanderVisible, value);
        }

        private sbyte xOffset;
        public sbyte XOffset
        {
            get => xOffset;
            set {
                if (value > sbyte.MaxValue)
                {
                    value = sbyte.MaxValue;
                }
                else if (value < sbyte.MinValue)
                {
                    value = sbyte.MinValue;
                }
                this.RaiseAndSetIfChanged(ref xOffset, value);
            }
        }

        private int yOffset;
        public int YOffset
        {
            get => yOffset;
            set
            {
                this.RaiseAndSetIfChanged(ref yOffset, value);
            }
        }

        private int _bigImageWidth = 900;
        public int BigImageWidth
        {
            get => _bigImageWidth;
            set
            {
                this.RaiseAndSetIfChanged(ref _bigImageWidth, value);
            }
        }

        private bool gm1PreviewTrue = true;
        public bool Gm1PreviewTrue
        {
            get => gm1PreviewTrue;
            set {
                this.RaiseAndSetIfChanged(ref gm1PreviewTrue, value);
                GfxPreviewTrue = !gm1PreviewTrue;
                if (value)
                {
                    ToggleButtonName = "GM1";
                }
                else
                {
                    ToggleButtonName = "GFX";
                }
            }
        }
        
        private bool gfxPreviewTrue = false;
        public bool GfxPreviewTrue
        {
            get => gfxPreviewTrue;
            set => this.RaiseAndSetIfChanged(ref gfxPreviewTrue, value);
        }

        private string toggleButtonName = "GM1";
        public string ToggleButtonName
        {
            get => toggleButtonName;
            set => this.RaiseAndSetIfChanged(ref toggleButtonName, value);
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

        private bool replaceWithSaveFileTgx = false;
        public bool ReplaceWithSaveFileTgx
        {
            get => replaceWithSaveFileTgx;
            set => this.RaiseAndSetIfChanged(ref replaceWithSaveFileTgx, value);
        }

        private bool colorButtonsEnabled = false;
        public bool ColorButtonsEnabled
        {
            get => colorButtonsEnabled;
            set => this.RaiseAndSetIfChanged(ref colorButtonsEnabled, value);
        }
        
        private bool tgxButtonExportEnabled = false;
        public bool TgxButtonExportEnabled
        {
            get => tgxButtonExportEnabled;
            set => this.RaiseAndSetIfChanged(ref tgxButtonExportEnabled, value);
        }

        private bool tgxButtonImportEnabled = false;
        public bool TgxButtonImportEnabled
        {
            get => tgxButtonImportEnabled;
            set => this.RaiseAndSetIfChanged(ref tgxButtonImportEnabled, value);
        }

        private bool orginalStrongholdAnimationButtonEnabled = false;
        public bool OrginalStrongholdAnimationButtonEnabled
        {
            get => orginalStrongholdAnimationButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref orginalStrongholdAnimationButtonEnabled, value);
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

        internal bool fileSelected = false;
        internal bool FileSelected {
            get => fileSelected;
            set => this.RaiseAndSetIfChanged(ref fileSelected, value);
        }

        internal DecodedFile file;
        internal DecodedFile File {
            get => file;
            set {
                file = value;
                FileSelected = (file != null);
            }
        }

        internal TGXImage _actualTGXImageSelection;
        internal TGXImage ActualTGXImageSelection
        {
            get => _actualTGXImageSelection;
            set => this.RaiseAndSetIfChanged(ref _actualTGXImageSelection, value);
        }

        public Dictionary<int, Point> OffsetsBuildings { get => offsetsBuildings; set => offsetsBuildings = value; }
        public byte[] StrongholdasBytes { get => _strongholdasBytes; set => _strongholdasBytes = value; }
        public byte[] StrongholdExtremeasBytes { get => _strongholdExtremeasBytes; set => _strongholdExtremeasBytes = value; }
        public Point Strongholdadress { get => _strongholdadress; set => _strongholdadress = value; }

        private byte[] _strongholdasBytes;
        private byte[] _strongholdExtremeasBytes;
        private Point _strongholdadress;

        ~MainWindowViewModel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (File != null)
            {
                File.Dispose();
            }
        }

        /// <summary>
        /// Load the GM1 Files from the CrusaderPath
        /// </summary>
        internal void LoadStrongholdFiles()
        {
            Logger.Log("LoadStrongholdFiles start");
            if (!String.IsNullOrEmpty(userConfig.CrusaderPath))
            {
                StrongholdFiles = Utility.GetFileNames(userConfig.CrusaderPath, "*.gm1");
                try
                {
                    GfxFiles = Utility.GetFileNames(userConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\gfx", "*.tgx");
                }
                catch (Exception e)
                {
                    Logger.Log(e.Message.ToString());
                }
            }
            Logger.Log("LoadStrongholdFiles end");
        }

        internal void LoadWorkfolderFiles()
        {
            if (!String.IsNullOrEmpty(userConfig.WorkFolderPath))
            {
                if (!Directory.Exists(userConfig.WorkFolderPath))
                {
                    Directory.CreateDirectory(userConfig.WorkFolderPath);
                }

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
            this.RaisePropertyChanging("File");

            if (Logger.Loggeractiv) Logger.Log("DecodeData:\nFile: "+ fileName);
            //Convert Selected file
            try
            {
                Dispose();
                File = new DecodedFile();
                if (!File.DecodeGm1File(Utility.FileToByteArray(userConfig.CrusaderPath + "\\" + fileName), fileName))
                {
                    MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, (GM1FileHeader.DataType)File.FileHeader.IDataType + Utility.GetText("TilesarenotSupportedyet"));
                    messageBox.ShowDialog(window);
                    return false;
                }

                if ((GM1FileHeader.DataType)File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject)
                {
                    ShowTileImgToWindow();
                }
                else
                {
                    ShowTGXImgToWindow();
                }
            }
            catch (Exception e)
            {
                if (Logger.Loggeractiv) Logger.Log("Exception:\n" + e.Message);
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + e.Message);
                messageBox.Show();
                return false;
            }

            this.RaisePropertyChanged("File");

            return true;
        }

        public TGXImage TgxImage;
        internal void DecodeTgxData(string fileName, MainWindow mainWindow)
        {
            if (Logger.Loggeractiv) Logger.Log("DecodeTgxData:\nFile: " + fileName);

            var array = Utility.FileToByteArray(userConfig.CrusaderPath.Replace("\\gm",String.Empty)+"\\gfx" + "\\" + fileName);
            TgxImage = new TGXImage();

            TgxImage.TgxWidth = BitConverter.ToUInt32(array, 0);
            TgxImage.TgxHeight = BitConverter.ToUInt32(array, 4);
            TgxImage.ImgFileAsBytearray = new byte[array.Length - 8];
            Array.Copy(array, 8, TgxImage.ImgFileAsBytearray, 0, TgxImage.ImgFileAsBytearray.Length);
            TgxImage.Bitmap = ImageConverter.GM1ByteArrayToImg(
                TgxImage.ImgFileAsBytearray,
                (int)TgxImage.TgxWidth,
                (int)TgxImage.TgxHeight,
                null);
            TGXImages = new ObservableCollection<Image>();
            var bitmap = TgxImage.Bitmap;
            Image image = new Image();
            image.MaxWidth = TgxImage.TgxWidth;
            image.MaxHeight = TgxImage.TgxHeight;
            image.Tag = TgxImage;
            image.Source = bitmap;
            TGXImages.Add(image);
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
                image.MaxHeight = File.ImagesTGX[j].Header.Height;
                image.MaxWidth = File.ImagesTGX[j].Header.Width;
                image.Source = bitmap;
                image.Tag = File.ImagesTGX[j];
                TGXImages.Add(image);
            }

            if (File.Palette != null)
            {
                ActuellColorTable = ColorTableConverter.GetBitmap(File.Palette.ColorTables[File.Palette.ActualPalette], Palette.width, Palette.height, Palette.pixelSize);
            }
        }

        /// <summary>
        /// Changes the actual Paletteimg
        /// </summary>
        /// <param name="number"></param>
        internal void ChangePalette(int number)
        {
            if (number > 0)
            {
                if (File.Palette.ActualPalette + number > 9)
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
            ActualPalette = File.Palette.ActualPalette + 1;
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

        private Dictionary<int, Point> offsetsBuildings = new Dictionary<int, Point>() {
            { 0, new Point(939615, 939608) },
            { 1, new Point(939841, 939834) },
            { 2, new Point(940022, 940015) },
            { 3, new Point(939728, 939721) },
            { 12, new Point(939000, 938996) },
            { 13, new Point(939031, 939027) },
            { 43, new Point(938935, 938928) },
            { 44, new Point(938969, 938962) },
            { 121, new Point(939943, 939936) },
            { 122, new Point(939943, 939936) },
            { 123, new Point(939574, 939567) },
            { 124, new Point(939536, 939529) },
            { 125, new Point(939574, 939567) },
            { 23, new Point(938858, 938851) }
        };
        
        public Dictionary<int, Point> NewOffsetsInExe { get; set; } = new Dictionary<int, Point>();
        internal void ChangeExeOffset(int index, Point strongholdadress, int xOffset, int yOffset)
        {
            if (NewOffsetsInExe.ContainsKey(index))
            {
                NewOffsetsInExe[index] = new Point(xOffset, yOffset);
            }
            else
            {
                NewOffsetsInExe.Add(index, new Point(xOffset, yOffset));
            }

            var offsetData = JsonConvert.SerializeObject(NewOffsetsInExe);
            System.IO.File.WriteAllText(UserConfig.WorkFolderPath + Path.DirectorySeparatorChar + "Offsets.json", offsetData);

            int strongholdValue = 912;
            var bytesArray = BitConverter.GetBytes(yOffset);
            if (_strongholdExtremeasBytes != null)
            {
                _strongholdExtremeasBytes[(int)strongholdadress.X] = (byte)xOffset;
            }

            _strongholdasBytes[(int)strongholdadress.X - strongholdValue] = (byte)xOffset;
     
            if (index == 12 || index == 13)
            {
                if (_strongholdExtremeasBytes != null)
                {
                    _strongholdExtremeasBytes[(int)strongholdadress.Y] = (byte)yOffset;
                }

                _strongholdasBytes[(int)strongholdadress.Y - strongholdValue] = (byte)yOffset;
            }
            else
            {
                if (_strongholdExtremeasBytes != null)
                {
                    for (int i = 0; i < bytesArray.Length; i++)
                    {
                        _strongholdExtremeasBytes[(int)strongholdadress.Y + i] = bytesArray[i];
                    }
                }
                for (int i = 0; i < bytesArray.Length; i++)
                {
                    _strongholdasBytes[(int)strongholdadress.Y - strongholdValue + i] = bytesArray[i];
                }
            }

            System.IO.File.WriteAllBytes(UserConfig.CrusaderPath.Replace("\\gm", string.Empty) + "\\Stronghold_Crusader_Extreme.exe", _strongholdExtremeasBytes);
            System.IO.File.WriteAllBytes(UserConfig.CrusaderPath.Replace("\\gm", string.Empty) + "\\Stronghold Crusader.exe", _strongholdasBytes);
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Win32;
using Files.Gm1Converter;
using Gm1KonverterCrossPlatform.HelperClasses;
using Gm1KonverterCrossPlatform.HelperClasses.Views;
using Gm1KonverterCrossPlatform.ViewModels;
using HelperClasses.Gm1Converter;
using SharpDX.WIC;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Palette = Files.Gm1Converter.Palette;

namespace Gm1KonverterCrossPlatform.Views
{
    public class MainWindow : Window
    {

        private MainWindowViewModel vm;
        public MainWindow()
        {


            InitializeComponent();
        #if DEBUG
            this.AttachDevTools();
#endif
            this.DataContextChanged += ViewModelSet;

            ListBox listbox = this.Get<ListBox>("Gm1FilesSelector");
            listbox.SelectionChanged += SelectedGm1File;
            ListBox TGXImageListBox = this.Get<ListBox>("TGXImageListBox");
            TGXImageListBox.SelectionChanged += TGXImageSelectionChanged;

            ListBox workfolderSelector = this.Get<ListBox>("WorkfolderSelector");
            workfolderSelector.DoubleTapped += OpenWorkfolderDirectory;

            MenuItem workfolderMenueItem = this.Get<MenuItem>("WorkfolderMenueItem");
            workfolderMenueItem.Click += ChangeWorkfolder;
            MenuItem crusaderfolderMenueItem = this.Get<MenuItem>("CrusaderfolderMenueItem");
            crusaderfolderMenueItem.Click += ChangeCrusaderfolder;
    
            MenuItem createnewGM1MenueItem = this.Get<MenuItem>("CreatenewGM1MenueItem");
            createnewGM1MenueItem.Click += CreatenewGM1;
            MenuItem replacewithSavedGM1FileMenueItem = this.Get<MenuItem>("ReplacewithSavedGM1FileMenueItem");
            replacewithSavedGM1FileMenueItem.Click += ReplacewithSavedGM1FileM;

            MenuItem exportColortableMenueItem = this.Get<MenuItem>("ExportColortableMenueItem");
            exportColortableMenueItem.Click += ExportColortable;

            MenuItem importColortableMenueItem = this.Get<MenuItem>("ImportColortableMenueItem");
            importColortableMenueItem.Click += ImportColortable;

            MenuItem exportImagesMenueItem = this.Get<MenuItem>("ExportImagesMenueItem");
            exportImagesMenueItem.Click += ExportImages;

            MenuItem importImagesMenueItem = this.Get<MenuItem>("ImportImagesMenueItem");
            importImagesMenueItem.Click += ImportImages;

            Image image = this.Get<Image>("HelpIcon");

            Avalonia.Media.Imaging.Bitmap bitmap = new Avalonia.Media.Imaging.Bitmap("Images/info.png");
            image.Source = bitmap;
            image.Tapped += OpenInfoWindow;
        }

        private void OpenInfoWindow(object sender, RoutedEventArgs e)
        {
            if (vm.File == null) return;

            InfoWindow infoWindow = new InfoWindow((GM1FileHeader.DataType)vm.File.FileHeader.IDataType);
            infoWindow.Show();
        }

        private void OpenWorkfolderDirectory(object sender, RoutedEventArgs e)
        {
            ListBox listbox = sender as ListBox;
            if (listbox.SelectedIndex == -1) return;
            Process.Start(vm.UserConfig.WorkFolderPath + "\\" + listbox.SelectedItem);
        }

        private void TGXImageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            if (listbox.SelectedIndex == -1) return;
            if (vm.File==null) return;
            if (listbox.SelectedIndex > vm.File.ImagesTGX.Count) return;
        }

        private void ImportImages(object sender, RoutedEventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            var files = Directory.GetFiles(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images", "*.png",SearchOption.TopDirectoryOnly);
            //sort because 11 is before 2
            files = files.OrderBy(x => x.Length).ThenBy(x => x).ToArray<String>();


            Utility.XOffsetBefore = 0;
            Utility.YOffsetBefore = 0;
            int counter = 1;
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename.Equals("Image"+ counter+".png"))
                {
                    counter++;
                    var fileindex = int.Parse(filename.Replace("Image", "").Replace(".png", "")) - 1;
                    int width, height;
                    var list = Utility.LoadImage(file,out width,out height, vm.File.ImagesTGX[fileindex].AnimatedColor,1,vm.File.FileHeader.IDataType);
                    if (list.Count == 0) return;
                    if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType != GM1FileHeader.DataType.TilesObject)
                    {
                        vm.File.ImagesTGX[fileindex].ConvertImageWithoutPaletteToByteArray(list, width, height);
                        vm.File.ImagesTGX[fileindex].Width = (ushort)width;
                        vm.File.ImagesTGX[fileindex].Height = (ushort)height;
                    }
                    else
                    {
                        vm.File.ConvertImgToTiles(list, (ushort)width, (ushort)height);
                    }

                }

            }
            if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject) vm.File.SetNewTileList();
            
            vm.File.ImagesTGX[0].SizeinByteArray = (uint)vm.File.ImagesTGX[0].ImgFileAsBytearray.Length;
            uint zaehler = 0;
            for (int i = 1; i < vm.File.ImagesTGX.Count; i++)
            {
                zaehler += vm.File.ImagesTGX[i-1].SizeinByteArray;
                vm.File.ImagesTGX[i].OffsetinByteArray = zaehler;
                vm.File.ImagesTGX[i].SizeinByteArray = (uint)vm.File.ImagesTGX[i].ImgFileAsBytearray.Length;
            }

            //datasize neu setzten
            uint newDataSize = vm.File.ImagesTGX[vm.File.ImagesTGX.Count - 1].OffsetinByteArray + vm.File.ImagesTGX[vm.File.ImagesTGX.Count - 1].SizeinByteArray; ;
          
            vm.File.FileHeader.IDataSize = newDataSize;
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void ExportImages(object sender, RoutedEventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            int img = 1;
          
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
           
            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images"))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images");
            }
            //Stream stream = new FileStream(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\ImageAsGif.gif", FileMode.Create);
            if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType==GM1FileHeader.DataType.TilesObject)
            {
                foreach (var image in vm.File.TilesImages)
                {
                    image.TileImage.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\Image" + img + ".png");
                    img++;
                }
            }
            else
            {
                
                //GifWriter gif = new GifWriter(stream,100,0);

                foreach (var image in vm.File.ImagesTGX)
                {
                    image.Bitmap.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\Image" + img + ".png");

                    //System.Drawing.Image imageGif = System.Drawing.Image.FromFile(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\Image" + img + ".png");
                    //gif.WriteFrame(imageGif);
                    img++;
                }
            }
            //stream.Flush();
            //stream.Dispose();



            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images");


            vm.LoadWorkfolderFiles();

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void ImportColortable(object sender, RoutedEventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            var files=Directory.GetFiles(vm.UserConfig.WorkFolderPath+"\\"+ filewithoutgm1ending+ "\\Colortables", "*.png");
  
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename.StartsWith("ColorTable"))
                {
                    int width, height;
                    var fileindex = int.Parse(filename.Replace("ColorTable", "").Replace(".png","")) - 1;
                    var list = Utility.LoadImage(file, out width, out height,1, Palette.pixelSize, vm.File.FileHeader.IDataType);
                    if (list.Count == 0) return;
                    vm.File.Palette.SetPaleteUInt(fileindex, list.ToArray());
                    var bitmap = vm.File.Palette.GetBitmap(fileindex, Palette.pixelSize);
                    vm.File.Palette.Bitmaps[fileindex] = bitmap;
                    vm.GeneratePaletteAndImgNew();
                    vm.File.Palette.Bitmaps[fileindex] = bitmap;
                }
               
            }
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
            vm.ActuellColorTable = vm.File.Palette.Bitmaps[vm.File.Palette.ActualPalette];
        }

        private void ExportColortable(object sender, RoutedEventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            int colorTable = 1;
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending+ "\\Colortables"))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Colortables");
            }
            foreach (var bitmap in vm.File.Palette.Bitmaps)
            {
                bitmap.Save(vm.UserConfig.WorkFolderPath+"\\"+ filewithoutgm1ending + "\\Colortables\\ColorTable" + colorTable + ".png");
                colorTable++;
            }
            if(vm.UserConfig.OpenFolderAfterExport)
            Process.Start(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Colortables");

            vm.LoadWorkfolderFiles();
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void ReplacewithSavedGM1FileM(object sender, RoutedEventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            File.Copy(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1",   vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, true);
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        String listboxItemBefore = null;
        private void SelectedGm1File(object sender, SelectionChangedEventArgs e)
        {
            
         
            var listbox = sender as ListBox;
            if (listboxItemBefore == listbox.SelectedItem.ToString())return;
            listboxItemBefore = listbox.SelectedItem.ToString();
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            if (vm.DecodeData(listboxItemBefore, this)){
             
                
                vm.Filetype = Utility.GetText("Datatype") + ((GM1FileHeader.DataType) vm.File.FileHeader.IDataType);
                if (vm.File.Palette == null)
                {
                    vm.ImportButtonEnabled = true;
                    vm.ColorButtonsEnabled = false;
                    vm.ActuellColorTable = null;
                }
                else
                {
                    vm.ColorButtonsEnabled = true;
                    vm.ImportButtonEnabled = false;
                }

                vm.ButtonsEnabled = true;
                var filewithoutgm1ending = listboxItemBefore.Replace(".gm1", "");
                if (!File.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1"))
                {
                    vm.ReplaceWithSaveFile = false;
                }
                else
                {
                    vm.ReplaceWithSaveFile = true;
                }
            }
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);

        }

       
        private void ViewModelSet(object sender, EventArgs e)
        {
            vm = DataContext as MainWindowViewModel;
            
            vm.UserConfig = new UserConfig();
            vm.UserConfig.LoadData();
            vm.ActualLanguage = vm.UserConfig.Language;
            vm.OpenFolderAfterExport = vm.UserConfig.OpenFolderAfterExport;
            vm.LoggerActiv = vm.UserConfig.ActivateLogger;
            vm.LoadStrongholdFiles();
            vm.LoadWorkfolderFiles();
            Logger.Path = vm.UserConfig.WorkFolderPath;
           if(File.Exists(vm.UserConfig.WorkFolderPath + "\\Logger\\Log.txt")) File.Delete(vm.UserConfig.WorkFolderPath+"\\Logger\\Log.txt");
        }

        private void CreatenewGM1(object sender, EventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            if (!Directory.Exists(vm.UserConfig.WorkFolderPath+"\\"+ filewithoutgm1ending))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending);
            }
            
            if (!File.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\"+ filewithoutgm1ending+"Save.gm1"))
            {
                File.Copy(vm.UserConfig.CrusaderPath+"\\"+ vm.File.FileHeader.Name, vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1");
             
            }
            var array = vm.File.GetNewGM1Bytes();
            Utility.ByteArraytoFile(vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, array);
            File.Copy(vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Modded.gm1", true);
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private async void ChangeCrusaderfolder(object sender, RoutedEventArgs e)
        {
            var folderFromTask = await GetFolderAsync(Utility.GetText("StrongholdFolder"), vm.UserConfig.CrusaderPath);
            vm.UserConfig.CrusaderPath = folderFromTask;
            vm.LoadStrongholdFiles();
        }

        private async void ChangeWorkfolder(object sender, RoutedEventArgs e)
        {
        
            var folderFromTask = await GetFolderAsync(Utility.GetText("Workfolder"), vm.UserConfig.WorkFolderPath);
            vm.UserConfig.WorkFolderPath = folderFromTask;
            vm.LoadWorkfolderFiles();
        }

        private async Task<string> GetFolderAsync(String name,String initialDirectory)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();

            openFolderDialog.Title = name;



            if (!String.IsNullOrEmpty(initialDirectory))
            {
                openFolderDialog.InitialDirectory = initialDirectory;
            }
            var file = await openFolderDialog.ShowAsync(this);
            if (String.IsNullOrEmpty(file))
            {
                file = initialDirectory;
            }
            return file;
        }
     
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        

        private void Button_ClickPalleteminus(object sender, RoutedEventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            vm.ChangePalette(-1);
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void Button_ClickPalleteplus(object sender, RoutedEventArgs e)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            vm.ChangePalette(1);
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }
        private void Button_ClickChangeColorTable(object sender, RoutedEventArgs e)
        {
            ChangeColorPalette changeColorPalette = new ChangeColorPalette();
            changeColorPalette.Closed += OnWindowClosed;
            changeColorPalette.DataContext = this.DataContext;
            changeColorPalette.LoadPalette();
            changeColorPalette.ShowDialog(this);
      

        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
          
            if (vm.File.Palette.PaletteChanged)
            {
                var bitmap = vm.File.Palette.GetBitmap(vm.File.Palette.ActualPalette, Palette.pixelSize);
                vm.ActuellColorTable = bitmap;
                vm.File.Palette.Bitmaps[vm.File.Palette.ActualPalette] = bitmap;
                vm.GeneratePaletteAndImgNew();
                vm.File.Palette.PaletteChanged = false;
                vm.DecodeButtonEnabled = true;
            }
            
        }


        
    }
}

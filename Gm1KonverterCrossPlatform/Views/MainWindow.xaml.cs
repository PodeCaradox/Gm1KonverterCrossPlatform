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
using System.Threading.Tasks;
using Palette = Files.Gm1Converter.Palette;

namespace Gm1KonverterCrossPlatform.Views
{
    public class MainWindow : Window
    {

        
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

        }

        private void TGXImageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            if (listbox.SelectedIndex == -1) return;
            if (vm.File==null) return;
            if (listbox.SelectedIndex > vm.File.ImagesTGX.Count) return;
            vm.XOffset = vm.File.ImagesTGX[listbox.SelectedIndex].OffsetX.ToString();
        }

        private void ImportImages(object sender, RoutedEventArgs e)
        {
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            var files = Directory.GetFiles(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images", "*.png");
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename.StartsWith("Image"))
                {
                    var fileindex = int.Parse(filename.Replace("Image", "").Replace(".png", "")) - 1;
                    int width, height;
                    var list = Utility.LoadImage(file,out width,out height, vm.File.ImagesTGX[fileindex].AnimatedColor);
                    if (list.Count == 0) return;
             
                    vm.File.ImagesTGX[fileindex].ConvertImageWithoutPaletteToByteArray(list,width,height, fileindex);
                    vm.File.ImagesTGX[fileindex].Width = (ushort)width;
                    vm.File.ImagesTGX[fileindex].Height = (ushort)height;
                }

            }
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
        }

        private void ExportImages(object sender, RoutedEventArgs e)
        {
            int img = 1;
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images"))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images");
            }
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
                foreach (var image in vm.File.ImagesTGX)
                {
                    image.Bitmap.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\Image" + img + ".png");
                    img++;
                }
            }
            
            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images");
        }

        private void ImportColortable(object sender, RoutedEventArgs e)
        {
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            var files=Directory.GetFiles(vm.UserConfig.WorkFolderPath+"\\"+ filewithoutgm1ending+ "\\Colortables", "*.png");
  
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename.StartsWith("ColorTable"))
                {
                    int width, height;
                    var fileindex = int.Parse(filename.Replace("ColorTable", "").Replace(".png","")) - 1;
                    var list = Utility.LoadImage(file, out width, out height,1, Palette.pixelSize);
                    if (list.Count == 0) return;
                    vm.File.Palette.SetPaleteUInt(fileindex, list.ToArray());
                    var bitmap = vm.File.Palette.GetBitmap(fileindex, Palette.pixelSize);
                    vm.File.Palette.Bitmaps[fileindex] = bitmap;
                    vm.GeneratePaletteAndImgNew();
                    vm.File.Palette.Bitmaps[fileindex] = bitmap;
                }
               
            }

            vm.ActuellColorTable = vm.File.Palette.Bitmaps[vm.File.Palette.ActualPalette];
        }

        private void ExportColortable(object sender, RoutedEventArgs e)
        {
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
        }

        private void ReplacewithSavedGM1FileM(object sender, RoutedEventArgs e)
        {
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            File.Copy(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1",   vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, true);
        }

        String listboxItemBefore = null;
        private void SelectedGm1File(object sender, SelectionChangedEventArgs e)
        {
            
         
            var listbox = sender as ListBox;
            if (listboxItemBefore == listbox.SelectedItem.ToString())return;
            listboxItemBefore = listbox.SelectedItem.ToString();
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            if (vm.DecodeData(listboxItemBefore, this)){

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

        private MainWindowViewModel vm;
        private void ViewModelSet(object sender, EventArgs e)
        {
            vm = DataContext as MainWindowViewModel;
            vm.UserConfig = new UserConfig();
            vm.UserConfig.LoadData();
            vm.OpenFolderAfterExport = vm.UserConfig.OpenFolderAfterExport;
            vm.LoadStrongholdFiles();
        }

        private void CreatenewGM1(object sender, EventArgs e)
        {
       
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
        }

        private async void ChangeCrusaderfolder(object sender, RoutedEventArgs e)
        {
            var folderFromTask = await GetFolderAsync("Crusaderfolder", vm.UserConfig.CrusaderPath);
            vm.UserConfig.CrusaderPath = folderFromTask;
            vm.LoadStrongholdFiles();
        }

        private async void ChangeWorkfolder(object sender, RoutedEventArgs e)
        {
        
            var folderFromTask = await GetFolderAsync("Workfolder", vm.UserConfig.WorkFolderPath);
            vm.UserConfig.WorkFolderPath = folderFromTask;
        }

        private async Task<string> GetFolderAsync(String name,String initialDirectory)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog
            {
                Title = name,
                

            };
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

        public async Task<string[]> GetFilesAsync()
        {
          
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Choose GM1 Files",
                //AllowMultiple = true, //will be added later
                InitialDirectory = vm.UserConfig.CrusaderPath,
            };

            openFileDialog.Filters = new System.Collections.Generic.List<FileDialogFilter>();
            openFileDialog.Filters.Add(
                    new FileDialogFilter
                    {
                        Name = "GM1 files (*.gm1)",
                        Extensions = { "gm1" },
                    }
                );


            var files = await openFileDialog.ShowAsync(this);

            return files;
        }


        

        private void Button_ClickSaveOffset(object sender, RoutedEventArgs e)
        {
            var listbox = this.Get<ListBox>("TGXImageListBox");
            vm.File.ImagesTGX[listbox.SelectedIndex].OffsetX = ushort.Parse(vm.XOffset);

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

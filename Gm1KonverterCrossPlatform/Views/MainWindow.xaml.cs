using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.Win32;
using Files.Gm1Converter;
using Gm1KonverterCrossPlatform.HelperClasses.Views;
using Gm1KonverterCrossPlatform.ViewModels;
using HelperClasses.Gm1Converter;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

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
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private string[] pathToFiles;
        
        private async void Button_ClickDirectory(object sender, RoutedEventArgs e)
        {
            var filesFromTask = await GetFilesAsync();

            if (filesFromTask == null || filesFromTask != null && filesFromTask.Length <= 0)
            {
                MessageBox messageBox = new MessageBox(MessageBox.MessageTyp.Info,"No Files Selected.");
                await messageBox.ShowDialog(this);
                return;
            }

            pathToFiles = filesFromTask;
            var vm = DataContext as MainWindowViewModel;
            vm.ConvertButtonEnabled = true;

            vm.DecodeData(pathToFiles,this);
        }

      

        public async Task<string[]> GetFilesAsync()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Choose GM1 Files",
                //AllowMultiple = true, //will be added later
                InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Stronghold Crusader Extreme",
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


        

        private void Button_ClickPalleteminus(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            vm.ChangePalette(-1);

        }

        private void Button_ClickPalleteplus(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            vm.ChangePalette(1);
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
            var vm = DataContext as MainWindowViewModel;
            if (vm.Files[0].Palette.PaletteChanged)
            {
                var bitmap = vm.Files[0].Palette.GetBitmap(vm.Files[0].Palette.ActualPalette, Files.Gm1Converter.Palette.pixelSize);
                vm.ActuellColorTable = bitmap;
                vm.Files[0].Palette.Bitmaps[vm.Files[0].Palette.ActualPalette] = bitmap;
                vm.GeneratePaletteAndImgNew();
                vm.Files[0].Palette.PaletteChanged = false;
                vm.DecodeButtonEnabled = true;
            }
            
        }

        private void Button_ClickConvertBack(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            foreach (var file in vm.Files)
            {
                var array=file.GetNewGM1Bytes();

                Utility.ByteArraytoFile(file.FileHeader.Name+"/"+ file.FileHeader.Name, array);
            }
        }
        
    }
}

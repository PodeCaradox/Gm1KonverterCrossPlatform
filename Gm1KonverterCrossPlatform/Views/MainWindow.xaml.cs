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
        }
        
        public async Task<string[]> GetFilesAsync()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Choose GM1 Files",
                AllowMultiple = true,
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

        private void Button_ClickConvert(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            vm.Files = new List<DecodedFile>();
            //Convert Selected files
            foreach (var file in pathToFiles)
            {
                var decodedFile = new DecodedFile();
                if(!decodedFile.DecodeGm1File(Utility.FileToByteArray(file), System.IO.Path.GetFileName(file)))
                {
                    MessageBox messageBox = new MessageBox(MessageBox.MessageTyp.Info, "Only Animation Tiles are Supported yet.");
                    messageBox.ShowDialog(this);
                    return;
                }
               
                vm.Files.Add(decodedFile);
            }

            for (int i = 0; i < vm.Files.Count; i++)
            {
                if (!Directory.Exists(vm.Files[i].FileHeader.Name))
                {
                    Directory.CreateDirectory(vm.Files[i].FileHeader.Name);
                }
                //Palette
                vm.Files[i].Palette.Bitmap.Save(vm.Files[i].FileHeader.Name +"/Palette.png"); ;


                for (int j = 0; j < vm.Files[i].Images.Count; j++)
                {
                    var bitmap = vm.Files[i].Images[j].bmp;

                    Image image = new Image();
                    image.MaxHeight = 100;
                    image.MaxWidth = 100;
                    image.Source = bitmap;
                    vm.TGXImages.Add(image);
                    bitmap.Save(vm.Files[i].FileHeader.Name+"/Bild"+j+ "Farbe" +j/ vm.Files[i].FileHeader.INumberOfPictureinFile+".png");

                }
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

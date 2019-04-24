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

        private string[] files;



        private async void Button_ClickDirectory(object sender, RoutedEventArgs e)
        {
            var filesFromTask = await GetFilesAsync();

            if (filesFromTask == null || filesFromTask != null && filesFromTask.Length <= 0)
            {
                MessageBox messageBox = new MessageBox(MessageBox.MessageTyp.Info,"No Files Selected.");
                await messageBox.ShowDialog(this);
                return;
            }

            files = filesFromTask;
            var vm = DataContext as MainWindowViewModel;
            vm.ConvertButtonEnabled = true;
        }

      

        public async Task<string[]> GetFilesAsync()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Choose GM1 Files",
                AllowMultiple = true,
                //InitialDirectory = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Stronghold Crusader Extreme",
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
            List<DecodedFile> filesConverted = new List<DecodedFile>();
            
            foreach (var file in files)
            {
                filesConverted.Add(new DecodedFile(Utility.FileToByteArray(file), System.IO.Path.GetFileName(file)));
            }

            for (int i = 0; i < filesConverted.Count; i++)
            {
                if (!Directory.Exists(filesConverted[i].FileHeader.Name))
                {
                    Directory.CreateDirectory(filesConverted[i].FileHeader.Name);
                }
                //Palette
                filesConverted[i].Palette.Bitmap.Save(filesConverted[i].FileHeader.Name +"/Palette.png"); ;


                for (int j = 0; j < filesConverted[i].Images.Count; j++)
                {
                    var bitmap = filesConverted[i].Images[j].bmp;

                    Image image = new Image();
                    image.MaxHeight = 100;
                    image.MaxWidth = 100;
                    image.Source = bitmap;
                    vm.TGXImages.Add(image);
                    bitmap.Save(filesConverted[i].FileHeader.Name+"/Bild"+j+ "Farbe" +j/ filesConverted[i].FileHeader.INumberOfPictureinFile+".png");

                }
            }

           
        }

        private void Button_ClickConvertBack(object sender, RoutedEventArgs e)
        {

        }
        
    }
}

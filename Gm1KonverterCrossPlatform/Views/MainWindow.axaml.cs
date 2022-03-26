using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.Files;
using Gm1KonverterCrossPlatform.Files.Converters;
using Gm1KonverterCrossPlatform.HelperClasses;
using Gm1KonverterCrossPlatform.ViewModels;
using Newtonsoft.Json;

namespace Gm1KonverterCrossPlatform.Views
{
    public class MainWindow : Window
    {
        private MainWindowViewModel vm;

        public MainWindow()
        {
            InitializeComponent();

            this.DataContextChanged += ViewModelSet;
        }

        private async void ImportOffsetsFromFile(object sender, RoutedEventArgs e)
        {
            String path = vm.UserConfig.WorkFolderPath + Path.DirectorySeparatorChar + "Offsets.json";
            if (!File.Exists(path)) {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Directory = vm.UserConfig.WorkFolderPath;
                dialog.Title = "Select Offset File";
                dialog.Filters.Add(new FileDialogFilter() { Name = "Text", Extensions = { "json" } });
                dialog.AllowMultiple = false;
                var paths = await dialog.ShowAsync(this);
                if (paths.Length == 0) return;
                path = paths[0];
            }
            var offsetData = File.ReadAllText(path);
            var dict = JsonConvert.DeserializeObject<Dictionary<int, Point>>(offsetData);

            if (vm.StrongholdasBytes == null && File.Exists(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold Crusader.exe"))
                vm.StrongholdasBytes = File.ReadAllBytes(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold Crusader.exe");

            if (vm.StrongholdExtremeasBytes == null && File.Exists(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold_Crusader_Extreme.exe"))
                vm.StrongholdExtremeasBytes = File.ReadAllBytes(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold_Crusader_Extreme.exe");

            foreach (var key in dict.Keys)
            {
                var dummy = dict[key];
                vm.ChangeExeOffset(key,vm.OffsetsBuildings[key], (int)dummy.X, (int)dummy.Y);
            }
        }

        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            var menueItem = sender as MenuItem;
            if (menueItem.Name == "OpenStrongholdFolderMenueItem")
            {
                Process.Start("explorer.exe", vm.UserConfig.CrusaderPath.Replace(Path.DirectorySeparatorChar + "gm",""));
            }
            else
            {
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath);
            }
        }

        private void ReplacewithSavedTgxFile(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedTgxFile start");
            var filewithoutgm1ending = actualName.Replace(".tgx", "");
            File.Copy(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.tgx", vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\" + actualName, true);
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedTgxFile end");
        }

        private void CreatenewTgx(object sender, RoutedEventArgs e)
        {
            var filewithoutgm1ending = actualName.Replace(".tgx", "");
            if (!File.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.tgx"))
                File.Copy(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\gfx\\" + actualName, vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.tgx", true);

            var array = new List<byte>();

            array.AddRange(BitConverter.GetBytes(vm.TgxImage.TgxWidth));
            array.AddRange(BitConverter.GetBytes(vm.TgxImage.TgxHeight));
            array.AddRange(vm.TgxImage.ImgFileAsBytearray);
            File.WriteAllBytes(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\gfx\\" + actualName, array.ToArray());
            vm.ReplaceWithSaveFileTgx = true;
        }

        String actualName = "";
        private void ExportTgxImage(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportTgxImage start");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(actualName);
            string directory = Path.Combine(vm.UserConfig.WorkFolderPath, fileNameWithoutExtension);
            string filePath = Path.Combine(directory, $"{fileNameWithoutExtension}.png");

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            vm.TgxImage.Bitmap.Save(filePath);

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", directory);

            vm.LoadWorkfolderFiles();
            vm.TgxButtonImportEnabled = true;

            if (Logger.Loggeractiv) Logger.Log("\n>>ExportTgxImage end");
        }

        private void ImportTgxImage(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportTgxImage start");

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(actualName);
            string directory = Path.Combine(vm.UserConfig.WorkFolderPath, fileNameWithoutExtension);
            string filePath = Path.Combine(directory, $"{fileNameWithoutExtension}.png");

            var imageData = Utility.LoadImageData(filePath);

            int width = imageData.Width;
            int height = imageData.Height;

            List<UInt16> colors = Utility.LoadImage(imageData, ref width, ref height);
            
            vm.TgxImage.ConvertImageWithoutPaletteToByteArray(colors, width, height);//todo animated color?
            vm.TgxImage.TgxWidth = (uint)width;
            vm.TgxImage.TgxHeight = (uint)height;
            vm.TgxImage.Bitmap = ImageConverter.GM1ByteArrayToImg(
                vm.TgxImage.ImgFileAsBytearray,
                (int)vm.TgxImage.TgxWidth,
                (int)vm.TgxImage.TgxHeight,
                null);
            vm.TGXImages.Clear();
            Image image = new Image()
            {
                MaxWidth = width,
                MaxHeight = height,
                Source = vm.TgxImage.Bitmap
            };
            vm.TGXImages.Add(image);

            if (Logger.Loggeractiv) Logger.Log("\n>>ImportTgxImage end");
        }

        private void OpenLogFile(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Logger.Path))
            {
                Directory.CreateDirectory(Logger.Path);
            }
            Process.Start("explorer.exe", Logger.Path);
        }

        private void ExportOrginalStrongholdAnimation(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportOrginalStrongholdAnimation start");
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            vm.File.Palette.ActualPalette = 0;
            vm.File.DecodeGm1File(vm.File.FileArray, vm.File.FileHeader.Name);
            for (int i = 0; i < 10; i++)
            {
                if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + (i + 1)))
                {
                    Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + (i + 1));
                }
                int img = 1;
                foreach (var image in vm.File.ImagesTGX)
                {
                    image.Bitmap.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + (i + 1) + "\\OrginalAnimationImg" + img + ".png");
                    img++;
                }
                vm.ChangePalette(1);
            }

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending);

            vm.LoadWorkfolderFiles();
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportOrginalStrongholdAnimation end");
        }

        private void ImportOrginalStrongholdAnimation(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportOrginalStrongholdAnimation start");
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            var files = Directory.GetFiles(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette1", "*.png", SearchOption.TopDirectoryOnly);
            //sort because 11 is before 2
            files = files.OrderBy(x => x.Length).ThenBy(x => x).ToArray<String>();
            int counter = 1;
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename.Equals("OrginalAnimationImg" + counter + ".png"))
                {
                    int width = 0, height = 0;
                    var list = Utility.LoadImage(file, ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    if (list.Count == 0) return;
                    List<ushort>[] colorsImages = new List<ushort>[9];

                    colorsImages[0] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 2 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[1] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 3 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[2] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 4 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[3] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 5 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[4] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 6 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[5] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 7 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[6] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 8 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[7] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 9 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[8] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 10 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    vm.File.ImagesTGX[counter - 1].ConvertImageWithPaletteToByteArray(list, width, height, vm.File.Palette, colorsImages);
                    vm.File.ImagesTGX[counter - 1].Header.Width = (ushort)width;
                    vm.File.ImagesTGX[counter - 1].Header.Height = (ushort)height;

                    counter++;
                }
            }

            if (vm.File.ImagesTGX.Count > 0) vm.File.ImagesTGX[0].SizeinByteArray = (uint)vm.File.ImagesTGX[0].ImgFileAsBytearray.Length;
            uint zaehler = 0;
            for (int i = 1; i < vm.File.ImagesTGX.Count; i++)
            {
                zaehler += vm.File.ImagesTGX[i - 1].SizeinByteArray;
                vm.File.ImagesTGX[i].OffsetinByteArray = zaehler;
                vm.File.ImagesTGX[i].SizeinByteArray = (uint)vm.File.ImagesTGX[i].ImgFileAsBytearray.Length;
            }

            //datasize neu setzten
            uint newDataSize = vm.File.ImagesTGX[vm.File.ImagesTGX.Count - 1].OffsetinByteArray + vm.File.ImagesTGX[vm.File.ImagesTGX.Count - 1].SizeinByteArray; ;
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportOrginalStrongholdAnimation end");
        }

        private void OpenInfoWindow(object sender, RoutedEventArgs e)
        {
            if (vm.File == null) return;

            GM1FileInfoWindow infoWindow = new GM1FileInfoWindow((GM1FileHeader.DataType)vm.File.FileHeader.IDataType);
            infoWindow.Show(this);
        }

        private void OpenWorkfolderDirectory(object sender, RoutedEventArgs e)
        {
            ListBox listbox = sender as ListBox;
            if (listbox.SelectedIndex == -1) return;
            Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + listbox.SelectedItem);
        }

        private void ImportImages(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportImages start");
            ImportImagesMethod(false);
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportImages end");
        }

        private void ImportBigImage(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportBigImage start");
            ImportImagesMethod(true);
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportBigImage end");
        }

        private void ImportImagesMethod(bool bigImage)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);

            Utility.XOffsetBefore = 0;
            Utility.YOffsetBefore = 0;

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(vm.File.FileHeader.Name);

            if (!bigImage)
            {
                string imagesDirectory = Path.Combine(vm.UserConfig.WorkFolderPath, fileNameWithoutExtension, "Images");
                var files = Directory.GetFiles(imagesDirectory, "*.png", SearchOption.TopDirectoryOnly);
                // sort because 11 is before 2
                files = files.OrderBy(x => x.Length).ThenBy(x => x).ToArray<String>();

                int counter = 1;

                foreach (var file in files)
                {
                    var filename = Path.GetFileName(file);
                    if (filename.Equals("Image" + counter + ".png"))
                    {
                        Bitmap image = new Bitmap(file);
                        vm.TGXImages[counter - 1].Source = image;
                        vm.TGXImages[counter - 1].MaxWidth = image.PixelSize.Width;
                        vm.TGXImages[counter - 1].MaxHeight = image.PixelSize.Height;
                        counter++;
                        var fileindex = int.Parse(filename.Replace("Image", "").Replace(".png", "")) - 1;
                        int width = 0, height = 0;
                        var list = Utility.LoadImage(file, ref width, ref height, vm.File.ImagesTGX[fileindex].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType);
                        if (list.Count == 0) return;

                        LoadNewDataForGm1File(fileindex, list, width, height);
                    }
                }
            }
            else
            {
                string bigImageDirectory = Path.Combine(vm.UserConfig.WorkFolderPath, fileNameWithoutExtension, "BigImage");
                string bigImageFilePath = Path.Combine(bigImageDirectory, $"{fileNameWithoutExtension}.png");

                var imageData = Utility.LoadImageData(bigImageFilePath);

                int maxwidth = imageData.Width;
                int fileindex = 0;
                int offsety = 0;
                int maxheight = 0;
                int offsetx = 0;
                int actualwidth = 0;
                int counter = 0;

                if (((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject))
                {
                    foreach (var image in vm.File.TilesImages)
                    {
                        int width = image.Width;
                        int height = image.Height;

                        actualwidth += width;
                        
                        if (maxwidth <= actualwidth)
                        {
                            offsety += maxheight;
                            actualwidth = width;
                            maxheight = 0;
                            offsetx = 0;
                        }

                        if (maxheight < height)
                        {
                            maxheight = height;
                        }

                        var colorList = Utility.LoadImage(imageData, ref width, ref height, vm.File.ImagesTGX[fileindex].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType, offsetx, offsety);
                        if (colorList.Count == 0) continue;
                        width = image.Width;
                        height = image.Height;

                        LoadNewDataForGm1File(fileindex, colorList, width, height);
                        fileindex++;

                        Bitmap newimage = Utility.LoadImageAsBitmap(imageData, ref width, ref height, offsetx, offsety);
                        offsetx += width;
                        vm.TGXImages[counter].Source = newimage;
                        vm.TGXImages[counter].MaxWidth = newimage.PixelSize.Width;
                        vm.TGXImages[counter].MaxHeight = newimage.PixelSize.Height;
                        counter++;
                    }

                    vm.File.SetNewTileList();
                }
                else
                {
                    foreach (var image in vm.File.ImagesTGX)
                    {
                        int width = image.Header.Width;
                        int height = image.Header.Height - ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression ? 7 : 0);

                        actualwidth += width;
                        if (maxwidth <= actualwidth)
                        {
                            offsety += maxheight;
                            actualwidth = width;
                            maxheight = 0;
                            offsetx = 0;
                        }

                        if (maxheight < height)
                        {
                            maxheight = height;
                        }
                        int oldOffsetx = offsetx;
                        int oldOffsety = offsety;
                        var colorList = Utility.LoadImage(imageData, ref width, ref height, vm.File.ImagesTGX[fileindex].Header.AnimatedColor, 1, vm.File.FileHeader.IDataType, offsetx, offsety);
                        if (colorList.Count == 0) continue;
                        width = image.Header.Width;
                        height = image.Header.Height - ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression ? 7 : 0);

                        LoadNewDataForGm1File(fileindex, colorList, width, height);
                        fileindex++;

                        Bitmap newimage = Utility.LoadImageAsBitmap(imageData, ref width, ref height, oldOffsetx, oldOffsety);
                        offsetx += width;
                        vm.TGXImages[counter].Source = newimage;
                        vm.TGXImages[counter].MaxWidth = newimage.PixelSize.Width;
                        vm.TGXImages[counter].MaxHeight = newimage.PixelSize.Height;
                        counter++;
                    }
                }
            }

            if (vm.File.ImagesTGX.Count > 0) vm.File.ImagesTGX[0].SizeinByteArray = (uint)vm.File.ImagesTGX[0].ImgFileAsBytearray.Length;
            
            uint zaehler = 0;
            for (int i = 1; i < vm.File.ImagesTGX.Count; i++)
            {
                zaehler += vm.File.ImagesTGX[i - 1].SizeinByteArray;
                vm.File.ImagesTGX[i].OffsetinByteArray = zaehler;
                vm.File.ImagesTGX[i].SizeinByteArray = (uint)vm.File.ImagesTGX[i].ImgFileAsBytearray.Length;
            }

            //datasize neu setzten
            uint newDataSize = vm.File.ImagesTGX[vm.File.ImagesTGX.Count - 1].OffsetinByteArray + vm.File.ImagesTGX[vm.File.ImagesTGX.Count - 1].SizeinByteArray;

            vm.File.FileHeader.IDataSize = newDataSize;

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void LoadNewDataForGm1File(int fileindex, List<ushort> list, int width, int height)
        {
            if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.Animations)
            {
                vm.File.ImagesTGX[fileindex].ConvertImageWithPaletteToByteArray(list, width, height, vm.File.Palette);
                vm.File.ImagesTGX[fileindex].Header.Width = (ushort)width;
                vm.File.ImagesTGX[fileindex].Header.Height = (ushort)height;
            }
            else if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression
                    || (GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression1)
            {
                vm.File.ImagesTGX[fileindex].ConvertNoCommpressionImageToByteArray(list, width, height);
                vm.File.ImagesTGX[fileindex].Header.Width = (ushort)width;
                vm.File.ImagesTGX[fileindex].Header.Height = (ushort)(height + ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression1 ? 0 : 7));//7 because stronghold want it so
            }
            else if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject)
            {
                vm.File.ConvertImgToTiles(list, (ushort)width, (ushort)height);
            }
            else
            {
                vm.File.ImagesTGX[fileindex].ConvertImageWithoutPaletteToByteArray(list, width, height);
                vm.File.ImagesTGX[fileindex].Header.Width = (ushort)width;
                vm.File.ImagesTGX[fileindex].Header.Height = (ushort)height;
            }
        }

        private void ExportImages(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportImages start");
            ExportImagesMethod(false);
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportImages end");
        }

        private void ExportBigImage(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportBigImage start");
            ExportImagesMethod(true);
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportBigImage end");
        }

        private void ExportImagesMethod(bool bigImage)
        {
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            int img = 1;

            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");

            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + ((!bigImage) ? "\\Images" : "\\BigImage")))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + ((!bigImage) ? "\\Images" : "\\BigImage"));
            }

            if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject)
            {
                if (!bigImage)
                {
                    foreach (var image in vm.File.TilesImages)
                    {
                        image.TileImage.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\Image" + img + ".png");
                        img++;
                    }
                }
                else
                {
                    Utility.CreateBigImage(vm.File.TilesImages,vm.BigImageWidth).Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\" + filewithoutgm1ending + ".png");
                }
            }
            else
            {
                if (!bigImage)
                {
                    foreach (var image in vm.File.ImagesTGX)
                    {
                        image.Bitmap.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\Image" + img + ".png");

                        img++;
                    }
                }
                else
                {
                    Utility.CreateBigImage(vm.File.ImagesTGX, vm.BigImageWidth).Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\" + filewithoutgm1ending + ".png"); ;
                }
            }

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + ((bigImage) ? "\\BigImage" : "\\Images"));

            vm.LoadWorkfolderFiles();

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void ImportColortable(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log(">>ImportColortables start");

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(vm.File.FileHeader.Name);
            string directory = Path.Combine(vm.UserConfig.WorkFolderPath, fileNameWithoutExtension, "Colortables");

            for (int i = 1; i <= Palette.ColorTableCount; i++)
            {
                string fileName = $"ColorTable{i}.png";
                string filePath = Path.Combine(directory, fileName);

                if (!File.Exists(filePath))
                {
                    throw new Exception($"File {filePath} not found!");
                }

                int width = 0, height = 0;
                int colorTableIndex = i - 1;
                var list = Utility.LoadImage(filePath, ref width, ref height, 1, Palette.pixelSize, vm.File.FileHeader.IDataType);
                if (list.Count == 0) return;
                vm.File.Palette.ColorTables[colorTableIndex] = new ColorTable(list.ToArray());
            }

            vm.GeneratePaletteAndImgNew();

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);

            if (Logger.Loggeractiv) Logger.Log(">>ImportColortables end");
        }

        private void ExportColortable(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportColortable start");

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);

            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Colortables"))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Colortables");
            }

            for (int i = 0; i < Palette.ColorTableCount; i++)
            {
                var bitmap = ColorTableConverter.GetBitmap(vm.File.Palette.ColorTables[i], Palette.width, Palette.height, Palette.pixelSize);
                bitmap.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Colortables\\ColorTable" + (i + 1) + ".png");
            }

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Colortables");

            vm.LoadWorkfolderFiles();

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);

            if (Logger.Loggeractiv) Logger.Log("\n>>ExportColortable end");
        }

        private void ReplacewithSavedGM1FileM(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedGM1FileM start");
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            File.Copy(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1", vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, true);
            LoadGm1File(vm.File.FileHeader.Name);
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedGM1FileM end");
        }

        String listboxItemBefore = null;
        private void SelectedGm1File(object sender, SelectionChangedEventArgs e)
        {
            vm.OffsetExpanderVisible = false;
            var listbox = sender as ListBox;
            if (listboxItemBefore == listbox.SelectedItem.ToString()) return;
            if (Logger.Loggeractiv) Logger.Log("\n>>SelectedGm1File start");
            listboxItemBefore = listbox.SelectedItem.ToString();
            LoadGm1File(listboxItemBefore);
            if (vm.File != null && vm.File.FileHeader != null && vm.File.FileHeader.Name.Contains("anim_castle"))
            {
                try
                {
                    if (File.Exists(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold Crusader.exe"))

                        vm.StrongholdasBytes = File.ReadAllBytes(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold Crusader.exe");

                    if (File.Exists(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold_Crusader_Extreme.exe"))
                        vm.StrongholdExtremeasBytes = File.ReadAllBytes(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold_Crusader_Extreme.exe");
                }
                catch (Exception em)
                {
                    if (Logger.Loggeractiv) Logger.Log("Exception:\n" + em.Message);
                    MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + em.Message);
                    messageBox.Show();
                }
            }
            if (Logger.Loggeractiv) Logger.Log("\n>>SelectedGm1File end");
        }

        private void TGXImageChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            int index = listbox.SelectedIndex;
            if(listbox.SelectedItem == null)return;
            var dummy = ((Image)listbox.SelectedItem).Tag;
            if (dummy is TGXImage) vm.ActualTGXImageSelection = (TGXImage)dummy;
            else vm.ActualTGXImageSelection = null;
            if (vm.File == null || vm.File.FileHeader == null || !vm.File.FileHeader.Name.Contains("anim_castle")) return;
            Point offset = default;
            int strongholdValue = 912;
            //Point offset = new Point(2496, 2496);
            if (vm.OffsetsBuildings.TryGetValue(index, out offset))
            {
                vm.OffsetExpanderVisible = true;
                vm.Strongholdadress = offset;
                if (vm.StrongholdasBytes != null)
                {
                    vm.XOffset = unchecked((sbyte)vm.StrongholdasBytes[(int)vm.Strongholdadress.X - 912]);
                    vm.YOffset = BitConverter.ToInt32(vm.StrongholdasBytes, (int)vm.Strongholdadress.Y - 912);
                    
                    if (index == 12 || index == 13)
                    {
                        vm.YOffset = unchecked((sbyte)vm.StrongholdasBytes[(int)vm.Strongholdadress.Y - 912]);
                    }
                }
            }
            else
            {
                vm.OffsetExpanderVisible = false;
            }
        }

        private void SelectedGfxFile(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            if (listboxItemBefore == listbox.SelectedItem.ToString()) return;

            if (Logger.Loggeractiv) Logger.Log("\n>>SelectedGm1File start");
            listboxItemBefore = listbox.SelectedItem.ToString();
            actualName = listboxItemBefore;
            LoadTgxFile(listboxItemBefore);

            if (Logger.Loggeractiv) Logger.Log("\n>>SelectedGm1File end");
        }

        private void LoadTgxFile(string listboxItemBefore)
        {
            if (Logger.Loggeractiv) Logger.Log("LoadGfxFile");
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            try
            {
                vm.ColorButtonsEnabled = false;
                vm.OrginalStrongholdAnimationButtonEnabled = false;
                vm.ActuellColorTable = null;
                vm.ReplaceWithSaveFile = false;
                vm.ButtonsEnabled = false;
                vm.ImportButtonEnabled = false;
                vm.TgxButtonExportEnabled = true;
                vm.ReplaceWithSaveFileTgx = false;
                vm.DecodeTgxData(listboxItemBefore, this);

                var filewithouttgxending = listboxItemBefore.Replace(".tgx", "");

                if (File.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithouttgxending + "\\" + filewithouttgxending + ".png"))
                    vm.TgxButtonImportEnabled = true;

                if (!File.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithouttgxending + "\\" + filewithouttgxending + "Save.tgx"))
                {
                    vm.ReplaceWithSaveFileTgx = false;
                }
                else
                {
                    vm.ReplaceWithSaveFileTgx = true;
                }
            }
            catch (Exception e)
            {
                if (Logger.Loggeractiv) Logger.Log("Exception:\n" + e.Message);
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + e.Message);
                messageBox.Show();
            }

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void LoadGm1File(string listboxItemBefore)
        {
            if (Logger.Loggeractiv) Logger.Log("LoadGm1File");
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            try
            {
                vm.TgxButtonExportEnabled = false;
                vm.TgxButtonImportEnabled = false;
                vm.ReplaceWithSaveFileTgx = false;
                if (vm.DecodeData(listboxItemBefore, this))
                {
                    Utility.datatype = (GM1FileHeader.DataType)vm.File.FileHeader.IDataType;

                    vm.Filetype = (GM1FileHeader.DataType)vm.File.FileHeader.IDataType;
                    if (vm.File.Palette == null)
                    {
                        vm.ImportButtonEnabled = true;
                        vm.ColorButtonsEnabled = false;
                        vm.OrginalStrongholdAnimationButtonEnabled = false;
                        vm.ActuellColorTable = null;
                    }
                    else
                    {
                        vm.OrginalStrongholdAnimationButtonEnabled = true;
                        vm.ColorButtonsEnabled = true;
                        vm.ImportButtonEnabled = true;
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
            }
            catch (Exception e)
            {
                if (Logger.Loggeractiv) Logger.Log("Exception:\n" + e.Message);
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + e.Message);
                messageBox.Show();
            }

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void ViewModelSet(object sender, EventArgs em)
        {
            try
            {
                vm = DataContext as MainWindowViewModel;

                vm.UserConfig = new UserConfig();
                vm.UserConfig.LoadData();
                vm.ActualLanguage = vm.UserConfig.Language;
                vm.ActualColorTheme = vm.UserConfig.ColorTheme;
                vm.OpenFolderAfterExport = vm.UserConfig.OpenFolderAfterExport;
                vm.LoggerActiv = vm.UserConfig.ActivateLogger;
                vm.LoadStrongholdFiles();
                vm.LoadWorkfolderFiles();

                if (File.Exists(vm.UserConfig.WorkFolderPath + "\\Logger\\Log.txt")) File.Delete(vm.UserConfig.WorkFolderPath + "\\Logger\\Log.txt");
            }
            catch (Exception e)
            {
                if (Logger.Loggeractiv) Logger.Log("Exception:\n" + e.Message);
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n" + e.Message);
                messageBox.Show();
            }
        }

        private void ChangeLanguage(object sender, RoutedEventArgs e)
        {
            MenuItem source = e.Source as MenuItem;
            Languages.Language language = (Languages.Language)source.Header;
            vm.ActualLanguage = language;
        }

        private void ChangeColorTheme(object sender, RoutedEventArgs e)
        {
            MenuItem source = e.Source as MenuItem;
            ColorThemes.ColorTheme colorTheme = (ColorThemes.ColorTheme)source.Header;
            vm.ActualColorTheme = colorTheme;
        }

        private void CreatenewGM1(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>CreatenewGM1 start");
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending);
            }

            if (!File.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1"))
            {
                File.Copy(vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1");
            }
            var array = vm.File.GetNewGM1Bytes();
            File.WriteAllBytes(vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, array);
            File.Copy(vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Modded.gm1", true);
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
            if (Logger.Loggeractiv) Logger.Log("\n>>CreatenewGM1 end");
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

        private async Task<string> GetFolderAsync(String name, String initialDirectory)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();

            openFolderDialog.Title = name;

            if (!String.IsNullOrEmpty(initialDirectory))
            {
                openFolderDialog.Directory = initialDirectory;
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
            ChangeColorTableWindow changeColorTableWindow = new ChangeColorTableWindow(
                vm.File.Palette.ColorTables[vm.File.Palette.ActualPalette].Copy(),
                OnWindowClosed
            );

            changeColorTableWindow.ShowDialog(this);
        }

        private void Button_ChangeOffset(object sender, RoutedEventArgs e)
        {
            ListBox tGXImageListBox = this.Get<ListBox>("TGXImageListBox");
            var index = tGXImageListBox.SelectedIndex;
            vm.ChangeExeOffset(index, vm.Strongholdadress, vm.XOffset, vm.YOffset);
        }

        private void Button_ClickGifExporter(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>Button_ClickGifExporter start");
            ListBox listBox = this.Get<ListBox>("TGXImageListBox");
            if (listBox.SelectedItems == null || listBox.SelectedItems.Count == 0) return;
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");

            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Gif"))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Gif");
            }

            Stream stream = new FileStream(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Gif\\ImageAsGif.gif", FileMode.Create);
            GifWriter gif = new GifWriter(stream, vm.Delay, 0);

            foreach (var img in listBox.SelectedItems)
            {
                if (gif.DefaultWidth < ((Image)img).Source.Size.Width) gif.DefaultWidth = (int)((Image)img).Source.Size.Width;
                if (gif.DefaultHeight < ((Image)img).Source.Size.Height) gif.DefaultHeight = (int)((Image)img).Source.Size.Height;
            }

            foreach (var img in listBox.SelectedItems)
            {
                Stream imgStream = new MemoryStream();
                ((Image)img).Source = new Bitmap(imgStream);
                System.Drawing.Image imageGif = System.Drawing.Image.FromStream(imgStream);
                gif.WriteFrame(imageGif);
            }
            stream.Flush();
            stream.Dispose();

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Gif");
            if (Logger.Loggeractiv) Logger.Log("\n>>Button_ClickGifExporter end");
        }

        private void OnWindowClosed(ColorTable colorTable)
        {
            vm.File.Palette.ColorTables[vm.File.Palette.ActualPalette] = colorTable;

            vm.GeneratePaletteAndImgNew();

            vm.DecodeButtonEnabled = true;
        }
    }
}

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

            ListBox tGXImageListBox = this.Get<ListBox>("TGXImageListBox");
            tGXImageListBox.SelectionChanged += TGXImageChanged;

            


            ListBox gfxFilesSelector = this.Get<ListBox>("GfxFilesSelector");
            gfxFilesSelector.SelectionChanged += SelectedGfxFile;

            ListBox workfolderSelector = this.Get<ListBox>("WorkfolderSelector");
            workfolderSelector.DoubleTapped += OpenWorkfolderDirectory;

            MenuItem workfolderMenueItem = this.Get<MenuItem>("WorkfolderMenueItem");
            workfolderMenueItem.Click += ChangeWorkfolder;
            MenuItem crusaderfolderMenueItem = this.Get<MenuItem>("CrusaderfolderMenueItem");
            crusaderfolderMenueItem.Click += ChangeCrusaderfolder;

            MenuItem openLogFile = this.Get<MenuItem>("OpenLogFile");
            openLogFile.Click += OpenLogFile;

            

            MenuItem createnewGM1MenueItem = this.Get<MenuItem>("CreatenewGM1MenueItem");
            createnewGM1MenueItem.Click += CreatenewGM1;
            MenuItem replacewithSavedGM1FileMenueItem = this.Get<MenuItem>("ReplacewithSavedGM1FileMenueItem");
            replacewithSavedGM1FileMenueItem.Click += ReplacewithSavedGM1FileM;


            MenuItem createnewTgxMenueItem = this.Get<MenuItem>("CreatenewTgxMenueItem");
            createnewTgxMenueItem.Click += CreatenewTgx;
            MenuItem replacewithSavedTgxFileMenueItem = this.Get<MenuItem>("ReplacewithSavedTgxFileMenueItem");
            replacewithSavedTgxFileMenueItem.Click += ReplacewithSavedTgxFile;
            


            MenuItem exportColortableMenueItem = this.Get<MenuItem>("ExportColortableMenueItem");
            exportColortableMenueItem.Click += ExportColortable;

            MenuItem importColortableMenueItem = this.Get<MenuItem>("ImportColortableMenueItem");
            importColortableMenueItem.Click += ImportColortable;

            MenuItem exportBigImageMenueItem = this.Get<MenuItem>("ExportBigImageMenueItem");
            exportBigImageMenueItem.Click += ExportBigImage;

            MenuItem importBigImageMenueItem = this.Get<MenuItem>("ImportBigImageMenueItem");
            importBigImageMenueItem.Click += ImportBigImage;
            
            MenuItem exportImagesMenueItem = this.Get<MenuItem>("ExportImagesMenueItem");
            exportImagesMenueItem.Click += ExportImages;

            MenuItem importImagesMenueItem = this.Get<MenuItem>("ImportImagesMenueItem");
            importImagesMenueItem.Click += ImportImages;

            MenuItem exportOrginalStrongholdAnimation = this.Get<MenuItem>("ExportOrginalStrongholdAnimation");
            exportOrginalStrongholdAnimation.Click += ExportOrginalStrongholdAnimation;

            MenuItem importOrginalStrongholdAnimation = this.Get<MenuItem>("ImportOrginalStrongholdAnimation");
            importOrginalStrongholdAnimation.Click += ImportOrginalStrongholdAnimation;

            MenuItem importTgxImageMenueItem = this.Get<MenuItem>("ImportTgxImageMenueItem");
            importTgxImageMenueItem.Click += ImportTgxImage;

            MenuItem exportTgxImageMenueItem = this.Get<MenuItem>("ExportTgxImageMenueItem");
            exportTgxImageMenueItem.Click += ExportTgxImage;




            Image image = this.Get<Image>("HelpIcon");
            image.Tapped += OpenInfoWindow;
        }

        

        private void ReplacewithSavedTgxFile(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedTgxFile start");
            var filewithoutgm1ending = actualName.Replace(".tgx", "");
            File.Copy(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.tgx", vm.UserConfig.CrusaderPath.Replace("\\gm",String.Empty) + "\\" + actualName, true);
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedTgxFile end");
        }

        private void CreatenewTgx(object sender, RoutedEventArgs e)
        {
            var filewithoutgm1ending = actualName.Replace(".tgx", "");
            if(!File.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.tgx"))
                File.Copy(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty)+"\\gfx\\"+ actualName, vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.tgx",true);

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
            var filewithouttgxending = actualName.Replace(".tgx", "");

            if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithouttgxending))
            {
                Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithouttgxending);
            }

            vm.TgxImage.Bitmap.Save(vm.UserConfig.WorkFolderPath + "\\"+ filewithouttgxending+ "\\" + filewithouttgxending + ".png");

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithouttgxending);


            vm.LoadWorkfolderFiles();
            vm.TgxButtonImportEnabled = true;

            if (Logger.Loggeractiv) Logger.Log("\n>>ExportTgxImage end");
        }

        private void ImportTgxImage(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportTgxImage start");
            var filewithouttgxending = actualName.Replace(".tgx", "");
          
          
            int width = 0;
            int height = 0;
            List<UInt16> colors = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithouttgxending + "\\" + filewithouttgxending + ".png",ref width,ref height);
            vm.TgxImage.ConvertImageWithoutPaletteToByteArray(colors,width,height);//todo animated color?
            vm.TgxImage.TgxWidth = (uint)width;
            vm.TgxImage.TgxHeight = (uint)height;
            vm.TgxImage.CreateImageFromByteArray(null, true);
            vm.TGXImages.Clear();
            Image image = new Image();
            image.MaxWidth = width;
            image.MaxHeight = height;
            image.Source = vm.TgxImage.Bitmap;
            vm.TGXImages.Add(image);


            if (Logger.Loggeractiv) Logger.Log("\n>>ImportTgxImage end");
        }

        private void OpenLogFile(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(Logger.Path)) {
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

                if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette"+(i+1)))
                {
                    Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + (i + 1));
                }
                int img = 1;
                foreach (var image in vm.File.ImagesTGX)
                {
                    image.Bitmap.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + (i + 1)+"\\OrginalAnimationImg" + img + ".png");
                    img++;
                }
                vm.ChangePalette(1);
            }

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending );

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
                    var list = Utility.LoadImage(file, ref width, ref height, vm.File.ImagesTGX[counter-1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    if (list.Count == 0) return;
                    List<ushort>[] colorsImages = new List<ushort>[9];

                    colorsImages[0] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 2 + "\\OrginalAnimationImg"+counter+".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[1] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 3 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[2] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 4 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[3] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 5 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[4] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 6 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[5] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 7 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[6] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 8 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[7] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 9 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    colorsImages[8] = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\OrginalAnimationPalette" + 10 + "\\OrginalAnimationImg" + counter + ".png", ref width, ref height, vm.File.ImagesTGX[counter - 1].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                    vm.File.ImagesTGX[counter - 1].ConvertImageWithPaletteToByteArray(list, width, height, vm.File.Palette, colorsImages);
                    vm.File.ImagesTGX[counter - 1].Width = (ushort)width;
                    vm.File.ImagesTGX[counter - 1].Height = (ushort)height;

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

            InfoWindow infoWindow = new InfoWindow((GM1FileHeader.DataType)vm.File.FileHeader.IDataType);
            infoWindow.Show();
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

            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");

            if (!bigImage)
            {
               
                var files = Directory.GetFiles(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images", "*.png", SearchOption.TopDirectoryOnly);
                //sort because 11 is before 2
                files = files.OrderBy(x => x.Length).ThenBy(x => x).ToArray<String>();


                int counter = 1;

                foreach (var file in files)
                {
                    var filename = Path.GetFileName(file);
                    if (filename.Equals("Image" + counter + ".png"))
                    {
                        Avalonia.Media.Imaging.Bitmap image = new Avalonia.Media.Imaging.Bitmap(file);
                        vm.TGXImages[counter - 1].Source = image;
                        vm.TGXImages[counter - 1].MaxWidth = image.PixelSize.Width;
                        vm.TGXImages[counter - 1].MaxHeight = image.PixelSize.Height;
                        counter++;
                        var fileindex = int.Parse(filename.Replace("Image", "").Replace(".png", "")) - 1;
                        int width = 0, height = 0;
                        var list = Utility.LoadImage(file, ref width, ref height, vm.File.ImagesTGX[fileindex].AnimatedColor, 1, vm.File.FileHeader.IDataType);
                        if (list.Count == 0) return;

                        LoadNewDataForGm1File(fileindex, list, width, height);
                    }

                }
            }
            else
            {
                int maxwidth = 650;
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
                        int height = image.Height - ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression ? 7 : 0);

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

                        var list = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\" + filewithoutgm1ending + ".png", ref width, ref height, vm.File.ImagesTGX[fileindex].AnimatedColor, 1, vm.File.FileHeader.IDataType, offsetx, offsety);
                        if (list.Count == 0) continue;
                        width = image.Width;
                        height = image.Height;

                        LoadNewDataForGm1File(fileindex, list, width, height);
                        fileindex++;
                        offsetx += width;
                       

                        Avalonia.Media.Imaging.Bitmap newimage = Utility.LoadImageAsBitmap(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\" + filewithoutgm1ending + ".png", ref width, ref height, offsetx, offsety);
                        vm.TGXImages[counter].Source = newimage;
                        vm.TGXImages[counter].MaxWidth = newimage.PixelSize.Width;
                        vm.TGXImages[counter].MaxHeight = newimage.PixelSize.Height;
                        counter++;
                    }
                } else {
                    foreach (var image in vm.File.ImagesTGX)
                    {
                        int width = image.Width;
                        int height = image.Height - ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression ? 7 : 0);



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
                        var list = Utility.LoadImage(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\" + filewithoutgm1ending + ".png", ref width, ref height, vm.File.ImagesTGX[fileindex].AnimatedColor, 1, vm.File.FileHeader.IDataType, offsetx, offsety);
                        if (list.Count == 0) continue;
                        width = image.Width;
                        height = image.Height - ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression ? 7 : 0);

                        LoadNewDataForGm1File(fileindex, list, width, height);
                        fileindex++;
                        offsetx += width;

                        Avalonia.Media.Imaging.Bitmap newimage = Utility.LoadImageAsBitmap(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\" + filewithoutgm1ending + ".png", ref width, ref height, oldOffsetx, oldOffsety);
                        vm.TGXImages[counter].Source = newimage;
                        vm.TGXImages[counter].MaxWidth = newimage.PixelSize.Width;
                        vm.TGXImages[counter].MaxHeight = newimage.PixelSize.Height;
                        counter++;
                    }
                }
              


                
            }
           
            if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject) vm.File.SetNewTileList();

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

            vm.File.FileHeader.IDataSize = newDataSize;
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void LoadNewDataForGm1File(int fileindex, List<ushort> list,int width,int height)
        {
            if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.Animations)
            {
                vm.File.ImagesTGX[fileindex].ConvertImageWithPaletteToByteArray(list, width, height, vm.File.Palette);
                vm.File.ImagesTGX[fileindex].Width = (ushort)width;
                vm.File.ImagesTGX[fileindex].Height = (ushort)height;
            }
            else if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression
                    || (GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression1)
            {
                vm.File.ImagesTGX[fileindex].ConvertNoCommpressionImageToByteArray(list, width, height);
                vm.File.ImagesTGX[fileindex].Width = (ushort)width;
                vm.File.ImagesTGX[fileindex].Height = (ushort)(height + ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.NOCompression1 ? 0 : 7));//7 because stronghold want it so
            }
            else if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject)
            {
                vm.File.ConvertImgToTiles(list, (ushort)width, (ushort)height);
            }
            else
            {
                vm.File.ImagesTGX[fileindex].ConvertImageWithoutPaletteToByteArray(list, width, height);
                vm.File.ImagesTGX[fileindex].Width = (ushort)width;
                vm.File.ImagesTGX[fileindex].Height = (ushort)height;
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
           
                if (!Directory.Exists(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + ((!bigImage) ?"\\Images": "\\BigImage")))
                {
                    Directory.CreateDirectory(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + ((!bigImage) ? "\\Images" : "\\BigImage"));
                }


            if ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType == GM1FileHeader.DataType.TilesObject)
            {
                if (!bigImage) {
                    foreach (var image in vm.File.TilesImages)
                    {
                        image.TileImage.Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Images\\Image" + img + ".png");
                        img++;
                    }
                }
                else
                {
                    Utility.CreateBigImage(vm.File.TilesImages).Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\"+ filewithoutgm1ending + ".png");
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
                    Utility.CreateBigImage(vm.File.ImagesTGX).Save(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\BigImage\\" + filewithoutgm1ending + ".png"); ;
                }

               
            }




            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending +((bigImage)?"\\BigImage": "\\Images"));


            vm.LoadWorkfolderFiles();

            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
        }

        private void ImportColortable(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportColortable start");
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Wait);
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            var files=Directory.GetFiles(vm.UserConfig.WorkFolderPath+"\\"+ filewithoutgm1ending+ "\\Colortables", "*.png");
  
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename.StartsWith("ColorTable"))
                {
                    int width = 0, height = 0;
                    var fileindex = int.Parse(filename.Replace("ColorTable", "").Replace(".png","")) - 1;
                    var list = Utility.LoadImage(file, ref width, ref height,1, Palette.pixelSize, vm.File.FileHeader.IDataType);
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
            if (Logger.Loggeractiv) Logger.Log("\n>>ImportColortable end");
        }

        private void ExportColortable(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportColortable start");
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
            Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Colortables");

            vm.LoadWorkfolderFiles();
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow);
            if (Logger.Loggeractiv) Logger.Log("\n>>ExportColortable end");
        }

        private void ReplacewithSavedGM1FileM(object sender, RoutedEventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedGM1FileM start");
            var filewithoutgm1ending = vm.File.FileHeader.Name.Replace(".gm1", "");
            File.Copy(vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\" + filewithoutgm1ending + "Save.gm1",   vm.UserConfig.CrusaderPath + "\\" + vm.File.FileHeader.Name, true);
            LoadGm1File(vm.File.FileHeader.Name);
            if (Logger.Loggeractiv) Logger.Log("\n>>ReplacewithSavedGM1FileM end");
        }

        String listboxItemBefore = null;
        private void SelectedGm1File(object sender, SelectionChangedEventArgs e)
        {

            vm.OffsetExpanderVisible = false;
             var listbox = sender as ListBox;
            if (listboxItemBefore == listbox.SelectedItem.ToString())return;
            if (Logger.Loggeractiv) Logger.Log("\n>>SelectedGm1File start");
            listboxItemBefore = listbox.SelectedItem.ToString();
            LoadGm1File(listboxItemBefore);
            if (vm.File.FileHeader.Name.Contains("anim_castle"))
            {
            
                   _strongholdasBytes = File.ReadAllBytes(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold_Crusader_Extreme.exe");

            }
            if (Logger.Loggeractiv) Logger.Log("\n>>SelectedGm1File end");
        }

        private byte[] _strongholdasBytes;
        private Point _strongholdadress;
        private void TGXImageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!vm.File.FileHeader.Name.Contains("anim_castle") ) return;
            var listbox = sender as ListBox;
            int index = listbox.SelectedIndex;
            if (index <= 3 || index >=121)
            {
                vm.OffsetExpanderVisible = true;
                switch (index)
                {
                    case 0:
                        _strongholdadress = new Point(939615, 939608);
                        break;
                    case 1:
                        _strongholdadress = new Point(939841, 939834);
                        break;
                    case 2:
                        _strongholdadress = new Point(940022, 940015);
                        break;
                    case 3:
                        _strongholdadress = new Point(939728, 939721);
                        break;
                    case 121:
                        _strongholdadress = new Point(939943, 939936);
                        break;
                    case 122:
                        _strongholdadress = new Point(939943, 939936);
                        break;
                    case 123:
                        _strongholdadress = new Point(939574, 939567);
                        break;
                    case 124:
                        _strongholdadress = new Point(939536, 939529);
                        break;
                    case 125:
                        _strongholdadress = new Point(939574, 939567);
                        break;
                    default:
                        break;
                }
                
                vm.XOffset = unchecked((sbyte)_strongholdasBytes[(int)_strongholdadress.X]);
                vm.YOffset = BitConverter.ToInt32(_strongholdasBytes, (int)_strongholdadress.Y);
                
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

                    vm.Filetype = Utility.GetText("Datatype") + ((GM1FileHeader.DataType)vm.File.FileHeader.IDataType);
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
                MessageBoxWindow messageBox = new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, "Something went wrong: pls add a issue on the Github Page\n\nError:\n"+e.Message);
                messageBox.Show();
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
            Logger.Path = vm.UserConfig.WorkFolderPath + "\\Logger";
            vm.LoadStrongholdFiles();
            vm.LoadWorkfolderFiles();
        
           if(File.Exists(vm.UserConfig.WorkFolderPath + "\\Logger\\Log.txt")) File.Delete(vm.UserConfig.WorkFolderPath+"\\Logger\\Log.txt");
        }

        private void CreatenewGM1(object sender, EventArgs e)
        {
            if (Logger.Loggeractiv) Logger.Log("\n>>CreatenewGM1 start");
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

        private async Task<string> GetFolderAsync(String name,String initialDirectory)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog();

            openFolderDialog.Title = name;



            if (!String.IsNullOrEmpty(initialDirectory))
            {
                openFolderDialog.DefaultDirectory = initialDirectory;
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
        private void Button_ChangeOffset(object sender, RoutedEventArgs e)
        {

            var bytesArray = BitConverter.GetBytes(vm.YOffset);
            _strongholdasBytes[(int)_strongholdadress.X] = (byte)vm.XOffset;
            for (int i = 0; i < bytesArray.Length; i++)
            {
                _strongholdasBytes[(int)_strongholdadress.Y + i] = bytesArray[i];
            }
            
            File.WriteAllBytes(vm.UserConfig.CrusaderPath.Replace("\\gm", String.Empty) + "\\Stronghold_Crusader_Extreme.exe", _strongholdasBytes);

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
            GifWriter gif = new GifWriter(stream,vm.Delay,0);

            foreach (var img in listBox.SelectedItems)
            {
               if(gif.DefaultWidth < ((Image)img).Source.PixelSize.Width) gif.DefaultWidth = ((Image)img).Source.PixelSize.Width;
               if (gif.DefaultHeight < ((Image)img).Source.PixelSize.Height) gif.DefaultHeight = ((Image)img).Source.PixelSize.Height;
            }

                foreach (var img in listBox.SelectedItems)
            {
                Stream imgStream = new MemoryStream();
                ((Image)img).Source.Save(imgStream);
                System.Drawing.Image imageGif = System.Drawing.Image.FromStream(imgStream);
                gif.WriteFrame(imageGif);
            }
            stream.Flush();
            stream.Dispose();

            if (vm.UserConfig.OpenFolderAfterExport)
                Process.Start("explorer.exe", vm.UserConfig.WorkFolderPath + "\\" + filewithoutgm1ending + "\\Gif");
            if (Logger.Loggeractiv) Logger.Log("\n>>Button_ClickGifExporter end");
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

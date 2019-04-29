using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Files.Gm1Converter;
using Gm1KonverterCrossPlatform.ViewModels;
using HelperClasses.Gm1Converter;
using ThemeEditor.Controls.ColorPicker;

namespace Gm1KonverterCrossPlatform.HelperClasses.Views
{
    public class ChangeColorPalette : Window
    {
        private static readonly int pixelsize = 20;
        public ChangeColorPalette()
        {
            this.InitializeComponent();
            #if DEBUG
            this.AttachDevTools();
            #endif
            this.Closing += WindowClosed;
            image = this.Get<Image>("PaletteImage");
            canvas = this.Get<Canvas>("Cnv");
            image.PointerPressed += MousePressed;
            var color = Color.FromArgb(100,255,0,0);
            rect[0] = new Rectangle();
            rect[0].Width = pixelsize;
            rect[0].Height = 2;
            rect[0].Fill = new SolidColorBrush(color);
            canvas.Children.Add(rect[0]);
            rect[1] = new Rectangle();
            rect[1].Width = pixelsize;
            rect[1].Height = 2;
            rect[1].Fill = new SolidColorBrush(color);
            canvas.Children.Add(rect[1]);
            rect[2] = new Rectangle();
            rect[2].Width = 2;
            rect[2].Height = pixelsize - 4;
            rect[2].Fill = new SolidColorBrush(color);
            canvas.Children.Add(rect[2]);
            rect[3] = new Rectangle();
            rect[3].Width = 2;
            rect[3].Height = pixelsize - 4;
            rect[3].Fill = new SolidColorBrush(color);
            canvas.Children.Add(rect[3]);
        }



        Image image;
        private Canvas canvas;
        Rectangle[] rect = new Rectangle[4];
        private int positionInPalette;
        private void MousePressed(object sender, PointerPressedEventArgs e)
        {
            var pos = e.GetPosition(image);
            var newPos = new Point(((int)pos.X) / pixelsize * pixelsize, ((int)pos.Y) / pixelsize * pixelsize);
            Canvas.SetTop(rect[0], newPos.Y - 160);
            Canvas.SetLeft(rect[0], newPos.X);
            Canvas.SetTop(rect[1], newPos.Y - 160 + 18);
            Canvas.SetLeft(rect[1], newPos.X);
            Canvas.SetTop(rect[2], newPos.Y - 160 + 2);
            Canvas.SetLeft(rect[2], newPos.X);
            Canvas.SetTop(rect[3], newPos.Y - 160 + 2);
            Canvas.SetLeft(rect[3], newPos.X + pixelsize - 2);

            var vm = DataContext as MainWindowViewModel;
            positionInPalette = (int)newPos.X / pixelsize + (int)(newPos.Y) / pixelsize * 32;
            var color = vm.File.Palette.ArrayPaletten[vm.File.Palette.ActualPalette, positionInPalette];
            byte r, g, b, a;
            Utility.ReadColor(color, out r, out g, out b, out a);
            vm.ColorAsText = "#" + a.ToString("X2") + r.ToString("X2") + g.ToString("X2") + b.ToString("X2");

           


         
        }
        private void Button_ClickGeneratePallete(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            var color = Color.Parse(vm.ColorAsText).ToUint32();
            var newColor=Utility.EncodeColorTo2Byte(color);

            vm.File.Palette.ArrayPaletten[vm.File.Palette.ActualPalette, positionInPalette] = newColor;
            vm.ActuellColorTableChangeColorWindow = vm.File.Palette.GetBitmap(vm.File.Palette.ActualPalette, pixelsize);


        }

        private void Button_ClickSavePalette(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            vm.File.Palette.PaletteChanged = true;

            this.Close();
        }
        ushort[,] safeList;
        private void WindowClosed(object sender, CancelEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            if (!vm.File.Palette.PaletteChanged)
            {
                vm.File.Palette.ArrayPaletten = safeList;
            }
         
        }

        public void LoadPalette()
        {
            var vm = DataContext as MainWindowViewModel;
            safeList =(ushort[,]) vm.File.Palette.ArrayPaletten.Clone();
            vm.ActuellColorTableChangeColorWindow = vm.File.Palette.GetBitmap(vm.File.Palette.ActualPalette, pixelsize);
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

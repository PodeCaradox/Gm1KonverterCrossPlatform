using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Gm1KonverterCrossPlatform.ViewModels;
using HelperClasses.Gm1Converter;

namespace Gm1KonverterCrossPlatform.HelperClasses.Views
{
    public class ChangeColorPalette : Window
    {
        private const int pixelsize = 20;

        private Image image;
        private Rectangle highlight;
        private int positionInPalette;

        public ChangeColorPalette()
        {
            this.InitializeComponent();

            this.Closing += WindowClosed;
            image = this.Get<Image>("PaletteImage");
            image.PointerPressed += MousePressed;

            Color color = Color.FromArgb(100, 255, 0, 0);

            highlight = new Rectangle();
            highlight.Width = pixelsize;
            highlight.Height = pixelsize;
            highlight.Stroke = new SolidColorBrush(color);
            highlight.StrokeThickness = 2;

            Canvas canvas = this.Get<Canvas>("Cnv");
            canvas.Children.Add(highlight);
        }

        private void MousePressed(object sender, PointerPressedEventArgs e)
        {
            var pos = e.GetPosition(image);
            var newPos = new Point(((int)pos.X) / pixelsize * pixelsize, ((int)pos.Y) / pixelsize * pixelsize);
            
            Canvas.SetTop(highlight, newPos.Y - 160);
            Canvas.SetLeft(highlight, newPos.X);

            var vm = DataContext as MainWindowViewModel;
            positionInPalette = (int)newPos.X / pixelsize + (int)(newPos.Y) / pixelsize * 32;
            var color = vm.File.Palette.ArrayPaletten[vm.File.Palette.ActualPalette, positionInPalette];
            byte r, g, b, a;
            Utility.ReadColor(color, out r, out g, out b, out a);
            vm.ColorAsText = "#" + a.ToString("X2") + r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
            vm.Red = r;
            vm.Green = g;
            vm.Blue = b;
        }

        private void Button_ClickGeneratePallete(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            var color = Color.Parse(vm.ColorAsText).ToUint32();
            var newColor = Utility.EncodeColorTo2Byte(color);

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

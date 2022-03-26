using System;
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
using Files.Gm1Converter;

namespace Gm1KonverterCrossPlatform.Views
{
    public class ChangeColorTableWindow : Window
    {
        private const int pixelSize = 20;

        private readonly ChangeColorTableViewModel viewModel;
        private readonly Action<ColorTable> callback;
        private readonly Image image;
        private readonly Rectangle highlight;

        public ChangeColorTableWindow() { }

        public ChangeColorTableWindow(ColorTable colorTable, Action<ColorTable> callback)
        {
            AvaloniaXamlLoader.Load(this);

            viewModel = new ChangeColorTableViewModel(colorTable);
            DataContext = viewModel;

            this.callback = callback;

            Closing += WindowClosed;

            image = this.Get<Image>("PaletteImage");
            highlight = this.Get<Rectangle>("PaletteImageHighlight");

            viewModel.Bitmap = HelperClasses.ImageConverter.ColorTableToImg(viewModel.ColorTable, 32, 8, pixelSize);
        }

        private void MousePressed(object sender, PointerPressedEventArgs e)
        {
            var pos = e.GetPosition(image);
            var newPos = new Point(((int)pos.X) / pixelSize * pixelSize, ((int)pos.Y) / pixelSize * pixelSize);

            Canvas.SetLeft(highlight, newPos.X);
            Canvas.SetTop(highlight, newPos.Y);

            viewModel.ColorPositionInColorTable = (int)newPos.X / pixelSize + (int)(newPos.Y) / pixelSize * 32;
            var color = viewModel.ColorTable.ColorList[viewModel.ColorPositionInColorTable];
            Utility.ReadColor(color, out byte r, out byte g, out byte b, out _);

            viewModel.SetColor(r, g, b);

            viewModel.ColorSelected = true;
        }

        private void Button_SaveColor(object sender, RoutedEventArgs e)
        {
            uint color = Color.FromRgb((byte)viewModel.Red, (byte)viewModel.Green, (byte)viewModel.Blue).ToUint32();
            ushort newColor = Utility.EncodeColorTo2Byte(color);

            viewModel.ColorTable.ColorList[viewModel.ColorPositionInColorTable] = newColor;
            viewModel.Bitmap = HelperClasses.ImageConverter.ColorTableToImg(viewModel.ColorTable, 32, 8, pixelSize);

            viewModel.ColorTableChanged = true;
        }

        private void Button_SaveColorTable(object sender, RoutedEventArgs e)
        {
            if (viewModel.ColorTableChanged)
            {
                SaveColorTableChanges();
            }

            Close();
        }

        private void WindowClosed(object sender, CancelEventArgs e)
        {
            if (viewModel.ColorTableChanged)
            {
                e.Cancel = true;

                var dialogBox = new ChangeColorTableWindowDialogBox(this);
                dialogBox.ShowDialog(this);
            }
        }

        public void SaveColorTableChanges()
        {
            callback(viewModel.ColorTable);
            viewModel.ColorTableChanged = false;
        }

        public void DiscardColorTableChanges()
        {
            viewModel.ColorTableChanged = false;
        }
    }
}

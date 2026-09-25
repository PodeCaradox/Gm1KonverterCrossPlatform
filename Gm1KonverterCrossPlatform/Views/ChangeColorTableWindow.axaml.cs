using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;
using Gm1KonverterCrossPlatform.HelperClasses;
using Gm1KonverterCrossPlatform.ViewModels;

namespace Gm1KonverterCrossPlatform.Views
{
    /// <summary>
    /// Edits a copy of a color table; the copy is handed to the callback when the user saves.
    /// </summary>
    public class ChangeColorTableWindow : Window
    {
        private const int CellSize = 20;

        private readonly ChangeColorTableViewModel viewModel;
        private readonly Action<ColorTable> onSave;
        private readonly Image image;
        private readonly Rectangle highlight;

        /// <summary>Only for the XAML designer.</summary>
        public ChangeColorTableWindow()
            : this(new ColorTable(new ushort[ColorTable.ColorCount]), _ => { })
        {
        }

        /// <param name="colorTable">The table to edit; pass a copy, it is changed directly.</param>
        /// <param name="onSave">Called with the edited table when the user saves.</param>
        public ChangeColorTableWindow(ColorTable colorTable, Action<ColorTable> onSave)
        {
            AvaloniaXamlLoader.Load(this);

            viewModel = new ChangeColorTableViewModel(colorTable);
            DataContext = viewModel;
            this.onSave = onSave ?? throw new ArgumentNullException(nameof(onSave));

            Closing += OnClosing;

            image = this.Get<Image>("PaletteImage");
            highlight = this.Get<Rectangle>("PaletteImageHighlight");

            UpdateBitmap();
        }

        public void SaveColorTableChanges()
        {
            onSave(viewModel.ColorTable);
            viewModel.ColorTableChanged = false;
        }

        public void DiscardColorTableChanges()
        {
            viewModel.ColorTableChanged = false;
        }

        private void UpdateBitmap()
        {
            viewModel.Bitmap = BitmapFactory.Create(ColorTableImage.Render(viewModel.ColorTable, CellSize));
        }

        private void MousePressed(object? sender, PointerPressedEventArgs e)
        {
            var position = e.GetPosition(image);
            int index = ColorTableImage.IndexAt(position.X, position.Y, CellSize);
            if (index < 0)
            {
                return;
            }

            Canvas.SetLeft(highlight, index % Palette.ImageColumns * CellSize);
            Canvas.SetTop(highlight, index / Palette.ImageColumns * CellSize);

            viewModel.ColorPositionInColorTable = index;
            Argb1555.Decode(viewModel.ColorTable[index], out byte r, out byte g, out byte b, out _);
            viewModel.SetColor(r, g, b);
            viewModel.ColorSelected = true;
        }

        private void Button_SaveColor(object? sender, RoutedEventArgs e)
        {
            if (!viewModel.ColorSelected)
            {
                return;
            }

            viewModel.ColorTable[viewModel.ColorPositionInColorTable] =
                Argb1555.Encode((byte)viewModel.Red, (byte)viewModel.Green, (byte)viewModel.Blue, byte.MaxValue);
            UpdateBitmap();

            viewModel.ColorTableChanged = true;
        }

        private void Button_SaveColorTable(object? sender, RoutedEventArgs e)
        {
            if (viewModel.ColorTableChanged)
            {
                SaveColorTableChanges();
            }

            Close();
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            if (viewModel.ColorTableChanged)
            {
                e.Cancel = true;
                new ChangeColorTableWindowDialogBox(this).ShowDialog(this);
            }
        }
    }
}

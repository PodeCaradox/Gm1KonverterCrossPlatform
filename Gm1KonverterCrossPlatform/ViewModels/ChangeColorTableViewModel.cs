using System;
using ReactiveUI;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Gm1KonverterCrossPlatform.Core.Files;

namespace Gm1KonverterCrossPlatform.ViewModels
{
	public class ChangeColorTableViewModel : ViewModelBase
    {
        /// <summary>Largest 8 bit value that can be stored in a 5 bit color channel.</summary>
        private const uint MaxChannelValue = 248;

        private WriteableBitmap? bitmap;
        private ColorTable colorTable;

        private bool colorTableChanged = false;
        private bool colorSelected = false;
        private int colorPositionInColorTable;
        private string colorAsText = ""; // rgb hex value
        private uint red = 0;
        private uint green = 0;
        private uint blue = 0;

        public ChangeColorTableViewModel(ColorTable colorTable)
        {
            this.colorTable = colorTable;
        }

        public bool ColorTableChanged
        {
            get => colorTableChanged;
            set
            {
                this.RaiseAndSetIfChanged(ref colorTableChanged, value);
            }
        }

        public bool ColorSelected
        {
            get => colorSelected;
            set
            {
                this.RaiseAndSetIfChanged(ref colorSelected, value);
            }
        }

        public int ColorPositionInColorTable
        {
            get => colorPositionInColorTable;
            set
            {
                this.RaiseAndSetIfChanged(ref colorPositionInColorTable, value);
            }
        }

        public WriteableBitmap? Bitmap
        {
            get => bitmap;
            set
            {
                this.RaiseAndSetIfChanged(ref bitmap, value);
            }
        }

        public ColorTable ColorTable
        {
            get => colorTable;
            set
            {
                this.RaiseAndSetIfChanged(ref colorTable, value);
            }
        }

        public string ColorAsText
        {
            get => colorAsText;
            set
            {
                value = FormatColorHexValue(value);
                if (colorAsText == value) return;

                // Keep the current color until the text is a valid color.
                if (Color.TryParse(value, out Color color))
                {
                    SetColor(color.R, color.G, color.B);
                }
            }
        }

        public uint Red
        {
            get => red;
            set
            {
                value = FormatColorValue(value);
                if (red == value) return;
                this.RaiseAndSetIfChanged(ref red, value);
                UpdateColorHexValue();
            }
        }

        public uint Green
        {
            get => green;
            set
            {
                value = FormatColorValue(value);
                if (green == value) return;
                this.RaiseAndSetIfChanged(ref green, value);
                UpdateColorHexValue();
            }
        }

        public uint Blue
        {
            get => blue;
            set
            {
                value = FormatColorValue(value);
                if (blue == value) return;
                this.RaiseAndSetIfChanged(ref blue, value);
                UpdateColorHexValue();
            }
        }

        public void SetColor(uint r, uint g, uint b)
        {
            red = FormatColorValue(r);
            this.RaisePropertyChanged(nameof(Red));

            green = FormatColorValue(g);
            this.RaisePropertyChanged(nameof(Green));

            blue = FormatColorValue(b);
            this.RaisePropertyChanged(nameof(Blue));

            UpdateColorHexValue();
        }

        private static uint FormatColorValue(uint value)
        {
            return Math.Min(value, MaxChannelValue) / 8 * 8;
        }

        private static string FormatColorHexValue(string? value)
        {
            value ??= string.Empty;
            if (!value.StartsWith("#", StringComparison.Ordinal))
            {
                value = "#" + value;
            }

            return value;
        }

        private void UpdateColorHexValue()
        {
            colorAsText = "#" + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
            this.RaisePropertyChanged(nameof(ColorAsText));
        }
	}
}

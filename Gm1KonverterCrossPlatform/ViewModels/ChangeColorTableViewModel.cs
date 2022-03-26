using ReactiveUI;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Files.Gm1Converter;

namespace Gm1KonverterCrossPlatform.ViewModels
{
	public class ChangeColorTableViewModel : ViewModelBase
    {
        private WriteableBitmap bitmap;
        private ColorTable colorTable;

        private bool colorTableChanged = false;
        private bool colorSelected = false;
        private int colorPositionInColorTable;
        private string colorAsText = ""; // rgb hex value
        private int red = 0;
        private int green = 0;
        private int blue = 0;

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

        public WriteableBitmap Bitmap
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
                try
                {
                    Color color = Color.Parse(value);
                    SetColor(color.R, color.G, color.B);
                }
                catch(System.Exception) { }
            }
        }

        public int Red
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

        public int Green
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

        public int Blue
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

        public void SetColor(int r, int g, int b)
        {
            red = FormatColorValue(r);
            this.RaisePropertyChanged("Red");

            green = FormatColorValue(g);
            this.RaisePropertyChanged("Green");

            blue = FormatColorValue(b);
            this.RaisePropertyChanged("Blue");

            UpdateColorHexValue();
        }

        private int FormatColorValue(int value)
        {
            if (value > 248)
            {
                value = 248;
            }
            else if (value < 0)
            {
                value = 0;
            }

            value = (value / 8) * 8;

            return value;
        }

        private string FormatColorHexValue(string value)
        {
            if (!value.StartsWith("#"))
            {
                value = "#" + value;
            }

            return value;
        }

        private void UpdateColorHexValue()
        {
            colorAsText = "#" + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
            this.RaisePropertyChanged("ColorAsText");
        }
	}
}

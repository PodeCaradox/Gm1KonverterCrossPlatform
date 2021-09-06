using ReactiveUI;
using Avalonia.Media.Imaging;
using Files.Gm1Converter;

namespace Gm1KonverterCrossPlatform.ViewModels
{
	public class ChangeColorTableViewModel : ViewModelBase
    {
        #region Variables

        private WriteableBitmap bitmap;
        private ColorTable colorTable;

        private bool colorTableChanged = false;
        private bool colorSelected = false;
        private int colorPositionInColorTable;
        private string colorAsText = ""; // argb hex value
        private int red = 0;
        private int green = 0;
        private int blue = 0;

        #endregion

        #region Construtor

        public ChangeColorTableViewModel(ColorTable colorTable)
        {
            this.colorTable = colorTable;
        }

        #endregion

        #region GetterSetter

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
                this.RaiseAndSetIfChanged(ref colorAsText, value);
            }
        }

        public int Red
        {
            get => red;
            set
            {
                value = TrimColorValue(value);
                this.RaiseAndSetIfChanged(ref red, value);
                SetColorAsText();
            }
        }

        public int Green
        {
            get => green;
            set
            {
                value = TrimColorValue(value);
                this.RaiseAndSetIfChanged(ref green, value);
                SetColorAsText();
            }
        }

        public int Blue
        {
            get => blue;
            set
            {
                value = TrimColorValue(value);
                this.RaiseAndSetIfChanged(ref blue, value);
                SetColorAsText();
            }
        }

        #endregion

		#region Methods

		private int TrimColorValue(int value)
        {
            if (value > 255)
            {
                value = 255;
            }
            else if (value < 0)
            {
                value = 0;
            }

            return value;
        }

        private void SetColorAsText()
        {
            ColorAsText = "#" + 255.ToString("X2") + red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
        }

		#endregion
	}
}

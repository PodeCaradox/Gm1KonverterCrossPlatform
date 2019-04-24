
using ReactiveUI;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Files.Gm1Converter;

namespace Gm1KonverterCrossPlatform.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        private bool convertButtonEnabled = false;
        public bool ConvertButtonEnabled {

            get => convertButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref convertButtonEnabled, value);
        }

        internal ObservableCollection<Image> images = new ObservableCollection<Image>();
        internal ObservableCollection<Image> TGXImages
        {
            get => images;
            set => this.RaiseAndSetIfChanged(ref images, value);
        }
        internal List<DecodedFile> Files { get; set; } = new List<DecodedFile>();


    }
}

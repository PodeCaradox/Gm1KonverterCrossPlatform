
using ReactiveUI;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gm1KonverterCrossPlatform.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public string Greeting => "Welcome to Avalonia!";

        private bool convertButtonEnabled = false;
        public bool ConvertButtonEnabled {

            get => convertButtonEnabled;
            set => this.RaiseAndSetIfChanged(ref convertButtonEnabled, value);
        }

        private ObservableCollection<Image> images = new ObservableCollection<Image>();
        public ObservableCollection<Image> TGXImages
        {
            get => images;
            set => this.RaiseAndSetIfChanged(ref images, value);
        }


    }
}

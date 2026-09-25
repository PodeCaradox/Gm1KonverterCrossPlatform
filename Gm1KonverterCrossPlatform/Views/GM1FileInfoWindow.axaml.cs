using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Gm1KonverterCrossPlatform.Core.Files;

namespace Gm1KonverterCrossPlatform.Views
{
    public class GM1FileInfoWindow : Window
    {
        /// <summary>Only for the XAML designer.</summary>
        public GM1FileInfoWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public GM1FileInfoWindow(Gm1DataType dataType)
        {
            AvaloniaXamlLoader.Load(this);

            StackPanel animation = this.Get<StackPanel>("Animation");
            animation.IsVisible = false;
            StackPanel interfaceS = this.Get<StackPanel>("Interface");
            interfaceS.IsVisible = false;
            StackPanel tiledObject = this.Get<StackPanel>("TiledObject");
            tiledObject.IsVisible = false;
            StackPanel noInfo = this.Get<StackPanel>("NoInfo");
            noInfo.IsVisible = false;
            
            switch (dataType)
            {
                case Gm1DataType.Animations:
                    animation.IsVisible = true;
                    break;
                case Gm1DataType.Interface:
                    interfaceS.IsVisible = true;
                    break;
                case Gm1DataType.TilesObject:
                    tiledObject.IsVisible = true;
                    break;
                case Gm1DataType.Font:
                    noInfo.IsVisible = true;
                    break;
                case Gm1DataType.NoCompression:
                    noInfo.IsVisible = true;
                    break;
                case Gm1DataType.TgxConstSize:
                    noInfo.IsVisible = true;
                    break;
                case Gm1DataType.NoCompression1:
                    noInfo.IsVisible = true;
                    break;
                default:
                    break;
            }
        }
    }
}

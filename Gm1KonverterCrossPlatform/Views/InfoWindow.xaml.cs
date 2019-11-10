using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Files.Gm1Converter;

namespace Gm1KonverterCrossPlatform.Views
{
    public class InfoWindow : Window
    {

        public InfoWindow()
        {


        }
            public InfoWindow(GM1FileHeader.DataType dataType)
        {
            this.InitializeComponent();
            #if DEBUG
               this.AttachDevTools();
#endif
         
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
                case GM1FileHeader.DataType.Animations:
                    animation.IsVisible = true;
                    break;

                case GM1FileHeader.DataType.Interface:
                    interfaceS.IsVisible = true;
                    break;
                case GM1FileHeader.DataType.TilesObject:
                    tiledObject.IsVisible = true;
                    break;
                case GM1FileHeader.DataType.Font:
                    noInfo.IsVisible = true;
                    break;
                case GM1FileHeader.DataType.NOCompression:
                    noInfo.IsVisible = true;
                    break;
                case GM1FileHeader.DataType.TGXConstSize:
                    noInfo.IsVisible = true;
                    break;
                case GM1FileHeader.DataType.NOCompression1:
                    noInfo.IsVisible = true;
                    break;
                default:
                    break;
            }


        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}

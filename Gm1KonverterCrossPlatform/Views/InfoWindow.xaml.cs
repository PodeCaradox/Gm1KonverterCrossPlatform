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
            Avalonia.Media.Imaging.Bitmap bitmap;
            Image image;
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

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/Palette1.png");
                    image = this.Get<Image>("Palette1");
                    image.Source = bitmap;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/Palette2.png");
                    image = this.Get<Image>("Palette2");
                    image.Source = bitmap;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/Palette3.png");
                    image = this.Get<Image>("Palette3");
                    image.Source = bitmap;

                    break;

                case GM1FileHeader.DataType.Interface:
                    interfaceS.IsVisible = true;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/Interface1.JPG");
                    image = this.Get<Image>("Interface1");
                    image.Source = bitmap;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/Interface2.JPG");
                    image = this.Get<Image>("Interface2");
                    image.Source = bitmap;

                    break;
                case GM1FileHeader.DataType.TilesObject:
                    tiledObject.IsVisible = true;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/TiledObject1.png");
                    image = this.Get<Image>("TiledObject1");
                    image.Source = bitmap;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/TiledObject2.png");
                    image = this.Get<Image>("TiledObject2");
                    image.Source = bitmap;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/TiledObject3.png");
                    image = this.Get<Image>("TiledObject3");
                    image.Source = bitmap;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/TiledObject4.png");
                    image = this.Get<Image>("TiledObject4");
                    image.Source = bitmap;

                    bitmap = new Avalonia.Media.Imaging.Bitmap("Images/TiledObject5.png");
                    image = this.Get<Image>("TiledObject5");
                    image.Source = bitmap;

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

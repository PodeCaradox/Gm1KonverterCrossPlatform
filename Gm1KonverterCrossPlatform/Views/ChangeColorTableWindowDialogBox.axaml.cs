using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace Gm1KonverterCrossPlatform.Views
{
    public class ChangeColorTableWindowDialogBox : Window
    {
        private readonly ChangeColorTableWindow changeColorTableWindow;

        public ChangeColorTableWindowDialogBox() { }

        public ChangeColorTableWindowDialogBox(ChangeColorTableWindow changeColorTableWindow)
        {
            AvaloniaXamlLoader.Load(this);

            this.changeColorTableWindow = changeColorTableWindow;
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Save(object sender, RoutedEventArgs e)
        {
            changeColorTableWindow.SaveColorTableChanges();
            changeColorTableWindow.Close();
            Close();
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            changeColorTableWindow.DiscardColorTableChanges();
            changeColorTableWindow.Close();
            Close();
        }
    }
}

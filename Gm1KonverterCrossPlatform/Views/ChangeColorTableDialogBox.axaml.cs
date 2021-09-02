using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace Gm1KonverterCrossPlatform.Views
{
    public class ChangeColorTableDialogBox : Window
    {
        private readonly ChangeColorTable changeColorTable;

        public ChangeColorTableDialogBox() { }

        public ChangeColorTableDialogBox(ChangeColorTable changeColorTable)
        {
            AvaloniaXamlLoader.Load(this);

            this.changeColorTable = changeColorTable;
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Save(object sender, RoutedEventArgs e)
        {
            changeColorTable.SaveColorTableChanges();
            changeColorTable.Close();
            Close();
        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            changeColorTable.DiscardColorTableChanges();
            changeColorTable.Close();
            Close();
        }
    }
}

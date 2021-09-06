using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Gm1KonverterCrossPlatform.Views
{
    public class MessageBoxWindow : Window
    {
        public enum MessageTyp { Fehler, Info }

        public MessageBoxWindow() { }

        public MessageBoxWindow(MessageTyp typ, string message)
        {
            AvaloniaXamlLoader.Load(this);

            this.Title = typ.ToString();
            var textBox = this.Get<TextBlock>("textBox");
            textBox.Text = message;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Gm1KonverterCrossPlatform.HelperClasses.Views
{
    public class MessageBoxWindow : Window
    {
        public MessageBoxWindow()
        {

        }

        public enum MessageTyp { Fehler, Info }

        public MessageBoxWindow(MessageTyp typ, string message)
        {
            this.InitializeComponent();

            this.Title = typ.ToString();
            var textBox = this.Get<TextBlock>("textBox");
            textBox.Text = message;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

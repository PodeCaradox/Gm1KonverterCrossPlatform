using Avalonia;
using Avalonia.Markup.Xaml;

namespace Gm1KonverterCrossPlatform.Views
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

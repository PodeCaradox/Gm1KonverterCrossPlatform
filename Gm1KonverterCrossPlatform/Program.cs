using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Gm1KonverterCrossPlatform.Core.Diagnostics;
using Gm1KonverterCrossPlatform.HelperClasses;
using Gm1KonverterCrossPlatform.ViewModels;
using Gm1KonverterCrossPlatform.Views;

namespace Gm1KonverterCrossPlatform
{
    internal static class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            Logger.Directory = System.IO.Path.Combine(Config.LocalAppDataPath, "Logs");
            AppDomain.CurrentDomain.FirstChanceException += Logger.LogFirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => Logger.LogException(e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Logger.LogException(e.Exception);
                e.SetObserved();
            };

            BuildAvaloniaApp().Start(AppMain, args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect();

        private static void AppMain(Application app, string[] args)
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };

            app.Run(window);
        }
    }
}

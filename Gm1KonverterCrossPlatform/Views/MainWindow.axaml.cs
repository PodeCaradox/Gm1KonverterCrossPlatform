using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gm1KonverterCrossPlatform.Core.Diagnostics;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Core.Settings;
using Gm1KonverterCrossPlatform.HelperClasses;
using Gm1KonverterCrossPlatform.ViewModels;

namespace Gm1KonverterCrossPlatform.Views
{
    /// <summary>
    /// Forwards user actions to the <see cref="MainWindowViewModel"/>. Every action runs through
    /// <see cref="Run(Action)"/>, so an error is shown to the user instead of closing the program.
    /// </summary>
    public class MainWindow : Window
    {
        private static readonly Cursor WaitCursor = new Cursor(StandardCursorType.Wait);
        private static readonly Cursor ArrowCursor = new Cursor(StandardCursorType.Arrow);

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += (sender, e) => Run(() => ViewModel?.Initialize());
        }

        private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

        private MainWindowViewModel RequiredViewModel => ViewModel ?? throw new InvalidOperationException("The view model is not set.");

        private void CreatenewGM1(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.InstallGm1File());

        private void ReplacewithSavedGM1File(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.RestoreGm1File());

        private void CreatenewTgx(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.InstallTgxFile());

        private void ReplacewithSavedTgxFile(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.RestoreTgxFile());

        private void ExportColortable(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ExportColorTables());

        private void ImportColortable(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ImportColorTables());

        private void ExportImages(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ExportImages());

        private void ImportImages(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ImportImages());

        private void ExportBigImage(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ExportBigImage());

        private void ImportBigImage(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ImportBigImage());

        private void ExportTgxImage(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ExportTgxImage());

        private void ImportTgxImage(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ImportTgxImage());

        private void ExportOrginalStrongholdAnimation(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ExportOriginalAnimation());

        private void ImportOrginalStrongholdAnimation(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ImportOriginalAnimation());

        private void OpenLogFile(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.OpenLogFolder());

        private void OpenWorkFolder(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.OpenWorkFolder());

        private void OpenStrongholdFolder(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.OpenStrongholdFolder());

        private void Button_ClickPalleteminus(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ChangeColorTable(-1));

        private void Button_ClickPalleteplus(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ChangeColorTable(1));

        private void Button_ChangeOffset(object? sender, RoutedEventArgs e) => Run(() => RequiredViewModel.ChangeSelectedOffset());

        private void OpenWorkfolderDirectory(object? sender, RoutedEventArgs e)
        {
            if ((sender as ListBox)?.SelectedItem is string folderName)
            {
                Run(() => RequiredViewModel.OpenWorkFolderEntry(folderName));
            }
        }

        private void SelectedGm1File(object? sender, SelectionChangedEventArgs e)
        {
            // The selection is cleared when the file list is reloaded.
            if ((sender as ListBox)?.SelectedItem is string fileName)
            {
                Run(() => RequiredViewModel.OpenGm1File(fileName));
            }
        }

        private void SelectedGfxFile(object? sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox)?.SelectedItem is string fileName)
            {
                Run(() => RequiredViewModel.OpenTgxFile(fileName));
            }
        }

        private void TGXImageChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = e.AddedItems.OfType<ImagePreviewItem>().LastOrDefault()
                ?? (sender as ListBox)?.SelectedItem as ImagePreviewItem;
            Run(() => RequiredViewModel.SelectImage(item));
        }

        private void ChangeLanguage(object? sender, RoutedEventArgs e)
        {
            // The click of the parent menu item (showing the current language) arrives here as well.
            if ((e.Source as MenuItem)?.Header is Language language)
            {
                Run(() => RequiredViewModel.ActualLanguage = language);
            }
        }

        private void ChangeColorTheme(object? sender, RoutedEventArgs e)
        {
            if ((e.Source as MenuItem)?.Header is ColorTheme colorTheme)
            {
                Run(() => RequiredViewModel.ActualColorTheme = colorTheme);
            }
        }

        private void OpenInfoWindow(object? sender, RoutedEventArgs e)
        {
            var document = ViewModel?.Gm1Document;
            if (document != null)
            {
                Run(() => new GM1FileInfoWindow(document.DataType).Show(this));
            }
        }

        private void Button_ClickChangeColorTable(object? sender, RoutedEventArgs e)
        {
            Run(() =>
            {
                var window = new ChangeColorTableWindow(
                    RequiredViewModel.CopyCurrentColorTable(),
                    colorTable => Run(() => RequiredViewModel.ReplaceCurrentColorTable(colorTable)));
                window.ShowDialog(this);
            });
        }

        private void Button_ClickGifExporter(object? sender, RoutedEventArgs e)
        {
            var selectedItems = this.Get<ListBox>("TGXImageListBox").SelectedItems?.OfType<ImagePreviewItem>().ToList()
                ?? new List<ImagePreviewItem>();
            Run(() => RequiredViewModel.ExportGif(selectedItems));
        }

        private async void ChangeCrusaderfolder(object? sender, RoutedEventArgs e)
        {
            await RunAsync(async () =>
            {
                string? folder = await SelectFolderAsync(Localization.GetText("StrongholdFolder"), RequiredViewModel.CrusaderPath);
                if (folder != null)
                {
                    RequiredViewModel.SetCrusaderPath(folder);
                }
            });
        }

        private async void ChangeWorkfolder(object? sender, RoutedEventArgs e)
        {
            await RunAsync(async () =>
            {
                string? folder = await SelectFolderAsync(Localization.GetText("Workfolder"), RequiredViewModel.WorkFolderPath);
                if (folder != null)
                {
                    RequiredViewModel.SetWorkFolderPath(folder);
                }
            });
        }

        private async void ImportOffsetsFromFile(object? sender, RoutedEventArgs e)
        {
            await RunAsync(async () =>
            {
                string? path = RequiredViewModel.ExistingOffsetsFile ?? await SelectOffsetFileAsync();
                if (path != null)
                {
                    RequiredViewModel.ApplyOffsetsFromFile(path);
                }
            });
        }

        private async Task<string?> SelectFolderAsync(string title, string? initialDirectory)
        {
            var dialog = new OpenFolderDialog { Title = title };
            if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
            {
                dialog.Directory = initialDirectory;
            }

            string? folder = await dialog.ShowAsync(this);
            return string.IsNullOrEmpty(folder) ? null : folder;
        }

        private async Task<string?> SelectOffsetFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Offset File",
                AllowMultiple = false,
                Directory = RequiredViewModel.WorkFolderPath
            };
            dialog.Filters.Add(new FileDialogFilter { Name = "Offsets", Extensions = { "json" } });

            string[]? paths = await dialog.ShowAsync(this);
            return paths != null && paths.Length > 0 ? paths[0] : null;
        }

        private void Run(Action action)
        {
            _ = RunAsync(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }

        /// <summary>Runs a user action with a wait cursor and shows any error in a message box.</summary>
        private async Task RunAsync(Func<Task> action)
        {
            Exception? error = null;
            Cursor = WaitCursor;
            try
            {
                await action();
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                Cursor = ArrowCursor;
            }

            if (error != null)
            {
                await ShowErrorAsync(error);
            }
        }

        private async Task ShowErrorAsync(Exception error)
        {
            string message;
            if (error is WorkflowException || error is InvalidDataException)
            {
                Logger.Log(error.ToString());
                message = error.Message;
            }
            else
            {
                Logger.LogException(error);
                message = $"{Localization.GetText("SomethingWentWrong")}{Environment.NewLine}{Environment.NewLine}Error:{Environment.NewLine}{error.Message}";
            }

            try
            {
                await new MessageBoxWindow(MessageBoxWindow.MessageTyp.Info, message).ShowDialog(this);
            }
            catch (Exception e)
            {
                Logger.LogException(e);
            }
        }
    }
}

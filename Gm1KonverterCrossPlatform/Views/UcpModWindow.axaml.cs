using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Gm1KonverterCrossPlatform.Core.Diagnostics;
using Gm1KonverterCrossPlatform.Core.Services;
using Gm1KonverterCrossPlatform.Core.Ucp;
using Gm1KonverterCrossPlatform.HelperClasses;
using Gm1KonverterCrossPlatform.ViewModels;

namespace Gm1KonverterCrossPlatform.Views
{
    /// <summary>Name, author and version of the UCP3 mod that receives the modified files.</summary>
    public class UcpModWindow : Window
    {
        private readonly MainWindowViewModel? viewModel;
        private readonly TextBox nameBox;
        private readonly TextBox authorBox;
        private readonly TextBox versionBox;
        private readonly TextBlock folderPreview;
        private readonly TextBlock statusText;

        /// <summary>Only for the XAML designer.</summary>
        public UcpModWindow()
        {
            AvaloniaXamlLoader.Load(this);
            nameBox = this.Get<TextBox>("NameBox");
            authorBox = this.Get<TextBox>("AuthorBox");
            versionBox = this.Get<TextBox>("VersionBox");
            folderPreview = this.Get<TextBlock>("FolderPreview");
            statusText = this.Get<TextBlock>("StatusText");
        }

        public UcpModWindow(MainWindowViewModel viewModel)
            : this()
        {
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            var mod = viewModel.UcpMod;
            nameBox.Text = mod.DisplayName;
            authorBox.Text = mod.Author;
            versionBox.Text = mod.Version;

            foreach (var box in new[] { nameBox, versionBox })
            {
                box.PropertyChanged += (sender, e) =>
                {
                    if (e.Property == TextBox.TextProperty)
                    {
                        UpdateFolderPreview();
                    }
                };
            }

            UpdateFolderPreview();
        }

        /// <summary>Shows the folder the mod will be written to, or why the input is not valid.</summary>
        private void UpdateFolderPreview()
        {
            var mod = UcpExtensionInfo.FromUserInput(nameBox.Text, authorBox.Text, versionBox.Text);
            folderPreview.Text = $"{UcpFolder.FolderName}/{UcpFolder.PluginsFolderName}/{mod.FolderName}";

            if (!string.IsNullOrWhiteSpace(versionBox.Text) && !UcpExtensionInfo.IsValidVersion(versionBox.Text))
            {
                ShowStatus(Localization.GetText("UcpModInvalidVersion"), isError: true);
            }
            else if (viewModel != null && !viewModel.IsUcpInstalled)
            {
                ShowStatus(Localization.GetText("UcpNotInstalled"), isError: false);
            }
            else
            {
                statusText.IsVisible = false;
            }
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            if (TryRun(() => viewModel?.SetUcpMod(nameBox.Text, authorBox.Text, versionBox.Text)))
            {
                Close();
            }
        }

        private void OpenFolder_Click(object? sender, RoutedEventArgs e) => TryRun(() => viewModel?.OpenUcpModFolder());

        private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();

        private bool TryRun(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (WorkflowException error)
            {
                ShowStatus(error.Message, isError: true);
            }
            catch (Exception error)
            {
                Logger.LogException(error);
                ShowStatus($"{Localization.GetText("SomethingWentWrong")}{Environment.NewLine}{error.Message}", isError: true);
            }

            return false;
        }

        private void ShowStatus(string message, bool isError)
        {
            statusText.Text = message;
            statusText.Foreground = isError ? Brushes.Firebrick : folderPreview.Foreground;
            statusText.IsVisible = true;
        }
    }
}

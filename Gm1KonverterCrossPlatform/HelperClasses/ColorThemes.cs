using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Gm1KonverterCrossPlatform.Core.Settings;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    public static class ColorThemes
    {
        private static readonly Uri BaseUri = new Uri("avares://Gm1KonverterCrossPlatform/Views/App.axaml");

        private static readonly Dictionary<ColorTheme, string> ColorThemeSources = new Dictionary<ColorTheme, string>
        {
            { ColorTheme.Light, "avares://Avalonia.Themes.Default/Accents/BaseLight.xaml" },
            { ColorTheme.Dark, "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml" },
        };

        private static StyleInclude? selectedColorThemeStyle;

        public static ColorTheme[] All { get; } = { ColorTheme.Light, ColorTheme.Dark };

        /// <summary>Change the application color theme.</summary>
        public static void SelectColorTheme(ColorTheme colorTheme)
        {
            if (!ColorThemeSources.TryGetValue(colorTheme, out string? source))
            {
                source = ColorThemeSources[ColorTheme.Light];
            }

            var appStyles = Application.Current!.Styles;
            if (selectedColorThemeStyle != null)
            {
                appStyles.Remove(selectedColorThemeStyle);
            }

            selectedColorThemeStyle = new StyleInclude(BaseUri) { Source = new Uri(source) };
            appStyles.Add(selectedColorThemeStyle);
        }
    }
}

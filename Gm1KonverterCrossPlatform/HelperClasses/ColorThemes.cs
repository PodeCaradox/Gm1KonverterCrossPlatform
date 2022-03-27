using System.Collections.Generic;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
	public static class ColorThemes
	{
        private static ColorTheme SelectedColorTheme;
        private static StyleInclude SelectedColorThemeStyle;

        /// <summary>
        /// Default ColorTheme to be used if none is set.
        /// </summary>
        public static readonly ColorTheme DefaultColorTheme = ColorTheme.Light;

        /// <summary>
        /// List of supported color themes.
        /// </summary>
        public enum ColorTheme { Light, Dark };

        private static readonly Dictionary<ColorTheme, string> ColorThemeSources = new Dictionary<ColorTheme, string>
        {
            { ColorTheme.Light, "avares://Avalonia.Themes.Default/Accents/BaseLight.xaml" },
            { ColorTheme.Dark, "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml" },
        };

        /// <summary>
        /// Change application ColorTheme.
        /// </summary>
        public static void SelectColorTheme(ColorTheme colorTheme)
        {
            /*if (colorTheme == SelectedColorTheme)
            {
                return;
            }*/

            Styles appStyles = Application.Current.Styles;

            // remove old style if exists
            if (SelectedColorThemeStyle != null)
            {
                appStyles.Remove(SelectedColorThemeStyle);
            }

            // create new style
            string source = ColorThemeSources[colorTheme];

            SelectedColorThemeStyle = new StyleInclude(new System.Uri("avares://Gm1KonverterCrossPlatform/Wiews/App.axaml"))
            {
                Source = new System.Uri(source)
            };

            // apply new style
            appStyles.Add(SelectedColorThemeStyle);

            SelectedColorTheme = colorTheme;
        }
    }
}

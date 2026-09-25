using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Gm1KonverterCrossPlatform.Core.Settings;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    public static class Languages
    {
        private static readonly Dictionary<Language, string> LanguageSources = new Dictionary<Language, string>
        {
            { Language.English, "avares://Gm1KonverterCrossPlatform/Languages/Language.en_US.xaml" },
            { Language.Deutsch, "avares://Gm1KonverterCrossPlatform/Languages/Language.de_DE.xaml" },
            { Language.Русский, "avares://Gm1KonverterCrossPlatform/Languages/Language.ru_RU.xaml" }
        };

        private static ResourceInclude? selectedLanguageDictionary;

        /// <summary>Languages in the order they are shown in the menu.</summary>
        public static Language[] All { get; } = { Language.Deutsch, Language.English, Language.Русский };

        /// <summary>Change the application language.</summary>
        public static void SelectLanguage(Language language)
        {
            if (!LanguageSources.TryGetValue(language, out string? source))
            {
                source = LanguageSources[Language.English];
            }

            var appDictionaries = Application.Current!.Resources.MergedDictionaries;
            if (selectedLanguageDictionary != null)
            {
                appDictionaries.Remove(selectedLanguageDictionary);
            }

            selectedLanguageDictionary = new ResourceInclude { Source = new Uri(source) };
            appDictionaries.Add(selectedLanguageDictionary);
        }
    }
}

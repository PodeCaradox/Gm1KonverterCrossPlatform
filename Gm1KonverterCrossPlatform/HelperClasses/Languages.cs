using Avalonia;
using Avalonia.Markup.Xaml.MarkupExtensions;
using System.Collections.Generic;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
	public static class Languages
	{
        private static Language SelectedLanguage;
        private static ResourceInclude SelectedLanguageDictionary;

        /// <summary>
        /// Default Language to be used if none is set.
        /// </summary>
        public static readonly Language DefaultLanguage = Language.English;

        /// <summary>
        /// List of supported languages.
        /// </summary>
        public enum Language { English, Deutsch, Русский };

        private static readonly Dictionary<Language, string> LanguageSources = new Dictionary<Language, string>
        {
            { Language.English, "avares://Gm1KonverterCrossPlatform/Languages/Language.en_US.xaml" },
            { Language.Deutsch, "avares://Gm1KonverterCrossPlatform/Languages/Language.de_DE.xaml" },
            { Language.Русский, "avares://Gm1KonverterCrossPlatform/Languages/Language.ru_RU.xaml" }
        };

        /// <summary>
        /// Change application Language.
        /// </summary>
        public static void SelectLanguage(Language language)
        {
            /*if (language == SelectedLanguage)
            {
                return;
            }*/

            var appDictionaries = Application.Current.Resources.MergedDictionaries;

            // remove old dictionary if exists
            if (SelectedLanguageDictionary != null)
            {
                appDictionaries.Remove(SelectedLanguageDictionary);
            }

            // create new dictionary
            string source = LanguageSources[language];

            SelectedLanguageDictionary = new ResourceInclude() {
                Source = new System.Uri(source)
            };

            // apply new dictionary
            appDictionaries.Add(SelectedLanguageDictionary);

            SelectedLanguage = language;
        }
    }
}

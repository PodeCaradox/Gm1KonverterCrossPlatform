using Avalonia;
using Avalonia.Controls;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    /// <summary>Access to the translated texts of the language files.</summary>
    internal static class Localization
    {
        /// <summary>The text for <paramref name="key"/> in the current language, or the key itself if it is missing.</summary>
        public static string GetText(string key)
        {
            var application = Application.Current;
            if (application != null && application.TryFindResource(key, out object? value) && value != null)
            {
                return value.ToString() ?? key;
            }

            return key;
        }
    }
}

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>
    /// Name and version of a UCP3 extension (plugin or module). UCP3 finds extensions by their folder
    /// name <c>&lt;name&gt;-&lt;version&gt;</c> and only accepts letters, digits and '-' in the name.
    /// </summary>
    public sealed class UcpExtensionInfo
    {
        public const string DefaultName = "Gm1Konverter-Textures";
        public const string DefaultVersion = "1.0.0";

        private static readonly Regex NamePattern = new Regex("^[A-Za-z0-9]+(-[A-Za-z0-9]+)*$", RegexOptions.CultureInvariant);
        private static readonly Regex VersionPattern = new Regex(@"^\d{1,9}\.\d{1,9}\.\d{1,9}$", RegexOptions.CultureInvariant);

        /// <exception cref="ArgumentException">The name or version cannot be used as UCP3 folder name.</exception>
        public UcpExtensionInfo(string name, string version, string? displayName = null, string? author = null)
        {
            if (!IsValidName(name)) throw new ArgumentException($"\"{name}\" is not a valid UCP extension name.", nameof(name));
            if (!IsValidVersion(version)) throw new ArgumentException($"\"{version}\" is not a version like 1.0.0.", nameof(version));

            Name = name;
            Version = version;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName!.Trim();
            Author = author?.Trim() ?? string.Empty;
        }

        /// <summary>The identifier UCP3 uses in dependencies and in the folder name.</summary>
        public string Name { get; }

        public string Version { get; }

        /// <summary>The name shown in the UCP3 GUI.</summary>
        public string DisplayName { get; }

        public string Author { get; }

        public string FolderName => $"{Name}-{Version}";

        /// <summary>
        /// Creates the info from user input: the name is derived from <paramref name="displayName"/> and an
        /// invalid version falls back to <see cref="DefaultVersion"/>.
        /// </summary>
        public static UcpExtensionInfo FromUserInput(string? displayName, string? author, string? version)
        {
            string name = ToExtensionName(displayName);
            string validVersion = IsValidVersion(version) ? version!.Trim() : DefaultVersion;
            return new UcpExtensionInfo(name, validVersion, string.IsNullOrWhiteSpace(displayName) ? name : displayName, author);
        }

        /// <summary>
        /// Turns any text into a valid extension name: other characters become '-', umlauts are transcribed.
        /// Returns <see cref="DefaultName"/> if nothing usable is left.
        /// </summary>
        public static string ToExtensionName(string? text)
        {
            var name = new StringBuilder();
            foreach (char c in (text ?? string.Empty).Normalize(NormalizationForm.FormKC))
            {
                string? replacement = Transcribe(c);
                if (replacement != null)
                {
                    name.Append(replacement);
                }
                else if (IsAsciiLetterOrDigit(c))
                {
                    name.Append(c);
                }
                else if (name.Length > 0 && name[name.Length - 1] != '-')
                {
                    name.Append('-');
                }
            }

            string result = name.ToString().Trim('-');
            return result.Length == 0 ? DefaultName : result;
        }

        public static bool IsValidName(string? name) => name != null && NamePattern.IsMatch(name);

        public static bool IsValidVersion(string? version) => version != null && VersionPattern.IsMatch(version.Trim());

        private static bool IsAsciiLetterOrDigit(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');

        private static string? Transcribe(char c)
        {
            switch (c)
            {
                case 'ä': return "ae";
                case 'ö': return "oe";
                case 'ü': return "ue";
                case 'Ä': return "Ae";
                case 'Ö': return "Oe";
                case 'Ü': return "Ue";
                case 'ß': return "ss";
                default: return null;
            }
        }
    }
}

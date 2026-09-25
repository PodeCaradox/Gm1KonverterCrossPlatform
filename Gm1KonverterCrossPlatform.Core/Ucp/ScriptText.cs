using System.Text;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>Quoting of user text for the generated YAML and Lua files.</summary>
    internal static class ScriptText
    {
        /// <summary>A double quoted YAML scalar. Control characters are dropped.</summary>
        public static string YamlString(string text) => "\"" + Escape(text) + "\"";

        /// <summary>A double quoted Lua string literal. Control characters are dropped.</summary>
        public static string LuaString(string text) => "\"" + Escape(text) + "\"";

        /// <summary>Text for a single Markdown line. Control characters are dropped.</summary>
        public static string SingleLine(string text)
        {
            var result = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (!char.IsControl(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        // Backslash and double quote are escaped the same way in YAML and Lua double quoted strings.
        private static string Escape(string text)
        {
            var result = new StringBuilder(text.Length + 2);
            foreach (char c in text)
            {
                if (c == '\\' || c == '"')
                {
                    result.Append('\\').Append(c);
                }
                else if (!char.IsControl(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}

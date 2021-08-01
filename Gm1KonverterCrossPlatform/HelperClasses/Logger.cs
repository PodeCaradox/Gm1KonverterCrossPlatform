using System;
using System.IO;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    internal static class Logger
    {
        public static bool Loggeractiv = false;
        public static string Path = "";
        public static void Log(string text)
        {
            if (!Loggeractiv) return;
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }

            var fileName = "Log " + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            var writer = File.AppendText(System.IO.Path.Combine(Path, fileName));

            writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteLine(text + Environment.NewLine);
            writer.Dispose();
            writer.Close();
        }
    }
}

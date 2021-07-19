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
            DateTime localDate = DateTime.Now;

            var fileName = "LogFile from " + localDate.Year + "." + localDate.Month + "." + localDate.Day + ".txt";
            var writer = File.AppendText(System.IO.Path.Combine(Path, fileName));
            writer.WriteLine(text);
            writer.Dispose();
            writer.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    internal static class Logger
    {
        public static bool Loggeractiv = false;
        public static string Path = "";
        public static void Log(String text)
        {
            if (!Loggeractiv) return;
            if (!Directory.Exists(Path+ "\\Logger"))
            {
                Directory.CreateDirectory(Path + "\\Logger");
            }
            var writer = File.AppendText(Path + "\\Logger\\Log.txt");
            writer.WriteLine(text);
            writer.Dispose();
            writer.Close();


        }



    }
}

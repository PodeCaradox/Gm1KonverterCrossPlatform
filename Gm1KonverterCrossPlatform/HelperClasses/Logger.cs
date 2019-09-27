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
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path );
            }
            DateTime localDate = DateTime.Now;
           
             var writer = File.AppendText(Path + "\\LogFile from "+ localDate.Year +"." + localDate.Month + "." + localDate.Day   + ".txt");
            writer.WriteLine(text);
            writer.Dispose();
            writer.Close();


        }



    }
}

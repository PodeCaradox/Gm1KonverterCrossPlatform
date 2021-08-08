using System;
using System.IO;
using System.Text;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    internal static class Logger
    {
        public static bool Loggeractiv = false;
        public static readonly string Path = System.IO.Path.Combine(Config.LocalAppDataPath, "Logs");

        public static void Log(string text)
        {
            if (!Loggeractiv) return;
            string fileName = "Log " + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            WriteText(fileName, text);
        }

        public static void LogException(Exception e)
        {
            string fileName = "ErrorLog " + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

            StringBuilder stringBuilder = new StringBuilder();

            Exception exception = e;

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            WriteText(fileName, stringBuilder.ToString());
        }

        internal static void LogFirstChanceException(object source, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            LogException(e.Exception);
        }

        private static void WriteText(string fileName, string text)
        {
            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }

            StreamWriter writer = File.AppendText(System.IO.Path.Combine(Path, fileName));
            writer.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            writer.WriteLine(text + Environment.NewLine);
            writer.Dispose();
            writer.Close();
        }
    }
}

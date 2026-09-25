using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Gm1KonverterCrossPlatform.Core.Diagnostics
{
    /// <summary>
    /// Writes log files into <see cref="Directory"/>. Logging never throws: a failure to write
    /// (locked file, missing permissions) is ignored so it can never crash the program.
    /// </summary>
    public static class Logger
    {
        private static readonly object WriteLock = new object();

        [ThreadStatic]
        private static bool isWriting;

        /// <summary>Enables <see cref="Log"/>. Errors are always logged.</summary>
        public static bool IsEnabled { get; set; }

        public static string Directory { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gm1ConverterCrossPlatform", "Logs");

        public static void Log(string text)
        {
            if (!IsEnabled) return;
            WriteText("Log", text);
        }

        public static void LogException(Exception exception)
        {
            if (exception == null) return;

            var builder = new StringBuilder();
            for (var current = exception; current != null; current = current.InnerException)
            {
                builder.AppendLine(current.GetType().FullName + ": " + current.Message);
                builder.AppendLine(current.StackTrace);
            }

            WriteText("ErrorLog", builder.ToString());
        }

        /// <summary>Handler for <see cref="AppDomain.FirstChanceException"/>, only active while logging is enabled.</summary>
        public static void LogFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (!IsEnabled) return;
            WriteText("Log", "First chance exception: " + e.Exception.GetType().FullName + ": " + e.Exception.Message);
        }

        private static void WriteText(string prefix, string text)
        {
            // Writing can throw, which raises another first chance exception; never recurse.
            if (isWriting) return;

            isWriting = true;
            try
            {
                lock (WriteLock)
                {
                    System.IO.Directory.CreateDirectory(Directory);
                    string file = Path.Combine(Directory, $"{prefix} {DateTime.Now:yyyy-MM-dd}.txt");
                    File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{Thread.CurrentThread.ManagedThreadId}]{Environment.NewLine}{text}{Environment.NewLine}{Environment.NewLine}");
                }
            }
            catch (Exception)
            {
                // A logger must never crash the program.
            }
            finally
            {
                isWriting = false;
            }
        }
    }
}

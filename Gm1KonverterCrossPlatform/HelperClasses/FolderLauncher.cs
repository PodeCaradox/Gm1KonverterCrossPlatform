using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
    /// <summary>Opens a folder in the file manager of the operating system.</summary>
    internal static class FolderLauncher
    {
        public static void Open(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("No folder given.", nameof(folder));

            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo(FileManager, Quote(folder)) { UseShellExecute = false })?.Dispose();
        }

        private static string FileManager
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "explorer.exe";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "open";
                return "xdg-open";
            }
        }

        private static string Quote(string path) => "\"" + path.Replace("\"", "\\\"") + "\"";
    }
}

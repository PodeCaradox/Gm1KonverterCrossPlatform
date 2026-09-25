using System;
using System.IO;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
	internal static class Config
	{
		public const string AppName = "Gm1ConverterCrossPlatform";

		// Without SpecialFolderOption.Create an empty string is returned on Linux if the folder does not exist yet,
		// which would put the files relative to the working directory.
		public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), AppName);
		public static readonly string LocalAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), AppName);
	}
}

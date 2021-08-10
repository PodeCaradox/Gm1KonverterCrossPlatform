using System;
using System.IO;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
	internal static class Config
	{
		public const string AppName = "Gm1ConverterCrossPlatform";

		public static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
		public static readonly string LocalAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
	}
}

using System;
using System.IO;

namespace Gm1KonverterCrossPlatform.HelperClasses
{
	internal static class Config
	{
		internal const string AppName = "Gm1ConverterCrossPlatform";

		internal static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
		internal static readonly string LocalAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
	}
}

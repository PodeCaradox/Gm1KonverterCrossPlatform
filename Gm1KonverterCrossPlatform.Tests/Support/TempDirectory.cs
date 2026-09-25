using System;
using System.IO;

namespace Gm1KonverterCrossPlatform.Tests.Support
{
    /// <summary>
    /// A unique directory below the system temp folder that is deleted on <see cref="Dispose"/>.
    /// </summary>
    internal sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string Combine(params string[] parts)
        {
            var all = new string[parts.Length + 1];
            all[0] = Path;
            Array.Copy(parts, 0, all, 1, parts.Length);
            return System.IO.Path.Combine(all);
        }

        /// <summary>Writes a file (creating its directory) and returns its path.</summary>
        public string WriteFile(string relativePath, byte[] content)
        {
            string path = Combine(relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, content);
            return path;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best effort, a leftover temp directory must not fail a test.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}

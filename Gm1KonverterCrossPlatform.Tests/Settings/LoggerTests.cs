using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gm1KonverterCrossPlatform.Core.Diagnostics;
using Gm1KonverterCrossPlatform.Tests.Support;
using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Settings
{
    /// <summary>
    /// <see cref="Logger"/> is static, so its tests must not run in parallel with each other or other tests.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class LoggerCollection
    {
        public const string Name = "Logger (static state)";
    }

    [Collection(LoggerCollection.Name)]
    public class LoggerTests : IDisposable
    {
        private readonly TempDirectory temp = new TempDirectory();
        private readonly string previousDirectory;
        private readonly bool previousIsEnabled;

        public LoggerTests()
        {
            previousDirectory = Logger.Directory;
            previousIsEnabled = Logger.IsEnabled;
            Logger.Directory = temp.Combine("logs");
            Logger.IsEnabled = false;
        }

        public void Dispose()
        {
            Logger.Directory = previousDirectory;
            Logger.IsEnabled = previousIsEnabled;
            temp.Dispose();
        }

        [Fact]
        public void Log_Disabled_WritesNothing()
        {
            Logger.Log("hello");

            Assert.Empty(LogFiles("*"));
        }

        [Fact]
        public void Log_Enabled_AppendsTextToDailyLogFile()
        {
            Logger.IsEnabled = true;

            Logger.Log("first entry");
            Logger.Log("second entry");

            string file = Assert.Single(LogFiles("Log"));
            Assert.Matches(@"^Log \d{4}-\d{2}-\d{2}\.txt$", Path.GetFileName(file));
            string text = File.ReadAllText(file);
            Assert.True(text.IndexOf("first entry", StringComparison.Ordinal) < text.IndexOf("second entry", StringComparison.Ordinal));
            Assert.Empty(LogFiles("ErrorLog"));
        }

        [Fact]
        public void LogException_Disabled_StillWritesErrorLog()
        {
            var exception = Thrown(new InvalidOperationException("outer message", Thrown(new ArgumentException("inner message"))));

            Logger.LogException(exception);

            string text = File.ReadAllText(Assert.Single(LogFiles("ErrorLog")));
            Assert.Contains("System.InvalidOperationException: outer message", text);
            Assert.Contains("System.ArgumentException: inner message", text);
            Assert.Contains(nameof(Thrown), text);
        }

        [Fact]
        public void LogException_Null_WritesNothing()
        {
            Logger.LogException(null!);

            Assert.Empty(LogFiles("*"));
        }

        [Fact]
        public void LogFirstChanceException_OnlyWritesWhenEnabled()
        {
            var args = new FirstChanceExceptionEventArgs(new TimeoutException("too slow"));

            Logger.LogFirstChanceException(null, args);
            Assert.Empty(LogFiles("*"));

            Logger.IsEnabled = true;
            Logger.LogFirstChanceException(null, args);
            Assert.Contains("First chance exception: System.TimeoutException: too slow", File.ReadAllText(Assert.Single(LogFiles("Log"))));
        }

        [Fact]
        public void Write_DirectoryIsAFile_DoesNotThrow()
        {
            string file = temp.WriteFile("not a directory", new byte[] { 1, 2, 3 });
            Logger.Directory = file;
            Logger.IsEnabled = true;

            Logger.Log("text");
            Logger.LogException(new InvalidOperationException("error"));
            Logger.LogFirstChanceException(null, new FirstChanceExceptionEventArgs(new InvalidOperationException("first chance")));

            Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(file));
        }

        [Fact]
        public void Write_InvalidDirectoryName_DoesNotThrow()
        {
            Logger.Directory = temp.Combine("invalid\0name");
            Logger.IsEnabled = true;

            Logger.Log("text");
            Logger.LogException(new InvalidOperationException("error"));
        }

        [Fact]
        public void LogFirstChanceException_FromManyThreads_WritesEveryEntry()
        {
            Logger.IsEnabled = true;
            const int count = 300;

            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
                Logger.LogFirstChanceException(null, new FirstChanceExceptionEventArgs(new InvalidOperationException($"parallel {i}"))));

            string text = File.ReadAllText(Assert.Single(LogFiles("Log")));
            Assert.Equal(count, Regex.Matches(text, "First chance exception").Count);
            Assert.All(Enumerable.Range(0, count), i => Assert.Contains($"parallel {i}{Environment.NewLine}", text));
        }

        [Fact]
        public void LogFirstChanceException_AsHandlerWhileWritingFails_DoesNotRecurse()
        {
            // Every failed write raises another first chance exception, which calls the handler again.
            Logger.Directory = temp.WriteFile("file instead of directory", new byte[] { 1 });
            Logger.IsEnabled = true;
            int calls = 0;
            EventHandler<FirstChanceExceptionEventArgs> handler = (sender, e) =>
            {
                Interlocked.Increment(ref calls);
                Logger.LogFirstChanceException(sender, e);
            };
            const int thrown = 200;

            AppDomain.CurrentDomain.FirstChanceException += handler;
            try
            {
                Parallel.For(0, thrown, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
                {
                    try
                    {
                        throw new InvalidOperationException($"test {i}");
                    }
                    catch (InvalidOperationException)
                    {
                    }
                });
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= handler;
            }

            Assert.InRange(calls, thrown, thrown * 10);
        }

        private string[] LogFiles(string prefix)
        {
            return Directory.Exists(Logger.Directory)
                ? Directory.GetFiles(Logger.Directory, prefix == "*" ? "*" : prefix + " *.txt")
                : Array.Empty<string>();
        }

        private static Exception Thrown(Exception exception)
        {
            try
            {
                throw exception;
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}

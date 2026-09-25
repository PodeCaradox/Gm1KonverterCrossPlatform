using Xunit;

namespace Gm1KonverterCrossPlatform.Tests.Legacy
{
    /// <summary>
    /// The original implementation keeps state in static fields (e.g. <c>LegacyUtility.datatype</c>),
    /// so tests using it must not run in parallel.
    /// </summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class LegacyCollection
    {
        public const string Name = "Legacy implementation (static state)";
    }
}

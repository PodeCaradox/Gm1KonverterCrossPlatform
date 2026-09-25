using System;
using System.Collections.Generic;
using System.IO;
using Gm1KonverterCrossPlatform.Core.IO;
using Newtonsoft.Json;

namespace Gm1KonverterCrossPlatform.Core.BuildingOffsets
{
    /// <summary>
    /// The changed building offsets (Offsets.json in the work folder), image index → offset.
    /// Allows applying the same changes again, e.g. after a game update.
    /// </summary>
    public sealed class BuildingOffsetStore
    {
        private readonly string path;
        private readonly SortedDictionary<int, BuildingOffset> offsets;

        private BuildingOffsetStore(string path, SortedDictionary<int, BuildingOffset> offsets)
        {
            this.path = path;
            this.offsets = offsets;
        }

        public IReadOnlyDictionary<int, BuildingOffset> Offsets => offsets;

        /// <summary>Loads the file; a missing file results in an empty store.</summary>
        /// <exception cref="InvalidDataException">The file is not valid JSON.</exception>
        public static BuildingOffsetStore Load(string path)
        {
            var offsets = new SortedDictionary<int, BuildingOffset>();
            if (File.Exists(path))
            {
                foreach (var entry in Parse(File.ReadAllText(path)))
                {
                    offsets[entry.Key] = entry.Value;
                }
            }

            return new BuildingOffsetStore(path, offsets);
        }

        public static IReadOnlyDictionary<int, BuildingOffset> Parse(string json)
        {
            try
            {
                var entries = JsonConvert.DeserializeObject<Dictionary<int, OffsetEntry>>(json) ?? new Dictionary<int, OffsetEntry>();
                var result = new Dictionary<int, BuildingOffset>();
                foreach (var entry in entries)
                {
                    result[entry.Key] = new BuildingOffset((int)Math.Round(entry.Value.X), (int)Math.Round(entry.Value.Y));
                }

                return result;
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"The offset file is not valid: {e.Message}", e);
            }
        }

        public void Set(int imageIndex, BuildingOffset offset)
        {
            offsets[imageIndex] = offset;
            Save();
        }

        private void Save()
        {
            var entries = new SortedDictionary<int, OffsetEntry>();
            foreach (var entry in offsets)
            {
                entries[entry.Key] = new OffsetEntry { X = entry.Value.X, Y = entry.Value.Y };
            }

            ImageFiles.EnsureDirectoryOf(path);
            File.WriteAllText(path, JsonConvert.SerializeObject(entries));
        }

        /// <summary>Older versions wrote Avalonia points (<c>{"X":1.0,"Y":2.0,"IsDefault":false}</c>).</summary>
        private sealed class OffsetEntry
        {
            public double X { get; set; }

            public double Y { get; set; }
        }
    }
}

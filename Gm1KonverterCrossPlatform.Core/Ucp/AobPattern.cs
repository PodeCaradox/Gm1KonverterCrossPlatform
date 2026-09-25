using System;
using System.Collections.Generic;
using System.Linq;

namespace Gm1KonverterCrossPlatform.Core.Ucp
{
    /// <summary>
    /// An "array of bytes" pattern as used by <c>core.scanForAOB</c> of UCP3, e.g. <c>"8B 4C ? ? 51"</c>.
    /// "?" matches any byte.
    /// </summary>
    public sealed class AobPattern
    {
        private const short Wildcard = -1;

        private readonly short[] values;

        private AobPattern(short[] values)
        {
            if (values.Length == 0 || values[0] == Wildcard || values[values.Length - 1] == Wildcard)
            {
                throw new ArgumentException("A pattern must start and end with a fixed byte.", nameof(values));
            }

            this.values = values;
        }

        public int Length => values.Length;

        /// <summary>Parses a pattern like <c>"8B 4C ? ? 51"</c>.</summary>
        /// <exception cref="FormatException">A token is neither "?" nor a hex byte.</exception>
        public static AobPattern Parse(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var values = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token == "?"
                    ? Wildcard
                    : token.Length == 2 && byte.TryParse(token, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out byte value)
                        ? value
                        : throw new FormatException($"\"{token}\" is not a byte of an AOB pattern."))
                .ToArray();
            return new AobPattern(values);
        }

        /// <summary>
        /// Creates a pattern from <paramref name="bytes"/>; bytes for which <paramref name="isWildcard"/> returns
        /// true become "?". Leading and trailing wildcards are removed, <paramref name="trimmedStart"/> is the
        /// number of bytes removed at the start.
        /// </summary>
        /// <returns>Null if every byte is a wildcard.</returns>
        public static AobPattern? Create(ReadOnlySpan<byte> bytes, Func<int, bool> isWildcard, out int trimmedStart)
        {
            var values = new short[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                values[i] = isWildcard(i) ? Wildcard : bytes[i];
            }

            trimmedStart = Array.FindIndex(values, value => value != Wildcard);
            if (trimmedStart < 0)
            {
                return null;
            }

            int last = Array.FindLastIndex(values, value => value != Wildcard);
            return new AobPattern(values.Skip(trimmedStart).Take(last - trimmedStart + 1).ToArray());
        }

        public bool IsMatch(ReadOnlySpan<byte> data, int index)
        {
            if (index < 0 || index > data.Length - values.Length)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != Wildcard && data[index + i] != values[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The positions of the first <paramref name="maxCount"/> matches in <paramref name="data"/>.</summary>
        public IReadOnlyList<int> FindAll(ReadOnlySpan<byte> data, int maxCount)
        {
            var matches = new List<int>();
            byte first = (byte)values[0];
            int position = 0;
            while (matches.Count < maxCount && position <= data.Length - values.Length)
            {
                int candidate = data.Slice(position, data.Length - values.Length + 1 - position).IndexOf(first);
                if (candidate < 0)
                {
                    break;
                }

                position += candidate;
                if (IsMatch(data, position))
                {
                    matches.Add(position);
                }

                position++;
            }

            return matches;
        }

        public override string ToString() => string.Join(" ", values.Select(value => value == Wildcard ? "?" : value.ToString("X2")));
    }
}

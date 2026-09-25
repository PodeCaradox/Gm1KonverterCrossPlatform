using Gm1KonverterCrossPlatform.Core.Files;

namespace Gm1KonverterCrossPlatform.Core.Codecs
{
    /// <summary>
    /// How a row without any visible pixel is encoded. The original files differ by type,
    /// so the encoder mimics them.
    /// </summary>
    public enum EmptyRowMode
    {
        /// <summary>Only a newline token.</summary>
        NewlineOnly,

        /// <summary>A transparent run over the whole row and a newline, except for trailing empty rows.</summary>
        TransparentRunUnlessRemainingRowsEmpty,

        /// <summary>Always a transparent run over the whole row and a newline.</summary>
        AlwaysTransparentRun
    }

    public sealed class TgxEncoderOptions
    {
        public EmptyRowMode EmptyRowMode { get; set; } = EmptyRowMode.NewlineOnly;

        /// <summary>Sets the alpha bit of every written color (images with <see cref="TgxImageHeader.AnimatedColor"/> 0).</summary>
        public bool ForceAlphaBit { get; set; }

        /// <summary>Writes 1 byte color table indices instead of 2 byte colors (animation files).</summary>
        public IPaletteIndexer? PaletteIndexer { get; set; }

        /// <summary>Options for standalone .tgx files.</summary>
        public static TgxEncoderOptions ForTgxFile() => new TgxEncoderOptions();

        /// <summary>Options for images inside a .gm1 file of the given type.</summary>
        public static TgxEncoderOptions ForGm1Image(Gm1DataType dataType, TgxImageHeader header, IPaletteIndexer? paletteIndexer = null)
        {
            return new TgxEncoderOptions
            {
                EmptyRowMode = EmptyRowModeFor(dataType),
                ForceAlphaBit = header.AnimatedColor == 0,
                PaletteIndexer = paletteIndexer
            };
        }

        public static EmptyRowMode EmptyRowModeFor(Gm1DataType dataType)
        {
            switch (dataType)
            {
                case Gm1DataType.TilesObject:
                    return EmptyRowMode.AlwaysTransparentRun;
                case Gm1DataType.Interface:
                case Gm1DataType.TgxConstSize:
                    return EmptyRowMode.TransparentRunUnlessRemainingRowsEmpty;
                default:
                    return EmptyRowMode.NewlineOnly;
            }
        }
    }
}

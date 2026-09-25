using System;
using System.Collections.Generic;
using System.IO;
using Gm1KonverterCrossPlatform.Core.Codecs;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Core.Documents
{
    /// <summary>
    /// An opened .gm1 file. Works with "items": the images of the file, or the buildings for building files
    /// (every building consists of several images).
    /// </summary>
    public sealed class Gm1Document
    {
        private int colorTableIndex;

        public Gm1Document(string fileName, Gm1File file)
        {
            if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("A file name is required.", nameof(fileName));

            FileName = fileName;
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public string FileName { get; }

        public Gm1File File { get; }

        public Gm1DataType DataType => File.DataType;

        public bool IsSupported => DataType.IsSupported();

        public bool HasColorTables => DataType.UsesPalette();

        public bool IsBuildingFile => DataType == Gm1DataType.TilesObject;

        /// <summary>The color table used to render animation images (0 - 9).</summary>
        public int ColorTableIndex
        {
            get => colorTableIndex;
            set
            {
                if (value < 0 || value >= Palette.ColorTableCount) throw new ArgumentOutOfRangeException(nameof(value));
                colorTableIndex = value;
            }
        }

        public ColorTable CurrentColorTable => File.Palette.ColorTables[ColorTableIndex];

        /// <summary>Number of images, or buildings for building files.</summary>
        public int ItemCount => IsBuildingFile ? TileObjectCodec.GetGroups(File.Images).Count : File.Images.Count;

        /// <exception cref="InvalidDataException">The file is not a valid .gm1 file.</exception>
        public static Gm1Document Load(string path)
        {
            return new Gm1Document(Path.GetFileName(path), Gm1File.Read(System.IO.File.ReadAllBytes(path)));
        }

        public byte[] ToBytes() => File.ToBytes();

        /// <summary>The first image of an item, used to show its header.</summary>
        public Gm1Image GetFirstImageOfItem(int itemIndex)
        {
            return IsBuildingFile
                ? File.Images[GetGroup(itemIndex).FirstImageIndex]
                : File.Images[itemIndex];
        }

        public IReadOnlyList<Argb1555Image> RenderItems() => RenderItems(ColorTableIndex);

        public IReadOnlyList<Argb1555Image> RenderItems(int colorTable)
        {
            EnsureSupported();

            var items = new List<Argb1555Image>();
            if (IsBuildingFile)
            {
                foreach (var group in TileObjectCodec.GetGroups(File.Images))
                {
                    items.Add(TileObjectCodec.Render(File.Images, group));
                }
            }
            else
            {
                foreach (var image in File.Images)
                {
                    items.Add(RenderImage(image, colorTable));
                }
            }

            return items;
        }

        public Argb1555Image RenderItem(int itemIndex) => RenderItem(itemIndex, ColorTableIndex);

        public Argb1555Image RenderItem(int itemIndex, int colorTable)
        {
            EnsureSupported();

            return IsBuildingFile
                ? TileObjectCodec.Render(File.Images, GetGroup(itemIndex))
                : RenderImage(File.Images[itemIndex], colorTable);
        }

        /// <summary>
        /// Replaces an item with a new image. Animation colors are looked up in the current color table.
        /// </summary>
        public void ReplaceItem(int itemIndex, Argb1555Image image)
        {
            IPaletteIndexer? indexer = HasColorTables ? new ColorTableIndexer(File.Palette, ColorTableIndex) : null;
            ReplaceItem(itemIndex, image, indexer);
        }

        /// <summary>
        /// Replaces several items. All images are checked first, so either all items are replaced or none.
        /// </summary>
        public void ReplaceItems(IReadOnlyList<(int ItemIndex, Argb1555Image Image)> replacements)
        {
            if (replacements == null) throw new ArgumentNullException(nameof(replacements));

            foreach (var replacement in replacements)
            {
                EnsureCanStore(replacement.Image);
            }

            foreach (var replacement in replacements)
            {
                ReplaceItem(replacement.ItemIndex, replacement.Image);
            }
        }

        /// <summary>
        /// Replaces an animation image using one version of the image per color table, which makes the
        /// color table indices unambiguous (see <see cref="MultiColorTableIndexer"/>).
        /// </summary>
        /// <param name="imagesPerColorTable">The image rendered with color table 0, 1, ...; index 0 is required.</param>
        public void ReplaceItemWithColorTableImages(int itemIndex, IReadOnlyList<Argb1555Image?> imagesPerColorTable)
        {
            if (!HasColorTables) throw new InvalidOperationException("Only animation files have color tables.");
            if (imagesPerColorTable == null) throw new ArgumentNullException(nameof(imagesPerColorTable));

            var image = imagesPerColorTable.Count > 0 ? imagesPerColorTable[0] : null;
            if (image == null) throw new ArgumentException("The image for the first color table is required.", nameof(imagesPerColorTable));

            ReplaceItem(itemIndex, image, new MultiColorTableIndexer(File.Palette, imagesPerColorTable));
        }

        private void ReplaceItem(int itemIndex, Argb1555Image image, IPaletteIndexer? indexer)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            EnsureSupported();

            if (IsBuildingFile)
            {
                ReplaceBuilding(itemIndex, image);
                return;
            }

            var entry = File.Images[itemIndex];

            // Validate and encode first, so a failure leaves the image unchanged.
            ushort width = CheckedDimension(image.Width);
            ushort height = CheckedDimension(image.Height + DataType.HeightPadding());
            byte[] data = DataType.IsUncompressed()
                ? NoCompressionCodec.Encode(image)
                : TgxCodec.Encode(image, TgxEncoderOptions.ForGm1Image(DataType, entry.Header, indexer));

            entry.Data = data;
            entry.Header.Width = width;
            entry.Header.Height = height;
            File.UpdateHeader();
        }

        private void ReplaceBuilding(int itemIndex, Argb1555Image image)
        {
            EnsureCanStore(image);
            var group = GetGroup(itemIndex);
            var newParts = TileObjectCodec.Encode(image);

            // The encoder cannot know this flag, keep it from the replaced parts.
            int preserved = Math.Min(group.PartCount, newParts.Count);
            for (int i = 0; i < preserved; i++)
            {
                newParts[i].Header.AnimatedColor = File.Images[group.FirstImageIndex + i].Header.AnimatedColor;
            }

            File.Images.RemoveRange(group.FirstImageIndex, group.PartCount);
            File.Images.InsertRange(group.FirstImageIndex, newParts);
            File.UpdateHeader();
        }

        private Argb1555Image RenderImage(Gm1Image image, int colorTable)
        {
            int width = image.Header.Width;
            int height = image.Header.Height;

            switch (DataType)
            {
                case Gm1DataType.Animations:
                    return TgxCodec.Decode(image.Data, width, height, File.Palette.ColorTables[colorTable]);

                case Gm1DataType.NoCompression:
                case Gm1DataType.NoCompression1:
                    return NoCompressionCodec.Decode(image.Data, width, Math.Max(0, height - DataType.HeightPadding()));

                default:
                    return TgxCodec.Decode(image.Data, width, height);
            }
        }

        private TileObjectGroup GetGroup(int itemIndex)
        {
            var groups = TileObjectCodec.GetGroups(File.Images);
            if (itemIndex < 0 || itemIndex >= groups.Count) throw new ArgumentOutOfRangeException(nameof(itemIndex));
            return groups[itemIndex];
        }

        private void EnsureSupported()
        {
            if (!IsSupported)
            {
                throw new NotSupportedException($"GM1 files of data type {(uint)DataType} are not supported.");
            }
        }

        /// <summary>Checks that an image fits into this file.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The image is too large for a .gm1 file.</exception>
        public void EnsureCanStore(Argb1555Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            CheckedDimension(image.Width);
            CheckedDimension(image.Height + DataType.HeightPadding());
        }

        private static ushort CheckedDimension(int value)
        {
            if (value > ushort.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Images can be at most {ushort.MaxValue} pixels wide and high.");
            }

            return (ushort)value;
        }
    }
}

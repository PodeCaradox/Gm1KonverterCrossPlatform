using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using Gm1KonverterCrossPlatform.Core.Files;
using Gm1KonverterCrossPlatform.Core.Imaging;

namespace Gm1KonverterCrossPlatform.Core.Codecs
{
    /// <summary>
    /// Buildings (GM1 data type 3). A building consists of n x n parts. Every part is a diamond shaped
    /// ground tile (30 x 16 pixels, 512 bytes uncompressed) optionally followed by an RLE encoded image
    /// that is drawn on top of the tile.
    /// </summary>
    public static class TileObjectCodec
    {
        public const int DiamondWidth = 30;
        public const int DiamondHeight = 16;

        /// <summary>Size of the uncompressed diamond at the start of every part.</summary>
        public const int DiamondByteSize = 512;

        /// <summary>Horizontal distance of two neighbouring diamonds (2 pixel gap).</summary>
        private const int DiamondSpacing = 32;

        /// <summary>Horizontal shift between two rows of diamonds.</summary>
        private const int RowShift = DiamondSpacing / 2;

        /// <summary>Extra space above the building so that images on top of the tiles fit.</summary>
        private const int SpaceAbove = 500;

        private const int RightPartImageShift = 14;

        /// <summary>Width of the image on top of the left or right corner part of a building.</summary>
        private const int SideImageWidth = 16;

        /// <summary>Number of pixels in every row of a diamond.</summary>
        private static readonly int[] DiamondRowWidths =
        {
            2, 6, 10, 14, 18, 22, 26, 30,
            30, 26, 22, 18, 14, 10, 6, 2
        };

        private enum PartDirection : byte
        {
            None = 0,
            Single = 1,
            Left = 2,
            Right = 3
        }

        /// <summary>
        /// Splits the images of a building file into buildings. A building starts with a part whose
        /// <see cref="TgxImageHeader.ImagePart"/> is 0.
        /// </summary>
        public static IReadOnlyList<TileObjectGroup> GetGroups(IReadOnlyList<Gm1Image> images)
        {
            if (images == null) throw new ArgumentNullException(nameof(images));

            var groups = new List<TileObjectGroup>();
            int start = 0;
            for (int i = 1; i <= images.Count; i++)
            {
                if (i == images.Count || images[i].Header.ImagePart == 0)
                {
                    if (i > start)
                    {
                        groups.Add(new TileObjectGroup(start, i - start));
                    }

                    start = i;
                }
            }

            return groups;
        }

        /// <summary>
        /// Number of diamonds per row of a building with <paramref name="partCount"/> parts
        /// (1 → 1, 4 → 2, 9 → 3, ...), 0 if the number of parts is invalid.
        /// </summary>
        public static int GetDiamondCountPerRow(int partCount)
        {
            int parts = 0;
            int diagonalLength = 1;
            while (true)
            {
                int remaining = partCount - parts - diagonalLength;
                if (remaining == 0)
                {
                    return diagonalLength - diagonalLength / 2;
                }

                if (remaining < 0)
                {
                    return 0;
                }

                parts += diagonalLength;
                diagonalLength += 2;
            }
        }

        /// <summary>
        /// Largest building that can be stored: the number of parts is stored in one byte (15 * 15 = 225).
        /// </summary>
        public const int MaxDiamondsPerRow = 15;

        /// <summary>Width of the rendered image of a building with <paramref name="diamondsPerRow"/> diamonds per row.</summary>
        public static int GetImageWidth(int diamondsPerRow) => diamondsPerRow * DiamondSpacing - 2;

        /// <summary>
        /// Draws all parts of a building into one image, the way it is exported.
        /// </summary>
        /// <exception cref="InvalidDataException">The number of parts is not a square number.</exception>
        public static Argb1555Image Render(IReadOnlyList<Gm1Image> images, TileObjectGroup group)
        {
            if (images == null) throw new ArgumentNullException(nameof(images));

            var firstHeader = images[group.FirstImageIndex].Header;
            int diamondsPerRow = GetDiamondCountPerRow(firstHeader.SubParts);
            if (diamondsPerRow == 0)
            {
                throw new InvalidDataException($"Building at image {group.FirstImageIndex + 1} has an invalid number of parts ({firstHeader.SubParts}).");
            }

            int width = GetImageWidth(diamondsPerRow);
            int lastPartIndex = group.FirstImageIndex + firstHeader.SubParts - 1;
            if (lastPartIndex >= group.FirstImageIndex + group.PartCount)
            {
                lastPartIndex = group.FirstImageIndex + group.PartCount - 1;
            }

            int canvasHeight = diamondsPerRow * DiamondHeight + images[lastPartIndex].Header.TileOffset + SpaceAbove;

            var placements = PlaceParts(images, group, diamondsPerRow, canvasHeight);

            // The image is cut off above the highest image on top of a tile.
            int firstVisibleRow = int.MaxValue;
            foreach (var placement in placements)
            {
                if (placement.HasImageOnTop)
                {
                    firstVisibleRow = Math.Min(firstVisibleRow, placement.ImageOnTopY);
                }
            }

            if (firstVisibleRow == int.MaxValue)
            {
                firstVisibleRow = SpaceAbove;
            }

            var canvas = new LinearCanvas(width, canvasHeight - firstVisibleRow, firstVisibleRow);
            foreach (var placement in placements)
            {
                var data = images[placement.ImageIndex].Data;
                if (placement.HasImageOnTop)
                {
                    DrawImageOnTop(canvas, data, placement.ImageOnTopX, placement.ImageOnTopY);
                }

                DrawDiamond(canvas, data, placement.X, placement.Y);
            }

            return canvas.ToImage();
        }

        /// <summary>
        /// Splits a building image into parts, the way Stronghold stores them.
        /// Produces the same parts as the original implementation (Utility.ConvertImgToTiles).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The image is too narrow or too wide for a building.</exception>
        public static List<Gm1Image> Encode(Argb1555Image building)
        {
            EnsureValidBuildingSize(building);
            return new BuildingEncoder(building).Encode();
        }

        /// <exception cref="ArgumentOutOfRangeException">The image is too narrow or too wide for a building.</exception>
        public static void EnsureValidBuildingSize(Argb1555Image building)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));

            int diamondsPerRow = GetDiamondCountPerRowFromImageWidth(building.Width);
            if (diamondsPerRow < 1 || diamondsPerRow > MaxDiamondsPerRow)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(building),
                    building.Width,
                    $"A building image must be between {GetImageWidth(1)} and {GetImageWidth(MaxDiamondsPerRow)} pixels wide, but it is {building.Width} pixels wide.");
            }
        }

        /// <summary>
        /// Number of diamonds per row for an exported building image. Exported images are
        /// <c>32 * n - 2</c> pixels wide; other widths are interpreted like the original implementation did.
        /// </summary>
        public static int GetDiamondCountPerRowFromImageWidth(int imageWidth)
        {
            return (imageWidth + 2) % DiamondSpacing == 0
                ? (imageWidth + 2) / DiamondSpacing
                : imageWidth / DiamondWidth;
        }

        private static List<PartPlacement> PlaceParts(IReadOnlyList<Gm1Image> images, TileObjectGroup group, int diamondsPerRow, int canvasHeight)
        {
            var placements = new List<PartPlacement>();

            int offsetY = canvasHeight - DiamondHeight;
            int offsetX = (diamondsPerRow / 2) * DiamondWidth + (diamondsPerRow - 1) - (diamondsPerRow % 2 == 0 ? 15 : 0);
            int rowStartX = offsetX;
            int partsPerRow = 1;
            int partsInCurrentRow = 0;
            bool halfReached = false;

            for (int i = group.FirstImageIndex; i < group.FirstImageIndex + group.PartCount; i++)
            {
                var image = images[i];
                bool hasImageOnTop = image.Data.Length > DiamondByteSize;
                int shift = image.Header.Direction == (byte)PartDirection.Right ? RightPartImageShift : 0;

                placements.Add(new PartPlacement(
                    i,
                    offsetX,
                    offsetY,
                    hasImageOnTop,
                    offsetX + shift,
                    offsetY - image.Header.TileOffset));

                offsetX += DiamondSpacing;
                partsInCurrentRow++;
                if (partsInCurrentRow == partsPerRow)
                {
                    offsetX = rowStartX;
                    offsetY -= DiamondHeight / 2;

                    if (partsPerRow < diamondsPerRow && !halfReached)
                    {
                        partsPerRow++;
                        offsetX -= RowShift;
                    }
                    else
                    {
                        partsPerRow--;
                        offsetX += RowShift;
                        halfReached = true;
                    }

                    rowStartX = offsetX;
                    partsInCurrentRow = 0;
                }
            }

            return placements;
        }

        private static void DrawDiamond(LinearCanvas canvas, byte[] data, int xOffset, int yOffset)
        {
            int bytePosition = 0;
            for (int y = 0; y < DiamondHeight; y++)
            {
                int rowWidth = DiamondRowWidths[y];
                for (int x = 0; x < rowWidth; x++)
                {
                    if (bytePosition + sizeof(ushort) > data.Length)
                    {
                        return;
                    }

                    ushort color = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(bytePosition));
                    bytePosition += sizeof(ushort);

                    // Ground tiles are never transparent, Stronghold ignores the alpha bit.
                    canvas.Set(canvas.Index(x + xOffset + 15 - rowWidth / 2, y + yOffset), (ushort)(color | 0x8000));
                }
            }
        }

        private static void DrawImageOnTop(LinearCanvas canvas, byte[] data, int offsetX, int offsetY)
        {
            int x = 0;
            int y = 0;
            int bytePosition = DiamondByteSize;

            while (bytePosition < data.Length)
            {
                byte token = data[bytePosition++];
                int tokenType = token >> 5;
                int length = (token & 0b1_1111) + 1;

                switch (tokenType)
                {
                    case 0: // stream of pixels
                        for (int i = 0; i < length; i++)
                        {
                            if (!TryReadOpaqueColor(data, ref bytePosition, out ushort color)) return;
                            canvas.Set(canvas.Index(x + offsetX, y + offsetY), color);
                            x++;
                        }

                        break;

                    case 2: // repeating pixels
                        if (!TryReadOpaqueColor(data, ref bytePosition, out ushort repeatedColor)) return;
                        for (int i = 0; i < length; i++)
                        {
                            canvas.Set(canvas.Index(x + offsetX, y + offsetY), repeatedColor);
                            x++;
                        }

                        break;

                    case 1: // transparent pixels
                        x += length;
                        break;

                    case 4: // newline
                        y++;
                        x = 0;
                        break;
                }
            }
        }

        private static bool TryReadOpaqueColor(byte[] data, ref int bytePosition, out ushort color)
        {
            if (bytePosition + sizeof(ushort) > data.Length)
            {
                color = 0;
                return false;
            }

            color = (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(bytePosition)) | 0x8000);
            bytePosition += sizeof(ushort);
            return true;
        }

        /// <summary>
        /// Pixel buffer addressed like the original implementation: row * width + column, so pixels of a
        /// row that is too long continue in the next row. Rows above <c>firstRow</c> are cut off.
        /// </summary>
        private sealed class LinearCanvas
        {
            private readonly ushort[] pixels;
            private readonly int width;
            private readonly int height;
            private readonly int firstRow;

            public LinearCanvas(int width, int height, int firstRow)
            {
                this.width = width;
                this.height = height;
                this.firstRow = firstRow;
                pixels = new ushort[width * height];
            }

            public long Index(int x, int y) => (long)width * (y - firstRow) + x;

            public void Set(long index, ushort color)
            {
                if (index >= 0 && index < pixels.Length)
                {
                    pixels[index] = color;
                }
            }

            public Argb1555Image ToImage() => new Argb1555Image(width, height, pixels);
        }

        private readonly struct PartPlacement
        {
            public PartPlacement(int imageIndex, int x, int y, bool hasImageOnTop, int imageOnTopX, int imageOnTopY)
            {
                ImageIndex = imageIndex;
                X = x;
                Y = y;
                HasImageOnTop = hasImageOnTop;
                ImageOnTopX = imageOnTopX;
                ImageOnTopY = imageOnTopY;
            }

            public int ImageIndex { get; }
            public int X { get; }
            public int Y { get; }
            public bool HasImageOnTop { get; }
            public int ImageOnTopX { get; }
            public int ImageOnTopY { get; }
        }

        /// <summary>Port of Utility.ConvertImgToTiles.</summary>
        private sealed class BuildingEncoder
        {
            private readonly ushort[] pixels;
            private readonly int width;
            private readonly int height;

            public BuildingEncoder(Argb1555Image building)
            {
                pixels = (ushort[])building.Pixels.Clone();
                width = building.Width;
                height = building.Height;
            }

            public List<Gm1Image> Encode()
            {
                var parts = new List<Gm1Image>();

                int diamondsPerRow = GetDiamondCountPerRowFromImageWidth(width);
                int totalParts = diamondsPerRow * diamondsPerRow;

                int rowStartX = width / 2;
                int xOffset = rowStartX;
                int yOffset = height - DiamondHeight;
                int partsPerRow = 1;
                int partInRow = 0;
                bool halfReached = false;

                for (int part = 0; part < totalParts; part++)
                {
                    partInRow++;

                    var data = new List<byte>(DiamondByteSize);
                    CutDiamond(data, xOffset, yOffset);

                    var header = new TgxImageHeader
                    {
                        Direction = (byte)PartDirection.None,
                        Width = DiamondWidth,
                        Height = DiamondHeight,
                        SubParts = (byte)totalParts,
                        ImagePart = (byte)part
                    };

                    if (totalParts == 1)
                    {
                        halfReached = true;
                    }

                    if (halfReached)
                    {
                        if (partInRow == 1)
                        {
                            bool isLastPart = part == totalParts - 1;
                            int imageWidth = isLastPart ? DiamondWidth : SideImageWidth;
                            header.BuildingWidth = (byte)imageWidth;
                            header.Direction = (byte)(isLastPart ? PartDirection.Single : PartDirection.Left);
                            AddImageOnTop(data, header, imageWidth, yOffset + 7, xOffset - 15);
                        }
                        else if (partInRow == partsPerRow)
                        {
                            header.BuildingWidth = SideImageWidth;
                            header.Direction = (byte)PartDirection.Right;
                            AddImageOnTop(data, header, SideImageWidth, yOffset + 7, xOffset - 1);
                            header.HorizontalOffsetOfImage = RightPartImageShift;
                        }
                    }

                    parts.Add(new Gm1Image(header, data.ToArray()));
                    xOffset += DiamondSpacing;

                    if (partInRow == partsPerRow)
                    {
                        yOffset -= DiamondHeight / 2;
                        partInRow = 0;

                        xOffset = rowStartX;
                        if (partsPerRow == diamondsPerRow - 1 && !halfReached)
                        {
                            halfReached = true;
                            partsPerRow += 2;
                            xOffset = -1;
                        }

                        if (!halfReached)
                        {
                            partsPerRow++;
                            xOffset -= RowShift;
                        }
                        else
                        {
                            xOffset += RowShift;
                            partsPerRow--;
                        }

                        rowStartX = xOffset;
                    }
                }

                return parts;
            }

            /// <summary>Copies the diamond at the given position and erases it from the building image.</summary>
            private void CutDiamond(List<byte> data, int xOffset, int yOffset)
            {
                for (int y = 0; y < DiamondHeight; y++)
                {
                    int rowWidth = DiamondRowWidths[y];
                    for (int x = 0; x < rowWidth; x++)
                    {
                        int index = width * (y + yOffset) + x + xOffset - rowWidth / 2;
                        ushort color = Get(index);
                        data.Add((byte)color);
                        data.Add((byte)(color >> 8));

                        if (index >= 0 && index < pixels.Length)
                        {
                            pixels[index] = Argb1555.TransparentMarker;
                        }
                    }
                }
            }

            /// <summary>
            /// Appends the image above a part (everything from the top of the building down to the middle of the diamond)
            /// and sets the header fields that describe it.
            /// </summary>
            private void AddImageOnTop(List<byte> data, TgxImageHeader header, int imageWidth, int imageHeight, int offsetX)
            {
                var image = CutImageOnTop(imageWidth, imageHeight, offsetX);
                if (image.Height == 0)
                {
                    return;
                }

                data.AddRange(TgxCodec.Encode(image, new TgxEncoderOptions { EmptyRowMode = EmptyRowMode.AlwaysTransparentRun }));

                int tileOffset = image.Height + 10 - DiamondHeight - 1;
                header.TileOffset = tileOffset < 0 ? (ushort)0 : (ushort)tileOffset;
                header.Height = (ushort)(image.Height + 9);
            }

            /// <summary>
            /// Copies a column of the building image, leaving out fully transparent rows at the top.
            /// The last 7 rows are always kept.
            /// </summary>
            private Argb1555Image CutImageOnTop(int imageWidth, int imageHeight, int offsetX)
            {
                var result = new List<ushort>();
                bool foundVisibleRow = false;

                for (int y = 0; y < imageHeight; y++)
                {
                    var row = new ushort[imageWidth];
                    for (int x = 0; x < imageWidth; x++)
                    {
                        ushort color = Get(width * y + x + offsetX);
                        if (color != Argb1555.TransparentMarker || y > imageHeight - 8)
                        {
                            row[x] = color;
                            foundVisibleRow = true;
                        }
                        else
                        {
                            row[x] = Argb1555.TransparentMarker;
                        }
                    }

                    if (foundVisibleRow)
                    {
                        result.AddRange(row);
                    }
                }

                return new Argb1555Image(imageWidth, result.Count / imageWidth, result.ToArray());
            }

            private ushort Get(int index)
            {
                return index >= 0 && index < pixels.Length ? pixels[index] : Argb1555.TransparentMarker;
            }
        }
    }

    /// <summary>The parts of one building inside a building file.</summary>
    public readonly struct TileObjectGroup
    {
        public TileObjectGroup(int firstImageIndex, int partCount)
        {
            FirstImageIndex = firstImageIndex;
            PartCount = partCount;
        }

        public int FirstImageIndex { get; }

        public int PartCount { get; }
    }
}

using System;
using System.Collections.Generic;
using Gm1KonverterCrossPlatform.Core.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;

namespace Gm1KonverterCrossPlatform.Core.IO
{
    /// <summary>
    /// Writes an endlessly looping animated GIF. Frames of different size are aligned to the bottom right corner.
    /// </summary>
    public static class GifExporter
    {
        /// <summary>GIF frame delays are stored in 1/100 seconds.</summary>
        private const int MillisecondsPerDelayUnit = 10;

        public static void Save(IReadOnlyList<Argb1555Image> frames, int delayMilliseconds, string path)
        {
            if (frames == null) throw new ArgumentNullException(nameof(frames));
            if (frames.Count == 0) throw new ArgumentException("At least one frame is needed.", nameof(frames));

            int width = 1;
            int height = 1;
            foreach (var frame in frames)
            {
                width = Math.Max(width, frame.Width);
                height = Math.Max(height, frame.Height);
            }

            int frameDelay = Math.Max(1, delayMilliseconds / MillisecondsPerDelayUnit);

            using var gif = new Image<Rgba32>(width, height);
            gif.Metadata.GetGifMetadata().RepeatCount = 0;

            foreach (var frame in frames)
            {
                using var canvas = new Image<Rgba32>(width, height);
                int offsetX = width - frame.Width;
                int offsetY = height - frame.Height;
                for (int y = 0; y < frame.Height; y++)
                {
                    for (int x = 0; x < frame.Width; x++)
                    {
                        canvas[offsetX + x, offsetY + y] = ImageFiles.ToRgba32(frame[x, y]);
                    }
                }

                var added = gif.Frames.AddFrame(canvas.Frames.RootFrame);
                var metadata = added.Metadata.GetGifMetadata();
                metadata.FrameDelay = frameDelay;
                metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;
            }

            gif.Frames.RemoveFrame(0);

            ImageFiles.EnsureDirectoryOf(path);
            gif.SaveAsGif(path);
        }
    }
}

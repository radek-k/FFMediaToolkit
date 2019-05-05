namespace FFMediaToolkit.Helpers
{
    using System;
    using System.Collections.Generic;
    using FFMediaToolkit.Common;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains helper methods for image converting
    /// </summary>
    internal unsafe class Scaler
    {
        private SwsContext* scaleContext;

        /// <summary>
        /// Overrides the <paramref name="destinationFrame"/> image buffer with rescaled specified bitmap. Used in encoding.
        /// </summary>
        /// <param name="bitmapPointer">Pointer to the input bitmap data</param>
        /// <param name="bitmapLayout">The input bitmap layout</param>
        /// <param name="destinationFrame">The <see cref="AVFrame"/> to override</param>
        /// <param name="frameLayout">The output <see cref="AVFrame"/> layout setting</param>
        public void FillAVFrame(IntPtr bitmapPointer, Layout bitmapLayout, AVFrame* destinationFrame, Layout frameLayout)
        {
            var context = GetCachedContext(ref scaleContext, bitmapLayout, frameLayout);
            var ptr = (byte*)bitmapPointer.ToPointer();
            var data = new byte*[4] { ptr, null, null, null };
            var linesize = new int[4] { bitmapLayout.Stride, 0, 0, 0 };
            ffmpeg.sws_scale(context, data, linesize, 0, bitmapLayout.Height, destinationFrame->data, destinationFrame->linesize);
        }

        /// <summary>
        /// Gets the byte size of a single image line
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="format">The image format</param>
        /// <returns>Size of single image line measured in bytes</returns>
        public static int GetStride(int width, ImagePixelFormat format) => GetBytesPerPixel(format) * width;

        /// <summary>
        /// Gets the <see cref="SwsContext"/> that can convert the <paramref name="source"/> layout to the <paramref name="destination"/> layout.
        /// </summary>
        /// <param name="cache">A field for use as object cache, to prevent unnecessary memory allocation. Can be <see langword="null"/>, it will be assigned with new object</param>
        /// <param name="source">Source image layout</param>
        /// <param name="destination">Destination image layout</param>
        /// <returns>The scale context</returns>
        private static SwsContext* GetCachedContext(ref SwsContext* cache, Layout source, Layout destination)
        {
            if (source == destination)
            {
                return null;
            }

            cache = ffmpeg.sws_getCachedContext(cache, source.Width, source.Height, source.PixelFormat, source.Width, source.Height, source.PixelFormat, ffmpeg.SWS_BICUBIC, null, null, null);
            return cache;
        }

        private static int GetBytesPerPixel(ImagePixelFormat format)
        {
            switch (format)
            {
                case ImagePixelFormat.BGR24:
                    return 3;
                case ImagePixelFormat.BGRA32:
                    return 4;
                case ImagePixelFormat.RGB24:
                    return 3;
                case ImagePixelFormat.ARGB32:
                    return 4;
                default:
                    return 0;
            }
        }

    }
}

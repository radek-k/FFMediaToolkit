namespace FFMediaToolkit.Helpers
{
    using System;
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a cache object for FFMpeg <see cref="SwsContext"/>. Useful when converting many bitmaps to the same format.
    /// </summary>
    public unsafe class Scaler : IDisposable
    {
        private SwsContext* scaleContext;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scaler"/> class.
        /// </summary>
        public Scaler()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Scaler"/> class.
        /// </summary>
        ~Scaler() => Disposing(false);

        /// <summary>
        /// Gets the estimated image line size based on the pixel format and width
        /// </summary>
        /// <param name="width">The width of the image</param>
        /// <param name="format">The image pixel format</param>
        /// <returns>The size of a single line of the image measured in bytes</returns>
        public static int EstimateStride(int width, ImagePixelFormat format) => GetBytesPerPixel(format) * width;

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        /// <summary>
        /// Overrides the <paramref name="destinationFrame"/> image buffer with rescaled specified bitmap. Used in encoding.
        /// </summary>
        /// <param name="bitmapPointer">Pointer to the input bitmap data</param>
        /// <param name="bitmapLayout">The input bitmap layout</param>
        /// <param name="destinationFrame">The <see cref="AVFrame"/> to override</param>
        /// <param name="frameLayout">The output <see cref="AVFrame"/> layout setting</param>
        internal void FillAVFrame(IntPtr bitmapPointer, Layout bitmapLayout, AVFrame* destinationFrame, Layout frameLayout)
        {
            var context = GetCachedContext(bitmapLayout, frameLayout);
            var ptr = (byte*)bitmapPointer.ToPointer();
            var data = new byte*[4] { ptr, null, null, null };
            var linesize = new int[4] { bitmapLayout.Stride, 0, 0, 0 };
            ffmpeg.sws_scale(context, data, linesize, 0, bitmapLayout.Height, destinationFrame->data, destinationFrame->linesize);
        }

        /// <summary>
        /// Converts a video <see cref="AVFrame"/> to bitmap data with a specified layout and writes its data to the specified memory buffer. Used in decoding.
        /// </summary>
        /// <param name="videoFrame">The video frame to convert</param>
        /// <param name="videoLayout">The video frame layout</param>
        /// <param name="destinationPointer">A pointer to the destination bitmap data buffer</param>
        /// <param name="destinationLayout">The destination bitmap layout</param>
        internal void AVFrameToBitmap(AVFrame* videoFrame, Layout videoLayout, IntPtr destinationPointer, Layout destinationLayout)
        {
            var context = GetCachedContext(videoLayout, destinationLayout);
            var ptr = (byte*)destinationPointer.ToPointer();
            var data = new byte*[4] { ptr, null, null, null };
            var linesize = new int[4] { destinationLayout.Stride, 0, 0, 0 };
            ffmpeg.sws_scale(context, videoFrame->data, videoFrame->linesize, 0, videoLayout.Height, data, linesize);
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

        private SwsContext* GetCachedContext(Layout source, Layout destination)
        {
            if (source == destination)
            {
                return null;
            }

            scaleContext = ffmpeg.sws_getCachedContext(scaleContext, source.Width, source.Height, source.PixelFormat, destination.Width, destination.Height, destination.PixelFormat, ffmpeg.SWS_BICUBIC, null, null, null);
            return scaleContext;
        }

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            ffmpeg.sws_freeContext(scaleContext);
            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}

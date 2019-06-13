namespace FFMediaToolkit.Common.Internal
{
    using System;
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a cache object for FFMpeg <see cref="SwsContext"/>. Useful when converting many bitmaps to the same format.
    /// </summary>
    internal unsafe class Scaler : Wrapper<SwsContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Scaler"/> class.
        /// </summary>
        public Scaler()
            : base(null)
        {
        }

        /// <summary>
        /// Overrides the <paramref name="destinationFrame"/> image buffer with rescaled specified bitmap. Used in encoding.
        /// </summary>
        /// <param name="bitmapPointer">Pointer to the input bitmap data.</param>
        /// <param name="bitmapLayout">The input bitmap layout.</param>
        /// <param name="destinationFrame">The <see cref="AVFrame"/> to override.</param>
        /// <param name="frameLayout">The output <see cref="AVFrame"/> layout setting.</param>
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
        /// <param name="videoFrame">The video frame to convert.</param>
        /// <param name="videoLayout">The video frame layout.</param>
        /// <param name="destinationPointer">A pointer to the destination bitmap data buffer.</param>
        /// <param name="destinationLayout">The destination bitmap layout.</param>
        internal void AVFrameToBitmap(AVFrame* videoFrame, Layout videoLayout, IntPtr destinationPointer, Layout destinationLayout)
        {
            var context = GetCachedContext(videoLayout, destinationLayout);
            var ptr = (byte*)destinationPointer.ToPointer();
            var data = new byte*[4] { ptr, null, null, null };
            var linesize = new int[4] { destinationLayout.Stride, 0, 0, 0 };
            ffmpeg.sws_scale(context, videoFrame->data, videoFrame->linesize, 0, videoLayout.Height, data, linesize);
        }

        /// <inheritdoc/>
        protected override void OnDisposing() => ffmpeg.sws_freeContext(Pointer);

        private SwsContext* GetCachedContext(Layout source, Layout destination)
        {
            if (source == destination)
            {
                return null;
            }

            // If don't change the dimensions of the image, there is no need to use the high quality bicubic method.
            var scaleMode = source.SizeEquals(destination) ? ffmpeg.SWS_POINT : ffmpeg.SWS_BICUBIC;

            UpdatePointer(ffmpeg.sws_getCachedContext(Pointer, source.Width, source.Height, source.PixelFormat, destination.Width, destination.Height, destination.PixelFormat, scaleMode, null, null, null));
            return Pointer;
        }
    }
}

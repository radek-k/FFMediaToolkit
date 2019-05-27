namespace FFMediaToolkit.Common
{
    using System;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represent a video frame.
    /// </summary>
    public unsafe class VideoFrame : MediaFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoFrame"/> class using existing <see cref="AVFrame"/> and <see cref="VideoStream"/>
        /// </summary>
        /// <param name="frame">The video <see cref="AVFrame"/>.</param>
        public VideoFrame(AVFrame* frame)
            : base(frame)
        {
            if (frame->channels > 0) // Checks frame content type
                throw new ArgumentException("Cannot create VideoFrame instance from AVFrame containing audio");
        }

        /// <summary>
        /// Gets the frame dimensions.
        /// </summary>
        public Layout Layout => Pointer != null ? new Layout((AVPixelFormat)Pointer->format, Pointer->width, Pointer->height) : default;

        /// <summary>
        /// Creates a video frame with given dimensions and allocates a buffer for it.
        /// </summary>
        /// <param name="dimensions">The dimensions of the video frame.</param>
        /// <returns>The new video frame.</returns>
        public static VideoFrame Create(Layout dimensions)
        {
            var frame = ffmpeg.av_frame_alloc();

            frame->width = dimensions.Width;
            frame->height = dimensions.Height;
            frame->format = (int)dimensions.PixelFormat;

            ffmpeg.av_frame_get_buffer(frame, 32);

            return new VideoFrame(frame);
        }

        /// <summary>
        /// Creates an empty frame for decoding.
        /// </summary>
        /// <returns>The empty <see cref="VideoFrame"/>.</returns>
        public static VideoFrame CreateEmpty() => new VideoFrame(ffmpeg.av_frame_alloc());

        /// <summary>
        /// Overrides this video frame data with the converted <paramref name="bitmap"/> using specified <see cref="Scaler"/> object.
        /// </summary>
        /// <param name="bitmap">The bitmap to convert.</param>
        /// <param name="scaler">A <see cref="Scaler"/> object, used for caching the FFMpeg <see cref="SwsContext"/> when converting many frames of the same video</param>
        public void UpdateFromBitmap(BitmapData bitmap, Scaler scaler)
        {
            fixed (byte* ptr = bitmap.Data.Span)
            {
                scaler.FillAVFrame((IntPtr)ptr, bitmap.Layout, Pointer, Layout);
            }
        }

        /// <summary>
        /// Converts this video frame to the <see cref="BitmapData"/> with the specified pixel format.
        /// </summary>
        /// <param name="scaler">A <see cref="Scaler"/> object, used for caching the FFMpeg <see cref="SwsContext"/> when converting many frames of the same video</param>
        /// <param name="targetFormat">The output bitmap pixel format</param>
        /// <returns>A <see cref="BitmapData"/> instance containg converted bitmap data</returns>
        public BitmapData ToBitmap(Scaler scaler, ImagePixelFormat targetFormat)
        {
            var bitmap = PooledBitmap.Create(Layout.Width, Layout.Height, targetFormat);
            var targetLayout = new Layout((AVPixelFormat)targetFormat, Layout.Width, Layout.Height);

            fixed (byte* ptr = bitmap.Data.Span)
            {
                scaler.AVFrameToBitmap(Pointer, Layout, (IntPtr)ptr, targetLayout);
            }

            return bitmap;
        }
    }
}

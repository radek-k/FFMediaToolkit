namespace FFMediaToolkit.Common.Internal
{
    using System;
    using System.Drawing;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represent a video frame.
    /// </summary>
    internal unsafe class VideoFrame : MediaFrame
    {
        private VideoFrame(AVFrame* frame)
            : base(frame)
        {
        }

        /// <summary>
        /// Gets the frame dimensions.
        /// </summary>
        public Size Layout => Pointer != null ? new Size(Pointer->width, Pointer->height) : default;

        /// <summary>
        /// Gets the frame pixel format.
        /// </summary>
        public AVPixelFormat PixelFormat => Pointer != null ? (AVPixelFormat)Pointer->format : default;

        /// <summary>
        /// Creates a video frame with given dimensions and allocates a buffer for it.
        /// </summary>
        /// <param name="dimensions">The dimensions of the video frame.</param>
        /// <param name="pixelFormat">The video pixel format.</param>
        /// <returns>The new video frame.</returns>
        public static VideoFrame Create(Size dimensions, AVPixelFormat pixelFormat)
        {
            var frame = ffmpeg.av_frame_alloc();
            if (frame == null)
                throw new FFmpegException("Cannot allocate video AVFrame", ffmpeg.ENOMEM);

            frame->width = dimensions.Width;
            frame->height = dimensions.Height;
            frame->format = (int)pixelFormat;

            ffmpeg.av_frame_get_buffer(frame, 0).ThrowIfError("Cannot allocate video AVFrame buffer");

            return new VideoFrame(frame);
        }

        /// <summary>
        /// Creates an empty frame for decoding.
        /// </summary>
        /// <returns>The empty <see cref="VideoFrame"/>.</returns>
        public static VideoFrame CreateEmpty()
        {
            var frame = ffmpeg.av_frame_alloc();
            if (frame == null)
                throw new FFmpegException("Cannot allocate video AVFrame", ffmpeg.ENOMEM);

            return new VideoFrame(frame);
        }
    }
}

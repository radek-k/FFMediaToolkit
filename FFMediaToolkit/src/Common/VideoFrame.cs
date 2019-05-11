namespace FFMediaToolkit.Common
{
    using System;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represent a video frame
    /// </summary>
    public unsafe class VideoFrame : MediaFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoFrame"/> class using existing <see cref="AVFrame"/> and <see cref="VideoStream"/>
        /// </summary>
        /// <param name="frame">Video frame</param>
        /// <param name="stream">Media stream for the frame</param>
        public VideoFrame(AVFrame* frame, VideoStream stream)
            : base(frame, stream.Index)
        {
            if (frame->channels > 0) // Checks frame content type
                throw new ArgumentException("Cannot create VideoFrame instance from AVFrame containing audio");

            Layout = stream.EncodedFrame.Layout;
        }

        /// <summary>
        /// Gets the frame layout
        /// </summary>
        public Layout Layout { get;  }

        /// <summary>
        /// Creates an empty video frame
        /// </summary>
        /// <param name="stream">Video stream</param>
        /// <returns>Allocated video frame</returns>
        public static VideoFrame CreateEmpty(VideoStream stream)
        {
            var frame = ffmpeg.av_frame_alloc();

            var size = ffmpeg.av_image_get_buffer_size(stream.FrameLayout.PixelFormat, stream.FrameLayout.Width, stream.FrameLayout.Height, 32);
            var buffer = (byte*)ffmpeg.av_malloc((ulong)size);
#pragma warning disable 618
            ffmpeg.avpicture_fill((AVPicture*)frame, buffer, stream.FrameLayout.PixelFormat, stream.FrameLayout.Width, stream.FrameLayout.Height);
#pragma warning restore
            return new VideoFrame(frame, stream);
        }

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

namespace FFMediaToolkit.Common
{
    using System;
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
        /// Overrides this video frame data with the converted, specified <paramref name="bitmap"/>
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        public void UpdateFromBitmap(BitmapData bitmap)
        {
            fixed (byte* ptr = bitmap.Data)
            {
                // TODO: Video frame converting
                // scaler.FillAVFrame((IntPtr)ptr, Layout, this);
            }
        }

        // public BitmapData ToBitmap()
        // {
        //    // var scaler = new Scaler();
        //    // scaler.
        // }
    }
}

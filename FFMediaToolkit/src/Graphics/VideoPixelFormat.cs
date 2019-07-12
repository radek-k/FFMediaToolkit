namespace FFMediaToolkit.Graphics
{
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the most used video YUV pixel formats. Partially compatible with <see cref="AVPixelFormat"/>.
    /// </summary>
    public enum VideoPixelFormat
    {
        /// <summary>
        /// Represents a YUV 24bpp 4:4:4 video pixel format.
        /// </summary>
        Yuv444 = AVPixelFormat.AV_PIX_FMT_YUV444P,

        /// <summary>
        /// Represents a YUV 16bpp 4:2:2 video pixel format.
        /// </summary>
        Yuv422 = AVPixelFormat.AV_PIX_FMT_YUV422P,

        /// <summary>
        /// Represents a YUV 12bpp 4:2:0 video pixel format.
        /// </summary>
        Yuv420 = AVPixelFormat.AV_PIX_FMT_YUV420P,
    }
}

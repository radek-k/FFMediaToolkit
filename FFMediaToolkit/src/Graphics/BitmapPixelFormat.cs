namespace FFMediaToolkit.Graphics
{
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the most used image pixel formats. Partially compatible with <see cref="AVPixelFormat"/>
    /// </summary>
    public enum ImagePixelFormat
    {
        /// <summary>
        /// Represents a BGR 24bpp bitmap pixel format.
        /// </summary>
        BGR24 = AVPixelFormat.AV_PIX_FMT_BGR24,

        /// <summary>
        /// Represents a BGRA(with alpha channel) 32bpp bitmap pixel format. Used by default in WPF graphics.
        /// </summary>
        BGRA32 = AVPixelFormat.AV_PIX_FMT_BGRA,

        /// <summary>
        /// Represents a RGB 24bpp bitmap pixel format.
        /// </summary>
        RGB24 = AVPixelFormat.AV_PIX_FMT_RGB24,

        /// <summary>
        /// Represents a ARGB(with alpha channel) 32bpp bitmap pixel format. Used by default in GDI+ (System.Drawing) graphics.
        /// </summary>
        ARGB32 = AVPixelFormat.AV_PIX_FMT_ARGB,

        /// <summary>
        /// Represents a YUV 24bpp 4:4:4 video pixel format.
        /// </summary>
        YUV444 = AVPixelFormat.AV_PIX_FMT_YUV444P,

        /// <summary>
        /// Represents a YUV 16bpp 4:2:2 video pixel format.
        /// </summary>
        YUV422 = AVPixelFormat.AV_PIX_FMT_YUV422P,

        /// <summary>
        /// Represents a YUV 12bpp 4:2:0 video pixel format.
        /// </summary>
        YUV420 = AVPixelFormat.AV_PIX_FMT_YUV420P,
    }
}

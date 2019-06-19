namespace FFMediaToolkit.Graphics
{
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the most used image pixel formats. Partially compatible with <see cref="AVPixelFormat"/>.
    /// </summary>
    public enum ImagePixelFormat
    {
        /// <summary>
        /// Represents a BGR 24bpp bitmap pixel format. Used by default in GDI+ and WPF graphics.
        /// </summary>
        BGR24 = AVPixelFormat.AV_PIX_FMT_BGR24,

        /// <summary>
        /// Represents a BGRA(with alpha channel) 32bpp bitmap pixel format.
        /// </summary>
        BGRA32 = AVPixelFormat.AV_PIX_FMT_BGRA,

        /// <summary>
        /// Represents a RGB 24bpp bitmap pixel format.
        /// </summary>
        RGB24 = AVPixelFormat.AV_PIX_FMT_RGB24,

        /// <summary>
        /// Represents a ARGB(with alpha channel) 32bpp bitmap pixel format.
        /// </summary>
        ARGB32 = AVPixelFormat.AV_PIX_FMT_ARGB,
    }
}

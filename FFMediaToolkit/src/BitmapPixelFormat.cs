namespace FFMediaToolkit
{
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a bitmap pixel format. Compatible with <see cref="AVPixelFormat"/>
    /// </summary>
    public enum BitmapPixelFormat
    {
        /// <summary>
        /// Represents a BGR 24bpp pixel format where data are stored in 8:8:8 BGRBGR...
        /// </summary>
        BGR24 = AVPixelFormat.AV_PIX_FMT_BGR24,

        /// <summary>
        /// Represents a BGRA(with alpha channel) 32bpp pixel format where data are stored in 8:8:8:8 BGRABGRA... Used by default in WPF graphics
        /// </summary>
        BGRA32 = AVPixelFormat.AV_PIX_FMT_BGRA,

        /// <summary>
        /// Represents a RGB 24bpp pixel format RGBRGB...
        /// </summary>
        RGB24 = AVPixelFormat.AV_PIX_FMT_RGB24,

        /// <summary>
        /// Represents a ARGB(with alpha channel) 32bpp pixel format ARGBARGB... Used by default in GDI+ (System.Drawing) graphics
        /// </summary>
        ARGB32 = AVPixelFormat.AV_PIX_FMT_ARGB
    }
}

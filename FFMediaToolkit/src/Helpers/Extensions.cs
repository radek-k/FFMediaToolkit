namespace FFMediaToolkit.Helpers
{
    using FFMediaToolkit.Common.Internal;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Gets the type of content in the <see cref="AVFrame"/>.
        /// </summary>
        /// <param name="frame">The <see cref="AVFrame"/>.</param>
        /// <returns>The type of frame content.</returns>
        internal static MediaType GetMediaType(this AVFrame frame)
        {
            if (frame.width > 0 && frame.height > 0)
            {
                return MediaType.Video;
            }

            if (frame.channels > 0)
            {
                return MediaType.Audio;
            }

            return MediaType.None;
        }
    }
}

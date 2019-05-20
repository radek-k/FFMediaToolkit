namespace FFMediaToolkit.Helpers
{
    /// <summary>
    /// Contains common methods for handling FFMpeg exceptions
    /// </summary>
    internal static class ExceptionHandler
    {
        /// <summary>
        /// Checks if specified integer is error code and throws an <see cref="FFmpegException"/>.
        /// </summary>
        /// <param name="errorCode">Exit code returned by the FFMpeg method call.</param>
        /// <param name="message">The exception message.</param>
        internal static void ThrowIfError(this int errorCode, string message)
        {
            if (errorCode >= 0)
            {
                return;
            }

            throw new FFmpegException(message, errorCode);
        }
    }
}

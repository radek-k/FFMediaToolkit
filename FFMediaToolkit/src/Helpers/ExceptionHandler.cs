namespace FFMediaToolkit.Helpers
{
    using System;
    using System.Collections.Generic;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains common methods for handling FFMpeg exceptions
    /// </summary>
    internal static class ExceptionHandler
    {
        /// <summary>
        /// Checks if specified integer is error code and throws an <see cref="FFMpegException"/>
        /// </summary>
        /// <param name="errorCode">Exit code returned by the FFMpeg method call</param>
        /// <param name="desc">Information when method was called. Used to create the <see cref="FFMpegException"/> message</param>
        internal static void ThrowIfError(this int errorCode, string desc)
        {
            if (errorCode >= 0)
            {
                return;
            }

            throw new FFMpegException(CreateMessage(errorCode, "ERR", desc));
        }

        private static string CreateMessage(int errorCode, string error, string desc)
            => $"An exception ocurred while {desc}. FFMpeg method call returned {error.ToLower()} error (code: {errorCode})";
    }
}

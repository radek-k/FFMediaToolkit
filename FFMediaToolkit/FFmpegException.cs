namespace FFMediaToolkit
{
    using System;
    using FFMediaToolkit.Helpers;

    /// <summary>
    /// Represents an exception thrown when FFMpeg method call returns an error code.
    /// </summary>
    [Serializable]
    public class FFmpegException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegException"/> class.
        /// </summary>
        public FFmpegException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegException"/> class using a message and a error code.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public FFmpegException(string message)
            : base(message)
            => ErrorMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFmpegException"/> class using a message and a error code.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="errorCode">The error code returned by the FFmpeg method.</param>
        public FFmpegException(string message, int errorCode)
            : base(CreateMessage(message, errorCode))
        {
            ErrorCode = errorCode;
            ErrorMessage = StringConverter.DecodeMessage(errorCode);
        }

        /// <summary>
        /// Gets the error code returned by the FFmpeg method.
        /// </summary>
        public int? ErrorCode { get; }

        /// <summary>
        /// Gets the message text decoded from error code.
        /// </summary>
        public string ErrorMessage { get; }

        private static string CreateMessage(string msg, int errCode)
            => $"{msg} Error code: {errCode} : {StringConverter.DecodeMessage(errCode)}";
    }
}

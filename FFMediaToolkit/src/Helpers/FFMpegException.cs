namespace FFMediaToolkit.Helpers
{
    using System;

    /// <summary>
    /// Represents an exception thrown when FFMpeg method call returns an error code
    /// </summary>
    [Serializable]
    public class FFMpegException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FFMpegException"/> class.
        /// </summary>
        public FFMpegException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FFMpegException"/> class with exception message
        /// </summary>
        /// <param name="message">Exception message</param>
        public FFMpegException(string message)
            : base(message) { }

        public int? FFMpegErrorCode { get; }

    }
}

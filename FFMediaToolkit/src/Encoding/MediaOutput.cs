namespace FFMediaToolkit.Encoding
{
    using System;
    using FFMediaToolkit.Common;

    /// <summary>
    /// Represents a multimedia output file.
    /// </summary>
    public class MediaOutput : IDisposable
    {
        private MediaContainer container;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOutput"/> class.
        /// </summary>
        /// <param name="mediaContainer">The <see cref="MediaContainer"/> object.</param>
        /// <param name="videoSettings">The video stream settings.</param>
        internal MediaOutput(MediaContainer mediaContainer, VideoEncoderSettings videoSettings)
        {
            container = mediaContainer;

            if (mediaContainer.Video != null)
            {
                Video = new VideoEncoderStream(mediaContainer.Video, videoSettings);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaOutput"/> class.
        /// </summary>
        ~MediaOutput() => Dispose();

        /// <summary>
        /// Gets the video stream in the media file.
        /// </summary>
        public VideoEncoderStream Video { get; }

        /// <summary>
        /// Gets a value indicating whether the media file contains video stream.
        /// </summary>
        public bool HasVideo => Video != null;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
                return;

            container.Dispose();

            isDisposed = true;
        }
    }
}

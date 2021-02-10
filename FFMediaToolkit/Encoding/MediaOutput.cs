namespace FFMediaToolkit.Encoding
{
    using System;
    using System.Linq;
    using FFMediaToolkit.Encoding.Internal;

    /// <summary>
    /// Represents a multimedia output file.
    /// </summary>
    public class MediaOutput : IDisposable
    {
        private readonly OutputContainer container;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOutput"/> class.
        /// </summary>
        /// <param name="mediaContainer">The <see cref="OutputContainer"/> object.</param>
        internal MediaOutput(OutputContainer mediaContainer)
        {
            container = mediaContainer;

            VideoStreams = container.Video
                .Select(o => new VideoOutputStream(o.stream, o.config))
                .ToArray();

            AudioStreams = container.Audio
                .Select(o => new AudioOutputStream(o.stream, o.config))
                .ToArray();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaOutput"/> class.
        /// </summary>
        ~MediaOutput() => Dispose();

        /// <summary>
        /// Gets the video streams in the media file.
        /// </summary>
        public VideoOutputStream[] VideoStreams { get; }

        /// <summary>
        /// Gets the audio streams in the media file.
        /// </summary>
        public AudioOutputStream[] AudioStreams { get; }

        /// <summary>
        /// Gets the first video stream in the media file.
        /// </summary>
        public VideoOutputStream Video => VideoStreams.FirstOrDefault();

        /// <summary>
        /// Gets the first audio stream in the media file.
        /// </summary>
        public AudioOutputStream Audio => AudioStreams.FirstOrDefault();

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

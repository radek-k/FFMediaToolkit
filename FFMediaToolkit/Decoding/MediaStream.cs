namespace FFMediaToolkit.Decoding
{
    using System;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;

    /// <summary>
    /// A base for streams of any kind of media.
    /// </summary>
    public class MediaStream : IDisposable
    {
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaStream"/> class.
        /// </summary>
        /// <param name="stream">The associated codec.</param>
        /// <param name="options">Extra options.</param>
        internal MediaStream(Decoder stream, MediaOptions options)
        {
            Stream = stream;
            Options = options;

            Threshold = TimeSpan.FromSeconds(0.5).ToTimestamp(Info.TimeBase);
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public StreamInfo Info => Stream.Info;

        /// <summary>
        /// Gets the timestamp of the recently decoded frame in the media stream.
        /// </summary>
        public TimeSpan Position => Math.Max(Stream.RecentlyDecodedFrame.PresentationTimestamp, 0).ToTimeSpan(Info.TimeBase);

        /// <summary>
        /// Indicates whether the stream has buffered frame data.
        /// </summary>
        public bool IsBufferEmpty => Stream.IsBufferEmpty;

        /// <summary>
        /// Gets the options configured for this <see cref="MediaStream"/>.
        /// </summary>
        protected MediaOptions Options { get; }

        private Decoder Stream { get; }

        private long Threshold { get; }

        /// <summary>
        /// Discards all buffered frame data associated with this stream.
        /// </summary>
        [Obsolete("Do not call this method. Buffered data is automatically discarded when required")]
        public void DiscardBufferedData() => Stream.DiscardBufferedData();

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            if (!isDisposed)
            {
                Stream.DiscardBufferedData();
                Stream.Dispose();
                isDisposed = true;
            }
        }

        /// <summary>
        /// Gets the data belonging to the next frame in the stream.
        /// </summary>
        /// <returns>The next frame's data.</returns>
        internal MediaFrame GetNextFrame() => Stream.GetNextFrame();

        /// <summary>
        /// Seeks the stream to the specified time and returns the nearest frame's data.
        /// </summary>
        /// <param name="time">A specific point in time in this stream.</param>
        /// <returns>The nearest frame's data.</returns>
        internal MediaFrame GetFrame(TimeSpan time)
        {
            var frame = Stream.RecentlyDecodedFrame;
            var ts = Math.Max(0, Math.Min(time.ToTimestamp(Info.TimeBase), Info.DurationRaw));

            if (ts < frame.PresentationTimestamp || ts >= frame.PresentationTimestamp + Threshold)
            {
                Stream.OwnerFile.SeekFile(ts, Info.Index);
            }

            if (ts < frame.PresentationTimestamp || ts >= frame.PresentationTimestamp + frame.Duration)
            {
                Stream.SkipFrames(ts);
            }

            return frame;
        }
    }
}

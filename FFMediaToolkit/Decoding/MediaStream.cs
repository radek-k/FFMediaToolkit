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
        private readonly Decoder decoder;
        private readonly long seekThreshold;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaStream"/> class.
        /// </summary>
        /// <param name="stream">The associated codec.</param>
        /// <param name="options">Extra options.</param>
        /// <param name="seekThreshold">Seek threshold in milliseconds.</param>
        internal MediaStream(Decoder stream, MediaOptions options, int seekThreshold)
        {
            decoder = stream;
            Options = options;
            this.seekThreshold = TimeSpan.FromMilliseconds(seekThreshold).ToTimestamp(Info.TimeBase);
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public StreamInfo Info => decoder.Info;

        /// <summary>
        /// Gets the timestamp of the recently decoded frame in the media stream.
        /// </summary>
        public TimeSpan Position => Math.Max(decoder.RecentlyDecodedFrame.PresentationTimestamp, 0).ToTimeSpan(Info.TimeBase);

        /// <summary>
        /// Indicates whether the stream has buffered frame data.
        /// </summary>
        public bool IsBufferEmpty => decoder.IsBufferEmpty;

        /// <summary>
        /// Gets the options configured for this <see cref="MediaStream"/>.
        /// </summary>
        protected MediaOptions Options { get; }

        /// <summary>
        /// Discards all buffered frame data associated with this stream.
        /// </summary>
        [Obsolete("Do not call this method. Buffered data is automatically discarded when required")]
        public void DiscardBufferedData() => decoder.DiscardBufferedData();

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            if (!isDisposed)
            {
                decoder.DiscardBufferedData();
                decoder.Dispose();
                isDisposed = true;
            }
        }

        /// <summary>
        /// Gets the data belonging to the next frame in the stream.
        /// </summary>
        /// <returns>The next frame's data.</returns>
        internal MediaFrame GetNextFrame()
        {
            decoder.ReadNextFrame();
            return decoder.RecentlyDecodedFrame;
        }

        /// <summary>
        /// Seeks the stream to the specified time and returns the nearest frame's data.
        /// </summary>
        /// <param name="time">A specific point in time in this stream.</param>
        /// <returns>The nearest frame's data.</returns>
        internal MediaFrame GetFrame(TimeSpan time)
        {
            var frame = decoder.RecentlyDecodedFrame;
            var requestedTs = Math.Max(0, Math.Min(time.ToTimestamp(Info.TimeBase), Info.DurationRaw));

            if (requestedTs < frame.PresentationTimestamp || requestedTs >= frame.PresentationTimestamp + seekThreshold)
            {
                decoder.OwnerFile.SeekFile(requestedTs, Info.Index);
            }

            while (frame.PresentationTimestamp + frame.Duration <= requestedTs)
            {
                decoder.ReadNextFrame();
            }

            return frame;
        }
    }
}

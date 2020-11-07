namespace FFMediaToolkit.Decoding
{
    using System;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;

    /// <summary>
    /// Represents a Audio stream in the <see cref="MediaFile"/>.
    /// </summary>
    public class AudioStream : IDisposable
    {
        private readonly Decoder<AudioFrame> stream;
        private readonly AudioFrame frame;
        private readonly MediaOptions mediaOptions;

        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStream"/> class.
        /// </summary>
        /// <param name="audio">The Audio stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal AudioStream(Decoder<AudioFrame> audio, MediaOptions options)
        {
            stream = audio;
            mediaOptions = options;
            frame = AudioFrame.CreateEmpty();
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public AudioStreamInfo Info => (AudioStreamInfo)stream.Info;

        /// <summary>
        /// Gets the index of the next frame in the Audio stream.
        /// </summary>
        public int FramePosition { get; private set; }

        /// <summary>
        /// Gets the timestamp of the next frame in the Audio stream.
        /// </summary>
        public TimeSpan Position => FramePosition.ToTimeSpan(Info.AvgFrameRate);

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            lock (syncLock)
            {
                stream.Dispose();
                frame.Dispose();
            }
        }

        public ReadOnlySpan<float> NextFrame() {
            var frame = stream.GetNextFrame();
            return frame.GetData(0);
        }

        private AudioFrame SeekToFrame(int frameNumber)
        {
            var ts = frameNumber.ToTimestamp(Info.RealFrameRate, Info.TimeBase);

            if (frameNumber < FramePosition || frameNumber > FramePosition + mediaOptions.VideoSeekThreshold)
            {
                stream.OwnerFile.SeekFile(ts, Info.Index);
            }

            stream.SkipFrames(frameNumber.ToTimestamp(Info.RealFrameRate, Info.TimeBase));
            return stream.RecentlyDecodedFrame;
        }
    }
}

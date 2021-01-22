namespace FFMediaToolkit.Encoding
{
    using System;
    using FFMediaToolkit.Audio;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Encoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents an audio encoder stream.
    /// </summary>
    public unsafe class AudioOutputStream : IDisposable
    {
        private readonly OutputStream<AudioFrame> stream;
        private readonly AudioFrame frame;

        private SwrContext* swrContext;

        private bool isDisposed;
        private long lastFramePts = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioOutputStream"/> class.
        /// </summary>
        /// <param name="stream">The audio stream.</param>
        /// <param name="config">The stream setting.</param>
        internal AudioOutputStream(OutputStream<AudioFrame> stream, AudioEncoderSettings config)
        {
            this.stream = stream;

            long channelLayout = ffmpeg.av_get_default_channel_layout(config.Channels);
            swrContext = ffmpeg.swr_alloc_set_opts(
                null,
                channelLayout,
                (AVSampleFormat)config.SampleFormat,
                config.SampleRate,
                channelLayout,
                (AVSampleFormat)SampleFormat.SingleP,
                config.SampleRate,
                0,
                null);

            ffmpeg.swr_init(swrContext);

            Configuration = config;
            frame = AudioFrame.Create(config.SampleRate, config.Channels, config.SamplesPerFrame, channelLayout, SampleFormat.SingleP);
        }

        /// <summary>
        /// Gets the video encoding configuration used to create this stream.
        /// </summary>
        public AudioEncoderSettings Configuration { get; }

        /// <summary>
        /// Gets the current duration of this stream.
        /// </summary>
        public TimeSpan CurrentDuration => lastFramePts.ToTimeSpan(Configuration.TimeBase);

        /// <summary>
        /// Writes the specified audio data to the stream as the next frame.
        /// </summary>
        /// <param name="data">The audio data to write.</param>
        /// <param name="customPtsValue">(optional) custom PTS value for the frame.</param>
        public void AddFrame(AudioData data, long customPtsValue)
        {
            if (customPtsValue <= lastFramePts)
                throw new Exception("Cannot add a frame that occurs chronologically before the most recently written frame!");

            frame.UpdateFromAudioData(data);

            var converted = AudioFrame.Create(
                frame.SampleRate,
                frame.NumChannels,
                frame.NumSamples,
                frame.ChannelLayout,
                Configuration.SampleFormat);
            converted.PresentationTimestamp = customPtsValue;

            ffmpeg.swr_convert_frame(swrContext, converted.Pointer, frame.Pointer);

            stream.Push(converted);
            converted.Dispose();

            lastFramePts = customPtsValue;
        }

        /// <summary>
        /// Writes the specified sample data to the stream as the next frame.
        /// </summary>
        /// <param name="samples">The sample data to write.</param>
        /// <param name="customPtsValue">(optional) custom PTS value for the frame.</param>
        public void AddFrame(float[][] samples, long customPtsValue)
        {
            if (customPtsValue <= lastFramePts)
                throw new Exception("Cannot add a frame that occurs chronologically before the most recently written frame!");

            frame.UpdateFromSampleData(samples);
            frame.PresentationTimestamp = customPtsValue;
            stream.Push(frame);

            lastFramePts = customPtsValue;
        }

        /// <summary>
        /// Writes the specified audio data to the stream as the next frame.
        /// </summary>
        /// <param name="data">The audio data to write.</param>
        /// <param name="customTime">Custom timestamp for this frame.</param>
        public void AddFrame(AudioData data, TimeSpan customTime) => AddFrame(data, customTime.ToTimestamp(Configuration.TimeBase));

        /// <summary>
        /// Writes the specified audio data to the stream as the next frame.
        /// </summary>
        /// <param name="data">The audio data to write.</param>
        public void AddFrame(AudioData data) => AddFrame(data, lastFramePts + 1);

        /// <summary>
        /// Writes the specified sample data to the stream as the next frame.
        /// </summary>
        /// <param name="samples">The sample data to write.</param>
        /// <param name="customTime">Custom timestamp for this frame.</param>
        public void AddFrame(float[][] samples, TimeSpan customTime) => AddFrame(samples, customTime.ToTimestamp(Configuration.TimeBase));

        /// <summary>
        /// Writes the specified sample data to the stream as the next frame.
        /// </summary>
        /// <param name="samples">The sample data to write.</param>
        public void AddFrame(float[][] samples) => AddFrame(samples, lastFramePts + 1);

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            stream.Dispose();
            frame.Dispose();

            fixed (SwrContext** ptr = &swrContext)
            {
                ffmpeg.swr_free(ptr);
            }

            isDisposed = true;
        }
    }
}

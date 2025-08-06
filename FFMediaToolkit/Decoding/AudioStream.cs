namespace FFMediaToolkit.Decoding
{
    using System;
    using System.IO;
    using FFMediaToolkit.Audio;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents an audio stream in the <see cref="MediaFile"/>.
    /// </summary>
    public unsafe class AudioStream : MediaStream
    {
        private SwrContext* swrContext;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStream"/> class.
        /// </summary>
        /// <param name="stream">The audio stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal AudioStream(Decoder stream, MediaOptions options)
            : base(stream, options, options.AudioSeekThreshold)
        {
            var layout = Info.ChannelLayout;
            SwrContext* context;
            ffmpeg.swr_alloc_set_opts2(
                &context,
                &layout,
                (AVSampleFormat)SampleFormat.SingleP,
                Info.SampleRate,
                &layout,
                (AVSampleFormat)Info.SampleFormat,
                Info.SampleRate,
                0,
                null).ThrowIfError("Cannot allocate SwrContext");
            ffmpeg.swr_init(context);
            swrContext = context;
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public new AudioStreamInfo Info => base.Info as AudioStreamInfo;

        /// <summary>
        /// Reads the next frame from the audio stream.
        /// </summary>
        /// <returns>The decoded audio data.</returns>
        public new AudioData GetNextFrame()
        {
            var frame = base.GetNextFrame() as AudioFrame;

            var converted = AudioFrame.Create(
                frame.SampleRate,
                frame.NumChannels,
                frame.NumSamples,
                frame.ChannelLayout,
                SampleFormat.SingleP,
                frame.DecodingTimestamp,
                frame.PresentationTimestamp);

            ffmpeg.swr_convert_frame(swrContext, converted.Pointer, frame.Pointer);

            return new AudioData(converted);
        }

        /// <summary>
        /// Reads the next frame from the audio stream.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="data">The decoded audio data.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryGetNextFrame(out AudioData data)
        {
            try
            {
                data = GetNextFrame();
                return true;
            }
            catch (EndOfStreamException)
            {
                data = default;
                return false;
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <returns>The decoded video frame.</returns>
        public new AudioData GetFrame(TimeSpan time)
        {
            var frame = base.GetFrame(time) as AudioFrame;

            var converted = AudioFrame.Create(
                frame.SampleRate,
                frame.NumChannels,
                frame.NumSamples,
                frame.ChannelLayout,
                SampleFormat.SingleP,
                frame.DecodingTimestamp,
                frame.PresentationTimestamp);

            ffmpeg.swr_convert_frame(swrContext, converted.Pointer, frame.Pointer);

            return new AudioData(converted);
        }

        /// <summary>
        /// Reads the audio data found at the specified timestamp.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <param name="data">The decoded audio data.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryGetFrame(TimeSpan time, out AudioData data)
        {
            try
            {
                data = GetFrame(time);
                return true;
            }
            catch (EndOfStreamException)
            {
                data = default;
                return false;
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (!isDisposed)
            {
                fixed (SwrContext** ptr = &swrContext)
                {
                    ffmpeg.swr_free(ptr);
                }

                isDisposed = true;
            }

            base.Dispose();
        }
    }
}

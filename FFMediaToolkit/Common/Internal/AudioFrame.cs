namespace FFMediaToolkit.Common.Internal {
    using System;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represent an audio frame.
    /// </summary>
    internal unsafe class AudioFrame : MediaFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFrame"/> class with empty frame data.
        /// </summary>
        public AudioFrame()
            : base(ffmpeg.av_frame_alloc())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioFrame"/> class using existing <see cref="AVFrame"/>.
        /// </summary>
        /// <param name="frame">The audio <see cref="AVFrame"/>.</param>
        public AudioFrame(AVFrame* frame)
            : base(frame)
        {
            if (frame->GetMediaType() != MediaType.Audio)
                throw new ArgumentException("Cannot create an AudioFrame instance from the AVFrame with type: " + frame->GetMediaType());
        }

        /// <summary>
        /// Gets the number of samples.
        /// </summary>
        public int NumSamples => Pointer != null ? Pointer->nb_samples : default;

        /// <summary>
        /// Gets the number of channels.
        /// </summary>
        public int NumChannels => Pointer != null ? Pointer->channels : default;

        /// <summary>
        /// Gets the audio sample format.
        /// </summary>
        public AVSampleFormat SampleFormat => Pointer != null ? (AVSampleFormat) Pointer->format : AVSampleFormat.AV_SAMPLE_FMT_NONE;

        /// <summary>
        /// Creates an audio frame with given dimensions and allocates a buffer for it.
        /// </summary>
        /// <param name="num_samples">The number of samples in the audio frame.</param>
        /// <param name="num_channels">The number of channels in the audio frame.</param>
        /// <param name="sampleFormat">The audio sample format.</param>
        /// <returns>The new audio frame.</returns>
        public static AudioFrame Create(int num_samples, int num_channels, AVSampleFormat sampleFormat)
        {
            var frame = ffmpeg.av_frame_alloc();
            frame->nb_samples = num_samples;
            frame->channels = num_channels;
            frame->format = (int)sampleFormat;

            ffmpeg.av_frame_get_buffer(frame, 32);

            return new AudioFrame(frame);
        }

        /// <summary>
        /// Creates an empty frame for decoding.
        /// </summary>
        /// <returns>The empty <see cref="AudioFrame"/>.</returns>
        public static AudioFrame CreateEmpty() => new AudioFrame();

        /// <summary>
        /// Fetches raw audio data from this video frame for specified channel.
        /// </summary>
        public ReadOnlySpan<float> GetData(uint channel)
        {
            return new ReadOnlySpan<float>(Pointer->data[channel], NumSamples);
        }

        /// <inheritdoc/>
        internal override unsafe void Update(AVFrame* newFrame)
        {
            if (newFrame->GetMediaType() != MediaType.Audio)
            {
                throw new ArgumentException("The new frame doesn't contain audio data.");
            }

            base.Update(newFrame);
        }
    }
}

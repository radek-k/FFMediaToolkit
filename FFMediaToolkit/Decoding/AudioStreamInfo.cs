namespace FFMediaToolkit.Decoding
{
    using System;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents informations about the audio stream.
    /// </summary>
    public class AudioStreamInfo : StreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStreamInfo"/> class.
        /// </summary>
        /// <param name="stream">A generic stream.</param>
        /// <param name="container">The input container.</param>
        internal unsafe AudioStreamInfo(AVStream* stream, InputContainer container)
             : base(stream, MediaType.Audio, container)
        {
            var codec = stream->codec;
            NumChannels = codec->channels;
            SampleRate = codec->sample_rate;
            long num_samples = stream->duration >= 0 ? stream->duration : container.Pointer->duration;
            AvgNumSamplesPerFrame = (int)Math.Round((double)num_samples / NumberOfFrames.Value);
            SampleFormat = codec->sample_fmt.FormatEnum(14);
            AvSampleFormat = codec->sample_fmt;
        }

        /// <summary>
        /// Gets the number of audio channels stored in the stream.
        /// </summary>
        public int NumChannels { get; }

        /// <summary>
        /// Gets the number of samples per second of the audio stream.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// Gets the average number of samples per frame (chunk of samples) calculated from metadata.
        /// It is used to calculate timestamps in the internal decoder methods.
        /// </summary>
        public int AvgNumSamplesPerFrame { get; }

        /// <summary>
        /// Gets a lowercase string representing the audio sample format.
        /// </summary>
        public string SampleFormat { get; }

        /// <summary>
        /// Gets the audio sample format.
        /// </summary>
        internal AVSampleFormat AvSampleFormat { get; }
    }
}

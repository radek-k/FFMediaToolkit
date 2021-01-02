namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents generic informations about the stream, specialized by subclasses for specific
    /// kinds of streams.
    /// </summary>
    public class StreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamInfo"/> class.
        /// </summary>
        /// <param name="stream">A generic stream.</param>
        /// <param name="container">The input container.</param>
        internal unsafe StreamInfo(AVStream* stream, InputContainer container)
        {
            var codec = stream->codec;
            Metadata = new ReadOnlyDictionary<string, string>(FFDictionary.ToDictionary(stream->metadata));
            CodecName = ffmpeg.avcodec_get_name(codec->codec_id);
            CodecId = codec->codec_id.FormatEnum(12);
            Index = stream->index;
            IsInterlaced = codec->field_order != AVFieldOrder.AV_FIELD_PROGRESSIVE &&
                           codec->field_order != AVFieldOrder.AV_FIELD_UNKNOWN;
            TimeBase = stream->time_base;

            RealFrameRate = stream->r_frame_rate;
            AvgFrameRate = stream->avg_frame_rate.ToDouble();
            IsVariableFrameRate = RealFrameRate.ToDouble() != AvgFrameRate;

            if (stream->duration >= 0)
            {
                Duration = stream->duration.ToTimeSpan(stream->time_base);
                DurationRaw = stream->duration;
            }
            else
            {
                Duration = TimeSpan.FromTicks(container.Pointer->duration * 10);
                DurationRaw = Duration.ToTimestamp(TimeBase);
            }

            var start = stream->start_time.ToTimeSpan(stream->time_base);
            StartTime = start == TimeSpan.MinValue ? TimeSpan.Zero : start;

            if (stream->nb_frames > 0)
            {
                IsFrameCountProvidedByContainer = true;
                NumberOfFrames = (int)stream->nb_frames;
                FrameCount = NumberOfFrames.Value;
            }
            else
            {
                FrameCount = Duration.ToFrameNumber(stream->avg_frame_rate);
                if (!IsVariableFrameRate)
                {
                    NumberOfFrames = FrameCount;
                }
                else
                {
                    NumberOfFrames = null;
                }
            }
        }

        /// <summary>
        /// Creates the apprioriate type of <see cref="StreamInfo"/> class depending on the kind
        /// of stream passed in <see cref="stream"></see>.
        /// </summary>
        /// <param name="stream">The represented stream.</param>
        /// <param name="owner">The input container.</param>
        internal static unsafe StreamInfo Create(AVStream* stream, InputContainer owner) {
            var codec = stream->codec;
            if (codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                return new AudioStreamInfo(stream, owner);
            if (codec->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO)
                return new VideoStreamInfo(stream, owner);
            return new StreamInfo(stream, owner);
        }

        /// <summary>
        /// Gets the stream index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the codec name.
        /// </summary>
        public string CodecName { get; }

        /// <summary>
        /// Gets the codec identifier.
        /// </summary>
        public string CodecId { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="FrameCount"/> value is know from the multimedia container metadata.
        /// </summary>
        public bool IsFrameCountProvidedByContainer { get; }

        /// <summary>
        /// Gets a value indicating whether the frames in the stream are interlaced.
        /// </summary>
        public bool IsInterlaced { get; }

        /// <summary>
        /// Gets the stream time base.
        /// </summary>
        public AVRational TimeBase { get; }

        /// <summary>
        /// Gets the average frame rate as a <see cref="double"/> value.
        /// </summary>
        public double AvgFrameRate { get; protected set; }

        /// <summary>
        /// Gets the number of frames value from the container metadata, if available (see <see cref="IsFrameCountProvidedByContainer"/>)
        /// Otherwise, it is estimated from the video duration and average frame rate.
        /// This value may not be accurate, e.g. for videos with variable frame rate (see <see cref="VideoStreamInfo.IsVariableFrameRate"/> property).
        /// </summary>
        [Obsolete("Please use \"StreamInfo.NumberOfFrames\" property instead.")]
        public int FrameCount { get; }

        /// <summary>
        /// Gets the stream duration.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the stream start time. Null if undefined.
        /// </summary>
        public TimeSpan? StartTime { get; }

        /// <summary>
        /// Gets the stream metadata.
        /// </summary>
        public ReadOnlyDictionary<string, string> Metadata { get; }

        /// <summary>
        /// Gets the duration of the stream in the time base units.
        /// </summary>
        internal long DurationRaw { get; }
    }

    /// <summary>
    /// Represents informations about the video stream.
    /// </summary>
    public class VideoStreamInfo : StreamInfo {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStreamInfo"/> class.
        /// </summary>
        /// <param name="stream">A generic stream.</param>
        /// <param name="container">The input container.</param>
        internal unsafe VideoStreamInfo(AVStream* stream, InputContainer container)
             : base(stream, container)
        {
            var codec = stream->codec;
            AvgFrameRate = stream->avg_frame_rate.ToDouble();
            IsVariableFrameRate = RealFrameRate.ToDouble() != AvgFrameRate;
            RealFrameRate = stream->r_frame_rate;
            FrameSize = new Size(codec->width, codec->height);
            PixelFormat = codec->pix_fmt.FormatEnum(11);
            AVPixelFormat = codec->pix_fmt;
        }

        /// <summary>
        /// Gets the frame rate as a <see cref="AVRational"/> value.
        /// It is used to calculate timestamps in the internal decoder methods.
        /// </summary>
        public AVRational RealFrameRate { get; }

        /// <summary>
        /// Gets a value indicating whether the video is variable frame rate (VFR).
        /// </summary>
        public bool IsVariableFrameRate { get; }

        /// <summary>
        /// Gets the video frame dimensions.
        /// </summary>
        public Size FrameSize { get; }

        /// <summary>
        /// Gets a lowercase string representing the video pixel format.
        /// </summary>
        public string PixelFormat { get; }

        /// <summary>
        /// Gets the video pixel format.
        /// </summary>
        internal AVPixelFormat AVPixelFormat { get; }
    }

    /// <summary>
    /// Represents informations about the audio stream.
    /// </summary>
    public class AudioStreamInfo : StreamInfo {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStreamInfo"/> class.
        /// Gets the number of frames value taken from the container metadata or estimated in constant frame rate videos. Returns <see langword="null"/> if not available.
        /// </summary>
        public int? NumberOfFrames { get; }

        /// <summary>
        /// Gets the stream duration.
        /// </summary>
        /// <param name="stream">A generic stream.</param>
        /// <param name="container">The input container.</param>
        internal unsafe AudioStreamInfo(AVStream* stream, InputContainer container)
             : base(stream, container)
        {
            var codec = stream->codec;
            NumChannels = codec->channels;
            SampleRate = codec->sample_rate;
            long num_samples = stream->duration >= 0 ? stream->duration : container.Pointer->duration;
            AvgNumSamplesPerFrame = (int)Math.Round((double)num_samples / FrameCount);
            AvgFrameRate = SampleRate / AvgNumSamplesPerFrame;
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
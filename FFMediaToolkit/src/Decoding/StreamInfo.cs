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
    /// Represents informations about the video stream.
    /// </summary>
    public class StreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamInfo"/> class.
        /// </summary>
        /// <param name="stream">The video stram.</param>
        /// <param name="container">The input container.</param>
        internal unsafe StreamInfo(AVStream* stream, InputContainer container)
        {
            var codec = stream->codec;
            Metadata = new ReadOnlyDictionary<string, string>(FFDictionary.ToDictionary(stream->metadata));
            CodecName = ffmpeg.avcodec_get_name(codec->codec_id);
            CodecId = FormatCodecId(codec->codec_id);
            Index = stream->index;
            IsInterlaced = codec->field_order != AVFieldOrder.AV_FIELD_PROGRESSIVE &&
                           codec->field_order != AVFieldOrder.AV_FIELD_UNKNOWN;
            FrameSize = new Size(codec->width, codec->height);
            PixelFormat = codec->pix_fmt;
            TimeBase = stream->time_base;
            RFrameRate = stream->r_frame_rate;
            FrameRate = RFrameRate.ToDouble();
            Duration = stream->duration >= 0
                ? stream->duration.ToTimeSpan(stream->time_base)
                : TimeSpan.FromTicks(container.Pointer->duration * 10);
            var start = stream->start_time.ToTimeSpan(stream->time_base);
            StartTime = start == TimeSpan.MinValue ? TimeSpan.Zero : start;
            FrameCount = Duration.ToFrameNumber(RFrameRate);
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
        /// Gets a value indicating whether the frames in the stream are interlaced.
        /// </summary>
        public bool IsInterlaced { get; }

        /// <summary>
        /// Gets the stream frame rate as a <see cref="double"/> value.
        /// </summary>
        public double FrameRate { get; }

        /// <summary>
        /// Gets the frame rate as a <see cref="AVRational"/> value.
        /// </summary>
        public AVRational RFrameRate { get; }

        /// <summary>
        /// Gets the stream time base.
        /// </summary>
        public AVRational TimeBase { get; }

        /// <summary>
        /// Gets the video frame dimensions.
        /// </summary>
        public Size FrameSize { get; }

        /// <summary>
        /// Gets the video pixel format.
        /// </summary>
        public AVPixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the estimated number of frames in the stream.
        /// </summary>
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

        private static string FormatCodecId(AVCodecID id) => id.ToString().Substring(12).ToLower();
    }
}
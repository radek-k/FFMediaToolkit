namespace FFMediaToolkit.Decoding
{
    using System.Drawing;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents informations about the video stream.
    /// </summary>
    public class VideoStreamInfo : StreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStreamInfo"/> class.
        /// </summary>
        /// <param name="stream">A generic stream.</param>
        /// <param name="container">The input container.</param>
        internal unsafe VideoStreamInfo(AVStream* stream, InputContainer container)
             : base(stream, MediaType.Video, container)
        {
            var codec = stream->codecpar;
            IsInterlaced = codec->field_order != AVFieldOrder.AV_FIELD_PROGRESSIVE &&
                           codec->field_order != AVFieldOrder.AV_FIELD_UNKNOWN;
            FrameSize = new Size(codec->width, codec->height);
            PixelFormat = ((AVPixelFormat)codec->format).FormatEnum(11);
            AVPixelFormat = (AVPixelFormat)codec->format;
        }

        /// <summary>
        /// Gets a value indicating whether the frames in the stream are interlaced.
        /// </summary>
        public bool IsInterlaced { get; }

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
}

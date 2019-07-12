namespace FFMediaToolkit.Encoding
{
    using System.Collections.Generic;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a video encoder configuration.
    /// </summary>
    public class VideoEncoderSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoEncoderSettings"/> class with default video settings values.
        /// </summary>
        /// <param name="width">The video frame width.</param>
        /// <param name="height">The video frame height.</param>
        public VideoEncoderSettings(int width, int height)
        {
            Bitrate = 30_000_000;
            KeyframeRate = 12;
            VideoWidth = width;
            VideoHeight = height;
            VideoFormat = VideoPixelFormat.Yuv420;
            Framerate = 30;
            CodecOptions = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the video stream bitrate (bytes per seconds). The default value is 30000000 B/s.
        /// </summary>
        public int Bitrate { get; set; }

        /// <summary>
        /// Gets or sets the GoP value. The default value is 12.
        /// </summary>
        public int KeyframeRate { get; set; }

        /// <summary>
        /// Gets or sets the video frame width.
        /// </summary>
        public int VideoWidth { get; set; }

        /// <summary>
        /// Gets or sets the video frame height.
        /// </summary>
        public int VideoHeight { get; set; }

        /// <summary>
        /// Gets or sets the output video pixel format. The default value is YUV420p.
        /// Added frames will be automatically converted to this format.
        /// </summary>
        public VideoPixelFormat VideoFormat { get; set; }

        /// <summary>
        /// Gets or sets video frame rate (FPS) value. The default value is 30 frames/s.
        /// </summary>
        public int Framerate { get; set; }

        /// <summary>
        /// Gets or sets the dictionary with custom codec options.
        /// </summary>
        public Dictionary<string, string> CodecOptions { get; set; }

        /// <summary>
        /// Gets or sets the codec for this stream.
        /// If <see langword="null"/>, encoder will use default video codec for current container.
        /// WARNING! Many codecs are deprecated or don't work with video.
        /// </summary>
        public AVCodecID? Codec { get; set; }
    }
}

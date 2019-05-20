namespace FFMediaToolkit.Encoding
{
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a reusable video encoder configuration
    /// </summary>
    public class VideoEncoderSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoEncoderSettings"/> class with default video settings values;
        /// </summary>
        /// <param name="width">The video width</param>
        /// <param name="height">The video height</param>
        public VideoEncoderSettings(int width, int height)
        {
            Bitrate = 30_000_000;
            KeyframeRate = 12;
            VideoWidth = width;
            VideoHeight = height;
            VideoPixelFormat = ImagePixelFormat.YUV420;
            Framerate = 30;
            CodecOptions = new FFDictionary();
        }

        /// <summary>
        /// Gets or sets the video stream bitrate (bytes per seconds). The default value is 30000000 B/s. If bitrate is too low, the result video will be pixelated
        /// </summary>
        public int Bitrate { get; set; }

        /// <summary>
        /// Gets or sets the GoP value. The default value is 12. Higher value = lower file size
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
        /// Sended frames will be automatically converted to this format
        /// </summary>
        public ImagePixelFormat VideoPixelFormat { get; set; }

        /// <summary>
        /// Gets or sets video frame rate (FPS) value. The default value is 30 frames/s
        /// </summary>
        public int Framerate { get; set; }

        /// <summary>
        /// Gets or sets the dictionary with custom codec options.
        /// </summary>
        public FFDictionary CodecOptions { get; set; }

        /// <summary>
        /// Gets or sets the codec for this stream.
        /// If <see langword="null"/>, encoder will use default video codec for current container.
        /// WARNING! Many codecs are deprecated or don't work with video.
        /// </summary>
        public AVCodecID? Codec { get; set; }

        /// <summary>
        /// Creates a <see cref="Layout"/> object, containg dimensions and pixel format, based on this settings
        /// </summary>
        /// <returns>New <see cref="Layout"/> object</returns>
        public Layout ToLayout() => new Layout((AVPixelFormat)VideoPixelFormat, VideoWidth, VideoHeight);
    }
}

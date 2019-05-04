namespace FFMediaToolkit
{
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a reusable video encoder configuration
    /// </summary>
    public class VideoEncoderSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoEncoderSettings"/> class with default video settings values;
        /// </summary>
        public VideoEncoderSettings()
        {
            Bitrate = 30_000_000;
            KeyframeRate = 12;
            VideoWidth = 0;
            VideoHeight = 0;
            VideoPixelFormat = BitmapPixelFormat.ARGB32;
            Framerate = 30;
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
        /// Gets or sets the video frame width. If not set, it will be set from the first pushed frame
        /// </summary>
        public int VideoWidth { get; set; }

        /// <summary>
        /// Gets or sets the video frame height. If not set, it will be set from the first pushed frame
        /// </summary>
        public int VideoHeight { get; set; }

        /// <summary>
        /// Gets or sets the output video pixel format. The default value is YUV444p.
        /// Sended frames will be automatically converted to this format
        /// </summary>
        public BitmapPixelFormat VideoPixelFormat { get; set; }

        /// <summary>
        /// Gets or sets video frame rate (FPS) value. The default value is 30 frames/s
        /// </summary>
        public int Framerate { get; set; }

        /// <summary>
        /// Creates a <see cref="Layout"/> object, containg dimensions and pixel format, based on this settings
        /// </summary>
        /// <returns>New <see cref="Layout"/> object</returns>
        public Layout ToLayout() => new Layout((AVPixelFormat)VideoPixelFormat, VideoWidth, VideoHeight);
    }
}

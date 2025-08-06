namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the audio/video streams loading modes.
    /// </summary>
    [Flags]
    public enum MediaMode
    {
        /// <summary>
        /// Enables loading only video streams.
        /// </summary>
        Video = 1 << AVMediaType.AVMEDIA_TYPE_VIDEO,

        /// <summary>
        /// Enables loading only audio streams.
        /// </summary>
        Audio = 1 << AVMediaType.AVMEDIA_TYPE_AUDIO,

        /// <summary>
        /// Enables loading both audio and video streams if they exist.
        /// </summary>
        AudioVideo = Audio | Video,
    }

    /// <summary>
    /// Represents the multimedia file container options.
    /// </summary>
    public class MediaOptions
    {
        private const string Threads = "threads";

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOptions"/> class.
        /// </summary>
        public MediaOptions()
        {
        }

        /// <summary>
        /// Gets or sets the limit of memory used by the packet buffer. Default limit is 40 MB per stream.
        /// </summary>
        public int PacketBufferSizeLimit { get; set; } = 40;

        /// <summary>
        /// Gets or sets the demuxer settings.
        /// </summary>
        public ContainerOptions DemuxerOptions { get; set; } = new ContainerOptions();

        /// <summary>
        /// Gets or sets the target pixel format for decoded video frames conversion. The default value is <c>Bgr24</c>.
        /// </summary>
        public ImagePixelFormat VideoPixelFormat { get; set; } = ImagePixelFormat.Bgr24;

        /// <summary>
        /// Gets or sets the target video size for decoded video frames conversion. <see langword="null"/>, if no rescale.
        /// </summary>
        public Size? TargetVideoSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether video frames will be flipped vertically.
        /// </summary>
        public bool FlipVertically { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="VideoStream.GetFrame"/> seek threshold in milliseconds, used to decide whether to call slow <c>av_seek_frame</c> method or to read subsequent frames until the desired frame is found. Default value is 500 ms.
        /// </summary>
        public int VideoSeekThreshold { get; set; } = 500;

        /// <summary>
        ///  Gets or sets the <see cref="AudioStream.GetFrame"/> seek threshold in milliseconds, used to decide whether to call slow <c>av_seek_frame</c> method or to read subsequent frames until the desired frame is found. Default value is 500 ms.
        /// </summary>
        public int AudioSeekThreshold { get; set; } = 500;

        /// <summary>
        /// Gets or sets the number of decoder threads (by the 'threads' flag). The default value is <see langword="null"/> - 'auto'.
        /// </summary>
        public int? DecoderThreads
        {
            get => DecoderOptions.TryGetValue(Threads, out string value) &&
                int.TryParse(value, out var count) ? (int?)count : null;
            set
            {
                if (value.HasValue)
                {
                    DecoderOptions[Threads] = value.ToString();
                }
                else
                {
                    DecoderOptions.Remove(Threads);
                }
            }
        }

        /// <summary>
        /// Gets or sets the dictionary with global options for the multimedia decoders.
        /// </summary>
        public Dictionary<string, string> DecoderOptions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets which streams (audio/video) will be loaded.
        /// </summary>
        public MediaMode StreamsToLoad { get; set; } = MediaMode.AudioVideo;

        /// <summary>
        /// Determines whether streams of a certain <see cref="AVMediaType"/> should be loaded
        /// (Based on <see cref="StreamsToLoad"/> property).
        /// </summary>
        /// <param name="type">A given <see cref="AVMediaType"/>.</param>
        /// <returns><see langword="true"/> if streams of the <see cref="AVMediaType"/> given are to be loaded.</returns>
        public bool ShouldLoadStreamsOfType(AVMediaType type)
        {
            var mode = (MediaMode)(1 << (int)type);
            return StreamsToLoad.HasFlag(mode);
        }
    }
}

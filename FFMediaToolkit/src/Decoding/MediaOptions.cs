namespace FFMediaToolkit.Decoding
{
    using System.Drawing;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Graphics;

    /// <summary>
    /// Represents the audio/video streams loading modes.
    /// </summary>
    internal enum MediaMode
    {
        /// <summary>
        /// Enables loading both audio and video streams if exists.
        /// </summary>
        AudioVideo,

        /// <summary>
        /// Enables loading only video stream.
        /// </summary>
        Video,

        /// <summary>
        /// Enables loading only audio stream.
        /// </summary>
        Audio,
    }

    /// <summary>
    /// Represents the multimedia file container options.
    /// </summary>
    public class MediaOptions
    {
        private const string Threads = "threads";
        private const string Auto = "auto";

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOptions"/> class.
        /// </summary>
        public MediaOptions()
        {
            DecoderThreads = null;
        }

        /// <summary>
        /// Gets or sets the demuxer settings.
        /// </summary>
        public ContainerOptions DemuxerOptions { get; set; } = new ContainerOptions();

        /// <summary>
        /// Gets or sets the target pixel format for decoded video frames conversion. The default value is <c>BGR24</c>.
        /// </summary>
        public ImagePixelFormat VideoPixelFormat { get; set; } = ImagePixelFormat.BGR24;

        /// <summary>
        /// Gets or sets the target video size for decoded video frames conversion. <see langword="null"/>, if no rescale.
        /// </summary>
        public Size? TargetVideoSize { get; set; }

        /// <summary>
        /// Gets or sets the number of decoder threads (by the 'threads' flag). The default value is <see langword="null"/> - 'auto'.
        /// </summary>
        public int? DecoderThreads
        {
            get => int.TryParse(DecoderOptions[Threads], out var count) ? (int?)count : null;
            set => DecoderOptions[Threads] = value.HasValue ? value.ToString() : Auto;
        }

        /// <summary>
        /// Gets or sets the dictionary with global options for the multimedia decoders.
        /// </summary>
        public FFDictionary DecoderOptions { get; set; } = new FFDictionary();

        /// <summary>
        /// Gets or sets which streams (audio/video) will be loaded.
        /// </summary>
        internal MediaMode StreamsToLoad { get; set; } = MediaMode.AudioVideo;
    }
}

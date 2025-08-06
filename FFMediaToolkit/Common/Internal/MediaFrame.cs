namespace FFMediaToolkit.Common.Internal
{
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a base class of audio and video frames.
    /// </summary>
    internal abstract unsafe class MediaFrame : Wrapper<AVFrame>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        /// <param name="frame">The <see cref="AVFrame"/> object.</param>
        protected MediaFrame(AVFrame* frame)
            : base(frame)
        {
        }

        /// <summary>
        /// Gets or sets the frame PTS value in the stream time base units.
        /// </summary>
        public long PresentationTimestamp
        {
            get => Pointer->pts;
            set => Pointer->pts = value;
        }

        /// <summary>
        /// Gets or sets the frame PTS value in the stream time base units.
        /// </summary>
        public long DecodingTimestamp
        {
            get => Pointer->pkt_dts;
            set => Pointer->pkt_dts = value;
        }

        /// <summary>
        /// Gets or sets the frame duration
        /// </summary>
        public long Duration
        {
            get => Pointer->duration;
            set => Pointer->duration = value;
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            var ptr = Pointer;
            ffmpeg.av_frame_free(&ptr);
        }
    }
}

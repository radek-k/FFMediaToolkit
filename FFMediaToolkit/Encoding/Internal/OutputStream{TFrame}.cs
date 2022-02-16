namespace FFMediaToolkit.Encoding.Internal
{
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a output multimedia stream.
    /// </summary>
    /// <typeparam name="TFrame">The type of frames in the stream.</typeparam>
    internal unsafe class OutputStream<TFrame> : Wrapper<AVStream>
        where TFrame : MediaFrame
    {
        private readonly MediaPacket packet;
        private AVCodecContext* codecContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputStream{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The multimedia stream.</param>
        /// <param name="codec">Codec context.</param>
        /// <param name="owner">The container that owns the stream.</param>
        public OutputStream(AVStream* stream, AVCodecContext* codec, OutputContainer owner)
            : base(stream)
        {
            OwnerFile = owner;
            codecContext = codec;
            packet = MediaPacket.AllocateEmpty();
        }

        /// <summary>
        /// Gets the media container that owns this stream.
        /// </summary>
        public OutputContainer OwnerFile { get; }

        /// <summary>
        /// Gets the stream index.
        /// </summary>
        public int Index => Pointer->index;

        /// <summary>
        /// Gets the stream time base.
        /// </summary>
        public AVRational TimeBase => Pointer->time_base;

        /// <summary>
        /// Writes the specified frame to this stream.
        /// </summary>
        /// <param name="frame">The media frame.</param>
        public void Push(TFrame frame)
        {
            ffmpeg.avcodec_send_frame(codecContext, frame.Pointer)
                .ThrowIfError("Cannot send a frame to the encoder.");

            if (ffmpeg.avcodec_receive_packet(codecContext, packet) == 0)
            {
                packet.RescaleTimestamp(codecContext->time_base, TimeBase);
                packet.StreamIndex = Index;

                OwnerFile.WritePacket(packet);
            }

            packet.Wipe();
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            FlushEncoder();
            packet.Dispose();

            ffmpeg.avcodec_close(codecContext);
            ffmpeg.av_free(codecContext);
            codecContext = null;
        }

        private void FlushEncoder()
        {
            while (true)
            {
                ffmpeg.avcodec_send_frame(codecContext, null);

                if (ffmpeg.avcodec_receive_packet(codecContext, packet) == 0)
                {
                    packet.RescaleTimestamp(codecContext->time_base, TimeBase);
                    packet.StreamIndex = Index;
                    OwnerFile.WritePacket(packet);
                }
                else
                {
                    break;
                }

                packet.Wipe();
            }
        }
    }
}

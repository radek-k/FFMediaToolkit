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

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputStream{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The multimedia stream.</param>
        /// <param name="owner">The container that owns the stream.</param>
        public OutputStream(AVStream* stream, OutputContainer owner)
            : base(stream)
        {
            OwnerFile = owner;
            packet = MediaPacket.AllocateEmpty(stream->index);
        }

        /// <summary>
        /// Gets the media container that owns this stream.
        /// </summary>
        public OutputContainer OwnerFile { get; }

        /// <summary>
        /// Gets a pointer to <see cref="AVCodecContext"/> for this stream.
        /// </summary>
        public AVCodecContext* CodecPointer => Pointer->codec;

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
            ffmpeg.avcodec_send_frame(CodecPointer, frame.Pointer)
                .ThrowIfError("Cannot send a frame to the encoder.");

            if (ffmpeg.avcodec_receive_packet(CodecPointer, packet) == 0)
            {
                packet.RescaleTimestamp(CodecPointer->time_base, TimeBase);

                if (CodecPointer->coded_frame->key_frame == 1)
                {
                    packet.IsKeyPacket = true;
                }

                OwnerFile.WritePacket(packet);
            }

            packet.Wipe();
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            FlushEncoder();
            packet.Dispose();

            var ptr = CodecPointer;
            ffmpeg.avcodec_close(ptr);
            ffmpeg.avcodec_free_context(&ptr);
        }

        private void FlushEncoder()
        {
            while (true)
            {
                ffmpeg.avcodec_send_frame(CodecPointer, null);

                if (ffmpeg.avcodec_receive_packet(CodecPointer, packet) == 0)
                {
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

namespace FFMediaToolkit.Encoding.Internal
{
    using System;
    using System.Runtime.CompilerServices;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a output multimedia stream.
    /// </summary>
    /// <typeparam name="TFrame">The type of frames in the stream.</typeparam>
    internal unsafe class Encoder<TFrame> : IDisposable
        where TFrame : MediaFrame
    {
        private readonly MediaPacket packet;
        private readonly AVCodecContext* codecContext;
        private readonly AVStream* stream;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Encoder{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The multimedia stream.</param>
        /// <param name="codec">Codec context.</param>
        /// <param name="owner">The container that owns the stream.</param>
        public Encoder(AVStream* stream, AVCodecContext* codec, OutputContainer owner)
        {
            OwnerFile = owner;
            codecContext = codec;
            this.stream = stream;
            packet = MediaPacket.AllocateEmpty();
        }

        /// <summary>
        /// Gets the media container that owns this stream.
        /// </summary>
        public OutputContainer OwnerFile { get; }

        /// <summary>
        /// Gets the stream index.
        /// </summary>
        public int Index => stream->index;

        /// <summary>
        /// Gets the stream time base.
        /// </summary>
        public AVRational TimeBase => stream->time_base;

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

        /// <summary>
        /// Writes buffered frames to the stream
        /// </summary>
        public void FlushEncoder()
        {
            var flushing = ffmpeg.avcodec_send_frame(codecContext, null) == 0;
            while (flushing)
            {
                packet.Wipe();
                flushing = ffmpeg.avcodec_receive_packet(codecContext, packet) == 0;
                if (flushing)
                {
                    packet.RescaleTimestamp(codecContext->time_base, TimeBase);
                    packet.StreamIndex = Index;
                    OwnerFile.WritePacket(packet);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
                return;

            packet.Dispose();

            fixed (AVCodecContext** codecContextRef = &codecContext)
            {
                ffmpeg.avcodec_free_context(codecContextRef);
            }

            isDisposed = true;
        }
    }
}

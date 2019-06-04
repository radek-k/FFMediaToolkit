namespace FFMediaToolkit.Decoding.Internal
{
    using System;
    using System.IO;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a input multimedia stream.
    /// </summary>
    /// <typeparam name="TFrame">The type of frames in the stream.</typeparam>
    internal unsafe class InputStream<TFrame> : Wrapper<AVStream>
        where TFrame : MediaFrame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputStream{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The multimedia stream.</param>
        /// <param name="owner">The container that owns the stream.</param>
        public InputStream(AVStream* stream, InputContainer owner)
            : base(stream)
        {
            OwnerFile = owner;
            PacketQueue = new ObservableQueue<MediaPacket>();

            Type = typeof(TFrame) == typeof(VideoFrame) ? MediaType.Video : MediaType.None;
        }

        /// <summary>
        /// Gets the media container that owns this stream.
        /// </summary>
        public InputContainer OwnerFile { get; }

        /// <summary>
        /// Gets a pointer to <see cref="AVCodecContext"/> for this stream.
        /// </summary>
        public AVCodecContext* CodecPointer => Pointer->codec;

        /// <summary>
        /// Gets the type of this stream.
        /// </summary>
        public MediaType Type { get; }

        /// <summary>
        /// Gets the stream index.
        /// </summary>
        public int Index => Pointer->index;

        /// <summary>
        /// Gets the stream time base.
        /// </summary>
        public AVRational TimeBase => Pointer->time_base;

        /// <summary>
        /// Gets the packet queue.
        /// </summary>
        public ObservableQueue<MediaPacket> PacketQueue { get; }

        /// <summary>
        /// Gets a value indicating whether the internal decoder buffer was filled.
        /// </summary>
        public bool IsDecoderBufferFull { get; private set; }

        /// <summary>
        /// Reads the next frame from the stream and writes its data to the specified <see cref="MediaFrame"/> object.
        /// </summary>
        /// <param name="frame">A media frame to override with the new decoded frame.</param>
        public void Read(TFrame frame)
        {
            while (true)
            {
                SendPacket();
                var result = ffmpeg.avcodec_receive_frame(CodecPointer, frame.Pointer);

                if (result >= 0)
                {
                    return;
                }
                else if (result == -ffmpeg.EAGAIN && IsDecoderBufferFull)
                {
                    IsDecoderBufferFull = false;
                }
                else
                {
                    result.CatchAll("An error ocurred while decoding the frame.");
                }
            }
        }

        /// <summary>
        /// Flushes the codec buffers.
        /// </summary>
        public void FlushBuffers() => ffmpeg.avcodec_flush_buffers(CodecPointer);

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            FlushBuffers();

            var ptr = CodecPointer;
            ffmpeg.avcodec_close(ptr);
            ffmpeg.avcodec_free_context(&ptr);
        }

        private void SendPacket()
        {
            if (IsDecoderBufferFull)
                return;

            if (!PacketQueue.TryPeek(out var pkt))
            {
                if (OwnerFile.IsAtEndOfFile)
                {
                    throw new EndOfStreamException("End of the media strem.");
                }
                else
                {
                    throw new Exception("No packets in queue.");
                }
            }

            var result = ffmpeg.avcodec_send_packet(CodecPointer, pkt);

            if (result == -ffmpeg.EAGAIN)
            {
                IsDecoderBufferFull = true;
            }
            else
            {
                result.CatchAll("Cannot send a packet to the decoder.");
            }

            if (!IsDecoderBufferFull)
            {
                PacketQueue.TryDequeue(out var _);
            }
        }
    }
}

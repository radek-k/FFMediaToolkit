namespace FFMediaToolkit.Decoding.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a input multimedia stream.
    /// </summary>
    /// <typeparam name="TFrame">The type of frames in the stream.</typeparam>
    internal unsafe class InputStream<TFrame> : Wrapper<AVStream>
        where TFrame : MediaFrame, new()
    {
        private readonly ConcurrentQueue<MediaPacket> packetQueue;
        private readonly TFrame decodedFrame;

        /// <summary>
        /// Initializes a new instance of the <see cref="InputStream{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The multimedia stream.</param>
        /// <param name="owner">The container that owns the stream.</param>
        public InputStream(AVStream* stream, InputContainer owner)
            : base(stream)
        {
            OwnerFile = owner;
            packetQueue = new ConcurrentQueue<MediaPacket>();

            Type = typeof(TFrame) == typeof(VideoFrame) ? MediaType.Video : MediaType.None;
            Info = new StreamInfo(stream, owner);
            decodedFrame = new TFrame();
        }

        /// <summary>
        /// Ocurrs when sending next packet from the file is required.
        /// </summary>
        public event Action PacketsNeeded;

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
        /// Gets informations about the stream.
        /// </summary>
        public StreamInfo Info { get; }

        /// <summary>
        /// Gets the recently decoded frame.
        /// </summary>
        public TFrame DecodedFrame => decodedFrame;

        /// <summary>
        /// Adds a packet readed from the file to the internal decoder queue.
        /// </summary>
        /// <param name="packet">A media packet.</param>
        public void FetchPacket(MediaPacket packet)
        {
            if (packet.StreamIndex == Info.Index)
            {
                packetQueue.Enqueue(packet);
            }
        }

        /// <summary>
        /// Reads the next frame from the stream.
        /// </summary>
        /// <returns>The decoded frame.</returns>
        public TFrame GetNextFrame()
        {
            ReadNextFrame();
            return decodedFrame;
        }

        /// <summary>
        /// Deletes the packet with the timestamp smaller than the specified.
        /// </summary>
        /// <param name="targetTs">The timestamp.</param>
        public void AdjustPackets(long targetTs)
        {
            do
            {
                ReadNextFrame();
            }
            while (decodedFrame.PresentationTime < targetTs);
        }

        /// <summary>
        /// Flushes the codec buffers.
        /// </summary>
        public void FlushBuffers() => ffmpeg.avcodec_flush_buffers(CodecPointer);

        /// <summary>
        /// Clears the packet queue.
        /// </summary>
        public void ClearQueue()
        {
            while (packetQueue.Count > 0)
            {
                packetQueue.TryDequeue(out var _);
            }
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            FlushBuffers();
            ffmpeg.avcodec_close(CodecPointer);
        }

        private void ReadNextFrame()
        {
            int error;

            do
            {
                DecodePacket(); // Gets the next packet and sends it to the decoder.
                error = ffmpeg.avcodec_receive_frame(CodecPointer, decodedFrame.Pointer); // Tries to decode frame from the packets.
            }
            while (error == -ffmpeg.EAGAIN || error == -35); // The EAGAIN code means that the frame decoding has not been completed and more packets are needed.

            error.ThrowIfError("An error ocurred while decoding the frame.");
        }

        private void DecodePacket()
        {
            if (!packetQueue.TryPeek(out var pkt))
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

            // Sends the packet to the decoder.
            var result = ffmpeg.avcodec_send_packet(CodecPointer, pkt);

            if (result == -ffmpeg.EAGAIN)
            {
                return;
            }
            else
            {
                result.ThrowIfError("Cannot send a packet to the decoder.");
                packetQueue.TryDequeue(out var _); // Remove the decoded packet from the queue.
                RequestForNextPacket();
            }
        }

        private void RequestForNextPacket()
        {
            if (packetQueue.Count < 5)
            {
                PacketsNeeded();
            }
        }
    }
}

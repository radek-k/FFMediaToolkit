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
        /// <summary>
        /// Initializes a new instance of the <see cref="InputStream{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The multimedia stream.</param>
        /// <param name="owner">The container that owns the stream.</param>
        public InputStream(AVStream* stream, InputContainer owner)
            : base(stream)
        {
            OwnerFile = owner;

            Type = typeof(TFrame) == typeof(VideoFrame) ? MediaType.Video : MediaType.None;
            Info = new StreamInfo(stream, owner);
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
        /// Gets informations about the stream.
        /// </summary>
        public StreamInfo Info { get; }

        /// <summary>
        /// Gets the recently decoded frame.
        /// </summary>
        public TFrame RecentlyDecodedFrame { get; private set; } = new TFrame();

        /// <summary>
        /// Reads the next frame from the stream.
        /// </summary>
        /// <returns>The decoded frame.</returns>
        public TFrame GetNextFrame()
        {
            ReadNextFrame();
            return RecentlyDecodedFrame;
        }

        /// <summary>
        /// Seeks stream by skipping next packets in the file. Useful to seek few frames forward.
        /// </summary>
        /// <param name="frameNumber">The target video frame number.</param>
        public void AdjustPackets(int frameNumber) => AdjustPackets(frameNumber.ToTimestamp(Info.RealFrameRate, Info.TimeBase));

        /// <summary>
        /// Seeks stream by skipping next packets in the file. Useful to seek few frames forward.
        /// </summary>
        /// <param name="targetTs">The target video time stamp.</param>
        public void AdjustPackets(long targetTs)
        {
            do
            {
                ReadNextFrame();
            }
            while (RecentlyDecodedFrame.PresentationTimestamp < targetTs);
        }

        /// <summary>
        /// Flushes the codec buffers.
        /// </summary>
        public void FlushBuffers() => ffmpeg.avcodec_flush_buffers(CodecPointer);

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            FlushBuffers();
            ffmpeg.avcodec_close(CodecPointer);
        }

        private void ReadNextFrame()
        {
            ffmpeg.av_frame_unref(RecentlyDecodedFrame.Pointer);
            int error;

            do
            {
                DecodePacket(); // Gets the next packet and sends it to the decoder.
                error = ffmpeg.avcodec_receive_frame(CodecPointer, RecentlyDecodedFrame.Pointer); // Tries to decode frame from the packets.
            }
            while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN) || error == -35); // The EAGAIN code means that the frame decoding has not been completed and more packets are needed.
            error.ThrowIfError("An error occurred while decoding the frame.");
        }

        private void DecodePacket()
        {
            var pkt = OwnerFile.ReadNextPacket(Info.Index);

            // Sends the packet to the decoder.
            var result = ffmpeg.avcodec_send_packet(CodecPointer, pkt);

            if (result == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                OwnerFile.ReuseLastPacket();
            }
            else
            {
                result.ThrowIfError("Cannot send a packet to the decoder.");
            }
        }
    }
}

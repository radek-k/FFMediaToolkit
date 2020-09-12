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
    internal unsafe class Decoder<TFrame> : Wrapper<AVCodecContext>
        where TFrame : MediaFrame, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Decoder{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The multimedia stream.</param>
        /// <param name="owner">The container that owns the stream.</param>
        public Decoder(AVCodecContext* codec, AVStream* stream, InputContainer owner)
            : base(codec)
        {
            OwnerFile = owner;
            Info = new StreamInfo(stream, owner);
        }

        /// <summary>
        /// Gets the media container that owns this stream.
        /// </summary>
        public InputContainer OwnerFile { get; }

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
        /// Decodes frames until reach the specified time stamp. Useful to seek few frames forward.
        /// </summary>
        /// <param name="targetTs">The target time stamp.</param>
        public void SkipFrames(long targetTs)
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
        public void FlushBuffers() => ffmpeg.avcodec_flush_buffers(Pointer);

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            RecentlyDecodedFrame.Dispose();
            FlushBuffers();
            ffmpeg.avcodec_close(Pointer);
        }

        private void ReadNextFrame()
        {
            ffmpeg.av_frame_unref(RecentlyDecodedFrame.Pointer);
            int error;

            do
            {
                DecodePacket(); // Gets the next packet and sends it to the decoder
                error = ffmpeg.avcodec_receive_frame(Pointer, RecentlyDecodedFrame.Pointer); // Tries to decode frame from the packets.
            }
            while (error == ffmpeg.AVERROR(ffmpeg.EAGAIN) || error == -35); // The EAGAIN code means that the frame decoding has not been completed and more packets are needed.
            error.ThrowIfError("An error occurred while decoding the frame.");
        }

        private void DecodePacket()
        {
            var pkt = OwnerFile.ReadNextPacket(Info.Index);

            // Sends the packet to the decoder.
            var result = ffmpeg.avcodec_send_packet(Pointer, pkt);

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

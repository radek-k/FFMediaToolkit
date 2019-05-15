namespace FFMediaToolkit.Common
{
    using System;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// This is a base class of video and audio FFmpeg streams
    /// </summary>
    /// <typeparam name="TFrame">Type of frame</typeparam>
    public abstract unsafe class MediaStream<TFrame> : MediaObject, IDisposable
        where TFrame : MediaFrame
    {
        private readonly IntPtr codec;
        private readonly IntPtr stream;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaStream{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The media stream</param>
        /// <param name="codec">The media <see cref="AVCodecContext"/></param>
        /// <param name="container">The media container</param>
        protected MediaStream(AVStream* stream, AVCodecContext* codec, MediaContainer container)
        {
            this.codec = (IntPtr)codec;
            this.stream = (IntPtr)stream;
            OwnerFile = container;
        }

        /// <summary>
        /// Gets an unsafe pointer to the underlying FFmpeg <see cref="AVStream"/>
        /// </summary>
        public AVStream* StreamPointer => stream != IntPtr.Zero ? (AVStream*)stream : null;

        /// <summary>
        /// Gets an unsafe pointer to the underlying FFmpeg <see cref="AVCodecContext"/>
        /// </summary>
        public AVCodecContext* CodecContextPointer => codec != IntPtr.Zero ? (AVCodecContext*)codec : null;

        /// <summary>
        /// Gets the acces mode of this stream
        /// </summary>
        public override MediaAccess Access => OwnerFile.Access;

        /// <summary>
        /// Gets the <see cref="MediaContainer"/> that owns this stream
        /// </summary>
        public MediaContainer OwnerFile { get; }

        /// <summary>
        /// Gets the current stream index
        /// </summary>
        public int Index => StreamPointer->index;

        /// <summary>
        /// Gets stream time base
        /// </summary>
        public AVRational TimeBase => StreamPointer->time_base;

        /// <summary>
        /// Sends the media frame to the encoder.
        /// Usable only in encoding mode, otherwise throws <see cref="InvalidOperationException"/>
        /// </summary>
        /// <param name="frame">Media frame to encode</param>
        public void PushFrame(TFrame frame)
        {
            CheckAccess(MediaAccess.Write);

            ffmpeg.avcodec_send_frame(CodecContextPointer, frame.Pointer).ThrowIfError("sending the frame");

            var packet = MediaPacket.AllocateEmpty(Index);

            if (ffmpeg.avcodec_receive_packet(CodecContextPointer, packet) == 0)
            {
                packet.RescaleTimestamp(CodecContextPointer->time_base, TimeBase);

                if (CodecContextPointer->coded_frame->key_frame == 1)
                {
                    packet.IsKeyPacket = true;
                }

                OwnerFile.WritePacket(packet);
            }
        }

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        /// <summary>
        /// Method called when frame is pushing
        /// </summary>
        /// <returns>null</returns>
        protected abstract TFrame OnReading();

        private void Flush()
        {
            while (true)
            {
                var packet = MediaPacket.AllocateEmpty(Index);
                ffmpeg.avcodec_send_frame(CodecContextPointer, null);

                if (ffmpeg.avcodec_receive_packet(CodecContextPointer, packet) == 0)
                    OwnerFile.WritePacket(packet);
                else
                    break;
            }
        }

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            if (Access == MediaAccess.Write)
                Flush();

            if (stream != IntPtr.Zero)
                ffmpeg.avcodec_close(StreamPointer->codec);

            if (codec != IntPtr.Zero)
            {
                var ptr = CodecContextPointer;
                ffmpeg.avcodec_free_context(&ptr);
            }

            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}

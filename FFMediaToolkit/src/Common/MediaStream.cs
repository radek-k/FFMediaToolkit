namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// This is a base class of video and audio FFmpeg streams.
    /// </summary>
    /// <typeparam name="TFrame">Type of frame.</typeparam>
    public abstract unsafe class MediaStream<TFrame> : MediaObject, IDisposable
        where TFrame : MediaFrame
    {
        private readonly IntPtr codec;
        private readonly IntPtr stream;
        private readonly MediaPacket packet;

        private readonly object syncLock = new object();
        private bool isDisposed;
        private bool packetFull;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaStream{TFrame}"/> class.
        /// </summary>
        /// <param name="stream">The media stream.</param>
        /// <param name="codec">The media <see cref="AVCodecContext"/>.</param>
        /// <param name="container">The media container.</param>
        protected MediaStream(AVStream* stream, AVCodecContext* codec, MediaContainer container)
        {
            this.codec = (IntPtr)codec;
            this.stream = (IntPtr)stream;
            OwnerFile = container;
            packet = MediaPacket.AllocateEmpty(stream->index);

            // Packet queue is currently supported only in the reading mode.
            if (OwnerFile.Access == MediaAccess.Read)
            {
                PacketQueue = new ConcurrentQueue<MediaPacket>();
            }
        }

        /// <summary>
        /// Event invoked when dequeuing packet.
        /// </summary>
        internal event EventHandler PacketDequeued;

        /// <summary>
        /// Gets the acces mode of this stream.
        /// </summary>
        public override MediaAccess Access => OwnerFile.Access;

        /// <summary>
        /// Gets the <see cref="MediaContainer"/> that owns this stream.
        /// </summary>
        public MediaContainer OwnerFile { get; }

        /// <summary>
        /// Gets the type of this stream.
        /// </summary>
        public abstract MediaType Type { get; }

        /// <summary>
        /// Gets the current stream index.
        /// </summary>
        public int Index => StreamPointer->index;

        /// <summary>
        /// Gets the stream time base.
        /// </summary>
        public AVRational TimeBase => StreamPointer->time_base;

        /// <summary>
        /// Gets the stream bit rate.
        /// </summary>
        public long Bitrate => CodecContextPointer->bit_rate;

        /// <summary>
        /// Gets the codec name.
        /// </summary>
        public string CodecName => ffmpeg.avcodec_get_name(CodecContextPointer->codec_id);

        /// <summary>
        /// Gets an unsafe pointer to the underlying FFmpeg <see cref="AVStream"/>.
        /// </summary>
        internal AVStream* StreamPointer => stream != IntPtr.Zero ? (AVStream*)stream : null;

        /// <summary>
        /// Gets an unsafe pointer to the underlying FFmpeg <see cref="AVCodecContext"/>.
        /// </summary>
        internal AVCodecContext* CodecContextPointer => codec != IntPtr.Zero ? (AVCodecContext*)codec : null;

        /// <summary>
        /// Gets the packet queue.
        /// </summary>
        internal ConcurrentQueue<MediaPacket> PacketQueue { get; }

        /// <summary>
        /// Sends the media frame to the encoder.
        /// Usable only in encoding mode, otherwise throws <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <param name="frame">Media frame to encode.</param>
        public void PushFrame(TFrame frame)
        {
            CheckAccess(MediaAccess.Write);

            lock (syncLock)
            {
                Push(frame);
            }
        }

        /// <summary>
        /// Reads the next frame from this stream.
        /// </summary>
        /// <param name="frame">A media frame to reuse.</param>
        /// <returns>The decoded media frame.</returns>
        public TFrame ReadNextFrame(TFrame frame)
        {
            CheckAccess(MediaAccess.Read);

            lock (syncLock)
            {
                var result = Read(frame);
                if (!result)
                {
                    throw new FFmpegException("An error ocurred during decoding the frame");
                }

                return frame;
            }
        }

        /// <summary>
        /// Seeks this stream to the specified frame position.
        /// </summary>
        /// <param name="frameIndex">The frame number.</param>
        /// <param name="seekToAny">If <see langword="true"/>, it will seek exactly to the specified frame (non keyframe). This doesn't work correctly on many formats.</param>
        public void SeekTo(int frameIndex, bool seekToAny)
        {
            CheckAccess(MediaAccess.Read);

            lock (syncLock)
            {
                var ts = frameIndex.ToTimestamp(TimeBase);
                ffmpeg.av_seek_frame(OwnerFile.FormatContextPointer, Index, ts, seekToAny ? ffmpeg.AVSEEK_FLAG_ANY : ffmpeg.AVSEEK_FLAG_BACKWARD);

                if (!seekToAny)
                {
                    // TODO: Frame reading
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        private bool Read(TFrame frame)
        {
            var frameDecoded = false;
            var continueDecoding = true;

            while (!frameDecoded && continueDecoding)
            {
                ReadPacket();
                var result = ffmpeg.avcodec_receive_frame(CodecContextPointer, frame.Pointer)
                   .Catch(0, x => frameDecoded = true)
                   .Catch(ffmpeg.AVERROR_EOF, x => throw new EndOfStreamException())
                   .Catch(-ffmpeg.EINVAL, x => continueDecoding = false);

                if (result == -ffmpeg.EAGAIN)
                {
                    if (packetFull)
                    {
                        continueDecoding = false;
                    }
                    else
                    {
                        packetFull = true;
                    }
                }
            }

            return frameDecoded;
        }

        private void ReadPacket()
        {
            if (packetFull)
                return;

            var result = PacketQueue.TryPeek(out var pkt);

            if (!result)
            {
                if (OwnerFile.IsAtEndOfFile)
                {
                    throw new EndOfStreamException();
                }
                else
                {
                    throw new Exception("No packets in queue!");
                }
            }

            ffmpeg.avcodec_send_packet(CodecContextPointer, pkt)
                .Catch(ffmpeg.AVERROR_EOF, x => throw new EndOfStreamException())
                .Catch(ffmpeg.EAGAIN, x => packetFull = true)
                .CatchAll("Cannot send a packet to the decoder.");

            if (!packetFull)
            {
                PacketQueue.TryDequeue(out var _);
                PacketDequeued?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Push(TFrame frame)
        {
            ffmpeg.avcodec_send_frame(CodecContextPointer, frame.Pointer)
                .CatchAll("Cannot send a frame to the encoder.");

            if (ffmpeg.avcodec_receive_packet(CodecContextPointer, packet) == 0)
            {
                packet.RescaleTimestamp(CodecContextPointer->time_base, TimeBase);

                if (CodecContextPointer->coded_frame->key_frame == 1)
                {
                    packet.IsKeyPacket = true;
                }

                OwnerFile.WritePacket(packet);
            }

            packet.Wipe();
        }

        private void FlushEncoder()
        {
            while (true)
            {
                ffmpeg.avcodec_send_frame(CodecContextPointer, null);

                if (ffmpeg.avcodec_receive_packet(CodecContextPointer, packet) == 0)
                    OwnerFile.WritePacket(packet);
                else
                    break;

                packet.Wipe();
            }
        }

        private void FlushBuffers() => ffmpeg.avcodec_flush_buffers(CodecContextPointer);

        private void Disposing(bool dispose)
        {
            lock (syncLock)
            {
                if (isDisposed)
                    return;

                if (Access == MediaAccess.Write)
                    FlushEncoder();

                packet.Dispose();

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
}

﻿namespace FFMediaToolkit.Common
{
    using System;
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
        }

        /// <summary>
        /// Gets the acces mode of this stream.
        /// </summary>
        public override MediaAccess Access => OwnerFile.Access;

        /// <summary>
        /// Gets the <see cref="MediaContainer"/> that owns this stream.
        /// </summary>
        public MediaContainer OwnerFile { get; }

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

            ffmpeg.av_read_frame(OwnerFile.FormatContextPointer, packet.Pointer)
                .Catch(ffmpeg.AVERROR_EOF, x => throw new EndOfStreamException())
                .CatchAll("Cannot read next packet from stream");

            ffmpeg.avcodec_send_packet(CodecContextPointer, packet)
                .Catch(ffmpeg.AVERROR_EOF, x => throw new EndOfStreamException())
                .Catch(ffmpeg.EAGAIN, x => packetFull = true)
                .CatchAll("Cannot send a packet to the decoder.");
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

namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a FFMpeg media packet
    /// </summary>
    public unsafe sealed class MediaPacket : IDisposable
    {
        private IntPtr pointer;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPacket"/> class.
        /// </summary>
        /// <param name="packet">A</param>
        private MediaPacket(AVPacket* packet)
        {
            pointer = new IntPtr(packet);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaPacket"/> class.
        /// </summary>
        ~MediaPacket() => Dispose();

        /// <summary>
        /// Gets the pointer to the underlying <see cref="AVPacket"/>
        /// </summary>
        public AVPacket* Pointer => disposed ? null : (AVPacket*)pointer;

        /// <summary>
        /// Gets or sets a value indicating whether the packet is key
        /// </summary>
        public bool IsKeyPacket
        {
            get => (Pointer->flags & ffmpeg.AV_PKT_FLAG_KEY) > 0;
            set => Pointer->flags |= value ? ffmpeg.AV_PKT_FLAG_KEY : ~ffmpeg.AV_PKT_FLAG_KEY;
        }

        public static implicit operator AVPacket*(MediaPacket packet) => packet.Pointer;

        /// <summary>
        /// Allocates a new empty packet
        /// </summary>
        /// <param name="streamIndex">Packet's stream</param>
        /// <returns>Allocated packet</returns>
        public static MediaPacket AllocateEmpty(int streamIndex)
        {
            var packet = ffmpeg.av_packet_alloc();
            ffmpeg.av_init_packet(packet);
            packet->data = null;
            packet->size = 0;
            packet->stream_index = streamIndex;
            return new MediaPacket(packet);
        }

        /// <summary>
        /// Sets valid PTS/DTS values. Used only in encoding.
        /// </summary>
        public void RescaleTimestamp(AVRational codec, AVRational stream) => ffmpeg.av_packet_rescale_ts(Pointer, codec, stream);

        /// <inheritdoc/>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            var ptr = Pointer;
            ffmpeg.av_packet_unref(ptr);
            ffmpeg.av_packet_free(&ptr);
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}

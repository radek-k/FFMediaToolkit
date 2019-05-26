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
        private readonly IntPtr pointer;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPacket"/> class.
        /// </summary>
        /// <param name="packet">The <see cref="AVPacket"/> object</param>
        private MediaPacket(AVPacket* packet) => pointer = new IntPtr(packet);

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaPacket"/> class.
        /// </summary>
        ~MediaPacket() => Disposing(false);

        /// <summary>
        /// Gets the pointer to the underlying <see cref="AVPacket"/>
        /// </summary>
        public AVPacket* Pointer => isDisposed ? null : (AVPacket*)pointer;

        /// <summary>
        /// Gets or sets a value indicating whether the packet is key
        /// </summary>
        public bool IsKeyPacket
        {
            get => (Pointer->flags & ffmpeg.AV_PKT_FLAG_KEY) > 0;
            set => Pointer->flags |= value ? ffmpeg.AV_PKT_FLAG_KEY : ~ffmpeg.AV_PKT_FLAG_KEY;
        }

        /// <summary>
        /// Gets the stream index.
        /// </summary>
        public int StreamIndex => Pointer->stream_index;

        /// <summary>
        /// Converts an instance of <see cref="MediaPacket"/> to the unmanaged pointer
        /// </summary>
        /// <param name="packet">A <see cref="MediaPacket"/> instance</param>
        public static implicit operator AVPacket*(MediaPacket packet) => packet.Pointer;

        /// <summary>
        /// Allocates a new empty packet.
        /// </summary>
        /// <param name="streamIndex">The packet stream</param>
        /// <returns>An allocated packet</returns>
        public static MediaPacket AllocateEmpty(int streamIndex)
        {
            var packet = ffmpeg.av_packet_alloc();
            packet->stream_index = streamIndex;
            return new MediaPacket(packet);
        }

        /// <summary>
        /// Sets valid PTS/DTS values. Used only in encoding.
        /// </summary>
        /// <param name="codecTimeBase">The encoder time base</param>
        /// <param name="streamTimeBase">The time base of media stream</param>
        public void RescaleTimestamp(AVRational codecTimeBase, AVRational streamTimeBase) => ffmpeg.av_packet_rescale_ts(Pointer, codecTimeBase, streamTimeBase);

        /// <summary>
        /// Wipes the packet data.
        /// </summary>
        public void Wipe() => ffmpeg.av_packet_unref(Pointer);

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            var ptr = Pointer;
            ffmpeg.av_packet_free(&ptr);
            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}

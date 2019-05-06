namespace FFMediaToolkit.Common
{
    using System;
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
        public AVStream* StreamPointer => codec != IntPtr.Zero ? (AVStream*)codec : null;

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
        public int Index { get; }

        /// <summary>
        /// Gets stream time base
        /// </summary>
        public AVRational TimeBase { get; }

        /// <summary>
        /// Sends the media frame to the encoder.
        /// Usable only in encoding mode, otherwise throws <see cref="InvalidOperationException"/>
        /// </summary>
        /// <param name="frame">Media frame to encode</param>
        public void PushFrame(TFrame frame)
        {
            CheckAccess(MediaAccess.Write);
            OnPushing(frame);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            if (stream != IntPtr.Zero)
            {
                ffmpeg.avcodec_close(StreamPointer->codec);
            }

            if (codec != IntPtr.Zero)
            {
                var ptr = CodecContextPointer;
                ffmpeg.avcodec_free_context(&ptr);
            }

            isDisposed = true;
        }

        /// <summary>
        /// Method called when frame is pushing
        /// </summary>
        /// <param name="frame">Media frame to encode</param>
        protected abstract void OnPushing(TFrame frame);

        /// <summary>
        /// Method called when frame is pushing
        /// </summary>
        /// <returns>null</returns>
        protected abstract TFrame OnReading();
    }
}

namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a base class of audio and video frames.
    /// </summary>
    public abstract unsafe class MediaFrame : IDisposable
    {
        private bool isDisposed;
        private IntPtr pointer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        /// <param name="frame">The <see cref="AVFrame"/> object.</param>
        protected MediaFrame(AVFrame* frame)
        {
            pointer = new IntPtr(frame);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        ~MediaFrame() => Disposing(false);

        /// <summary>
        /// Gets a pointer to the underlying <see cref="AVFrame"/>.
        /// </summary>
        public AVFrame* Pointer => pointer != IntPtr.Zero ? null : (AVFrame*)pointer;

        /// <summary>
        /// Gets or sets the frame PTS value in the stream time base units.
        /// </summary>
        public long PresentationTime
        {
            get => Pointer->pts;
            set => Pointer->pts = value;
        }

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            var ptr = Pointer;
            ffmpeg.av_frame_free(&ptr);
            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}

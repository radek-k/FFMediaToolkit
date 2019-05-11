namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a base class of audio and video frames
    /// </summary>
    public abstract unsafe class MediaFrame : IDisposable
    {
        private bool isDisposed;
        private IntPtr pointer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        /// <param name="frame">The <see cref="AVFrame"/> object</param>
        /// <param name="stream">The index of the frame stream</param>
        protected MediaFrame(AVFrame* frame, int stream)
        {
            pointer = new IntPtr(frame);
            StreamIndex = stream;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        ~MediaFrame() => Disposing(false);

        /// <summary>
        /// Gets a pointer to the underlying <see cref="AVFrame"/>
        /// </summary>
        public AVFrame* Pointer => isDisposed ? null : (AVFrame*)pointer;

        /// <summary>
        /// Gets the frame stream
        /// </summary>
        public int StreamIndex { get; }

        /// <summary>
        /// Gets or sets the frame PTS value in the stream time base units
        /// </summary>
        public long PresentationTime
        {
            get => ((AVFrame*)Pointer)->pts;
            set => ((AVFrame*)Pointer)->pts = value;
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

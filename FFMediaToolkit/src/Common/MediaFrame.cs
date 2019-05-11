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

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        /// <param name="frame">x</param>
        /// <param name="stream">c </param>
        protected MediaFrame(AVFrame* frame, int stream)
        {
            Pointer = new IntPtr(frame);
            StreamIndex = stream;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        ~MediaFrame() => Disposing(false);

        /// <summary>
        /// Gets a pointer to the underlying <see cref="AVFrame"/>
        /// </summary>
        public IntPtr Pointer { get; private set; }

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

        /// <summary>
        /// Gets a unsafe pointer to the frame
        /// </summary>
        /// <returns>Unsafe pointer</returns>
        public AVFrame* ToPointer() => (AVFrame*)Pointer;

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        /// <summary>
        /// Updates this instance with new <see cref="AVFrame"/>
        /// </summary>
        /// <param name="frame">New frame</param>
        protected void Override(AVFrame* frame)
        {
            Pointer = new IntPtr(frame);
            isDisposed = false;
        }

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            var ptr = (AVFrame*)Pointer;
            ffmpeg.av_frame_free(&ptr);
            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}

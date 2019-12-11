﻿namespace FFMediaToolkit.Common.Internal
{
    using System;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a base class of audio and video frames.
    /// </summary>
    internal abstract unsafe class MediaFrame : Wrapper<AVFrame>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFrame"/> class.
        /// </summary>
        /// <param name="frame">The <see cref="AVFrame"/> object.</param>
        public MediaFrame(AVFrame* frame)
            : base(frame)
        {
        }

        /// <summary>
        /// Gets or sets the frame PTS value in the stream time base units.
        /// </summary>
        public long PresentationTime
        {
            get => Pointer->pts;
            set => Pointer->pts = value;
        }

        /// <summary>
        /// Changes the pointer to the media frame.
        /// </summary>
        /// <param name="newFrame">The new pointer to a <see cref="AVFrame"/> object.</param>
        internal virtual void Update(AVFrame* newFrame) => UpdatePointer(newFrame);

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            if (Pointer != null)
            {
                var ptr = Pointer;
                ffmpeg.av_frame_free(&ptr);
                pointer = IntPtr.Zero;
            }
        }
    }
}

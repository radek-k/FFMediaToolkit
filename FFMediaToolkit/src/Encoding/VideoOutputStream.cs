namespace FFMediaToolkit.Encoding
{
    using System;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Encoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;

    /// <summary>
    /// Represents a video encoder stream.
    /// </summary>
    public class VideoOutputStream : IDisposable
    {
        private readonly OutputStream<VideoFrame> stream;
        private readonly VideoFrame encodedFrame;
        private readonly Scaler scaler;

        private readonly object syncLock = new object();
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoOutputStream"/> class.
        /// </summary>
        /// <param name="stream">The video stream.</param>
        /// <param name="config">The stream setting.</param>
        internal VideoOutputStream(OutputStream<VideoFrame> stream, VideoEncoderSettings config)
        {
            this.stream = stream;
            Configuration = config;
            encodedFrame = VideoFrame.Create(GetStreamLayout(stream));
            scaler = new Scaler();
        }

        /// <summary>
        /// Gets the video encoding configuration used to create this stream.
        /// </summary>
        public VideoEncoderSettings Configuration { get; }

        /// <summary>
        /// Gets the total number of video frames encoded to this stream.
        /// </summary>
        public int FramesCount { get; private set; }

        /// <summary>
        /// Gets the current duration of this stream.
        /// </summary>
        public TimeSpan CurrentDuration => TimeSpan.FromMilliseconds(FramesCount * (1D / Configuration.Framerate));

        /// <summary>
        /// Gets the video frame layout.
        /// </summary>
        internal Layout Layout { get; }

        /// <summary>
        /// Writes the specified bitmap to the video stream as the next frame.
        /// </summary>
        /// <param name="frame">The bitmap to write.</param>
        public void AddFrame(BitmapData frame)
        {
            lock (syncLock)
            {
                encodedFrame.UpdateFromBitmap(frame, scaler);
                stream.Push(encodedFrame);
                FramesCount++;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (syncLock)
            {
                if (isDisposed)
                {
                    return;
                }

                stream.Dispose();

                isDisposed = true;
            }
        }

        private static unsafe Layout GetStreamLayout(OutputStream<VideoFrame> videoStream)
        {
            var codec = videoStream.Pointer->codec;
            return new Layout(codec->pix_fmt, codec->width, codec->height);
        }
    }
}

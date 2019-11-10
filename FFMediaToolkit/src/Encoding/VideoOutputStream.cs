namespace FFMediaToolkit.Encoding
{
    using System;
    using System.Drawing;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Encoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a video encoder stream.
    /// </summary>
    public class VideoOutputStream : IDisposable
    {
        private readonly OutputStream<VideoFrame> stream;
        private readonly VideoFrame encodedFrame;
        private readonly ImageConverter converter;

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
            converter = new ImageConverter();

            var (size, format) = GetStreamLayout(stream);
            encodedFrame = VideoFrame.Create(size, format);
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
        public TimeSpan CurrentDuration => FramesCount.ToTimeSpan(Configuration.Framerate);

        /// <summary>
        /// Writes the specified bitmap to the video stream as the next frame.
        /// </summary>
        /// <param name="frame">The bitmap to write.</param>
        public void AddFrame(ImageData frame)
        {
            lock (syncLock)
            {
                encodedFrame.UpdateFromBitmap(frame, converter);
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
                encodedFrame.Dispose();
                converter.Dispose();

                isDisposed = true;
            }
        }

        private static unsafe (Size, AVPixelFormat) GetStreamLayout(OutputStream<VideoFrame> videoStream)
        {
            var codec = videoStream.Pointer->codec;
            return (new Size(codec->width, codec->height), codec->pix_fmt);
        }
    }
}

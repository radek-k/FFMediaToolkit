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

        private bool isDisposed;
        private long lastFramePts = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoOutputStream"/> class.
        /// </summary>
        /// <param name="stream">The video stream.</param>
        /// <param name="config">The stream setting.</param>
        internal VideoOutputStream(OutputStream<VideoFrame> stream, VideoEncoderSettings config)
        {
            this.stream = stream;
            Configuration = config;

            var frameSize = new Size(config.VideoWidth, config.VideoHeight);
            encodedFrame = VideoFrame.Create(frameSize, (AVPixelFormat)config.VideoFormat);
            converter = new ImageConverter(frameSize, (AVPixelFormat)config.VideoFormat);
        }

        /// <summary>
        /// Gets the video encoding configuration used to create this stream.
        /// </summary>
        public VideoEncoderSettings Configuration { get; }

        /// <summary>
        /// Gets the current duration of this stream.
        /// </summary>
        public TimeSpan CurrentDuration => lastFramePts.ToTimeSpan(Configuration.TimeBase);

        /// <summary>
        /// Writes the specified bitmap to the video stream as the next frame.
        /// </summary>
        /// <param name="frame">The bitmap to write.</param>
        /// <param name="customPtsValue">(optional) custom PTS value for the frame.</param>
        public void AddFrame(ImageData frame, long customPtsValue)
        {
            if (customPtsValue <= lastFramePts)
                throw new Exception("Cannot add a frame that occurs chronologically before the most recently written frame!");

            encodedFrame.UpdateFromBitmap(frame, converter);
            encodedFrame.PresentationTimestamp = customPtsValue;
            stream.Push(encodedFrame);

            lastFramePts = customPtsValue;
        }

        /// <summary>
        /// Writes the specified bitmap to the video stream as the next frame.
        /// </summary>
        /// <param name="frame">The bitmap to write.</param>
        /// <param name="customTime">Custom timestamp for this frame.</param>
        public void AddFrame(ImageData frame, TimeSpan customTime) => AddFrame(frame, customTime.ToTimestamp(Configuration.TimeBase));

        /// <summary>
        /// Writes the specified bitmap to the video stream as the next frame.
        /// </summary>
        /// <param name="frame">The bitmap to write.</param>
        public void AddFrame(ImageData frame) => AddFrame(frame, lastFramePts + 1);

        /// <inheritdoc/>
        public void Dispose()
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
}

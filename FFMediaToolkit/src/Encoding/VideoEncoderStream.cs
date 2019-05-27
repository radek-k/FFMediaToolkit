namespace FFMediaToolkit.Encoding
{
    using System;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Graphics;
    using Helpers;

    /// <summary>
    /// Represents a video encoder stream
    /// </summary>
    public class VideoEncoderStream
    {
        private readonly VideoStream stream;
        private readonly VideoFrame encodedFrame;
        private readonly Scaler scaler;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoEncoderStream"/> class.
        /// </summary>
        /// <param name="stream">The video stream in encoding mode</param>
        /// <param name="config">The stream setting</param>
        internal VideoEncoderStream(VideoStream stream, VideoEncoderSettings config)
        {
            this.stream = stream;
            Configuration = config;
            encodedFrame = VideoFrame.Create(stream.FrameLayout);
            scaler = new Scaler();
        }

        /// <summary>
        /// Gets the video encoding configuration used to create this stream
        /// </summary>
        public VideoEncoderSettings Configuration { get; }

        /// <summary>
        /// Gets the total number of video frames encoded to this stream
        /// </summary>
        public int FramesCount { get; private set; }

        /// <summary>
        /// Gets the current duration of this stream
        /// </summary>
        public TimeSpan CurrentDuration => TimeSpan.FromMilliseconds(FramesCount * (1D / Configuration.Framerate));

        /// <summary>
        /// Gets the layout
        /// </summary>
        internal Layout Layout => stream.FrameLayout;

        /// <summary>
        /// Writes the specified bitmap to the video stream as the next frame
        /// </summary>
        /// <param name="frame">Bitmap to write</param>
        public void AddFrame(BitmapData frame)
        {
            encodedFrame.UpdateFromBitmap(frame, scaler);
            stream.PushFrame(encodedFrame);
            FramesCount++;
        }
    }
}

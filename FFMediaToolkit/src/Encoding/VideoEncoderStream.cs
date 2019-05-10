namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;
    using Helpers;

    /// <summary>
    /// Represents a video encoder stream
    /// </summary>
    public class VideoEncoderStream
    {
        private VideoStream stream;
        private VideoFrame encodedFrame;
        private Scaler scaler;

        private VideoEncoderStream(VideoStream stream, VideoEncoderSettings config)
        {
            this.stream = stream;
            Configuration = config;
            encodedFrame = VideoFrame.CreateEmpty(stream);
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

        /// <summary>
        /// Creates a new video stream in the specified container.
        /// </summary>
        /// <param name="container">The <see cref="MediaContainer"/> object in encoding mode</param>
        /// <param name="settings">The stream setting</param>
        /// <returns>The new instance of <see cref="VideoEncoderStream"/></returns>
        internal static VideoEncoderStream Create(MediaContainer container, VideoEncoderSettings settings)
        {
            var stream = VideoStream.CreateNew(container, settings);
            return new VideoEncoderStream(stream, settings);
        }
    }
}

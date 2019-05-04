namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using FFmpeg.AutoGen;
    using Helpers;

    /// <summary>
    /// Represents a video stream
    /// </summary>
    internal unsafe class VideoEncoderStream
    {
        // TODO: Implement VideoEncoderStream
        private AVCodecContext* codec;
        private AVStream* stream;
        private AVFormatContext* format;
        private VideoFrame encodedFrame;
        private Scaler scaler;

        /// <summary>
        /// Gets the video encoding configuration used to create this stream
        /// </summary>
        public VideoEncoderSettings Configuration { get; private set; }

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
        internal Layout Layout { get; }

        /// <summary>
        /// Writes the specified bitmap to the video stream as the next frame
        /// </summary>
        /// <param name="frame">Bitmap to write</param>
        public void AddFrame(BitmapData frame)
        {
            fixed (byte* ptr = frame.Data)
            {
                scaler.FillAVFrame((IntPtr)ptr, frame.Layout, encodedFrame);

                // PushFrame(encodedFrame);
            }
        }
    }
}

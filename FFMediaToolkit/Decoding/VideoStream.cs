namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Drawing;
    using System.IO;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a video stream in the <see cref="MediaFile"/>.
    /// </summary>
    public class VideoStream : MediaStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStream"/> class.
        /// </summary>
        /// <param name="stream">The video stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal VideoStream(Decoder stream, MediaOptions options)
            : base(stream, options)
        {
            OutputFrameSize = options.TargetVideoSize ?? Info.FrameSize;
            Converter = new Lazy<ImageConverter>(() => new ImageConverter(Info.FrameSize, Info.AVPixelFormat, OutputFrameSize, (AVPixelFormat)options.VideoPixelFormat));
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public new VideoStreamInfo Info => base.Info as VideoStreamInfo;

        private Lazy<ImageConverter> Converter { get; }

        private Size OutputFrameSize { get; }

        /// <summary>
        /// Reads the next frame from the video stream.
        /// </summary>
        /// <returns>A decoded bitmap.</returns>
        public new ImageData GetNextFrame()
        {
            var frame = base.GetNextFrame() as VideoFrame;
            return frame.ToBitmap(Converter.Value, Options.VideoPixelFormat, OutputFrameSize);
        }

        /// <summary>
        /// Reads the next frame from the video stream.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="bitmap">The decoded video frame.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryGetNextFrame(out ImageData bitmap)
        {
            try
            {
                bitmap = GetNextFrame();
                return true;
            }
            catch (EndOfStreamException)
            {
                bitmap = default;
                return false;
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <returns>The decoded video frame.</returns>
        public new ImageData GetFrame(TimeSpan time)
        {
            var frame = base.GetFrame(time) as VideoFrame;
            return frame.ToBitmap(Converter.Value, Options.VideoPixelFormat, OutputFrameSize);
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <param name="bitmap">The decoded video frame.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryGetFrame(TimeSpan time, out ImageData bitmap)
        {
            try
            {
                bitmap = GetFrame(time);
                return true;
            }
            catch (EndOfStreamException)
            {
                bitmap = default;
                return false;
            }
        }
    }
}

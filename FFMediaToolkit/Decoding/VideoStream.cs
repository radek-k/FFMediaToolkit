namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Drawing;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a video stream in the <see cref="MediaFile"/>.
    /// </summary>
    public class VideoStream : IDisposable
    {
        private readonly InputStream<VideoFrame> stream;
        private readonly VideoFrame frame;
        private readonly Lazy<ImageConverter> converter;
        private readonly MediaOptions mediaOptions;
        private readonly Size outputFrameSize;

        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStream"/> class.
        /// </summary>
        /// <param name="video">The video stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal VideoStream(InputStream<VideoFrame> video, MediaOptions options)
        {
            stream = video;
            mediaOptions = options;
            frame = VideoFrame.CreateEmpty();
            outputFrameSize = options.TargetVideoSize ?? video.Info.FrameSize;
            converter = new Lazy<ImageConverter>(() => new ImageConverter(video.Info.FrameSize, video.Info.AVPixelFormat, outputFrameSize, (AVPixelFormat)options.VideoPixelFormat));
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public StreamInfo Info => stream.Info;

        /// <summary>
        /// Gets the index of the next frame in the video stream.
        /// </summary>
        public int FramePosition { get; private set; }

        /// <summary>
        /// Gets the timestamp of the next frame in the video stream.
        /// </summary>
        public TimeSpan Position => FramePosition.ToTimeSpan(Info.AvgFrameRate);

        /// <summary>
        /// Reads the specified video frame.
        /// This does not work with Variable Frame Rate videos! Use the <see cref="ReadFrame(TimeSpan)"/> overload instead.
        /// </summary>
        /// <param name="frameNumber">The frame index (zero-based number).</param>
        /// <returns>The decoded video frame.</returns>
        public ImageData ReadFrame(int frameNumber)
        {
            lock (syncLock)
            {
                frameNumber = frameNumber.Clamp(0, Info.FrameCount != 0 ? Info.FrameCount - 1 : int.MaxValue);

                if (frameNumber == FramePosition)
                {
                    return GetNextFrameAsBitmap();
                }
                else if (frameNumber == FramePosition - 1)
                {
                    return ConvertVideoFrameToBitmap(stream.RecentlyDecodedFrame);
                }
                else
                {
                    var frame = SeekToFrame(frameNumber);
                    FramePosition = frameNumber + 1;

                    return ConvertVideoFrameToBitmap(frame);
                }
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// </summary>
        /// <param name="targetTime">The frame timestamp.</param>
        /// <returns>The decoded video frame.</returns>
        public ImageData ReadFrame(TimeSpan targetTime) => ReadFrame(targetTime.ToFrameNumber(Info.RealFrameRate));

        /// <summary>
        /// Reads the next frame from this stream.
        /// </summary>
        /// <returns>The decoded video frame.</returns>
        public unsafe ImageData ReadNextFrame()
        {
            lock (syncLock)
            {
                return GetNextFrameAsBitmap();
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            lock (syncLock)
            {
                stream.Dispose();
                frame.Dispose();
                if (converter.IsValueCreated)
                {
                    converter.Value.Dispose();
                }
            }
        }

        private ImageData GetNextFrameAsBitmap()
        {
            var bmp = ConvertVideoFrameToBitmap(stream.GetNextFrame());
            FramePosition++;
            return bmp;
        }

        private unsafe ImageData ConvertVideoFrameToBitmap(VideoFrame frame)
        {
            // Gets the target size of the frame (it may be set by the MediaOptions.TargetVideoSize).
            var bitmap = ImageData.CreatePooled(outputFrameSize, mediaOptions.VideoPixelFormat); // Rents memory for the output bitmap.
            converter.Value.AVFrameToBitmap(frame, bitmap); // Converts the raw video frame using the given size and pixel format and writes it to the ImageData bitmap.
            return bitmap;
        }

        private VideoFrame SeekToFrame(int frameNumber)
        {
            var ts = frameNumber.ToTimestamp(Info.RealFrameRate, Info.TimeBase);

            if (frameNumber < FramePosition || frameNumber > FramePosition + mediaOptions.VideoSeekThreshold)
            {
                stream.OwnerFile.SeekFile(ts, Info.Index);
            }

            stream.AdjustPackets(frameNumber.ToTimestamp(Info.RealFrameRate, Info.TimeBase));
            return stream.RecentlyDecodedFrame;
        }
    }
}

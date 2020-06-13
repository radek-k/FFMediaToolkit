namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Drawing;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;

    /// <summary>
    /// Represents a video stream in the <see cref="MediaFile"/>.
    /// </summary>
    public class VideoStream : IDisposable
    {
        private readonly InputStream<VideoFrame> stream;
        private readonly VideoFrame frame;
        private readonly ImageConverter converter;
        private readonly MediaOptions mediaOptions;

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
            converter = new ImageConverter();
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
        public TimeSpan Position => FramePosition.ToTimeSpan(Info.FrameRate);

        /// <summary>
        /// Reads the specified video frame.
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
                    return ConvertVideoFrameToBitmap(frame);
                }
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// </summary>
        /// <param name="targetTime">The frame timestamp.</param>
        /// <returns>The decoded video frame.</returns>
        public ImageData ReadFrame(TimeSpan targetTime) => ReadFrame(targetTime.ToFrameNumber(Info.RFrameRate));

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
                converter.Dispose();
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
            var targetLayout = GetTargetSize(); // Gets the target size of the frame (it may be set by the MediaOptions.TargetVideoSize).
            var bitmap = ImageData.CreatePooled(targetLayout, mediaOptions.VideoPixelFormat); // Rents memory for the output bitmap.
            converter.AVFrameToBitmap(frame, bitmap); // Converts the raw video frame using the given size and pixel format and writes it to the ImageData bitmap.
            return bitmap;
        }

        private VideoFrame SeekToFrame(int frameNumber)
        {
            var ts = frameNumber.ToTimestamp(Info.RFrameRate, Info.TimeBase);

            if (frameNumber < FramePosition || frameNumber > FramePosition + mediaOptions.VideoSeekThreshold)
            {
                stream.OwnerFile.SeekFile(ts, Info.Index);
            }

            stream.AdjustPackets(frameNumber.ToTimestamp(Info.RFrameRate, Info.TimeBase));

            FramePosition = frameNumber + 1;
            return stream.RecentlyDecodedFrame;
        }

        private Size GetTargetSize() => mediaOptions.TargetVideoSize ?? stream.Info.FrameSize;
    }
}

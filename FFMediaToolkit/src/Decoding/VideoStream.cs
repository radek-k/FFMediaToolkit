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
        private readonly Scaler scaler;
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
            scaler = new Scaler();
        }

        /// <summary>
        /// Gets the stream informations.
        /// </summary>
        public StreamInfo Info => stream.Info;

        /// <summary>
        /// Gets the current stream position in frames.
        /// </summary>
        public int FramePosition { get; private set; }

        /// <summary>
        /// Gets the current stream time position.
        /// </summary>
        public TimeSpan Position => FramePosition.ToTimeSpan(Info.FrameRate);

        /// <summary>
        /// Reads the specified frame from the video stream.
        /// </summary>
        /// <param name="frameNumber">The frame number.</param>
        /// <returns>The video frame.</returns>
        public ImageData ReadFrame(int frameNumber)
        {
            lock (syncLock)
            {
                SeekToFrame(frameNumber);
                return Read();
            }
        }

        /// <summary>
        /// Reads the frame at the specified time from the video stream.
        /// </summary>
        /// <param name="targetTime">The frame time.</param>
        /// <returns>The video frame.</returns>
        public ImageData ReadFrame(TimeSpan targetTime) => ReadFrame(targetTime.ToFrameNumber(Info.RFrameRate));

        /// <summary>
        /// Gets the next frame from the video stream.
        /// </summary>
        /// <returns>The video frame.</returns>
        public unsafe ImageData ReadNextFrame()
        {
            lock (syncLock)
            {
                return Read();
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            lock (syncLock)
            {
                stream.Dispose();
                frame.Dispose();
                scaler.Dispose();
            }
        }

        private unsafe ImageData Read()
        {
            stream.Read(frame);
            FramePosition++;

            var targetLayout = GetTargetSize();
            var bitmap = ImageData.CreatePooled(targetLayout, mediaOptions.VideoPixelFormat);
            scaler.AVFrameToBitmap(frame, bitmap);
            return bitmap;
        }

        private void SeekToFrame(int frameNumber)
        {
            frameNumber = frameNumber.Clamp(0, Info.FrameCount != 0 ? Info.FrameCount : int.MaxValue);

            if (frameNumber == FramePosition + 1)
            {
                return;
            }

            if (frameNumber <= FramePosition || frameNumber >= FramePosition + mediaOptions.VideoSeekThreshold)
            {
                stream.OwnerFile.SeekFile(frameNumber);
            }
            else
            {
                stream.OwnerFile.SeekForward(frameNumber);
            }

            FramePosition = frameNumber - 1;
        }

        private Size GetTargetSize() => mediaOptions.TargetVideoSize ?? stream.Info.FrameSize;
    }
}

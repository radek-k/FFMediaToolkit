namespace FFMediaToolkit.Decoding
{
    using System;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the video stream in a media file.
    /// </summary>
    public class VideoStream : IDisposable
    {
        private const int FrameSeekThreshold = 5;

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
        public BitmapData ReadFrame(int frameNumber)
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
        public BitmapData ReadFrame(TimeSpan targetTime) => ReadFrame(targetTime.ToFrameNumber(Info.FrameRate));

        /// <summary>
        /// Gets the next frame from the video stream.
        /// </summary>
        /// <returns>The video frame.</returns>
        public unsafe BitmapData ReadNextFrame()
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

        private unsafe BitmapData Read()
        {
            stream.Read(frame);
            FramePosition++;

            var targetLayout = GetTargetLayout();
            var bitmap = BitmapData.CreatePooled(targetLayout.Width, targetLayout.Height, mediaOptions.VideoPixelFormat);

            fixed (byte* ptr = bitmap.Data)
            {
                scaler.AVFrameToBitmap(frame.Pointer, frame.Layout, new IntPtr(ptr), targetLayout);
            }

            return bitmap;
        }

        private void SeekToFrame(int frameNumber)
        {
            if (frameNumber == FramePosition + 1)
            {
                return;
            }

            if (frameNumber <= FramePosition || frameNumber > FramePosition + FrameSeekThreshold)
            {
                stream.OwnerFile.SeekFile(frameNumber);
            }
            else
            {
                stream.OwnerFile.SeekForward(frameNumber);
            }

            FramePosition = frameNumber - 1;
        }

        private Layout GetTargetLayout()
        {
            var target = mediaOptions.TargetVideoSize ?? stream.Info.Dimensions.Size;
            return new Layout((AVPixelFormat)mediaOptions.VideoPixelFormat, target);
        }
    }
}

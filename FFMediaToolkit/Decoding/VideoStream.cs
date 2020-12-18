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
        private readonly Decoder<VideoFrame> stream;
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
        internal VideoStream(Decoder<VideoFrame> video, MediaOptions options)
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
                var frame = GetFrame(frameNumber);
                return frame.ToBitmap(converter.Value, mediaOptions.VideoPixelFormat, outputFrameSize);
            }
        }

        /// <summary>
        /// Reads the specified video frame and writes the converted bitmap bytes directly to the provided buffer.
        /// This does not work with Variable Frame Rate videos! Use the <see cref="ReadFrame(TimeSpan)"/> overload instead.
        /// </summary>
        /// <param name="frameNumber">Index of frame to read (zero-based number).</param>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <param name="bufferStride">Number of bytes in a single row of pixel data.</param>
        public void ReadFrameToPointer(int frameNumber, IntPtr buffer, int bufferStride)
        {
            lock (syncLock)
            {
                var frame = GetFrame(frameNumber);
                CopyFrameToMemory(frame, buffer, bufferStride);
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
        public ImageData ReadNextFrame()
        {
            lock (syncLock)
            {
                var frame = GetNextFrame();
                return frame.ToBitmap(converter.Value, mediaOptions.VideoPixelFormat, outputFrameSize);
            }
        }

        /// <summary>
        /// Reads the next frame from this stream and writes the converted bitmap bytes directly to the provided buffer.
        /// </summary>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <param name="bufferStride">Number of bytes in a single row of pixel data.</param>
        public void ReadNextFrameToPointer(IntPtr buffer, int bufferStride)
        {
            lock (syncLock)
            {
                var frame = GetNextFrame();
                CopyFrameToMemory(frame, buffer, bufferStride);
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

        private VideoFrame GetFrame(int frameNumber)
        {
            frameNumber = frameNumber.Clamp(0, Info.FrameCount != 0 ? Info.FrameCount - 1 : int.MaxValue);

            if (frameNumber == FramePosition)
            {
                return GetNextFrame();
            }
            else if (frameNumber != FramePosition - 1)
            {
                var ts = frameNumber.ToTimestamp(Info.RealFrameRate, Info.TimeBase);

                if (frameNumber < FramePosition || frameNumber > FramePosition + mediaOptions.VideoSeekThreshold)
                {
                    stream.OwnerFile.SeekFile(ts, Info.Index);
                }

                stream.SkipFrames(frameNumber.ToTimestamp(Info.RealFrameRate, Info.TimeBase));
                FramePosition = frameNumber + 1;
            }

            return stream.RecentlyDecodedFrame;
        }

        private VideoFrame GetNextFrame()
        {
            var frame = stream.GetNextFrame();
            FramePosition++;
            return frame;
        }

        private unsafe void CopyFrameToMemory(VideoFrame frame, IntPtr target, int stride)
        {
            var ptr = (byte*)target.ToPointer();
            converter.Value.AVFrameToBitmap(frame, ptr, stride);
        }
    }
}

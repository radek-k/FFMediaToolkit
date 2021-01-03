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
    public class VideoStream : IDisposable
    {
        private readonly Decoder stream;
        private readonly Lazy<ImageConverter> converter;
        private readonly MediaOptions mediaOptions;
        private readonly Size outputFrameSize;
        private readonly long threshold;

        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStream"/> class.
        /// </summary>
        /// <param name="video">The video stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal VideoStream(Decoder video, MediaOptions options)
        {
            stream = video;
            mediaOptions = options;

            outputFrameSize = options.TargetVideoSize ?? Info.FrameSize;
            converter = new Lazy<ImageConverter>(() => new ImageConverter(Info.FrameSize, Info.AVPixelFormat, outputFrameSize, (AVPixelFormat)options.VideoPixelFormat));
            threshold = TimeSpan.FromSeconds(0.5).ToTimestamp(Info.TimeBase);
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public VideoStreamInfo Info => (VideoStreamInfo)stream.Info;

        /// <summary>
        /// Gets the index of the next frame in the video stream.
        /// </summary>
        public int FramePosition { get; private set; }

        /// <summary>
        /// Gets the timestamp of the recently decoded frame in the video stream.
        /// </summary>
        public TimeSpan Position => stream.RecentlyDecodedFrame.PresentationTimestamp.ToTimeSpan(Info.TimeBase);

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
                var frame = GetFrame(frameNumber, Info.NumberOfFrames.Value);
                return frame.ToBitmap(converter.Value, mediaOptions.VideoPixelFormat, outputFrameSize);
            }
        }

        /// <summary>
        /// Reads the specified video frame.
        /// This does not work with Variable Frame Rate videos! Use the <see cref="ReadFrame(TimeSpan)"/> overload instead.
        /// A <see langword="false"/> return value indicates that reached end of stream so frame was not read.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="frameNumber">The frame index (zero-based number).</param>
        /// <param name="bitmap">The decoded video frame.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryReadFrame(int frameNumber, out ImageData bitmap)
        {
            if (Info.IsVariableFrameRate)
            {
                throw new NotSupportedException("Access to frame by index is not supported in variable frame rate video.");
            }

            lock (syncLock)
            {
                VideoFrame frame;
                try
                {
                    frame = GetFrame(frameNumber, Info.NumberOfFrames.Value);
                }
                catch (EndOfStreamException)
                {
                    bitmap = default;
                    return false;
                }

                bitmap = frame.ToBitmap(converter.Value, mediaOptions.VideoPixelFormat, outputFrameSize);
                return true;
            }
        }

        /// <summary>
        /// Reads the specified video frame and writes the converted bitmap bytes directly to the provided buffer. A <see langword="false"/> return value indicates that reached end of stream so frame was not read. The method throws exception if another error has occurred.
        /// This does not work with Variable Frame Rate videos! Use the <see cref="ReadFrame(TimeSpan)"/> overload instead.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="frameNumber">The frame index (zero-based number).</param>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <param name="bufferStride">Number of bytes in a single row of pixel data.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryReadFrameToPointer(int frameNumber, IntPtr buffer, int bufferStride)
        {
            if (Info.IsVariableFrameRate)
            {
                throw new NotSupportedException("Access to frame by index is not supported in variable frame rate video.");
            }

            lock (syncLock)
            {
                VideoFrame frame;
                try
                {
                    frame = GetFrame(frameNumber, Info.NumberOfFrames.Value);
                }
                catch (EndOfStreamException)
                {
                    return false;
                }

                CopyFrameToMemory(frame, buffer, bufferStride);
                return true;
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// </summary>
        /// <param name="targetTime">The frame timestamp.</param>
        /// <returns>The decoded video frame.</returns>
        public ImageData ReadFrame(TimeSpan targetTime)
        {
            lock (syncLock)
            {
                var frame = GetFrameByTimestamp(targetTime.ToTimestamp(Info.TimeBase));
                return frame.ToBitmap(converter.Value, mediaOptions.VideoPixelFormat, outputFrameSize);
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="targetTime">The frame timestamp.</param>
        /// <param name="bitmap">The decoded video frame.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryReadFrame(TimeSpan targetTime, out ImageData bitmap)
        {
            lock (syncLock)
            {
                VideoFrame frame;
                try
                {
                    frame = GetFrameByTimestamp(targetTime.ToTimestamp(Info.TimeBase));
                }
                catch (EndOfStreamException)
                {
                    bitmap = default;
                    return false;
                }

                bitmap = frame.ToBitmap(converter.Value, mediaOptions.VideoPixelFormat, outputFrameSize);
                return true;
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="targetTime">The frame timestamp.</param>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <param name="bufferStride">Number of bytes in a single row of pixel data.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryReadFrameToPointer(TimeSpan targetTime, IntPtr buffer, int bufferStride)
        {
            lock (syncLock)
            {
                VideoFrame frame;
                try
                {
                    frame = GetFrameByTimestamp(targetTime.ToTimestamp(Info.TimeBase));
                }
                catch (EndOfStreamException)
                {
                    return false;
                }

                CopyFrameToMemory(frame, buffer, bufferStride);
                return true;
            }
        }

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
        /// Reads the next frame from this stream.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="bitmap">The decoded video frame.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryReadNextFrame(out ImageData bitmap)
        {
            lock (syncLock)
            {
                VideoFrame frame;
                try
                {
                    frame = GetNextFrame();
                }
                catch (EndOfStreamException)
                {
                    bitmap = default;
                    return false;
                }

                bitmap = frame.ToBitmap(converter.Value, mediaOptions.VideoPixelFormat, outputFrameSize);
                return true;
            }
        }

        /// <summary>
        /// Reads the next frame from this stream and writes the converted bitmap bytes directly to the provided buffer.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <param name="bufferStride">Number of bytes in a single row of pixel data.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryReadNextFrameToPointer(IntPtr buffer, int bufferStride)
        {
            lock (syncLock)
            {
                VideoFrame frame;
                try
                {
                    frame = GetNextFrame();
                }
                catch (EndOfStreamException)
                {
                    return false;
                }

                CopyFrameToMemory(frame, buffer, bufferStride);
                return true;
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            lock (syncLock)
            {
                stream.Dispose();

                if (converter.IsValueCreated)
                {
                    converter.Value.Dispose();
                }
            }
        }

        private VideoFrame GetFrame(int frameNumber, int frameCount)
        {
            frameNumber = frameNumber.Clamp(0, frameCount != 0 ? frameCount - 1 : int.MaxValue);

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

            return stream.RecentlyDecodedFrame as VideoFrame;
        }

        private VideoFrame GetFrameByTimestamp(long ts)
        {
            var frame = stream.RecentlyDecodedFrame;
            ts = Math.Max(0, Math.Min(ts, Info.DurationRaw));

            if (ts > frame.PresentationTimestamp && ts < frame.PresentationTimestamp + threshold)
            {
                return GetNextFrame();
            }
            else if (ts != frame.PresentationTimestamp)
            {
                if (ts < frame.PresentationTimestamp || ts >= frame.PresentationTimestamp + threshold)
                {
                    stream.OwnerFile.SeekFile(ts, Info.Index);
                }

                stream.SkipFrames(ts);
                FramePosition = Position.ToFrameNumber(Info.RealFrameRate) + 1;
            }

            return stream.RecentlyDecodedFrame as VideoFrame;
        }

        private VideoFrame GetNextFrame()
        {
            var frame = stream.GetNextFrame();
            FramePosition++;
            return frame as VideoFrame;
        }

        private unsafe void CopyFrameToMemory(VideoFrame frame, IntPtr target, int stride)
        {
            if (stride != ImageData.EstimateStride(outputFrameSize.Width, mediaOptions.VideoPixelFormat))
            {
                throw new ArgumentOutOfRangeException(nameof(stride), "Stride does not match output bitmap size and pixel format.");
            }

            var ptr = (byte*)target.ToPointer();
            converter.Value.AVFrameToBitmap(frame, ptr, stride);
        }
    }
}

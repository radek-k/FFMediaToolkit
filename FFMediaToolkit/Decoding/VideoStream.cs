namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Drawing;
    using System.IO;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a video stream in the <see cref="MediaFile"/>.
    /// </summary>
    public class VideoStream : MediaStream
    {
        private readonly int outputFrameStride;
        private readonly int requiredBufferSize;
        private readonly ImageConverter converter;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStream"/> class.
        /// </summary>
        /// <param name="stream">The video stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal VideoStream(Decoder stream, MediaOptions options)
            : base(stream, options)
        {
            OutputFrameSize = options.TargetVideoSize ?? Info.FrameSize;
            converter = new ImageConverter(OutputFrameSize, (AVPixelFormat)options.VideoPixelFormat);

            outputFrameStride = ImageData.EstimateStride(OutputFrameSize.Width, Options.VideoPixelFormat);
            requiredBufferSize = outputFrameStride * OutputFrameSize.Height;
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public new VideoStreamInfo Info => base.Info as VideoStreamInfo;

        private Size OutputFrameSize { get; }

        /// <summary>
        /// Reads the next frame from the video stream.
        /// </summary>
        /// <returns>A decoded bitmap.</returns>
        /// <exception cref="EndOfStreamException">End of the stream.</exception>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
        public new ImageData GetNextFrame()
        {
            var frame = base.GetNextFrame() as VideoFrame;
            return frame.ToBitmap(converter, Options.VideoPixelFormat, OutputFrameSize);
        }

        /// <summary>
        /// Reads the next frame from the video stream.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="bitmap">The decoded video frame.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
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
        /// Reads the next frame from the video stream  and writes the converted bitmap data directly to the provided buffer.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        /// <exception cref="ArgumentException">Too small buffer.</exception>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
        public unsafe bool TryGetNextFrame(Span<byte> buffer)
        {
            if (buffer.Length < requiredBufferSize)
            {
                throw new ArgumentException(nameof(buffer), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                fixed (byte* ptr = buffer)
                {
                    ConvertCopyFrameToMemory(base.GetNextFrame() as VideoFrame, ptr);
                }

                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the next frame from the video stream  and writes the converted bitmap data directly to the provided buffer.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <param name="bufferStride">Size in bytes of a single row of pixels.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        /// <exception cref="ArgumentException">Too small buffer.</exception>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
        public unsafe bool TryGetNextFrame(IntPtr buffer, int bufferStride)
        {
            if (bufferStride != outputFrameStride)
            {
                throw new ArgumentException(nameof(bufferStride), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                ConvertCopyFrameToMemory(base.GetNextFrame() as VideoFrame, (byte*)buffer);
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <returns>The decoded video frame.</returns>
        /// <exception cref="EndOfStreamException">End of the stream.</exception>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
        public new ImageData GetFrame(TimeSpan time)
        {
            var frame = base.GetFrame(time) as VideoFrame;
            return frame.ToBitmap(converter, Options.VideoPixelFormat, OutputFrameSize);
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <param name="bitmap">The decoded video frame.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
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

        /// <summary>
        /// Reads the video frame found at the specified timestamp and writes the converted bitmap data directly to the provided buffer.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        /// <exception cref="ArgumentException">Too small buffer.</exception>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
        public unsafe bool TryGetFrame(TimeSpan time, Span<byte> buffer)
        {
            if (buffer.Length < requiredBufferSize)
            {
                throw new ArgumentException(nameof(buffer), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                fixed (byte* ptr = buffer)
                {
                    ConvertCopyFrameToMemory(base.GetFrame(time) as VideoFrame, ptr);
                }

                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp and writes the converted bitmap data directly to the provided buffer.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <param name="buffer">Pointer to the memory buffer.</param>
        /// <param name="bufferStride">Size in bytes of a single row of pixels.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        /// <exception cref="ArgumentException">Too small buffer.</exception>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
        public unsafe bool TryGetFrame(TimeSpan time, IntPtr buffer, int bufferStride)
        {
            if (bufferStride != outputFrameStride)
            {
                throw new ArgumentException(nameof(bufferStride), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                ConvertCopyFrameToMemory(base.GetFrame(time) as VideoFrame, (byte*)buffer);
                return true;
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            if (!isDisposed)
            {
                converter.Dispose();
                isDisposed = true;
            }

            base.Dispose();
        }

        private unsafe void ConvertCopyFrameToMemory(VideoFrame frame, byte* target)
        {
            converter.AVFrameToBitmap(frame, target, outputFrameStride);
        }
    }
}

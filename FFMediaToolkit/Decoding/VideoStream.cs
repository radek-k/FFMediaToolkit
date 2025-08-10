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
        private readonly Size outputFrameSize;
        private readonly ImageConverter converter;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStream"/> class.
        /// </summary>
        /// <param name="stream">The video stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal VideoStream(Decoder stream, MediaOptions options)
            : base(stream, options, options.VideoSeekThreshold)
        {
            outputFrameSize = options.TargetVideoSize ?? Info.FrameSize;
            converter = new ImageConverter(outputFrameSize, (AVPixelFormat)options.VideoPixelFormat, options.FlipVertically);

            FrameStride = ImageData.EstimateStride(outputFrameSize.Width, Options.VideoPixelFormat);
            FrameByteCount = FrameStride * outputFrameSize.Height;
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public new VideoStreamInfo Info => base.Info as VideoStreamInfo;

        /// <summary>
        /// Gets the number of bytes in a single row of decoded frame data.
        /// </summary>
        public int FrameStride { get; }

        /// <summary>
        /// Gets the number of bytes required to store the decoded frame.
        /// </summary>
        public int FrameByteCount { get; }

        /// <summary>
        /// Reads the next frame from the video stream.
        /// </summary>
        /// <returns>A decoded bitmap.</returns>
        /// <exception cref="EndOfStreamException">End of the stream.</exception>
        /// <exception cref="FFmpegException">Internal decoding error.</exception>
        public new ImageData GetNextFrame() => CreatePooledBitmap(base.GetNextFrame() as VideoFrame);

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
            if (buffer.Length < FrameByteCount)
            {
                throw new ArgumentException(nameof(buffer), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                fixed (byte* ptr = buffer)
                {
                    converter.AVFrameToBitmap(base.GetNextFrame() as VideoFrame, ptr, FrameStride);
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
            if (bufferStride != FrameStride)
            {
                throw new ArgumentException(nameof(bufferStride), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                converter.AVFrameToBitmap(base.GetNextFrame() as VideoFrame, (byte*)buffer, FrameStride);
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
        public new ImageData GetFrame(TimeSpan time) => CreatePooledBitmap(base.GetFrame(time) as VideoFrame);

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
            if (buffer.Length < FrameByteCount)
            {
                throw new ArgumentException(nameof(buffer), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                fixed (byte* ptr = buffer)
                {
                    converter.AVFrameToBitmap(base.GetFrame(time) as VideoFrame, ptr, FrameStride);
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
            if (bufferStride != FrameStride)
            {
                throw new ArgumentException(nameof(bufferStride), "Destination buffer is smaller than the converted bitmap data.");
            }

            try
            {
                converter.AVFrameToBitmap(base.GetFrame(time) as VideoFrame, (byte*)buffer, FrameStride);
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

        private unsafe ImageData CreatePooledBitmap(VideoFrame frame)
        {
            var bitmap = ImageData.CreatePooled(outputFrameSize, Options.VideoPixelFormat);
            fixed (byte* ptr = bitmap.Data)
            {
                converter.AVFrameToBitmap(frame, ptr, bitmap.Stride);
            }

            return bitmap;
        }
    }
}

namespace FFMediaToolkit.Common.Internal
{
    using System;
    using System.Drawing;
    using FFMediaToolkit.Graphics;
    using FFmpeg.AutoGen;

    /// <summary>
    /// A class used to convert ffmpeg <see cref="AVFrame"/>s to <see cref="ImageData"/> objects with specified image size and color format.
    /// </summary>
    internal unsafe class ImageConverter : Wrapper<SwsContext>
    {
        // sws_scale requires up to 16 extra bytes allocated in the input buffer when resizing an image
        // (reference: https://www.ffmpeg.org/doxygen/6.0/frame_8h_source.html#l00340)
        private const int BufferPaddingSize = 16;
        private byte[] tmpBuffer = { };

        private readonly Size destinationSize;
        private readonly AVPixelFormat destinationFormat;
        private Size lastSourceSize;
        private AVPixelFormat lastSourcePixelFormat;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageConverter"/> class.
        /// </summary>
        /// <param name="destinationSize">Destination image size.</param>
        /// <param name="destinationFormat">Destination image format.</param>
        public ImageConverter(Size destinationSize, AVPixelFormat destinationFormat)
            : base(null)
        {
            this.destinationSize = destinationSize;
            this.destinationFormat = destinationFormat;
        }

        /// <summary>
        /// Overrides the <paramref name="destinationFrame"/> image buffer with the converted <see cref="ImageData"/> bitmap. Used in encoding.
        /// </summary>
        /// <param name="bitmap">The input bitmap.</param>
        /// <param name="destinationFrame">The <see cref="AVFrame"/> to override.</param>
        internal void FillAVFrame(ImageData bitmap, VideoFrame destinationFrame)
        {
            UpdateContext(bitmap.ImageSize, (AVPixelFormat)bitmap.PixelFormat);

            var requiredBufferLength = (bitmap.Stride * bitmap.ImageSize.Height) + BufferPaddingSize;
            var shouldUseTmpBuffer = bitmap.ImageSize != destinationSize && bitmap.Data.Length < requiredBufferLength;

            if (shouldUseTmpBuffer)
            {
                if (tmpBuffer.Length < requiredBufferLength)
                {
                    tmpBuffer = new byte[requiredBufferLength];
                }

                bitmap.Data.CopyTo(tmpBuffer);
            }

            var source = shouldUseTmpBuffer ? tmpBuffer : bitmap.Data;
            fixed (byte* ptr = source)
            {
                var data = new byte*[4] { ptr, null, null, null };
                var linesize = new int[4] { bitmap.Stride, 0, 0, 0 };
                ffmpeg.sws_scale(Pointer, data, linesize, 0, bitmap.ImageSize.Height, destinationFrame.Pointer->data, destinationFrame.Pointer->linesize);
            }
        }

        /// <summary>
        /// Converts a video <see cref="AVFrame"/> to the specified <see cref="ImageData"/> bitmap. Used in decoding.
        /// </summary>
        /// <param name="videoFrame">The video frame to convert.</param>
        /// <param name="destination">The destination <see cref="ImageData"/>.</param>
        /// <param name="stride">Size of the single bitmap row.</param>
        internal void AVFrameToBitmap(VideoFrame videoFrame, byte* destination, int stride)
        {
            UpdateContext(videoFrame.Layout, videoFrame.PixelFormat);

            var data = new byte*[4] { destination, null, null, null };
            var linesize = new int[4] { stride, 0, 0, 0 };
            ffmpeg.sws_scale(Pointer, videoFrame.Pointer->data, videoFrame.Pointer->linesize, 0, videoFrame.Layout.Height, data, linesize);
        }

        /// <inheritdoc/>
        protected override void OnDisposing() => ffmpeg.sws_freeContext(Pointer);

        private void UpdateContext(Size sourceSize, AVPixelFormat sourceFormat)
        {
            if (sourceSize != lastSourceSize || sourceFormat != lastSourcePixelFormat)
            {
                ffmpeg.sws_freeContext(Pointer);

                var scaleMode = sourceSize == destinationSize ? ffmpeg.SWS_POINT : ffmpeg.SWS_BICUBIC;
                var swsContext = ffmpeg.sws_getContext(sourceSize.Width, sourceSize.Height, sourceFormat, destinationSize.Width, destinationSize.Height, destinationFormat, scaleMode, null, null, null);

                if (swsContext == null)
                {
                    throw new FFmpegException("Cannot allocate SwsContext.");
                }

                UpdatePointer(swsContext);
                lastSourceSize = sourceSize;
                lastSourcePixelFormat = sourceFormat;
            }
        }
    }
}

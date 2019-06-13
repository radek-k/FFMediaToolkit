namespace FFMediaToolkit.Graphics
{
    using System;
    using System.Buffers;
    using System.Drawing;

    /// <summary>
    /// Represent a wrapper of different bitmap formats.
    /// </summary>
    public ref struct BitmapData
    {
        private readonly Span<byte> span;
        private readonly IMemoryOwner<byte> pooledMemory;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapData"/> struct using a <see cref="Span{T}"/> as the data source.
        /// </summary>
        /// <param name="data">The bitmap data.</param>
        /// <param name="imageSize">The image dimensions.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <exception cref="ArgumentException">When data span size doesn't match size calculated from width, height and the pixel format.</exception>
        public BitmapData(Span<byte> data, Size imageSize, ImagePixelFormat pixelFormat)
        {
            var size = EstimateStride(imageSize.Width, pixelFormat) * imageSize.Height;
            if (data.Length != size)
            {
                throw new ArgumentException("Pixel buffer size doesn't match size required by this image format.");
            }

            span = data;
            pooledMemory = null;

            ImageSize = imageSize;
            PixelFormat = pixelFormat;
        }

        private BitmapData(IMemoryOwner<byte> memory, Size size, ImagePixelFormat pixelFormat)
        {
            span = null;
            pooledMemory = memory;

            ImageSize = size;
            PixelFormat = pixelFormat;
        }

        /// <summary>
        /// Gets the <see cref="Span{T}"/> object containing the bitmap data.
        /// </summary>
        public Span<byte> Data => IsPooled ? pooledMemory.Memory.Span : span;

        /// <summary>
        /// Gets a value indicating whether this instance of <see cref="BitmapData"/> uses memory pooling.
        /// </summary>
        public bool IsPooled => pooledMemory != null;

        /// <summary>
        /// Gets the image size.
        /// </summary>
        public Size ImageSize { get; }

        /// <summary>
        /// Gets the bitmap pixel format.
        /// </summary>
        public ImagePixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the estimated image linesize.
        /// </summary>
        public int Stride => EstimateStride(ImageSize.Width, PixelFormat);

        /// <summary>
        /// Rents a memory buffer from pool and creates a new instance of <see cref="BitmapData"/> class from it.
        /// </summary>
        /// <param name="imageSize">The image dimensions.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>The new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData CreatePooled(Size imageSize, ImagePixelFormat pixelFormat)
        {
            var size = EstimateStride(imageSize.Width, pixelFormat) * imageSize.Height;
            var pool = MemoryPool<byte>.Shared;
            var memory = pool.Rent(size);
            return new BitmapData(memory, imageSize, pixelFormat);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitmapData"/> class using a byte array as the data source.
        /// </summary>
        /// <param name="pixels">The byte array containing bitmap data.</param>
        /// <param name="imageSize">The image dimensions.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>A new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData FromArray(byte[] pixels, Size imageSize, ImagePixelFormat pixelFormat)
            => new BitmapData(new Span<byte>(pixels), imageSize, pixelFormat);

        /// <summary>
        /// Creates a new instance of the <see cref="BitmapData"/> class using a pointer to the unmanaged memory as the data source.
        /// </summary>
        /// <param name="pointer">The byte array containing bitmap data.</param>
        /// <param name="imageSize">The image dimensions.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>A new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData FromPointer(IntPtr pointer, Size imageSize, ImagePixelFormat pixelFormat)
        {
            var span = CreateSpan(pointer, imageSize, pixelFormat);
            return new BitmapData(span, imageSize, pixelFormat);
        }

        /// <summary>
        /// Gets the estimated image line size based on the pixel format and width.
        /// </summary>
        /// <param name="width">The image width.</param>
        /// <param name="format">The image pixel format.</param>
        /// <returns>The size of a single line of the image measured in bytes.</returns>
        public static int EstimateStride(int width, ImagePixelFormat format) => GetBytesPerPixel(format) * width;

        private static unsafe Span<byte> CreateSpan(IntPtr pointer, Size imageSize, ImagePixelFormat pixelFormat)
        {
            var size = EstimateStride(imageSize.Width, pixelFormat) * imageSize.Height;
            return new Span<byte>((void*)pointer, size);
        }

        private static int GetBytesPerPixel(ImagePixelFormat format)
        {
            switch (format)
            {
                case ImagePixelFormat.BGR24:
                    return 3;
                case ImagePixelFormat.BGRA32:
                    return 4;
                case ImagePixelFormat.RGB24:
                    return 3;
                case ImagePixelFormat.ARGB32:
                    return 4;
                default:
                    return 0;
            }
        }
    }
}

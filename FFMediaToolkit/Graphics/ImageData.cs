namespace FFMediaToolkit.Graphics
{
    using System;
    using System.Buffers;
    using System.Drawing;

    /// <summary>
    /// Represent a lightweight container for bitmap images.
    /// </summary>
    public ref struct ImageData
    {
        private readonly Span<byte> span;
        private readonly IMemoryOwner<byte> pooledMemory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageData"/> struct using a <see cref="Span{T}"/> as the data source.
        /// </summary>
        /// <param name="data">The bitmap data.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <param name="imageSize">The image dimensions.</param>
        /// <exception cref="ArgumentException">When data span size doesn't match size calculated from width, height and the pixel format.</exception>
        public ImageData(Span<byte> data, ImagePixelFormat pixelFormat, Size imageSize)
        {
            var size = EstimateStride(imageSize.Width, pixelFormat) * imageSize.Height;
            if (data.Length < size)
            {
                throw new ArgumentException("Pixel buffer size doesn't match size required by this image format.");
            }

            span = data;
            pooledMemory = null;

            ImageSize = imageSize;
            PixelFormat = pixelFormat;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageData"/> struct using a <see cref="Span{T}"/> as the data source.
        /// </summary>
        /// <param name="data">The bitmap data.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <exception cref="ArgumentException">When data span size doesn't match size calculated from width, height and the pixel format.</exception>
        public ImageData(Span<byte> data, ImagePixelFormat pixelFormat, int width, int height)
            : this(data, pixelFormat, new Size(width, height))
        {
        }

        private ImageData(IMemoryOwner<byte> memory, Size size, ImagePixelFormat pixelFormat)
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
        /// Gets a value indicating whether this instance of <see cref="ImageData"/> uses memory pooling.
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
        /// Gets the estimated number of bytes in one row of image pixels.
        /// </summary>
        public int Stride => EstimateStride(ImageSize.Width, PixelFormat);

        /// <summary>
        /// Rents a memory buffer from pool and creates a new instance of <see cref="ImageData"/> class from it.
        /// </summary>
        /// <param name="imageSize">The image dimensions.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>The new <see cref="ImageData"/> instance.</returns>
        public static ImageData CreatePooled(Size imageSize, ImagePixelFormat pixelFormat)
        {
            var size = EstimateStride(imageSize.Width, pixelFormat) * imageSize.Height;
            var pool = MemoryPool<byte>.Shared;
            var memory = pool.Rent(size);
            return new ImageData(memory, imageSize, pixelFormat);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ImageData"/> class using a byte array as the data source.
        /// </summary>
        /// <param name="pixels">The byte array containing bitmap data.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <param name="imageSize">The image dimensions.</param>
        /// <returns>A new <see cref="ImageData"/> instance.</returns>
        public static ImageData FromArray(byte[] pixels, ImagePixelFormat pixelFormat, Size imageSize)
            => new ImageData(new Span<byte>(pixels), pixelFormat, imageSize);

        /// <summary>
        /// Creates a new instance of the <see cref="ImageData"/> class using a byte array as the data source.
        /// </summary>
        /// <param name="pixels">The byte array containing bitmap data.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <returns>A new <see cref="ImageData"/> instance.</returns>
        public static ImageData FromArray(byte[] pixels, ImagePixelFormat pixelFormat, int width, int height)
            => FromArray(pixels, pixelFormat, new Size(width, height));

        /// <summary>
        /// Creates a new instance of the <see cref="ImageData"/> class using a pointer to the unmanaged memory as the data source.
        /// </summary>
        /// <param name="pointer">The byte array containing bitmap data.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <param name="imageSize">The image dimensions.</param>
        /// <returns>A new <see cref="ImageData"/> instance.</returns>
        public static ImageData FromPointer(IntPtr pointer, ImagePixelFormat pixelFormat, Size imageSize)
        {
            var span = CreateSpan(pointer, imageSize, pixelFormat);
            return new ImageData(span, pixelFormat, imageSize);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ImageData"/> class using a pointer to the unmanaged memory as the data source.
        /// </summary>
        /// <param name="pointer">The byte array containing bitmap data.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <returns>A new <see cref="ImageData"/> instance.</returns>
        public static ImageData FromPointer(IntPtr pointer, ImagePixelFormat pixelFormat, int width, int height)
            => FromPointer(pointer, pixelFormat, new Size(width, height));

        /// <summary>
        /// Gets the estimated image line size based on the pixel format and width.
        /// </summary>
        /// <param name="width">The image width.</param>
        /// <param name="format">The image pixel format.</param>
        /// <returns>The size of a single line of the image measured in bytes.</returns>
        public static int EstimateStride(int width, ImagePixelFormat format) => 4 * (((GetBitsPerPixel(format) * width) + 31) / 32);

        private static unsafe Span<byte> CreateSpan(IntPtr pointer, Size imageSize, ImagePixelFormat pixelFormat)
        {
            var size = EstimateStride(imageSize.Width, pixelFormat) * imageSize.Height;
            return new Span<byte>((void*)pointer, size);
        }

        private static int GetBitsPerPixel(ImagePixelFormat format)
        {
            switch (format)
            {
                case ImagePixelFormat.Bgr24:
                    return 24;
                case ImagePixelFormat.Bgra32:
                    return 32;
                case ImagePixelFormat.Rgb24:
                    return 24;
                case ImagePixelFormat.Rgba32:
                    return 32;
                case ImagePixelFormat.Argb32:
                    return 32;
                case ImagePixelFormat.Uyvy422:
                    return 16;
                case ImagePixelFormat.Yuv420:
                    return 12;
                case ImagePixelFormat.Yuv422:
                    return 16;
                case ImagePixelFormat.Yuv444:
                    return 24;
                case ImagePixelFormat.Gray16:
                    return 16;
                case ImagePixelFormat.Gray8:
                    return 8;
                default:
                    return 0;
            }
        }
    }
}

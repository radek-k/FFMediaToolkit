namespace FFMediaToolkit.Graphics
{
    using System;
    using System.Buffers;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

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
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <exception cref="ArgumentException">When data span size doesn't match size calculated from width, height and the pixel format.</exception>
        public BitmapData(Span<byte> data, int width, int height, ImagePixelFormat pixelFormat)
        {
            var size = Layout.EstimateStride(width, pixelFormat) * height;
            if (data.Length != size)
            {
                throw new ArgumentException("Pixel buffer size doesn't match size required by this image format.");
            }

            span = data;
            pooledMemory = null;

            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
        }

        private BitmapData(IMemoryOwner<byte> memory, int width, int height, ImagePixelFormat pixelFormat)
        {
            span = null;
            pooledMemory = memory;

            Width = width;
            Height = height;
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
        /// Gets the image width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the image height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the bitmap pixel format.
        /// </summary>
        public ImagePixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the image layout data.
        /// </summary>
        internal Layout Layout => new Layout((AVPixelFormat)PixelFormat, Width, Height);

        /// <summary>
        /// Rents a memory buffer from pool and creates a new instance of <see cref="BitmapData"/> class from it.
        /// </summary>
        /// <param name="width">The bitmap width.</param>
        /// <param name="height">The bitmap heigth.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>The new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData CreatePooled(int width, int height, ImagePixelFormat pixelFormat)
        {
            var size = Layout.EstimateStride(width, pixelFormat) * height;
            var pool = MemoryPool<byte>.Shared;
            var memory = pool.Rent(size);
            return new BitmapData(memory, width, height, pixelFormat);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BitmapData"/> class using a byte array as the data source.
        /// </summary>
        /// <param name="pixels">The byte array containing bitmap data.</param>
        /// <param name="width">The bitmap width.</param>
        /// <param name="height">The bitmap height.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>A new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData FromArray(byte[] pixels, int width, int height, ImagePixelFormat pixelFormat)
            => new BitmapData(new Span<byte>(pixels), width, height, pixelFormat);

        /// <summary>
        /// Creates a new instance of the <see cref="BitmapData"/> class using a pointer to the unmanaged memory as the data source.
        /// </summary>
        /// <param name="pointer">The byte array containing bitmap data.</param>
        /// <param name="width">The bitmap width.</param>
        /// <param name="height">The bitmap height.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>A new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData FromPointer(IntPtr pointer, int width, int height, ImagePixelFormat pixelFormat)
            => new BitmapData(CreateSpan(pointer, width, height, pixelFormat), width, height, pixelFormat);

        private static unsafe Span<byte> CreateSpan(IntPtr pointer, int width, int heigth, ImagePixelFormat pixelFormat)
        {
            var size = Layout.EstimateStride(width, pixelFormat) * heigth;
            return new Span<byte>((void*)pointer, size);
        }
    }
}

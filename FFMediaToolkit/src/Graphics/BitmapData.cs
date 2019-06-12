namespace FFMediaToolkit.Graphics
{
    using System;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represent a wrapper of different bitmap formats.
    /// </summary>
    public ref struct BitmapData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapData"/> struct using a <see cref="Memory{T}"/> as the data source.
        /// </summary>
        /// <param name="data">The bitmap data.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <exception cref="ArgumentException">When data span size doesn't match size calculated from width, height and the pixel format.</exception>
        public BitmapData(Memory<byte> data, int width, int height, ImagePixelFormat pixelFormat)
        {
            var size = Scaler.EstimateStride(width, pixelFormat) * height;
            if (data.Length != size)
            {
                throw new ArgumentException("Pixel buffer size doesn't match size required by this image format.");
            }

            Data = data;
            Width = width;
            Height = height;
            PixelFormat = pixelFormat;
        }

        /// <summary>
        /// Gets the <see cref="Memory{T}"/> object containing the bitmap data.
        /// </summary>
        public Memory<byte> Data { get; }

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
        /// Creates a new instance of the <see cref="BitmapData"/> class using a byte array as the data source.
        /// </summary>
        /// <param name="pixels">The byte array containing bitmap data.</param>
        /// <param name="width">The bitmap width.</param>
        /// <param name="height">The bitmap height.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>A new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData FromArray(byte[] pixels, int width, int height, ImagePixelFormat pixelFormat)
            => new BitmapData(new Memory<byte>(pixels), width, height, pixelFormat);

        /// <summary>
        /// Creates a new instance of the <see cref="BitmapData"/> class using a pointer to the unmanaged memory as the data source.
        /// </summary>
        /// <param name="pointer">The byte array containing bitmap data.</param>
        /// <param name="width">The bitmap width.</param>
        /// <param name="height">The bitmap height.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>A new <see cref="BitmapData"/> instance.</returns>
        public static BitmapData FromPointer(IntPtr pointer, int width, int height, ImagePixelFormat pixelFormat)
            => new BitmapData(CreateMemory(pointer, width, height, pixelFormat), width, height, pixelFormat);

        private static unsafe Memory<byte> CreateMemory(IntPtr pointer, int width, int heigth, ImagePixelFormat pixelFormat)
        {
            var size = Scaler.EstimateStride(width, pixelFormat) * heigth;
            var manager = new UnmanagedMemoryManager<byte>((byte*)pointer.ToPointer(), size);
            return manager.Memory;
        }
    }
}

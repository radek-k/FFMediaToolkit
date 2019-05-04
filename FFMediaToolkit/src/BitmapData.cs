namespace FFMediaToolkit
{
    using System;
    using FFmpeg.AutoGen;
    using Helpers;

    /// <summary>
    /// Represent a wrapper of different bitmap formats
    /// </summary>
    public ref struct BitmapData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapData"/> struct using a <see cref="Span{T}"/> as the data source
        /// </summary>
        /// <param name="data">Bitmap data</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <param name="pixelFormat">The pixel format</param>
        /// <exception cref="ArgumentException">When data span size doesn't match size calculated from width, height an pixelFormat</exception>
        public BitmapData(Span<byte> data, int width, int height, BitmapPixelFormat pixelFormat)
        {
            var size = Scaler.GetStride(width, pixelFormat) * height;
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
        /// Initializes a new instance of the <see cref="BitmapData"/> struct using a byte array as the data source
        /// </summary>
        /// <param name="pixels">Byte array containing pixels data</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <param name="pixelFormat">The pixel format</param>
        public BitmapData(byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat)
            : this(new Span<byte>(pixels), width, height, pixelFormat)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapData"/> struct using a pointer to the unmanaged memory as the data source
        /// </summary>
        /// <param name="pointer">Byte array containing pixels data</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <param name="pixelFormat">The pixel format</param>
        public unsafe BitmapData(IntPtr pointer, int width, int height, BitmapPixelFormat pixelFormat)
            : this(new Span<byte>(pointer.ToPointer(), Scaler.GetStride(width, pixelFormat) * height), width, height, pixelFormat)
        {
        }

        /// <summary>
        /// Gets a pointer to the image's pixels data
        /// </summary>
        public Span<byte> Data { get; }

        /// <summary>
        /// Gets the image width
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the image height
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the bitmap pixel format
        /// </summary>
        public BitmapPixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the image layout data
        /// </summary>
        internal Layout Layout => new Layout((AVPixelFormat)PixelFormat, Width, Height);
    }
}

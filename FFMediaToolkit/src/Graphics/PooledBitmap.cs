namespace FFMediaToolkit.Graphics
{
    using System;
    using System.Buffers;
    using FFMediaToolkit.Helpers;

    /// <summary>
    /// A <see cref="BitmapData"/> implementation that uses memory pooling for bitmap buffers.
    /// </summary>
    internal class PooledBitmap : BitmapData, IDisposable
    {
        private bool isDisposed;

        private PooledBitmap(IMemoryOwner<byte> data, int width, int height, ImagePixelFormat pixelFormat)
            : base(data.Memory, width, height, pixelFormat)
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PooledBitmap"/> class.
        /// </summary>
        ~PooledBitmap() => Dispose();

        /// <inheritdoc/>
        public override Memory<byte> Data => isDisposed ? null : PooledMemory.Memory;

        /// <summary>
        /// Gets the pooled memory.
        /// </summary>
        public IMemoryOwner<byte> PooledMemory { get; }

        /// <summary>
        /// Rents a memory buffer from pool and creates a new instance of <see cref="PooledBitmap"/> class from it.
        /// </summary>
        /// <param name="width">The bitmap width.</param>
        /// <param name="height">The bitmap heigth.</param>
        /// <param name="pixelFormat">The bitmap pixel format.</param>
        /// <returns>The new <see cref="PooledBitmap"/> instance.</returns>
        public static PooledBitmap Create(int width, int height, ImagePixelFormat pixelFormat)
        {
            var size = Scaler.EstimateStride(width, pixelFormat) * height;
            var pool = MemoryPool<byte>.Shared;
            var memory = pool.Rent(size);
            return new PooledBitmap(memory, width, height, pixelFormat);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
                return;

            PooledMemory.Dispose();

            isDisposed = true;
        }
    }
}

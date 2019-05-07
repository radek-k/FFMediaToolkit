namespace FFMediaToolkit.Helpers
{
    using System;
    using System.Buffers;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A <see cref="MemoryManager{T}"/> for creating <see cref="Memory{T}"/> objects from pointers to the unmanaged memory
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    public sealed unsafe class UnmanagedMemoryManager<T> : MemoryManager<T>
        where T : unmanaged
    {
        private readonly T* pointer;
        private readonly int length;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedMemoryManager{T}"/> class using the given <see cref="Span{T}"/>
        /// </summary>
        /// <param name="span">A <see cref="Span{T}"/> object</param>
        public UnmanagedMemoryManager(Span<T> span)
        {
            fixed (T* ptr = &MemoryMarshal.GetReference(span))
            {
                pointer = ptr;
                length = span.Length;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedMemoryManager{T}"/> class using the specified pointer and length.
        /// </summary>
        /// <param name="pointer">A pointer to the unmanaged memory</param>
        /// <param name="length">THe length of the memory</param>
        public UnmanagedMemoryManager(T* pointer, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            this.pointer = pointer;
            this.length = length;
        }

        /// <inheritdoc/>
        public override Span<T> GetSpan() => new Span<T>(pointer, length);

        /// <inheritdoc/>
        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if (elementIndex < 0 || elementIndex >= length)
                throw new ArgumentOutOfRangeException(nameof(elementIndex));

            return new MemoryHandle(pointer + elementIndex);
        }

        /// <inheritdoc/>
        public override void Unpin()
        {
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
        }
    }
}

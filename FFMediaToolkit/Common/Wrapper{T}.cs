namespace FFMediaToolkit.Common
{
    using System;

    /// <summary>
    /// A base class for wrappers of unmanaged objects with <see cref="IDisposable"/> implementation.
    /// </summary>
    /// <typeparam name="T">The type of the unmanaged object.</typeparam>
    internal abstract unsafe class Wrapper<T> : IDisposable
        where T : unmanaged
    {
        private IntPtr pointer;
        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Wrapper{T}"/> class.
        /// </summary>
        /// <param name="pointer">A pointer to a unmanaged object.</param>
        protected Wrapper(T* pointer) => this.pointer = new IntPtr(pointer);

        /// <summary>
        /// Finalizes an instance of the <see cref="Wrapper{T}"/> class.
        /// </summary>
        ~Wrapper() => Disposing(false);

        /// <summary>
        /// Gets a pointer to the underlying object.
        /// </summary>
        public T* Pointer => isDisposed ? null : (T*)pointer;

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        /// <summary>
        /// Updates the pointer to the object.
        /// </summary>
        /// <param name="newPointer">The new pointer.</param>
        protected void UpdatePointer(T* newPointer) => pointer = new IntPtr(newPointer);

        /// <summary>
        /// Free the unmanaged resources.
        /// </summary>
        protected abstract void OnDisposing();

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            OnDisposing();

            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}
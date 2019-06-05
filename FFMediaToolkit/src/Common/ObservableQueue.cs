namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Represents a thread-safe queue that provides notification when item added or removed.
    /// </summary>
    /// <typeparam name="T">Type of the queue items.</typeparam>
    public class ObservableQueue<T>
    {
        private readonly ConcurrentQueue<T> queue;

        private readonly object readLock = new object();
        private readonly object writeLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableQueue{T}"/> class.
        /// </summary>
        public ObservableQueue() => queue = new ConcurrentQueue<T>();

        /// <summary>
        /// Ocurrs when item is enqueued(added).
        /// </summary>
        public event EventHandler Enqueued;

        /// <summary>
        /// Ocurrs when item is dequeued(removed).
        /// </summary>
        public event EventHandler Dequeued;

        /// <summary>
        /// Gets a value indicating whether the queue is empty.
        /// </summary>
        public bool IsEmpty => queue.IsEmpty;

        /// <summary>
        /// Gets the number of elements in queue.
        /// </summary>
        public int Count => queue.Count;

        /// <summary>
        /// Adds an object to the end of the <see cref="ObservableQueue{T}"/>.
        /// </summary>
        /// <param name="item">The object to add.</param>
        public void Enqueue(T item)
        {
            lock (writeLock)
            {
                queue.Enqueue(item);
                Enqueued?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Tries to remove and return the object at the beginning of the concurrent queue.
        /// </summary>
        /// <param name="item">The object removed from the queue, otherwise <see langword="default"/>.</param>
        /// <returns>If succes <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public bool TryDequeue(out T item)
        {
            lock (readLock)
            {
                if (queue.TryDequeue(out var obj))
                {
                    item = obj;
                    Dequeued?.Invoke(this, EventArgs.Empty);
                    return true;
                }
                else
                {
                    item = default;
                    return false;
                }
            }
        }

        /// <summary>
        /// Tries to return an object from the beginning of the queue without removing it.
        /// </summary>
        /// <param name="item">The object.</param>
        /// <returns>If succes <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public bool TryPeek(out T item)
        {
            lock (readLock)
            {
                var result = queue.TryPeek(out var obj);
                item = result ? obj : default;
                return result;
            }
        }

        /// <summary>
        /// Removes all items from the queue.
        /// </summary>
        public void Clear()
        {
            lock (writeLock)
            {
                while (Count > 0)
                {
                    queue.TryDequeue(out var _);
                }
            }
        }
    }
}

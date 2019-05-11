namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a wrapper of <see cref="AVDictionary"/>. Used for applying codec and container settings.
    /// </summary>
    public unsafe class FFDictionary : IDisposable
    {
        private bool isDisposed;
        private AVDictionary* dict;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFDictionary"/> class.
        /// </summary>
        public FFDictionary()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FFDictionary"/> class from given <see cref="AVDictionary"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="AVDictionary"/></param>
        internal FFDictionary(AVDictionary* dictionary) => dict = dictionary;

        /// <summary>
        /// Finalizes an instance of the <see cref="FFDictionary"/> class.
        /// </summary>
        ~FFDictionary() => Disposing(false);

        /// <summary>
        /// Gets a pointer to the underlying <see cref="AVDictionary"/>
        /// </summary>
        public AVDictionary* Pointer => isDisposed ? null : dict;

        /// <summary>
        /// Creates a new instance of the <see cref="FFDictionary"/> class using given string dictionary.
        /// </summary>
        /// <param name="dictionary">The string dictionary</param>
        /// <returns>A new instance of <see cref="FFDictionary"/> filled with data from specified string dictionary</returns>
        public static FFDictionary FromDictionary(Dictionary<string, string> dictionary)
        {
            // TODO: FFDictionary from string
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            if (Pointer != null)
            {
                fixed (AVDictionary** ptr = &dict)
                {
                    ffmpeg.av_dict_free(ptr);
                }
            }

            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}

namespace FFMediaToolkit.Decoding
{
    using System;
    using System.IO;
    using FFMediaToolkit.Decoding.Internal;

    /// <summary>
    /// Represents a multimedia file.
    /// </summary>
    public class MediaFile : IDisposable
    {
        private readonly InputContainer container;
        private bool isDisposed;

        private unsafe MediaFile(InputContainer container, MediaOptions options)
        {
            this.container = container;

            if (container.Video != null)
            {
                Video = new VideoStream(container.Video, options);
            }

            Info = new MediaInfo(container.Pointer);
        }

        /// <summary>
        /// Gets the video stream.
        /// </summary>
        public VideoStream Video { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the file contains video stream and the stream is loaded.
        /// </summary>
        public bool HasVideo => Video != null;

        /// <summary>
        /// Gets informations about the media container.
        /// </summary>
        public MediaInfo Info { get; }

        /// <summary>
        /// Opens a media file from the specified path with default settings.
        /// </summary>
        /// <param name="path">A path to the media file.</param>
        /// <returns>The opened <see cref="MediaFile"/>.</returns>
        public static MediaFile Open(string path) => Open(path, new MediaOptions());

        /// <summary>
        /// Opens a media file from the specified path.
        /// </summary>
        /// <param name="path">A path to the media file.</param>
        /// <param name="options">The decoder settings.</param>
        /// <returns>The opened <see cref="MediaFile"/>.</returns>
        public static MediaFile Open(string path, MediaOptions options)
        {
            try
            {
                var container = InputContainer.LoadFile(path, options);
                return new MediaFile(container, options);
            }
            catch (DirectoryNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to open the media file", ex);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            if (HasVideo)
            {
                ((IDisposable)Video).Dispose();
                Video = null;
            }

            container.Dispose();

            isDisposed = true;
        }
    }
}

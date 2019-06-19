namespace FFMediaToolkit.Encoding.Internal
{
    using System;
    using System.IO;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the multimedia file container used for encoding.
    /// </summary>
    internal unsafe class OutputContainer : Wrapper<AVFormatContext>
    {
        private OutputContainer(AVFormatContext* formatContext)
            : base(formatContext)
        {
        }

        /// <summary>
        /// Gets the video stream.
        /// </summary>
        public OutputStream<VideoFrame> Video { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the file is created.
        /// </summary>
        public bool IsFileCreated { get; private set; }

        /// <summary>
        /// Creates an empty FFmpeg format container for encoding.
        /// </summary>
        /// <param name="path">A output file path. It is used only to guess the container format.</param>
        /// <returns>A new instance of the <see cref="OutputContainer"/>.</returns>
        /// <remarks>Before you write frames to the container, you must call the <see cref="CreateFile(string)"/> method to create an ouput file.</remarks>
        public static OutputContainer Create(string path)
        {
            MediaToolkit.LoadFFmpeg();

            if (!Path.HasExtension(path))
                throw new ArgumentException("The file path has no extension.");

            var format = ffmpeg.av_guess_format(null, path, null);

            if (format == null)
                throw new NotSupportedException($"Cannot find a container format for the \"{Path.GetExtension(path)}\" file extension.");

            var formatContext = ffmpeg.avformat_alloc_context();
            formatContext->oformat = format;
            return new OutputContainer(formatContext);
        }

        /// <summary>
        /// Adds a new video stream to the container. Usable only in encoding, before locking file.
        /// </summary>
        /// <param name="config">The stream configuration.</param>
        public void AddVideoStream(VideoEncoderSettings config)
        {
            if (IsFileCreated)
            {
                throw new InvalidOperationException("The stream must be added before creating a file.");
            }

            if (Video != null)
            {
                throw new InvalidOperationException("The video stream was already created.");
            }

            Video = OutputStreamFactory.CreateVideo(this, config);
        }

        /// <summary>
        /// Creates a media file for this container and writes format header into it. Usable only in encoding.
        /// </summary>
        /// <param name="path">A path to create the file.</param>
        public void CreateFile(string path)
        {
            if (IsFileCreated)
            {
                return;
            }

            if (Video == null)
            {
                throw new InvalidOperationException("Cannot create empty media file. You have to add video stream before locking the file");
            }

            ffmpeg.avio_open(&Pointer->pb, path, ffmpeg.AVIO_FLAG_WRITE).ThrowIfError("Cannot create the output file.");
            ffmpeg.avformat_write_header(Pointer, null);

            IsFileCreated = true;
        }

        /// <summary>
        /// Writes the specified packet to the container by the <see cref="ffmpeg.av_interleaved_write_frame(AVFormatContext*, AVPacket*)"/> method.
        /// </summary>
        /// <param name="packet">The media packet to write.</param>
        public void WritePacket(MediaPacket packet)
        {
            if (!IsFileCreated)
            {
                throw new InvalidOperationException("The file must be opened before writing a packet. Use the OutputContainer.CreateFile() method.");
            }

            ffmpeg.av_interleaved_write_frame(Pointer, packet);
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            Video?.Dispose();

            if (IsFileCreated)
            {
                ffmpeg.av_write_trailer(Pointer);
                ffmpeg.avio_close(Pointer->pb);
            }

            ffmpeg.avformat_free_context(Pointer);
        }
    }
}

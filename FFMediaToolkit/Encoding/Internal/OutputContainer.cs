namespace FFMediaToolkit.Encoding.Internal
{
    using System;
    using System.Collections.Generic;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the multimedia file container used for encoding.
    /// </summary>
    internal unsafe class OutputContainer : Wrapper<AVFormatContext>
    {
        private bool isHeaderWritten;

        private OutputContainer(AVFormatContext* formatContext)
            : base(formatContext)
        {
            Video = new List<(Encoder<VideoFrame>, VideoEncoderSettings)>();
            Audio = new List<(Encoder<AudioFrame>, AudioEncoderSettings)>();
        }

        /// <summary>
        /// Gets the video streams.
        /// </summary>
        public List<(Encoder<VideoFrame> stream, VideoEncoderSettings config)> Video { get; }

        /// <summary>
        /// Gets the audio streams.
        /// </summary>
        public List<(Encoder<AudioFrame> stream, AudioEncoderSettings config)> Audio { get; }

        /// <summary>
        /// Gets a value indicating whether the file is created.
        /// </summary>
        public bool IsFileCreated { get; private set; }

        /// <summary>
        /// Gets a dictionary containing format options.
        /// </summary>
        internal FFDictionary ContainerOptions { get; private set; } = new FFDictionary();

        /// <summary>
        /// Creates an empty FFmpeg format container for encoding.
        /// </summary>
        /// <param name="extension">A output file extension. It is used only to guess the container format.</param>
        /// <returns>A new instance of the <see cref="OutputContainer"/>.</returns>
        /// <remarks>Before you write frames to the container, you must call the <see cref="CreateFile(string)"/> method to create an output file.</remarks>
        public static OutputContainer Create(string extension)
        {
            FFmpegLoader.LoadFFmpeg();

            var format = ffmpeg.av_guess_format(null, "x." + extension, null);

            if (format == null)
                throw new NotSupportedException($"Cannot find a container format for the \"{extension}\" file extension.");

            AVFormatContext* formatContext = null;
            ffmpeg.avformat_alloc_output_context2(&formatContext, format, null, null).ThrowIfError("Cannot allocate output format context");

            return new OutputContainer(formatContext);
        }

        /// <summary>
        /// Applies a set of metadata fields to the output file.
        /// </summary>
        /// <param name="metadata">The metadata object to set.</param>
        public void SetMetadata(ContainerMetadata metadata)
        {
            foreach (var item in metadata.Metadata)
            {
                ffmpeg.av_dict_set(&Pointer->metadata, item.Key, item.Value, 0);
            }
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

            Video.Add((EncoderFactory.CreateVideo(this, config), config));
        }

        /// <summary>
        /// Adds a new audio stream to the container. Usable only in encoding, before locking file.
        /// </summary>
        /// <param name="config">The stream configuration.</param>
        public void AddAudioStream(AudioEncoderSettings config)
        {
            if (IsFileCreated)
            {
                throw new InvalidOperationException("The stream must be added before creating a file.");
            }

            Audio.Add((EncoderFactory.CreateAudio(this, config), config));
        }

        /// <summary>
        /// Creates a media file for this container and writes format header into it.
        /// </summary>
        /// <param name="path">A path to create the file.</param>
        public void CreateFile(string path)
        {
            if (IsFileCreated)
            {
                return;
            }

            if (Video.Count == 0 && Audio.Count == 0)
            {
                throw new InvalidOperationException("Cannot create empty media file. You have to add stream before locking the file");
            }

            var ptr = ContainerOptions.Pointer;

            ffmpeg.avio_open(&Pointer->pb, path, ffmpeg.AVIO_FLAG_WRITE).ThrowIfError("Cannot create the output file.");
            IsFileCreated = true;
            ffmpeg.avformat_write_header(Pointer, &ptr).ThrowIfError("Cannot write format header");
            isHeaderWritten = true;
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

            ffmpeg.av_interleaved_write_frame(Pointer, packet).ThrowIfError("Cannot write packet");
        }

        /// <summary>
        /// Writes file trailer
        /// </summary>
        public void WriteTrailer()
        {
            if (isHeaderWritten)
            {
                ffmpeg.av_write_trailer(Pointer).ThrowIfError("Cannot write file trailer");
            }
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            if (IsFileCreated)
            {
                ffmpeg.avio_closep(&Pointer->pb);
            }

            foreach (var output in Video)
            {
                output.stream.Dispose();
            }

            foreach (var output in Audio)
            {
                output.stream.Dispose();
            }

            ffmpeg.avformat_free_context(Pointer);
        }
    }
}

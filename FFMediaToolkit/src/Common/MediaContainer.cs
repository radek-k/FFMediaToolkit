namespace FFMediaToolkit.Common
{
    using System;
    using System.IO;
    using FFMediaToolkit.Decoding;
    using FFMediaToolkit.Encoding;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the multimedia stream types.
    /// </summary>
    public enum MediaType
    {
        /// <summary>
        /// Video.
        /// </summary>
        Video,

        /// <summary>
        /// Audio.
        /// </summary>
        Audio,

        /// <summary>
        /// None.
        /// </summary>
        None,
    }

    /// <summary>
    /// Represent a multimedia file context.
    /// </summary>
    public unsafe sealed class MediaContainer : MediaObject, IDisposable
    {
        private bool isDisposed;

        private MediaContainer(AVFormatContext* format, VideoStream stream, MediaAccess acces)
        {
            MediaCore.LoadFFmpeg();

            FormatContextPointer = format;
            Video = stream;
            Access = acces;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="MediaContainer"/> class.
        /// </summary>
        ~MediaContainer() => Disposing(false);

        /// <summary>
        /// Gets the video stream in the container. To set the stream in encoding mode, please use the <see cref="AddVideoStream(VideoEncoderSettings)"/> method.
        /// </summary>
        public VideoStream Video { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the reader is at end of the file.
        /// </summary>
        public bool IsAtEndOfFile { get; private set; }

        /// <summary>
        /// Gets a pointer to the underlying <see cref="AVFormatContext"/>.
        /// </summary>
        internal AVFormatContext* FormatContextPointer { get; private set; }

        /// <summary>
        /// Creates an empty FFmpeg format container for encoding.
        /// After you add media streams configurations, you have to call the <see cref="LockFile(string)"/> before pushing frames.
        /// </summary>
        /// <param name="path">A output file path.</param>
        /// <returns>A new instance of the <see cref="MediaContainer"/>.</returns>
        /// <remarks>Before you write frames to a new container, you must call the <see cref="LockFile(string)"/> method to create an ouput file.</remarks>
        public static MediaContainer CreateOutput(string path)
        {
            if (Path.HasExtension(path))
                throw new ArgumentException("The file path has no extension.");

            var format = ffmpeg.av_guess_format(null, path, null);

            if (format == null)
                throw new NotSupportedException($"Cannot find a container format for the \"{Path.GetExtension(path)}\" file extension.");

            var formatContext = ffmpeg.avformat_alloc_context();
            formatContext->oformat = format;
            return new MediaContainer(formatContext, null, MediaAccess.WriteInit);
        }

        /// <summary>
        /// Opens a media container and stream codecs from given path.
        /// </summary>
        /// <param name="path">A path to the multimedia file.</param>
        /// <param name="options">The media settings.</param>
        /// <returns>A new instance of the <see cref="MediaContainer"/> class.</returns>
        public static MediaContainer LoadFile(string path, MediaOptions options)
        {
            var context = ffmpeg.avformat_alloc_context();
            options.DemuxerOptions.ApplyFlags(context);
            var dict = options.DemuxerOptions.PrivateOptions.Pointer;

            ffmpeg.avformat_open_input(&context, path, null, &dict);

            options.DemuxerOptions.PrivateOptions.Update(dict);

            var container = new MediaContainer(context, null, MediaAccess.Read);
            container.OpenStreams(options);
            return container;
        }

        /// <summary>
        /// Adds a new video stream to the container. Usable only in encoding, before locking file.
        /// </summary>
        /// <param name="config">The stream configuration.</param>
        public void AddVideoStream(VideoEncoderSettings config)
        {
            CheckAccess(MediaAccess.WriteInit);
            if (Video != null)
            {
                throw new InvalidOperationException("The video stream was already created");
            }

            Video = VideoStream.CreateNew(this, config);
        }

        /// <summary>
        /// Creates a media file for this container and writes format header into it. Usable only in encoding.
        /// </summary>
        /// <param name="path">A path to create the file.</param>
        public void LockFile(string path)
        {
            CheckAccess(MediaAccess.WriteInit);
            if (Video == null /*&& AudioStream == null*/)
            {
                throw new InvalidOperationException("Cannot create empty media file. You have to add video or audio stream before locking the file");
            }

            ffmpeg.avio_open(&FormatContextPointer->pb, path, ffmpeg.AVIO_FLAG_WRITE).CatchAll("Cannot create the output file.");
            ffmpeg.avformat_write_header(FormatContextPointer, null);

            Access = MediaAccess.Write;
        }

        /// <inheritdoc/>
        public void Dispose() => Disposing(true);

        /// <summary>
        /// Writes specified packet to the container. Uses <see cref="ffmpeg.av_interleaved_write_frame(AVFormatContext*, AVPacket*)"/>.
        /// </summary>
        /// <param name="packet">Media packet to write.</param>
        public void WritePacket(MediaPacket packet)
        {
            CheckAccess(MediaAccess.Write);
            ffmpeg.av_interleaved_write_frame(FormatContextPointer, packet);
        }

        /// <summary>
        /// Reads the next packet from this file and sends it to the packet queue.
        /// </summary>
        /// <returns>Type of the readed packet.</returns>
        public MediaType ReadPacket()
        {
            CheckAccess(MediaAccess.Read);
            var packet = MediaPacket.AllocateEmpty(0);
            var result = ffmpeg.av_read_frame(FormatContextPointer, packet.Pointer);

            if (result == ffmpeg.AVERROR_EOF)
            {
                IsAtEndOfFile = true;
                return MediaType.None;
            }
            else
            {
                result.CatchAll("Cannot read next packet from the file");
            }

            IsAtEndOfFile = false;
            return EnqueuePacket(packet);
        }

        private static int? FindBestStream(AVFormatContext* container, AVMediaType type, int relStream = -1)
        {
            AVCodec* codec = null;
            var id = ffmpeg.av_find_best_stream(container, type, -1, relStream, &codec, 0);
            return id >= 0 ? (int?)id : null;
        }

        private MediaType EnqueuePacket(MediaPacket packet)
        {
            if (Video != null && packet.StreamIndex == Video.Index)
            {
                Video.PacketQueue.Enqueue(packet);
                return MediaType.Video;
            }

            // if (Audio != null && packet.StreamIndex == Audio.Index)
            // {
            //     Audio.PacketQueue.Enqueue(packet);
            //     return MediaType.Audio;
            // }
            return MediaType.None;
        }

        private void OpenStreams(MediaOptions options)
        {
            if (options.StreamsToLoad != MediaMode.Audio)
            {
                var index = FindBestStream(FormatContextPointer, AVMediaType.AVMEDIA_TYPE_VIDEO);
                if (index != null)
                {
                    Video = VideoStream.Open(this, index.Value, options);
                }
            }

            // if (options.StreamsToLoad != MediaMode.Video)
            // {
            //    var index = FindBestStream(context, AVMediaType.AVMEDIA_TYPE_AUDIO);
            //    if (index != null)
            //    {
            //        Audio = AudioStream.Open(this, index.Value, options);
            //    }
            // }
        }

        private void Disposing(bool dispose)
        {
            if (isDisposed)
                return;

            Video.Dispose();

            if (Access == MediaAccess.Write)
            {
                ffmpeg.av_write_trailer(FormatContextPointer);
                ffmpeg.avio_close(FormatContextPointer->pb);
            }

            if (Access == MediaAccess.Read)
            {
                var ptr = FormatContextPointer;
                ffmpeg.avformat_close_input(&ptr);
                FormatContextPointer = null;
            }

            if (FormatContextPointer != null)
            {
                ffmpeg.avformat_free_context(FormatContextPointer);
                FormatContextPointer = null;
            }

            isDisposed = true;

            if (dispose)
                GC.SuppressFinalize(this);
        }
    }
}

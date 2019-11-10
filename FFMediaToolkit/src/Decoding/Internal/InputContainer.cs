namespace FFMediaToolkit.Decoding.Internal
{
    using System;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the multimedia file container.
    /// </summary>
    internal unsafe class InputContainer : Wrapper<AVFormatContext>
    {
        private InputContainer(AVFormatContext* formatContext)
            : base(formatContext)
        {
        }

        /// <summary>
        /// Gets the video stream.
        /// </summary>
        public InputStream<VideoFrame> Video { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the reader is at end of the file.
        /// </summary>
        public bool IsAtEndOfFile { get; private set; }

        /// <summary>
        /// Opens a media container and stream codecs from given path.
        /// </summary>
        /// <param name="path">A path to the multimedia file.</param>
        /// <param name="options">The media settings.</param>
        /// <returns>A new instance of the <see cref="InputContainer"/> class.</returns>
        public static InputContainer LoadFile(string path, MediaOptions options)
        {
            MediaToolkit.LoadFFmpeg();

            var context = ffmpeg.avformat_alloc_context();
            options.DemuxerOptions.ApplyFlags(context);
            var dict = new FFDictionary(options.DemuxerOptions.PrivateOptions);
            var ptr = dict.Pointer;

            ffmpeg.avformat_open_input(&context, path, null, &ptr)
                .ThrowIfError("An error ocurred while opening the file");

            ffmpeg.avformat_find_stream_info(context, null)
                .ThrowIfError("Cannot find stream info");

            dict.Update(ptr);

            var container = new InputContainer(context);
            container.OpenStreams(options);
            return container;
        }

        /// <summary>
        /// Reads the next packet from this file and sends it to the packet queue.
        /// </summary>
        /// <returns>Type of the readed packet.</returns>
        public MediaType ReadPacket()
        {
            var packet = MediaPacket.AllocateEmpty(0);
            var result = ffmpeg.av_read_frame(Pointer, packet.Pointer); // Gets the next packet from the file.

            // Check if the end of file error ocurred
            if (result == ffmpeg.AVERROR_EOF)
            {
                IsAtEndOfFile = true;
                return MediaType.None;
            }
            else
            {
                result.ThrowIfError("Cannot read next packet from the file");
            }

            IsAtEndOfFile = false;
            return EnqueuePacket(packet); // Sends packet to the internal decoder queue.
        }

        /// <summary>
        /// Seeks stream to the specified video frame.
        /// </summary>
        /// <param name="frameNumber">The target video frame number.</param>
        public void SeekFile(int frameNumber) => SeekFile(frameNumber.ToTimestamp(Video.Info.RFrameRate, Video.Info.TimeBase));

        /// <summary>
        /// Seeks stream to the specified target timestamp.
        /// </summary>
        /// <param name="targetTs">The target timestamp in the default stream time base.</param>
        public void SeekFile(long targetTs)
        {
            ffmpeg.av_seek_frame(Pointer, Video.Info.Index, targetTs, ffmpeg.AVSEEK_FLAG_BACKWARD).ThrowIfError($"Seek to {targetTs} failed.");
            IsAtEndOfFile = false;

            Video.ClearQueue();
            Video.FlushBuffers();
            AddPacket(MediaType.Video);
            Video.AdjustPackets(targetTs);
        }

        /// <summary>
        /// Seeks stream by skipping next packets in the file. Useful to seek few frames forward.
        /// </summary>
        /// <param name="frameNumber">The target video frame number.</param>
        public void SeekForward(int frameNumber) => SeekForward(frameNumber.ToTimestamp(Video.Info.RFrameRate, Video.Info.TimeBase));

        /// <summary>
        /// Seeks stream by skipping next packets in the file. Useful to seek few frames forward.
        /// </summary>
        /// <param name="targetTs">The target timestamp in the default stream time base.</param>
        public void SeekForward(long targetTs) => Video.AdjustPackets(targetTs);

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            Video?.Dispose();

            var ptr = Pointer;
            ffmpeg.avformat_close_input(&ptr);
        }

        /// <summary>
        /// Finds the best stream of the specified type in the file.
        /// </summary>
        /// <param name="container">The media container.</param>
        /// <param name="type">Type of the stream to find.</param>
        /// <param name="relStream">Optional. Index of the related stream.</param>
        /// <returns>Index of the found stream, otherwise <see langword="null"/>.</returns>
        private static int? FindBestStream(AVFormatContext* container, AVMediaType type, int relStream = -1)
        {
            AVCodec* codec = null;
            var id = ffmpeg.av_find_best_stream(container, type, -1, relStream, &codec, 0);
            return id >= 0 ? (int?)id : null;
        }

        /// <summary>
        /// Sends a packet to the appropriate queue.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <returns>The detected type of the packet.</returns>
        private MediaType EnqueuePacket(MediaPacket packet)
        {
            if (Video != null && packet.StreamIndex == Video.Info.Index)
            {
                Video.FetchPacket(packet);
                return MediaType.Video;
            }

            return MediaType.None;
        }

        /// <summary>
        /// Opens the streams in the file using the specified <see cref="MediaOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MediaOptions"/> object.</param>
        private void OpenStreams(MediaOptions options)
        {
            if (options.StreamsToLoad != MediaMode.Audio)
            {
                var index = FindBestStream(Pointer, AVMediaType.AVMEDIA_TYPE_VIDEO);
                if (index != null)
                {
                    Video = InputStreamFactory.OpenVideo(this, index.Value, options);
                    Video.PacketsNeeded += () => AddPacket(MediaType.Video); // Adds the event handler.
                    AddPacket(MediaType.Video); // Requests for the first packet.
                }
            }
        }

        /// <summary>
        /// Handles the request to send one (or more) packet of the specified type to the decoder queue.
        /// </summary>
        /// <param name="type">The type of media packet.</param>
        private void AddPacket(MediaType type)
        {
            if (type == MediaType.None)
            {
                return;
            }

            var packetAdded = false;
            while (!packetAdded)
            {
                if (IsAtEndOfFile)
                {
                    return;
                }

                packetAdded = ReadPacket() == type;
            }
        }
    }
}

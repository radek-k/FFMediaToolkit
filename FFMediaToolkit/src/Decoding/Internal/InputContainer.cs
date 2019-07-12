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
            var result = ffmpeg.av_read_frame(Pointer, packet.Pointer);

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
            return EnqueuePacket(packet);
        }

        /// <summary>
        /// Seeks stream to the specified video frame.
        /// </summary>
        /// <param name="frameNumber">The target video frame number.</param>
        public void SeekFile(int frameNumber) => SeekFile(frameNumber.ToTimeSpan(Video.Info.FrameRate).ToTimestamp(Video.Info.TimeBase));

        /// <summary>
        /// Seeks stream to the specified target time.
        /// </summary>
        /// <param name="targetTime">The absolute target time.</param>
        public void SeekFile(TimeSpan targetTime) => SeekFile(targetTime.ToTimestamp(Video.Info.TimeBase));

        /// <summary>
        /// Seeks stream to the specified target timestamp.
        /// </summary>
        /// <param name="targetTs">The target timestamp in the default stream time base.</param>
        public void SeekFile(long targetTs)
        {
            ffmpeg.av_seek_frame(Pointer, Video.Info.Index, targetTs, ffmpeg.AVSEEK_FLAG_BACKWARD).ThrowIfError($"Seek to {targetTs} failed.");
            IsAtEndOfFile = false;

            Video.PacketQueue.Clear();
            Video.FlushBuffers();
            AddPacket(MediaType.Video);
            AdjustSeekPackets(Video, targetTs, VideoFrame.CreateEmpty());
        }

        /// <summary>
        /// Seeks stream by skipping next packets in the file. Useful to seek few frames forward.
        /// </summary>
        /// <param name="frameNumber">The target video frame number.</param>
        public void SeekForward(int frameNumber) => SeekForward(frameNumber.ToTimeSpan(Video.Info.FrameRate).ToTimestamp(Video.Info.TimeBase));

        /// <summary>
        /// Seeks stream by skipping next packets in the file. Useful to seek few frames forward.
        /// </summary>
        /// <param name="targetTime">The absolute target time.</param>
        public void SeekForward(TimeSpan targetTime) => SeekForward(targetTime.ToTimestamp(Video.Info.TimeBase));

        /// <summary>
        /// Seeks stream by skipping next packets in the file. Useful to seek few frames forward.
        /// </summary>
        /// <param name="targetTs">The target timestamp in the default stream time base.</param>
        public void SeekForward(long targetTs) => AdjustSeekPackets(Video, targetTs, VideoFrame.CreateEmpty());

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            Video?.Dispose();

            var ptr = Pointer;
            ffmpeg.avformat_close_input(&ptr);
        }

        private static int? FindBestStream(AVFormatContext* container, AVMediaType type, int relStream = -1)
        {
            AVCodec* codec = null;
            var id = ffmpeg.av_find_best_stream(container, type, -1, relStream, &codec, 0);
            return id >= 0 ? (int?)id : null;
        }

        private void AdjustSeekPackets<T>(InputStream<T> stream, long targetTs, T emptyFrame)
            where T : MediaFrame
        {
            stream.PacketQueue.TryPeek(out var packet);

            while (packet?.Timestamp < targetTs)
            {
                stream.Read(emptyFrame);
                stream.PacketQueue.TryPeek(out packet);
            }
        }

        private MediaType EnqueuePacket(MediaPacket packet)
        {
            if (Video != null && packet.StreamIndex == Video.Info.Index)
            {
                Video.PacketQueue.Enqueue(packet);
                return MediaType.Video;
            }

            return MediaType.None;
        }

        private void OpenStreams(MediaOptions options)
        {
            if (options.StreamsToLoad != MediaMode.Audio)
            {
                var index = FindBestStream(Pointer, AVMediaType.AVMEDIA_TYPE_VIDEO);
                if (index != null)
                {
                    Video = InputStreamFactory.OpenVideo(this, index.Value, options);
                    Video.PacketQueue.Dequeued += OnPacketDequeued;
                    AddPacket(MediaType.Video);
                }
            }
        }

        private void OnPacketDequeued(object sender, MediaPacket packet)
        {
            var stream = packet.StreamIndex == Video?.Info.Index ? Video : null;
            if (stream?.PacketQueue.Count > 5)
            {
                return;
            }

            AddPacket(stream?.Type ?? MediaType.None);
        }

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

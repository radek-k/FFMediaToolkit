namespace FFMediaToolkit.Decoding.Internal
{
    using System.IO;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the multimedia file container.
    /// </summary>
    internal unsafe class InputContainer : Wrapper<AVFormatContext>
    {
        private readonly MediaPacket packet;
        private bool canReusePacket = false;

        private InputContainer(AVFormatContext* formatContext)
            : base(formatContext) => packet = MediaPacket.AllocateEmpty(0);

        /// <summary>
        /// Gets the video stream.
        /// </summary>
        public Decoder<VideoFrame> Video { get; private set; }

        /// <summary>
        /// Opens a media container and stream codecs from given path.
        /// </summary>
        /// <param name="path">A path to the multimedia file.</param>
        /// <param name="options">The media settings.</param>
        /// <returns>A new instance of the <see cref="InputContainer"/> class.</returns>
        public static InputContainer LoadFile(string path, MediaOptions options)
        {
            FFmpegLoader.LoadFFmpeg();

            var context = ffmpeg.avformat_alloc_context();
            options.DemuxerOptions.ApplyFlags(context);
            var dict = new FFDictionary(options.DemuxerOptions.PrivateOptions, false).Pointer;

            ffmpeg.avformat_open_input(&context, path, null, &dict)
                .ThrowIfError("An error occurred while opening the file");

            ffmpeg.avformat_find_stream_info(context, null)
                .ThrowIfError("Cannot find stream info");

            var container = new InputContainer(context);
            container.OpenStreams(options);
            return container;
        }

        /// <summary>
        /// Reads the next packet from the specified stream.
        /// </summary>
        /// <param name="streamIndex">Index of the stream.</param>
        /// <returns>The read packet as <see cref="MediaPacket"/> object.</returns>
        public MediaPacket ReadNextPacket(int streamIndex)
        {
            if (canReusePacket)
            {
                canReusePacket = false;
                if (packet.StreamIndex != streamIndex)
                    packet.Wipe();
                else
                    return packet;
            }

            GetPacketFromStream(streamIndex);

            return packet;
        }

        /// <summary>
        /// Allows to return the last decoded packet with the next <see cref="ReadNextPacket(int)"/> call.
        /// </summary>
        public void ReuseLastPacket() => canReusePacket = true;

        /// <summary>
        /// Seeks all streams in the container to the first key frame before the specified time stamp.
        /// </summary>
        /// <param name="targetTs">The target time stamp in a stream time base.</param>
        /// <param name="streamIndex">The stream index. It will be used only to get the correct time base value.</param>
        public void SeekFile(long targetTs, int streamIndex)
        {
            ffmpeg.av_seek_frame(Pointer, streamIndex, targetTs, ffmpeg.AVSEEK_FLAG_BACKWARD).ThrowIfError($"Seek to {targetTs} failed.");

            Video.FlushBuffers();
            GetPacketFromStream(streamIndex);
            canReusePacket = true;
        }

        /// <inheritdoc/>
        protected override void OnDisposing()
        {
            Video?.Dispose();

            var ptr = Pointer;
            ffmpeg.avformat_close_input(&ptr);
        }

        /// <summary>
        /// Opens the streams in the file using the specified <see cref="MediaOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MediaOptions"/> object.</param>
        private void OpenStreams(MediaOptions options)
        {
            // if (options.StreamsToLoad != MediaMode.Audio)
            Video = DecoderFactory.OpenVideo(this, options);
            if (Video != null)
            {
                GetPacketFromStream(Video.Info.Index); // Requests for the first packet.
                canReusePacket = true;
            }
        }

        /// <summary>
        /// Reads the next packet from this file.
        /// </summary>
        private void ReadPacket()
        {
            var result = ffmpeg.av_read_frame(Pointer, packet.Pointer); // Gets the next packet from the file.

            // Check if the end of file error occurred
            if (result == ffmpeg.AVERROR_EOF)
            {
                throw new EndOfStreamException("End of the file.");
            }
            else
            {
                result.ThrowIfError("Cannot read next packet from the file");
            }
        }

        private void GetPacketFromStream(int streamIndex)
        {
            do
            {
                ReadPacket();
                if (packet.StreamIndex != streamIndex)
                    packet.Wipe();
                else
                    break;
            }
            while (true);
         }
    }
}

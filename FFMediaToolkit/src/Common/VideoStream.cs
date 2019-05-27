namespace FFMediaToolkit.Common
{
    using System;
    using FFMediaToolkit.Decoding;
    using FFMediaToolkit.Encoding;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a FFmpeg video stream.
    /// </summary>
    public unsafe class VideoStream : MediaStream<VideoFrame>
    {
        private VideoStream(AVStream* stream, AVCodecContext* codec, MediaContainer container, Layout frameLayout)
            : base(stream, codec, container) => FrameLayout = frameLayout;

        /// <summary>
        /// Gets the dimensions of video frames.
        /// </summary>
        public Layout FrameLayout { get; }

        /// <inheritdoc/>
        public override MediaType Type => MediaType.Video;

        /// <summary>
        /// Opens the video stream with the specified index in the media container.
        /// </summary>
        /// <param name="container">The media container.</param>
        /// <param name="index">The video stream index.</param>
        /// <param name="options">The media options.</param>
        /// <returns>The opened <see cref="VideoStream"/>.</returns>
        internal static VideoStream Open(MediaContainer container, int index, MediaOptions options)
        {
            if (container.Access != MediaAccess.WriteInit)
                throw new InvalidOperationException("The Media container must be in Read acces mode");

            var format = container.FormatContextPointer;
            var stream = format->streams[index];

            var codecContext = ffmpeg.avcodec_alloc_context3(null);
            ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar)
                .CatchAll("Cannot create codec parameters.");

            codecContext->pkt_timebase = stream->time_base;
            var codec = ffmpeg.avcodec_find_decoder(stream->codec->codec_id);

            if (codec == null)
                throw new InvalidOperationException("Cannot find a codec for this stream.");

            var dict = options.DecoderOptions.Pointer;

            ffmpeg.avcodec_open2(codecContext, codec, &dict)
                .CatchAll("Cannot open the video codec");

            options.DecoderOptions.Update(dict);

            var layout = new Layout(codecContext->pix_fmt, codecContext->width, codecContext->height);

            return new VideoStream(stream, codecContext, container, layout);
        }

        /// <summary>
        /// Creates a new video stream in the specified format context.
        /// </summary>
        /// <param name="container">A output file format context.</param>
        /// <param name="config">A <see cref="VideoEncoderSettings"/> object containing the configuration of the video stream.</param>
        /// <returns>Video stream added to the file.</returns>
        internal static VideoStream CreateNew(MediaContainer container, VideoEncoderSettings config)
        {
            if (container.Access != MediaAccess.WriteInit)
                throw new InvalidOperationException("The Media container must be in WriteInit acces mode");

            var codecId = config.Codec ?? container.FormatContextPointer->oformat->video_codec;

            if (codecId == AVCodecID.AV_CODEC_ID_NONE)
                throw new InvalidOperationException("The media container doesn't support video!");

            var codec = ffmpeg.avcodec_find_encoder(codecId);

            if (codec != null)
                throw new InvalidOperationException($"Cannot find an encoder with the {codecId}!");

            if (codec->type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new InvalidOperationException($"The {codecId} encoder doesn't support video!");

            var videoStream = ffmpeg.avformat_new_stream(container.FormatContextPointer, codec);
            var codecContext = videoStream->codec;
            codecContext->codec_id = codecId;
            codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;

            codecContext->bit_rate = config.Bitrate;
            codecContext->width = config.VideoWidth;
            codecContext->height = config.VideoHeight;

            codecContext->time_base.den = config.Framerate;
            codecContext->time_base.num = 1;
            codecContext->gop_size = config.KeyframeRate;
            codecContext->pix_fmt = (AVPixelFormat)config.VideoPixelFormat;

            if ((container.FormatContextPointer->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                codecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            var dict = config.CodecOptions.Pointer;
            ffmpeg.avcodec_open2(codecContext, codec, &dict);

            return new VideoStream(videoStream, codecContext, container, config.ToLayout());
        }
    }
}

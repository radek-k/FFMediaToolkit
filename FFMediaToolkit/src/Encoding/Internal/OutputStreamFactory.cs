namespace FFMediaToolkit.Encoding.Internal
{
    using System;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains method for creating media streams.
    /// </summary>
    internal static unsafe class OutputStreamFactory
    {
        /// <summary>
        /// Creates a new video stream for the specified <see cref="OutputContainer"/>.
        /// </summary>
        /// <param name="container">The media container.</param>
        /// <param name="config">The stream settings.</param>
        /// <returns>The new video stream.</returns>
        public static OutputStream<VideoFrame> CreateVideo(OutputContainer container, VideoEncoderSettings config)
        {
            var codecId = config.Codec ?? container.Pointer->oformat->video_codec;

            if (codecId == AVCodecID.AV_CODEC_ID_NONE)
                throw new InvalidOperationException("The media container doesn't support video!");

            var codec = ffmpeg.avcodec_find_encoder(codecId);

            if (codec == null)
                throw new InvalidOperationException($"Cannot find an encoder with the {codecId}!");

            if (codec->type != AVMediaType.AVMEDIA_TYPE_VIDEO)
                throw new InvalidOperationException($"The {codecId} encoder doesn't support video!");

            var videoStream = ffmpeg.avformat_new_stream(container.Pointer, codec);
            var codecContext = videoStream->codec;
            codecContext->codec_id = codecId;
            codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;

            codecContext->bit_rate = config.Bitrate;
            codecContext->width = config.VideoWidth;
            codecContext->height = config.VideoHeight;

            codecContext->time_base.den = config.Framerate;
            codecContext->time_base.num = 1;
            codecContext->gop_size = config.KeyframeRate;
            codecContext->pix_fmt = (AVPixelFormat)config.VideoFormat;

            if ((container.Pointer->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                codecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            var dict = new FFDictionary(config.CodecOptions);
            var ptr = dict.Pointer;

            ffmpeg.avcodec_open2(codecContext, codec, &ptr);

            dict.Update(ptr);

            return new OutputStream<VideoFrame>(videoStream, container);
        }
    }
}

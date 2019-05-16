namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using FFMediaToolkit.Encoding;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a low-level wrapper of FFMPEG AvStream
    /// </summary>
    public unsafe class VideoStream : MediaStream<VideoFrame>
    {
        private VideoStream(AVStream* stream, AVCodecContext* codec, MediaContainer container, Layout frameLayout)
            : base(stream, codec, container) => FrameLayout = frameLayout;

        /// <summary>
        /// Gets the dimensions of video frames
        /// </summary>
        public Layout FrameLayout { get; }

        /// <summary>
        /// Creates a new video stream in the specified format context
        /// </summary>
        /// <param name="container">A output file's format context</param>
        /// <param name="config">A <see cref="VideoEncoderSettings"/> object containing the configuration of the video stream</param>
        /// <returns>Video stream added to file</returns>
        internal static VideoStream CreateNew(MediaContainer container, VideoEncoderSettings config)
        {
            if (container.Access != MediaAccess.WriteInit)
                throw new InvalidOperationException("The Media container must be in WriteInit acces mode");

            var codecId = config.Codec ?? container.FormatContextPointer->oformat->video_codec;
            var codec = ffmpeg.avcodec_find_encoder(codecId);

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

        /// <inheritdoc/>
        protected override VideoFrame OnReading() => throw new NotImplementedException();
    }
}

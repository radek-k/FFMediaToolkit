namespace FFMediaToolkit.Common
{
    using System;
    using System.Collections.Generic;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a low-level wrapper of FFMPEG AvStream
    /// </summary>
    public unsafe class VideoStream : MediaStream<VideoFrame>
    {
        private VideoStream(AVStream* stream, AVCodecContext* codec, MediaContainer container, Layout frameLayout)
            : base(stream, codec, container)
        {
        }

        /// <summary>
        /// Gets the dimensions of video frames
        /// </summary>
        public Layout FrameLayout { get; }

        /// <summary>
        /// Gets the currently encoded frame
        /// </summary>
        internal VideoFrame EncodedFrame { get; private set; }

        /// <summary>
        /// Creates a new video stream in the specified format context
        /// </summary>
        /// <param name="container">A output file's format context</param>
        /// <param name="config">A <see cref="VideoEncoderSettings"/> object containing the configuration of the video stream</param>
        /// <returns>Video stream added to file</returns>
        internal static VideoStream CreateNew(MediaContainer container, VideoEncoderSettings config)
        {
            var codec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MPEG4);

            var videoStream = ffmpeg.avformat_new_stream(container.FormatContextPointer, codec);
            var codecContext = videoStream->codec;
            codecContext->codec_id = AVCodecID.AV_CODEC_ID_MPEG4;
            codecContext->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;

            codecContext->bit_rate = config.Bitrate;
            codecContext->width = config.VideoWidth;
            codecContext->height = config.VideoHeight;

            codecContext->time_base.den = config.Framerate;
            codecContext->time_base.num = 1;
            codecContext->gop_size = config.KeyframeRate;
            codecContext->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

            if ((container.FormatContextPointer->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                codecContext->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            var dict = config.CodecOptions.Pointer;
            ffmpeg.avcodec_open2(codecContext, codec, &dict);

            return new VideoStream(videoStream, codecContext, container, config.ToLayout());
        }

        /// <inheritdoc/>
        protected override void OnPushing(VideoFrame frame)
        {
            ffmpeg.avcodec_send_frame(CodecContextPointer, frame.ToPointer()).ThrowIfError("sending the frame");

            var packet = MediaPacket.AllocateEmpty(Index);

            if (ffmpeg.avcodec_receive_packet(CodecContextPointer, packet) == 0)
            {
                packet.RescaleTimestamp(TimeBase, StreamPointer->time_base);

                if (CodecContextPointer->coded_frame->key_frame == 1)
                {
                    packet.IsKeyPacket = true;
                }

                OwnerFile.WritePacket(packet);
            }
        }

        /// <inheritdoc/>
        protected override VideoFrame OnReading() => throw new NotImplementedException();
    }
}

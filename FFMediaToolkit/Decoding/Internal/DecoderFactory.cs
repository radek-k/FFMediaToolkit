namespace FFMediaToolkit.Decoding.Internal
{
    using System;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains method for opening media streams.
    /// </summary>
    internal unsafe class DecoderFactory
    {
        /// <summary>
        /// Opens the video stream with the specified index in the media container.
        /// </summary>
        /// <param name="container">The media container.</param>
        /// <param name="options">The media options.</param>
        /// <returns>The opened <see cref="Decoder{TFrame}"/>.</returns>
        internal static Decoder<VideoFrame> OpenVideo(InputContainer container, MediaOptions options)
        {
            var format = container.Pointer;
            AVCodec* codec = null;

            var index = ffmpeg.av_find_best_stream(format, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
            index.IfError(ffmpeg.AVERROR_DECODER_NOT_FOUND, "Cannot find a codec for the video stream.");
            if (index < 0)
            {
                return null;
            }

            var stream = format->streams[index];
            var codecContext = ffmpeg.avcodec_alloc_context3(codec);
            ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar)
                .ThrowIfError("Cannot open the video codec!");
            codecContext->pkt_timebase = stream->time_base;

            var dict = new FFDictionary(options.DecoderOptions, false).Pointer;

            ffmpeg.avcodec_open2(codecContext, codec, &dict)
                .ThrowIfError("Cannot open the video codec");

            return new Decoder<VideoFrame>(codecContext, stream, container);
        }

        /// <summary>
        /// Opens the audio stream with the specified index in the media container.
        /// </summary>
        /// <param name="container">The media container.</param>
        /// <param name="options">The media options.</param>
        /// <returns>The opened <see cref="Decoder{TFrame}"/>.</returns>
        internal static Decoder<AudioFrame> OpenAudio(InputContainer container, MediaOptions options)
        {
            var format = container.Pointer;
            AVCodec* codec = null;

            var index = ffmpeg.av_find_best_stream(format, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &codec, 0);
            index.IfError(ffmpeg.AVERROR_DECODER_NOT_FOUND, "Cannot find a codec for the audio stream.");
            if (index < 0)
            {
                return null;
            }

            var stream = format->streams[index];
            var codecContext = ffmpeg.avcodec_alloc_context3(codec);
            ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar)
                .ThrowIfError("Cannot open the audio codec!");
            codecContext->pkt_timebase = stream->time_base;

            var dict = new FFDictionary(options.DecoderOptions, false).Pointer;

            ffmpeg.avcodec_open2(codecContext, codec, &dict)
                .ThrowIfError("Cannot open the audio codec");

            return new Decoder<AudioFrame>(codecContext, stream, container);
        }
    }
}

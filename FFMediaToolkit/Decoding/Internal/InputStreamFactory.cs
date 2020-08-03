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
    internal unsafe class InputStreamFactory
    {
        /// <summary>
        /// Opens the video stream with the specified index in the media container.
        /// </summary>
        /// <param name="container">The media container.</param>
        /// <param name="index">The video stream index.</param>
        /// <param name="options">The media options.</param>
        /// <returns>The opened <see cref="InputStream{TFrame}"/>.</returns>
        internal static InputStream<VideoFrame> OpenVideo(InputContainer container, int index, MediaOptions options)
        {
            var format = container.Pointer;
            var stream = format->streams[index];
            var codecContext = stream->codec;

            codecContext->pkt_timebase = stream->time_base;

            var codec = ffmpeg.avcodec_find_decoder(codecContext->codec_id);

            if (codec == null)
                throw new InvalidOperationException("Cannot find a codec for this stream.");

            var dict = new FFDictionary(options.DecoderOptions);
            var ptr = dict.Pointer;

            ffmpeg.avcodec_open2(codecContext, codec, &ptr)
                .ThrowIfError("Cannot open the video codec");

            dict.Update(ptr);

            return new InputStream<VideoFrame>(stream, container);
        }
    }
}

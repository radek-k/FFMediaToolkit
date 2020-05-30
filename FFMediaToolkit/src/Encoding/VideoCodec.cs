namespace FFMediaToolkit.Encoding
{
    using FFmpeg.AutoGen;

    /// <summary>
    /// The new video codec enum that contains only supported by FFMediaToolkit video encoders.
    /// If you must use a codec not included to this enum, you can cast <see cref="AVCodecID"/> to <see cref="VideoCodec"/>.
    /// </summary>
    public enum VideoCodec
    {
        // WORK IN PROGRESS
        // Some codecs aren't tested yet.
        // TODO: Write XML comments
        Default = AVCodecID.AV_CODEC_ID_NONE,
        H263 = AVCodecID.AV_CODEC_ID_H263,
        H263I = AVCodecID.AV_CODEC_ID_H263I,
        H263P = AVCodecID.AV_CODEC_ID_H263P,
        H264 = AVCodecID.AV_CODEC_ID_H264,
        H265 = AVCodecID.AV_CODEC_ID_HEVC,
        WMV = AVCodecID.AV_CODEC_ID_MSMPEG4V2,
        MPEG = AVCodecID.AV_CODEC_ID_MPEG1VIDEO,
        MPEG2 = AVCodecID.AV_CODEC_ID_MPEG2VIDEO,
        MPEG4 = AVCodecID.AV_CODEC_ID_MPEG4,
        VP8 = AVCodecID.AV_CODEC_ID_VP8,
        VP9 = AVCodecID.AV_CODEC_ID_VP9,
        Theora = AVCodecID.AV_CODEC_ID_THEORA,
        Dirac = AVCodecID.AV_CODEC_ID_DIRAC,
        MJPEG = AVCodecID.AV_CODEC_ID_MJPEG,
        AV1 = AVCodecID.AV_CODEC_ID_AV1,
        DV = AVCodecID.AV_CODEC_ID_DVVIDEO,
        Cinepak = AVCodecID.AV_CODEC_ID_CINEPAK,
    }
}

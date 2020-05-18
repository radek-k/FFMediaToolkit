namespace FFMediaToolkit.Encoding
{
    /// <summary>
    /// Video container formats supported by FFMediaToolkit.
    /// </summary>
    public enum ContainerFormat
    {
        /// <summary>
        /// The 3GPP container (.3gp)
        /// </summary>
        ThridGPP,

        /// <summary>
        /// The 2nd 3GPP container (.3g2)
        /// </summary>
        ThridGPP2,

        /// <summary>
        /// The Microsoft Advanced Systems Formats container (.asf)
        /// Use this container when encoding a .wmv (Windows Media) video file.
        /// </summary>
        ASF,

        /// <summary>
        /// The Audio Video Interleave container (.avi)
        /// </summary>
        AVI,

        /// <summary>
        /// The Matroska Multimedia Container (.mkv)
        /// </summary>
        MKV,

        /// <summary>
        /// The MPEG-4 container (.mp4)
        /// </summary>
        MP4,

        /// <summary>
        /// The Ogg container (.ogv for video files)
        /// </summary>
        Ogg,

        /// <summary>
        /// The WebM container (.webm)
        /// </summary>
        WebM,
    }
}

namespace FFMediaToolkit.Encoding
{
    /// <summary>
    /// The presets for H264 and H265 (HEVC) video encoders.
    /// <para>Fast presets = faster encoding, worse compression.</para>
    /// <para>Slow presets = longer encoding, better compression.</para>
    /// </summary>
    public enum EncoderPreset
    {
        /// <summary>
        /// Port of 'ultrafast'
        /// </summary>
        UltraFast = 0,

        /// <summary>
        /// Port of 'superfast'
        /// </summary>
        SuperFast = 1,

        /// <summary>
        /// Port of 'veryfast'
        /// </summary>
        VeryFast = 2,

        /// <summary>
        /// Port of 'faster'
        /// </summary>
        Faster = 3,

        /// <summary>
        /// Port of 'fast'
        /// </summary>
        Fast = 4,

        /// <summary>
        /// The default preset. Port of 'medium'
        /// </summary>
        Medium = 5,

        /// <summary>
        /// Port of 'slow'
        /// </summary>
        Slow = 6,

        /// <summary>
        /// Port of 'slower'
        /// </summary>
        Slower = 7,

        /// <summary>
        /// Port of 'veryslow'
        /// </summary>
        VerySlow = 8,
    }
}

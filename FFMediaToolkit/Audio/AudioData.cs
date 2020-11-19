namespace FFMediaToolkit.Audio
{
    using System;
    using FFMediaToolkit.Common.Internal;

    /// <summary>
    /// Represents a lightweight container for audio data.
    /// </summary>
    public ref struct AudioData
    {
        private readonly AudioFrame frame;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioData"/> struct.
        /// </summary>
        /// <param name="frame">frame object containing raw audio data.</param>
        internal AudioData(AudioFrame frame)
        {
            this.frame = frame;
        }

        /// <summary>
        /// Gets the number of samples.
        /// </summary>
        public int NumSamples => frame.NumSamples;

        /// <summary>
        /// Gets the number of channels.
        /// </summary>
        public int NumChannels => frame.NumChannels;

        /// <summary>
        /// Fetches raw audio data from this audio frame for specified channel.
        /// </summary>
        /// <param name="channel">The index of audio channel that should be retrieved, allowed range: [0..<see cref="NumChannels"/>).</param>
        /// <returns>The span with samples in range of [-1.0, ..., 1.0].</returns>
        public ReadOnlySpan<float> GetChannelData(uint channel)
        {
            return frame.GetChannelData(channel);
        }
    }
}

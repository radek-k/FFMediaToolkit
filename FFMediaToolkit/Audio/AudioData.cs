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
        public Span<float> GetChannelData(uint channel)
        {
            return frame.GetChannelData(channel);
        }

        /// <summary>
        /// Copies raw multichannel audio data from this frame to a heap allocated array.
        /// </summary>
        /// <returns>
        /// The span with <see cref="NumChannels"/> rows and <see cref="NumSamples"/> columns;
        /// samples in range of [-1.0, ..., 1.0].
        /// </returns>
        public float[][] GetSampleData()
        {
            return frame.GetSampleData();
        }

        /// <summary>
        /// Updates the specified channel of this audio frame with the given sample data.
        /// </summary>
        /// <param name="samples">An array of samples with length <see cref="NumSamples"/>.</param>
        /// <param name="channel">The index of audio channel that should be updated, allowed range: [0..<see cref="NumChannels"/>).</param>
        public void UpdateChannelData(float[] samples, uint channel)
        {
            frame.UpdateChannelData(samples, channel);
        }

        /// <summary>
        /// Updates this audio frame with the specified multi-channel sample data.
        /// </summary>
        /// <param name="samples">
        /// A 2D jagged array of multi-channel sample data
        /// with <see cref="NumChannels"/> rows and <see cref="NumSamples"/> columns.
        /// </param>
        public void UpdateFromSampleData(float[][] samples)
        {
            frame.UpdateFromSampleData(samples);
        }

        /// <summary>
        /// Releases all unmanaged resources associated with this instance.
        /// </summary>
        public void Dispose()
        {
            frame.Dispose();
        }
    }
}

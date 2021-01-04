namespace FFMediaToolkit.Decoding
{
    using System;
    using System.IO;
    using FFMediaToolkit.Audio;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;

    /// <summary>
    /// Represents an audio stream in the <see cref="MediaFile"/>.
    /// </summary>
    public class AudioStream : MediaStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStream"/> class.
        /// </summary>
        /// <param name="stream">The audio stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal AudioStream(Decoder stream, MediaOptions options)
            : base(stream, options)
        {
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public new AudioStreamInfo Info => base.Info as AudioStreamInfo;

        /// <summary>
        /// Reads the next frame from the audio stream.
        /// </summary>
        /// <returns>The decoded audio data.</returns>
        public new AudioData GetNextFrame()
        {
            var frame = base.GetNextFrame() as AudioFrame;
            return new AudioData(frame);
        }

        /// <summary>
        /// Reads the next frame from the audio stream.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="data">The decoded audio data.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryGetNextFrame(out AudioData data)
        {
            try
            {
                data = GetNextFrame();
                return true;
            }
            catch (EndOfStreamException)
            {
                data = default;
                return false;
            }
        }

        /// <summary>
        /// Reads the video frame found at the specified timestamp.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <returns>The decoded video frame.</returns>
        public new AudioData GetFrame(TimeSpan time)
        {
            var frame = base.GetFrame(time) as AudioFrame;
            return new AudioData(frame);
        }

        /// <summary>
        /// Reads the audio data found at the specified timestamp.
        /// A <see langword="false"/> return value indicates that reached end of stream.
        /// The method throws exception if another error has occurred.
        /// </summary>
        /// <param name="time">The frame timestamp.</param>
        /// <param name="data">The decoded audio data.</param>
        /// <returns><see langword="false"/> if reached end of the stream.</returns>
        public bool TryGetFrame(TimeSpan time, out AudioData data)
        {
            try
            {
                data = GetFrame(time);
                return true;
            }
            catch (EndOfStreamException)
            {
                data = default;
                return false;
            }
        }
    }
}

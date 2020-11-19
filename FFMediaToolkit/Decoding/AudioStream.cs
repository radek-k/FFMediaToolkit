namespace FFMediaToolkit.Decoding
{
    using System;
    using FFMediaToolkit.Audio;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents an audio stream in the <see cref="MediaFile"/>.
    /// </summary>
    public class AudioStream : IDisposable
    {
        private readonly Decoder<AudioFrame> stream;
        private readonly AudioFrame frame;
        private readonly MediaOptions mediaOptions;

        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStream"/> class.
        /// </summary>
        /// <param name="audio">The Audio stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal AudioStream(Decoder<AudioFrame> audio, MediaOptions options)
        {
            stream = audio;
            mediaOptions = options;
            frame = AudioFrame.CreateEmpty();
        }

        /// <summary>
        /// Gets informations about this stream.
        /// </summary>
        public AudioStreamInfo Info => (AudioStreamInfo)stream.Info;

        /// <summary>
        /// Gets the index of the next frame in the Audio stream.
        /// </summary>
        public int FramePosition { get; private set; }

        /// <summary>
        /// Gets the timestamp of the next frame in the Audio stream.
        /// </summary>
        public TimeSpan Position => FramePosition.ToTimeSpan(Info.AvgFrameRate);

        /// <summary>
        /// Reads the specified audio frame (chunk of audio samples sized by ffmedia settings).
        /// </summary>
        /// <param name="frameNumber">The frame index (zero-based number).</param>
        /// <returns>The decoded audio frame.</returns>
        public AudioData ReadFrame(int frameNumber)
        {
            lock (syncLock)
            {
                frameNumber = frameNumber.Clamp(0, Info.FrameCount != 0 ? Info.FrameCount - 1 : int.MaxValue);

                if (frameNumber == FramePosition)
                {
                    return GetNextFrameData();
                }
                else if (frameNumber == FramePosition - 1)
                {
                    return new AudioData(stream.RecentlyDecodedFrame);
                }
                else
                {
                    var frame = SeekToFrame(frameNumber);
                    FramePosition = frameNumber + 1;

                    return new AudioData(frame);
                }
            }
        }

        /// <summary>
        /// Reads the audio frame found at the specified timestamp.
        /// </summary>
        /// <param name="targetTime">The frame timestamp.</param>
        /// <returns>The decoded audio frame.</returns>
        public AudioData ReadFrame(TimeSpan targetTime) => ReadFrame((int)(targetTime.TotalSeconds * Info.AvgFrameRate));

        /// <summary>
        /// Reads the next frame from this stream.
        /// </summary>
        /// <returns>The decoded audio frame.</returns>
        public AudioData ReadNextFrame()
        {
            lock (syncLock)
            {
              return GetNextFrameData();
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            lock (syncLock)
            {
                stream.Dispose();
                frame.Dispose();
            }
        }

        private AudioData GetNextFrameData()
        {
            var bmp = new AudioData(stream.GetNextFrame());
            FramePosition++;
            return bmp;
        }

        private AudioFrame SeekToFrame(int frameNumber)
        {
            var ts = frameNumber * Info.AvgNumSamplesPerFrame;

            if (frameNumber < FramePosition || frameNumber > FramePosition + mediaOptions.AudioSeekThreshold)
            {
                stream.OwnerFile.SeekFile(ts, Info.Index);
            }

            stream.SkipFrames(ts);
            return stream.RecentlyDecodedFrame;
        }
    }
}

namespace FFMediaToolkit.Decoding
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Graphics;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents the video stream in a media file.
    /// </summary>
    public class VideoStream : IDisposable
    {
        private readonly InputStream<VideoFrame> stream;
        private readonly VideoFrame frame;
        private readonly Scaler scaler;
        private readonly MediaOptions mediaOptions;

        private readonly object syncLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStream"/> class.
        /// </summary>
        /// <param name="video">The video stream.</param>
        /// <param name="options">The decoder settings.</param>
        internal VideoStream(InputStream<VideoFrame> video, MediaOptions options)
        {
            stream = video;
            mediaOptions = options;
            frame = VideoFrame.CreateEmpty();
            scaler = new Scaler();
        }

        /// <summary>
        /// Gets the stream informations.
        /// </summary>
        public StreamInfo Info => stream.Info;

        /// <summary>
        /// Gets the current stream position in frames.
        /// </summary>
        public int FramePosition { get; private set; }

        /// <summary>
        /// Gets the next frame from the video file.
        /// </summary>
        /// <returns>The video frame.</returns>
        public unsafe BitmapData ReadFrame()
        {
            lock (syncLock)
            {
                stream.Read(frame);
                FramePosition++;

                var targetLayout = GetTargetLayout();
                var bitmap = PooledBitmap.Create(targetLayout.Width, targetLayout.Height, mediaOptions.VideoPixelFormat);

                fixed (byte* ptr = bitmap.Data.Span)
                {
                    scaler.AVFrameToBitmap(frame.Pointer, frame.Layout, new IntPtr(ptr), targetLayout);
                }

                return bitmap;
            }
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            lock (syncLock)
            {
                stream.Dispose();
                frame.Dispose();
                scaler.Dispose();
            }
        }

        private Layout GetTargetLayout()
        {
            var target = mediaOptions.TargetVideoSize ?? stream.Info.Dimensions.Size;
            return new Layout((AVPixelFormat)mediaOptions.VideoPixelFormat, target);
        }
    }
}

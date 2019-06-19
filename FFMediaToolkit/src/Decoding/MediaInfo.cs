namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Collections.ObjectModel;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents informations about the media container.
    /// </summary>
    public class MediaInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfo"/> class.
        /// </summary>
        /// <param name="container">The input container context.</param>
        internal unsafe MediaInfo(AVFormatContext* container)
        {
            FilePath = new IntPtr(container->url).Utf8ToString();
            ContainerFormat = new IntPtr(container->iformat->name).Utf8ToString();
            Metadata = new ReadOnlyDictionary<string, string>(FFDictionary.ToDictionary(container->metadata));
            Bitrate = container->bit_rate > 0 ? container->bit_rate : 0;

            var timeBase = new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE };
            Duration = container->duration != ffmpeg.AV_NOPTS_VALUE ? container->duration.ToTimeSpan(timeBase) : TimeSpan.Zero;
            StartTime = container->start_time != ffmpeg.AV_NOPTS_VALUE ? container->start_time.ToTimeSpan(timeBase) : TimeSpan.Zero;
        }

        /// <summary>
        /// Gets the file path used to open the container.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the container format name.
        /// </summary>
        public string ContainerFormat { get; }

        /// <summary>
        /// Gets the container bitrate in bytes per second. 0 if unknow.
        /// </summary>
        public long Bitrate { get; }

        /// <summary>
        /// Gets the duration of the media container.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the media container start time.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Gets the container file metadata. Streams may contain additional metadata.
        /// </summary>
        public ReadOnlyDictionary<string, string> Metadata { get; }
    }
}

namespace FFMediaToolkit
{
    using System.IO;
    using FFMediaToolkit.Interop;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains methods for managing FFmpeg libraries.
    /// </summary>
    public static class MediaToolkit
    {
        private static LogLevel logLevel = LogLevel.Error;

        /// <summary>
        /// Gets or sets the verbosity level of FFMpeg logs printed to standard error/output. Default value is <see cref="LogLevel.Error"/>.
        /// </summary>
        public static LogLevel LogVerbosity
        {
            get => logLevel;
            set
            {
                if (IsPathSetByUser)
                {
                    ffmpeg.av_log_set_level((int)value);
                }

                logLevel = value;
            }
        }

        /// <summary>
        /// Gets or sets path to the directory containing FFmpeg binaries.
        /// </summary>
        public static string FFmpegPath
        {
            get => ffmpeg.RootPath ?? string.Empty;
            set
            {
                if (!Directory.Exists(value))
                {
                    throw new DirectoryNotFoundException("Cannot found FFmpeg directory");
                }

                NativeMethods.SetDllLoadingDirectory(value);
                ffmpeg.RootPath = value;
                IsPathSetByUser = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a path to the FFmpeg binaries was set by user.
        /// </summary>
        internal static bool IsPathSetByUser { get; private set; }

        /// <summary>
        /// Loads FFmpeg libraries from the default path for current platform.
        /// </summary>
        internal static void LoadFFmpeg()
        {
            if (!IsPathSetByUser)
            {
                FFmpegPath = NativeMethods.GetDefaultDirectory();
            }

            LogVerbosity = logLevel;
        }
    }
}

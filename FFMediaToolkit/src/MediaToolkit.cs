namespace FFMediaToolkit
{
    using System;
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
                if (IsPathSet)
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
                    throw new DirectoryNotFoundException("The specified FFmpeg directory does not exist!");
                }

                ffmpeg.RootPath = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a path to the FFmpeg binaries was set.
        /// </summary>
        internal static bool IsPathSet => !string.IsNullOrEmpty(FFmpegPath);

        /// <summary>
        /// Loads FFmpeg libraries from the default path for current platform.
        /// </summary>
        internal static void LoadFFmpeg()
        {
            if (!IsPathSet)
            {
                try
                {
                    FFmpegPath = NativeMethods.GetFFMpegDirectory();
                }
                catch (DirectoryNotFoundException)
                {
                    throw new DirectoryNotFoundException("Cannot found the default FFmpeg directory.\n" +
                        "On Windows make sure the \".\\runtimes\\\" directory exists in the application output directory.\n" +
                        "For more informations please see https://github.com/radek-k/FFMediaToolkit#setup");
                }
            }

            try
            {
                LogVerbosity = logLevel;
            }
            catch (DllNotFoundException ex)
            {
                HandleLibraryLoadError(ex);
            }
        }

        /// <summary>
        /// Throws a FFmpeg library loading exception.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        internal static void HandleLibraryLoadError(Exception exception)
        {
            throw new DllNotFoundException($"Cannot load required FFmpeg libraries from {FFmpegPath} directory.\nFor more informations please see https://github.com/radek-k/FFMediaToolkit#setup", exception);
        }
    }
}

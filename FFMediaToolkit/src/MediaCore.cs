namespace FFMediaToolkit
{
    using System;
    using System.IO;
    using Interop;

    /// <summary>
    /// Contains methods for managing FFmpeg libraries
    /// </summary>
    public static class MediaCore
    {
        private static LibraryManager Libraries { get; } = new LibraryManager();

        /// <summary>
        /// Loads FFMpeg libraries from given path
        /// </summary>
        /// <param name="path">A path to the directory containing FFMpeg assembles</param>
        public static void LoadFFmpeg(string path)
        {
            var dir = path ?? NativeMethods.GetDefaultDirectory();
            if (!Directory.Exists(dir))
            {
                throw new DirectoryNotFoundException("Cannot found FFmpeg directory");
            }

            NativeMethods.SetDllLoadingDirectory(dir);

            Libraries.LoadAll(dir);
        }

        /// <summary>
        /// Loads FFmpeg libraries from
        /// </summary>
        internal static void LoadFFmpeg() => LoadFFmpeg(null);
    }
}

namespace FFMediaToolkit
{
    using System.IO;
    using System.Runtime.InteropServices;
    using Interop;

    /// <summary>
    /// Contains methods for managing FFmpeg libraries.
    /// </summary>
    public static class MediaCore
    {
        private static LibraryManager Libraries { get; } = new LibraryManager();

        /// <summary>
        /// Loads FFMpeg libraries from the given path.
        /// </summary>
        /// <param name="path">A path to the directory containing FFMpeg assembles.</param>
        public static void LoadFFmpeg(string path)
        {
            if (Libraries.IsLoaded)
                return;

            var dir = path ?? NativeMethods.GetDefaultDirectory();
            if (!Directory.Exists(dir))
            {
                throw new DirectoryNotFoundException("Cannot found FFmpeg directory");
            }

            NativeMethods.SetDllLoadingDirectory(dir);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Libraries.LoadAll(dir);
        }

        /// <summary>
        /// Loads FFmpeg libraries from the default path.
        /// </summary>
        internal static void LoadFFmpeg() => LoadFFmpeg(null);
    }
}

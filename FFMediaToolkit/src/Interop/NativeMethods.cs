namespace FFMediaToolkit.Interop
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Contains the native operating system methods.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Gets the default FFmpeg directory for current platform.
        /// </summary>
        /// <returns>A path to the default directory for FFmpeg libraries.</returns>
        internal static string GetDefaultDirectory()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return "/opt/local/lib/";
                case PlatformID.Unix:
                    return Environment.Is64BitOperatingSystem ? "/usr/lib/x86_64-linux-gnu" : "/usr/lib/x86-linux-gnu";
                case PlatformID.Win32NT:
                    var root = GetAssemblyDirectory();
                    var dir = Environment.Is64BitProcess ? @"ffmpeg\x86_64" : @"ffmpeg\x86";
                    var path = Path.Combine(root, dir);
                    return path;
                default:
                    throw new NotSupportedException($"The {Environment.OSVersion.Platform.ToString()} platform are not supported");
            }
        }

        /// <summary>
        /// Sets DLL loading directory if current OS is Windows.
        /// </summary>
        /// <param name="dir">The directory path.</param>
        internal static void SetDllLoadingDirectory(string dir)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetDllDirectory(dir);
            }
        }

        private static string GetAssemblyDirectory()
        {
            var path = Assembly.GetExecutingAssembly().Location;
            return Directory.GetParent(path).FullName;
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}

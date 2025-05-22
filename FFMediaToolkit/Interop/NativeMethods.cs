namespace FFMediaToolkit.Interop
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Contains the native operating system methods.
    /// </summary>
    internal static class NativeMethods
    {
        private static string MacOSDefautDirectory => "/usr/local/lib";

        private static string LinuxDefaultDirectory => "/usr/lib/{0}-linux-gnu";

        private static string WindowsDefaultDirectory => @"\runtimes\{0}\native";

        /// <summary>
        /// Gets the default FFmpeg directory for current platform.
        /// </summary>
        /// <returns>A path to the default directory for FFmpeg libraries.</returns>
        internal static string GetFFmpegDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string archName;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        archName = "win-x86";
                        break;
                    case Architecture.X64:
                        archName = "win-x64";
                        break;
                    case Architecture.Arm64:
                        archName = "win-arm64";
                        break;
                    default:
                        throw new PlatformNotSupportedException("This OS architecture is not supported by the FFMediaToolkit");
                }

                return AppDomain.CurrentDomain.BaseDirectory + string.Format(WindowsDefaultDirectory, archName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string archName;
                switch (RuntimeInformation.ProcessArchitecture)
                {
                    case Architecture.X86:
                        archName = "x86";
                        break;
                    case Architecture.X64:
                        archName = "x86_64";
                        break;
                    case Architecture.Arm:
                        archName = "arm";
                        break;
                    case Architecture.Arm64:
                        archName = "aarch64";
                        break;
                    default:
                        throw new PlatformNotSupportedException("This OS architecture is not supported by the FFMediaToolkit");
                }

                return string.Format(LinuxDefaultDirectory, archName);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return MacOSDefautDirectory;
            }
            else
            {
                throw new PlatformNotSupportedException("This OS platform is not supported by the FFMediaToolkit");
            }
        }
    }
}

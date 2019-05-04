namespace FFMediaToolkit.Interop
{
    using System;
    using System.IO;
    using FFmpeg.AutoGen.Native;

    /// <summary>
    /// Represents a wrapper of FFMpeg native library
    /// </summary>
    public class Library
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Library"/> class.
        /// </summary>
        /// <param name="name">The library name</param>
        /// <param name="version">The library version</param>
        public Library(string name, int version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Gets the library name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the library version
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Gets the pointer to the loaded library
        /// </summary>
        public IntPtr Handle { get; private set; } = IntPtr.Zero;

        /// <summary>
        /// Gets a value indicating whether the library is loaded
        /// </summary>
        public bool IsLoaded => Handle != IntPtr.Zero;

        /// <summary>
        /// Loads the library from the specified path.
        /// </summary>
        /// <param name="dir">Path to directory containing libraries</param>
        public void Load(string dir)
        {
            if (IsLoaded)
            {
                throw new InvalidOperationException($"The {Name} library was loaded");
            }

            var ptr = LibraryLoader.LoadNativeLibrary(dir, Name, Version);

            if (ptr == IntPtr.Zero)
            {
                throw new FileNotFoundException($"The {Name} library cannot be loaded from {dir}");
            }

            Handle = ptr;
        }
    }
}

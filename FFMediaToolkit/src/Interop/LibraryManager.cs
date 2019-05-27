namespace FFMediaToolkit.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Represents a FFMpeg native library manager.
    /// </summary>
    public class LibraryManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryManager"/> class.
        /// </summary>
        internal LibraryManager()
        {
            All = new ReadOnlyCollection<Library>(new List<Library>
            {
                AVCodec,
                AVFormat,
                AVUtil,
                SWResample,
                SWScale,
            });
        }

        /// <summary>
        /// Gets the <c>avcodec</c> library.
        /// </summary>
        public Library AVCodec { get; } = new Library(Names.AVCodec, Versions.AVCodec);

        /// <summary>
        /// Gets the <code>avformat</code> library.
        /// </summary>
        public Library AVFormat { get; } = new Library(Names.AVFormat, Versions.AVFormat);

        /// <summary>
        /// Gets the <c>avutil</c> library.
        /// </summary>
        public Library AVUtil { get; } = new Library(Names.AVUtil, Versions.AVUtil);

        /// <summary>
        /// Gets the <c>swresample</c> library.
        /// </summary>
        public Library SWResample { get; } = new Library(Names.SWResample, Versions.SWResample);

        /// <summary>
        /// Gets the <c>swscale</c> library.
        /// </summary>
        public Library SWScale { get; } = new Library(Names.SWScale, Versions.SWScale);

        /// <summary>
        /// Gets all FFmpeg libraries instances.
        /// </summary>
        public IReadOnlyCollection<Library> All { get; }

        /// <summary>
        /// Gets a value indicating whether all FFMpeg libraries are loaded correctly.
        /// </summary>
        public bool IsLoaded => All.All(x => x.IsLoaded);

        /// <summary>
        /// Loads all FFmpeg libraries.
        /// </summary>
        /// <param name="dir">Path to the directory containing FFmpeg assembles.</param>
        internal void LoadAll(string dir)
        {
            foreach (var lib in All)
            {
                lib.Load(dir);
            }
        }

        private class Names
        {
            public const string AVCodec = "avcodec";
            public const string AVFormat = "avformat";
            public const string AVUtil = "avutil";
            public const string SWResample = "swresample";
            public const string SWScale = "swscale";
        }

        private class Versions
        {
            public const int AVCodec = 58;
            public const int AVFormat = 58;
            public const int AVUtil = 56;
            public const int SWResample = 3;
            public const int SWScale = 5;
        }
    }
}

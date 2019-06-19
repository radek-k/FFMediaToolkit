namespace FFMediaToolkit.Encoding
{
    using System;
    using System.IO;
    using FFMediaToolkit.Encoding.Internal;

    /// <summary>
    /// Represents a multimedia file creator.
    /// </summary>
    public class MediaBuilder
    {
        private readonly OutputContainer container;
        private readonly string outputPath;
        private VideoEncoderSettings videoSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaBuilder"/> class.
        /// </summary>
        /// <param name="path">The output media file path.</param>
        public MediaBuilder(string path)
        {
            if (!Path.IsPathRooted(path))
                throw new ArgumentException($"The path \"{path}\" is not valid");

            container = OutputContainer.Create(path);
            outputPath = path;
        }

        /// <summary>
        /// Adds a new video stream to the file.
        /// </summary>
        /// <param name="settings">The video stream settings.</param>
        /// <returns>This <see cref="MediaBuilder"/> object.</returns>
        public MediaBuilder WithVideo(VideoEncoderSettings settings)
        {
            container.AddVideoStream(settings);
            videoSettings = settings;
            return this;
        }

        // TODO: Audio encoding

        /// <summary>
        /// Creates a multimedia file for specified video stream.
        /// </summary>
        /// <returns>A new <see cref="MediaOutput"/>.</returns>
        public MediaOutput Create()
        {
            container.CreateFile(outputPath);

            return new MediaOutput(container, videoSettings);
        }
    }
}

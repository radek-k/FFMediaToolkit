namespace FFMediaToolkit
{
    using System;
    using FFmpeg.AutoGen;
    using Helpers;

    /// <summary>
    /// Represents a bitmap configuration
    /// </summary>
    public struct Layout : IEquatable<Layout>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Layout"/> struct.
        /// </summary>
        /// <param name="pixelFormat">The pixel format</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        public Layout(AVPixelFormat pixelFormat, int width, int height)
        {
            PixelFormat = pixelFormat;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the image pixel format
        /// </summary>
        public AVPixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the image width
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the image height
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the image line size
        /// </summary>
        public int Stride => Scaler.GetStride(Width, (ImagePixelFormat)PixelFormat);

        public static bool operator ==(Layout left, Layout right) => left.Equals(right);

        public static bool operator !=(Layout left, Layout right) => !(left == right);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Layout layout && Equals(layout);

        /// <inheritdoc/>
        public bool Equals(Layout other) => PixelFormat == other.PixelFormat && Width == other.Width && Height == other.Height;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -235648583;
            hashCode = (hashCode * -1521134295) + PixelFormat.GetHashCode();
            hashCode = (hashCode * -1521134295) + Width.GetHashCode();
            hashCode = (hashCode * -1521134295) + Height.GetHashCode();
            return hashCode;
        }
    }
}

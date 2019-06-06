namespace FFMediaToolkit.Graphics
{
    using System;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents a bitmap configuration.
    /// </summary>
    public struct Layout : IEquatable<Layout>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Layout"/> struct.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        public Layout(AVPixelFormat pixelFormat, int width, int height)
        {
            PixelFormat = pixelFormat;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the image pixel format.
        /// </summary>
        public AVPixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the image width.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the image height.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the image line size.
        /// </summary>
        public int Stride => Scaler.EstimateStride(Width, (ImagePixelFormat)PixelFormat);

        /// <summary>
        /// Checks if the <paramref name="left"/> value equals the <paramref name="right"/> value.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true"/>, if equals.</returns>
        public static bool operator ==(Layout left, Layout right) => left.Equals(right);

        /// <summary>
        /// Checks if the <paramref name="left"/> value not equals the <paramref name="right"/> value.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns><see langword="true"/>, if not equals.</returns>
        public static bool operator !=(Layout left, Layout right) => !(left == right);

        /// <summary>
        /// Indicates whether the width and height of this layout are equal to the other.
        /// </summary>
        /// <param name="other">The other <see cref="Layout"/> object to compare.</param>
        /// <returns><see langword="true"/> if equal, otherwise <see langword="false"/>.</returns>
        public bool SizeEquals(Layout other) => Width == other.Width && Height == other.Height;

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

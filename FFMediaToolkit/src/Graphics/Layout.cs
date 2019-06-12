namespace FFMediaToolkit.Graphics
{
    using System;
    using System.Drawing;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents bitmap size and pixel format.
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
            Size = new Size(width, height);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Layout"/> struct.
        /// </summary>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <param name="size">The size.</param>
        public Layout(AVPixelFormat pixelFormat, Size size)
        {
            PixelFormat = pixelFormat;
            Size = size;
        }

        /// <summary>
        /// Gets the image pixel format.
        /// </summary>
        public AVPixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the image width.
        /// </summary>
        public int Width => Size.Width;

        /// <summary>
        /// Gets the image height.
        /// </summary>
        public int Height => Size.Height;

        /// <summary>
        /// Gets the image size.
        /// </summary>
        public Size Size { get; }

        /// <summary>
        /// Gets the estimated image linesize.
        /// </summary>
        public int Stride => EstimateStride(Width, (ImagePixelFormat)PixelFormat);

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
        /// Gets the estimated image line size based on the pixel format and width.
        /// </summary>
        /// <param name="width">The image width.</param>
        /// <param name="format">The image pixel format.</param>
        /// <returns>The size of a single line of the image measured in bytes.</returns>
        public static int EstimateStride(int width, ImagePixelFormat format) => GetBytesPerPixel(format) * width;

        /// <summary>
        /// Indicates whether the width and height of this layout are equal to the other.
        /// </summary>
        /// <param name="other">The other <see cref="Layout"/> object to compare.</param>
        /// <returns><see langword="true"/> if equal, otherwise <see langword="false"/>.</returns>
        public bool SizeEquals(Layout other) => Size == other.Size;

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is Layout layout && Equals(layout);

        /// <inheritdoc/>
        public bool Equals(Layout other) => PixelFormat == other.PixelFormat && Size == other.Size;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -235648583;
            hashCode = (hashCode * -1521134295) + PixelFormat.GetHashCode();
            hashCode = (hashCode * -1521134295) + Width.GetHashCode();
            hashCode = (hashCode * -1521134295) + Height.GetHashCode();
            return hashCode;
        }

        private static int GetBytesPerPixel(ImagePixelFormat format)
        {
            switch (format)
            {
                case ImagePixelFormat.BGR24:
                    return 3;
                case ImagePixelFormat.BGRA32:
                    return 4;
                case ImagePixelFormat.RGB24:
                    return 3;
                case ImagePixelFormat.ARGB32:
                    return 4;
                default:
                    return 0;
            }
        }
    }
}

namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Decoding.Internal;
    using FFMediaToolkit.Helpers;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Represents informations about the video stream.
    /// </summary>
    public class VideoStreamInfo : StreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoStreamInfo"/> class.
        /// </summary>
        /// <param name="stream">A generic stream.</param>
        /// <param name="container">The input container.</param>
        internal unsafe VideoStreamInfo(AVStream* stream, InputContainer container)
             : base(stream, MediaType.Video, container)
        {
            var codec = stream->codecpar;
            IsInterlaced = codec->field_order != AVFieldOrder.AV_FIELD_PROGRESSIVE &&
                           codec->field_order != AVFieldOrder.AV_FIELD_UNKNOWN;
            FrameSize = new Size(codec->width, codec->height);
            PixelFormat = ((AVPixelFormat)codec->format).FormatEnum(11);
            AVPixelFormat = (AVPixelFormat)codec->format;

            var matrix = (IntPtr)ffmpeg.av_stream_get_side_data(stream, AVPacketSideDataType.AV_PKT_DATA_DISPLAYMATRIX, null);
            Rotation = CalculateRotation(matrix);
        }

        /// <summary>
        /// Gets the clockwise rotation angle computed from the display matrix.
        /// </summary>
        public double Rotation { get; }

        /// <summary>
        /// Gets a value indicating whether the frames in the stream are interlaced.
        /// </summary>
        public bool IsInterlaced { get; }

        /// <summary>
        /// Gets the video frame dimensions.
        /// </summary>
        public Size FrameSize { get; }

        /// <summary>
        /// Gets a lowercase string representing the video pixel format.
        /// </summary>
        public string PixelFormat { get; }

        /// <summary>
        /// Gets the video pixel format.
        /// </summary>
        internal AVPixelFormat AVPixelFormat { get; }

        private static double CalculateRotation(IntPtr displayMatrix)
        {
            const int matrixLength = 9;

            if (displayMatrix == IntPtr.Zero)
                return 0;

            var matrix = new int[matrixLength];
            Marshal.Copy(displayMatrix, matrix, 0, matrixLength);

            var scale = new double[2];
            scale[0] = (matrix[0] != 0 && matrix[3] != 0) ? CalculateHypotenuse(matrix[0], matrix[3]) : 1;
            scale[1] = (matrix[1] != 0 && matrix[4] != 0) ? CalculateHypotenuse(matrix[1], matrix[4]) : 1;

            var rotation = Math.Atan2(matrix[1] / scale[1], matrix[0] / scale[0]) * 180 / Math.PI;
            rotation -= 360 * Math.Floor((rotation / 360) + (0.9 / 360));

            return rotation;
        }

        private static double CalculateHypotenuse(int a, int b) => Math.Sqrt((a * a) + (b * b));
    }
}

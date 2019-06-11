namespace FFMediaToolkit.Helpers
{
    using System;
    using FFMediaToolkit.Common;
    using FFMediaToolkit.Common.Internal;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains some extensions methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts a rational number to a double.
        /// </summary>
        /// <param name="rational">The <see cref="AVRational"/> to convert.</param>
        /// <returns>The <see cref="double"/> value.</returns>
        public static double ToDouble(this AVRational rational)
            => rational.den == 0 ? 0 : Convert.ToDouble(rational.num) / Convert.ToDouble(rational.den);

        /// <summary>
        /// Converts the given <paramref name="timestamp"/> in the <paramref name="timeBase"/> units to a <see cref="TimeSpan"/> object.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <param name="timeBase">The time base unit.</param>
        /// <returns>The converted <see cref="TimeSpan"/>.</returns>
        public static TimeSpan ToTimeSpan(this long timestamp, AVRational timeBase)
        {
            var ts = Convert.ToDouble(timestamp);

            // if (Math.Abs(ts - ffmpeg.AV_NOPTS_VALUE) == 0)
            // {
            //    return TimeSpan.MinValue;
            // }

            return TimeSpan.FromTicks(Convert.ToInt64(TimeSpan.TicksPerMillisecond * 1000 * ts / timeBase.ToDouble()));
        }

        /// <summary>
        /// Converts the frame number to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="frameNumber">The frame number.</param>
        /// <param name="fps">The stream frame rate.</param>
        /// <returns>The converted <see cref="TimeSpan"/>.</returns>
        public static TimeSpan ToTimeSpan(this int frameNumber, double fps) => TimeSpan.FromMilliseconds(frameNumber * (1000 / fps));

        /// <summary>
        /// Converts this <see cref="TimeSpan"/> to a frame number based on the specified frame rate/>.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="framerate">The stream frame rate.</param>
        /// <returns>The frame number.</returns>
        public static int ToFrameNumber(this TimeSpan time, double framerate)
            => (int)(time.Milliseconds / framerate);

        /// <summary>
        /// Converts this frame number to a timestamp in the <paramref name="timeBase"/> units.
        /// </summary>
        /// <param name="frameNumber">The frame number.</param>
        /// <param name="timeBase">The stream time base.</param>
        /// <returns>The timestamp.</returns>
        public static long ToTimestamp(this int frameNumber, AVRational timeBase)
            => timeBase.den == 0 ? 0 : timeBase.num * frameNumber / timeBase.den;

        /// <summary>
        /// Converts the <see cref="TimeSpan"/> to a timestamp in the <paramref name="timeBase"/> units.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="timeBase">The stream time base.</param>
        /// <returns>The timestamp.</returns>
        public static long ToTimestamp(this TimeSpan time, AVRational timeBase)
            => timeBase.num == 0 ? 0 : Convert.ToInt64(time.TotalSeconds * timeBase.den / timeBase.num);

        /// <summary>
        /// Gets the type of content in the <see cref="AVFrame"/>.
        /// </summary>
        /// <param name="frame">The <see cref="AVFrame"/>.</param>
        /// <returns>The type of frame content.</returns>
        internal static MediaType GetMediaType(this AVFrame frame)
        {
            if (frame.width > 0 && frame.height > 0)
            {
                return MediaType.Video;
            }

            if (frame.channels > 0)
            {
                return MediaType.Audio;
            }

            return MediaType.None;
        }
    }
}

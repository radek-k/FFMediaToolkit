namespace FFMediaToolkit.Helpers
{
    using System;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains extension methods for math types.
    /// </summary>
    internal static class MathHelper
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
            => TimeSpan.FromTicks(timestamp * TimeSpan.TicksPerSecond * timeBase.num / timeBase.den);

        /// <summary>
        /// Converts this <see cref="TimeSpan"/> to a frame number based on the specified frame rate/>.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="framerate">The stream frame rate.</param>
        /// <returns>The frame number.</returns>
        public static int ToFrameNumber(this TimeSpan time, AVRational framerate)
            => framerate.den == 0 ? 0 : (int)(time.Ticks * framerate.num / (framerate.den * TimeSpan.TicksPerSecond));

        /// <summary>
        /// Converts the <see cref="TimeSpan"/> to a timestamp in the <paramref name="timeBase"/> units.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="timeBase">The stream time base.</param>
        /// <returns>The timestamp.</returns>
        public static long ToTimestamp(this TimeSpan time, AVRational timeBase)
            => time.Ticks * timeBase.den / (timeBase.num * TimeSpan.TicksPerSecond);
    }
}

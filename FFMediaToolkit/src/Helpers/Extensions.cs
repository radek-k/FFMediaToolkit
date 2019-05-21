namespace FFMediaToolkit.Helpers
{
    using System;
    using FFmpeg.AutoGen;

    /// <summary>
    /// Contains some extensions methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Converts a rational number to a double
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
        /// <returns>The converted <see cref="TimeSpan"/></returns>
        public static TimeSpan ToTimeSpan(this long timestamp, AVRational timeBase)
        {
            var ts = Convert.ToDouble(timestamp);

            if (Math.Abs(ts - ffmpeg.AV_NOPTS_VALUE) <= 0)
            {
                return TimeSpan.MinValue;
            }

            return TimeSpan.FromTicks(Convert.ToInt64(TimeSpan.TicksPerMillisecond * 1000 * ts / timeBase.ToDouble()));
        }
    }
}

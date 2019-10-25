namespace FFMediaToolkit.Decoding
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Refers to a video chapter.
    /// </summary>
    public class StreamChapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamChapter"/> class.
        /// </summary>
        /// <param name="start">The starting time of this chapter.</param>
        /// <param name="end">The ending time of this chapter.</param>
        /// <param name="metadata">This chapter's metadata.</param>
        internal StreamChapter(TimeSpan start, TimeSpan end, Dictionary<string, string> metadata)
        {
            Start = start;
            End = end;
            Metadata = metadata;
        }

        /// <summary>
        /// Gets the starting time of this chapter.
        /// </summary>
        public TimeSpan Start { get; }

        /// <summary>
        /// Gets the ending time of this chapter.
        /// </summary>
        public TimeSpan End { get; }

        /// <summary>
        /// Gets the metadata for this chapter (such as name).
        /// </summary>
        public Dictionary<string, string> Metadata { get; }
    }
}
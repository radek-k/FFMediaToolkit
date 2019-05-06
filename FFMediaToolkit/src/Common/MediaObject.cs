namespace FFMediaToolkit.Common
{
    using System;

    /// <summary>
    /// Represents a multimedia codec acces mode
    /// </summary>
    public enum MediaAccess
    {
        /// <summary>
        /// When media is in the decoding mode
        /// </summary>
        Read,

        /// <summary>
        /// When media is in the encoding mode
        /// </summary>
        Write,

        /// <summary>
        /// When a media is in the encoding mode, but not yet configured for writing file
        /// </summary>
        WriteInit
    }

    /// <summary>
    /// A base class for multimedia objects that use acces checking.
    /// </summary>
    public abstract class MediaObject
    {
        /// <summary>
        /// Gets the current acces mode to the object.
        /// </summary>
        public virtual MediaAccess Access { get; protected set; }

        /// <summary>
        /// Checks whether the current <see cref="MediaAccess"/> mode matches the required one. If not, throws an <see cref="InvalidOperationException"/>
        /// </summary>
        /// <param name="access">The required <see cref="MediaAccess"/>.</param>
        protected void CheckAccess(MediaAccess access)
        {
            if (Access != access)
            {
                throw new InvalidOperationException($"This operation requires {access.ToString()} access, but the current is {Access.ToString()}");
            }
        }
    }
}

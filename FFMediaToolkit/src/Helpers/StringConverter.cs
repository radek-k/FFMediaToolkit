namespace FFMediaToolkit.Helpers
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Contains string conversion methods.
    /// </summary>
    public static class StringConverter
    {
        /// <summary>
        /// Creates a new <see cref="string"/> from a pointer to the unmanaged UTF-8 string.
        /// </summary>
        /// <param name="pointer">A pointer to the umanaged string.</param>
        /// <returns>The converted string.</returns>
        public static string StringFromUtf8(IntPtr pointer)
        {
            var lenght = 0;

            while (Marshal.ReadByte(pointer, lenght) != 0)
            {
                ++lenght;
            }

            var buffer = new byte[lenght];
            Marshal.Copy(pointer, buffer, 0, lenght);

            return Encoding.UTF8.GetString(buffer);
        }
    }
}

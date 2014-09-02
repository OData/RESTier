namespace System
{
    internal static partial class Ensure
    {
        public static void NotNull<T>(T? value, string paramName = null)
            where T : struct
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        public static void NotNull<T>(T value, string paramName = null)
            where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }
    }
}

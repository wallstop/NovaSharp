namespace NovaSharp.Cli
{
    using System;

    /// <summary>
    /// Provides helper methods for argument validation across frameworks.
    /// </summary>
    internal static class ArgumentValidation
    {
        public static void ThrowIfNull(object value, string paramName)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(value, paramName);
#else
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
#endif
        }
    }
}

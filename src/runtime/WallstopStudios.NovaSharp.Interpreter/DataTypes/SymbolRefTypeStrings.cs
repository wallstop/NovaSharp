namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;

    /// <summary>
    /// Provides cached string representations for <see cref="SymbolRefType"/> values
    /// to avoid allocations from ToString() calls.
    /// </summary>
    internal static class SymbolRefTypeStrings
    {
        private static readonly string[] Names =
        {
            "Unknown", // 0
            "Local", // 1
            "UpValue", // 2
            "Global", // 3
            "DefaultEnv", // 4
        };

        /// <summary>
        /// Gets the cached string name for the specified <see cref="SymbolRefType"/>.
        /// </summary>
        /// <param name="type">The symbol reference type.</param>
        /// <returns>The string representation of the type.</returns>
        public static string GetName(SymbolRefType type)
        {
            int index = (int)type;
            if (index >= 0 && index < Names.Length)
            {
                return Names[index];
            }
            return type.ToString();
        }
    }
}

namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;

    /// <summary>
    /// Provides cached string representations for <see cref="ModLoadState"/> values
    /// to avoid allocations from ToString() calls.
    /// </summary>
    internal static class ModLoadStateStrings
    {
        private static readonly string[] Names =
        {
            "Unknown", // 0
            "Unloaded", // 1
            "Loading", // 2
            "Loaded", // 3
            "Unloading", // 4
            "Reloading", // 5
            "Faulted", // 6
        };

        /// <summary>
        /// Gets the cached string name for the specified <see cref="ModLoadState"/>.
        /// </summary>
        /// <param name="state">The mod load state.</param>
        /// <returns>The string representation of the state.</returns>
        public static string GetName(ModLoadState state)
        {
            int index = (int)state;
            if (index >= 0 && index < Names.Length)
            {
                return Names[index];
            }
            return state.ToString();
        }
    }
}

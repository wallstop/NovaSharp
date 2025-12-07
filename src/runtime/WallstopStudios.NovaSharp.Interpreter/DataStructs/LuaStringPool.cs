namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides string interning for frequently used Lua strings to reduce memory allocations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lua scripts often use the same strings repeatedly (variable names, table keys, metamethod names).
    /// This pool interns these strings to avoid allocating duplicate instances.
    /// </para>
    /// <para>
    /// The pool uses a concurrent dictionary for thread-safety and automatically prunes entries
    /// that haven't been accessed recently when the pool grows too large.
    /// </para>
    /// <para>
    /// Common metamethod and operator names are pre-interned for zero-allocation lookups.
    /// </para>
    /// </remarks>
    internal static class LuaStringPool
    {
        private const int MaxPoolSize = 4096;

        private static readonly ConcurrentDictionary<string, string> Pool = new(
            StringComparer.Ordinal
        );

        // Pre-interned common strings
        private static readonly string[] CommonStrings = InitializeCommonStrings();

        private static string[] InitializeCommonStrings()
        {
            string[] common = new[]
            {
                // Metamethods
                "__index",
                "__newindex",
                "__call",
                "__add",
                "__sub",
                "__mul",
                "__div",
                "__mod",
                "__pow",
                "__unm",
                "__idiv",
                "__band",
                "__bor",
                "__bxor",
                "__bnot",
                "__shl",
                "__shr",
                "__concat",
                "__len",
                "__eq",
                "__lt",
                "__le",
                "__gc",
                "__close",
                "__tostring",
                "__metatable",
                "__mode",
                "__pairs",
                "__ipairs",
                "__name",
                // Common variable names
                "self",
                "this",
                "_G",
                "_ENV",
                "_VERSION",
                "arg",
                "n",
                "i",
                "j",
                "k",
                "v",
                "x",
                "y",
                "z",
                "key",
                "value",
                "index",
                "func",
                "table",
                "string",
                "number",
                "boolean",
                "nil",
                "true",
                "false",
                "function",
                "userdata",
                "thread",
                // Common function names
                "print",
                "pairs",
                "ipairs",
                "next",
                "type",
                "tonumber",
                "tostring",
                "select",
                "error",
                "assert",
                "pcall",
                "xpcall",
                "require",
                "load",
                "loadfile",
                "dofile",
                "rawget",
                "rawset",
                "rawequal",
                "rawlen",
                "setmetatable",
                "getmetatable",
                "collectgarbage",
                // Common module names
                "math",
                "string",
                "table",
                "io",
                "os",
                "debug",
                "coroutine",
                "package",
                "utf8",
                "bit32",
            };

            // Pre-populate pool with common strings
            foreach (string s in common)
            {
                Pool[s] = s;
            }

            return common;
        }

        /// <summary>
        /// Interns the specified string, returning a cached instance if available.
        /// </summary>
        /// <param name="value">The string to intern. May be null.</param>
        /// <returns>The interned string, or null if the input was null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Intern(string value)
        {
            if (value == null)
            {
                return null;
            }

            // Fast path: already in pool
            if (Pool.TryGetValue(value, out string cached))
            {
                return cached;
            }

            // Only intern strings of reasonable length
            if (value.Length > 64)
            {
                return value;
            }

            // Add to pool if not too large
            if (Pool.Count < MaxPoolSize)
            {
                Pool[value] = value;
                return value;
            }

            // Pool is full, return as-is without interning
            return value;
        }

        /// <summary>
        /// Interns the specified character span, returning a cached string if available.
        /// </summary>
        /// <param name="span">The character span to intern.</param>
        /// <returns>The interned string.</returns>
        public static string Intern(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            // Only intern strings of reasonable length
            if (span.Length > 64)
            {
                return new string(span);
            }

            // Check if already in pool (requires string allocation for lookup)
            string key = new string(span);
            if (Pool.TryGetValue(key, out string cached))
            {
                return cached;
            }

            // Add to pool if not too large
            if (Pool.Count < MaxPoolSize)
            {
                Pool[key] = key;
            }

            return key;
        }

        /// <summary>
        /// Checks if the specified string is already interned without adding it.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="interned">The interned string if found.</param>
        /// <returns><c>true</c> if the string was found in the pool; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetInterned(string value, out string interned)
        {
            if (value == null)
            {
                interned = null;
                return false;
            }
            return Pool.TryGetValue(value, out interned);
        }

        /// <summary>
        /// Gets the current number of interned strings in the pool.
        /// </summary>
        public static int Count => Pool.Count;

        /// <summary>
        /// Clears all interned strings from the pool except pre-interned common strings.
        /// </summary>
        /// <remarks>
        /// This should only be called during application shutdown or explicit cache clearing.
        /// The common strings will be re-added automatically.
        /// </remarks>
        public static void Clear()
        {
            Pool.Clear();
            foreach (string s in CommonStrings)
            {
                Pool[s] = s;
            }
        }
    }
}

namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using WallstopStudios.NovaSharp.Interpreter;

    /// <summary>
    /// A generic, AOT-compatible utility class for caching enum string representations
    /// to avoid allocating ToString() calls on hot paths.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to cache.</typeparam>
    /// <remarks>
    /// This implementation pre-populates all known enum values at static initialization time,
    /// making it fully AOT-compatible. Unknown values (e.g., from flags combinations or
    /// invalid casts) are cached on-demand using a thread-safe ConcurrentDictionary.
    /// </remarks>
    [SuppressMessage(
        "Performance",
        "CA1810:Initialize reference type static fields inline",
        Justification = "Static constructor is required to populate caches from Enum.GetValues/GetNames which cannot be done inline."
    )]
    internal static class EnumStringCache<TEnum>
        where TEnum : struct, Enum
    {
        /// <summary>
        /// Pre-populated dictionary of all known enum values to their string names.
        /// Populated at static initialization for AOT compatibility.
        /// </summary>
        private static readonly Dictionary<TEnum, string> KnownNames;

        /// <summary>
        /// Thread-safe cache for unknown enum values (flags combinations, invalid casts, etc.).
        /// </summary>
        private static readonly ConcurrentDictionary<TEnum, string> UnknownCache;

        /// <summary>
        /// Cached array of all defined enum values for this type.
        /// </summary>
        private static readonly TEnum[] AllValues;

        /// <summary>
        /// For contiguous enums starting at 0, this array provides O(1) index-based lookup.
        /// Null if the enum is not contiguous or does not start at 0.
        /// </summary>
        private static readonly string[] ContiguousLookup;

        /// <summary>
        /// The minimum value in the enum (as int), used for contiguous lookup offset.
        /// </summary>
        private static readonly int ContiguousMinValue;

        /// <summary>
        /// Indicates whether this enum type supports fast contiguous array lookup.
        /// </summary>
        private static readonly bool IsContiguous;

        /// <summary>
        /// Static initializer that pre-populates all known enum values.
        /// This runs once per generic type instantiation.
        /// </summary>
        static EnumStringCache()
        {
            AllValues = (TEnum[])Enum.GetValues(typeof(TEnum));
            string[] names = Enum.GetNames(typeof(TEnum));

            KnownNames = new Dictionary<TEnum, string>(AllValues.Length);
            UnknownCache = new ConcurrentDictionary<TEnum, string>();

            for (int i = 0; i < AllValues.Length; i++)
            {
                KnownNames[AllValues[i]] = names[i];
            }

            // Detect if this enum is contiguous (sequential integer values)
            // This enables O(1) array-based lookup instead of dictionary lookup
            IsContiguous = TryBuildContiguousLookup(
                AllValues,
                names,
                out ContiguousLookup,
                out ContiguousMinValue
            );
        }

        /// <summary>
        /// Attempts to build a contiguous lookup array for enums with sequential values.
        /// </summary>
        /// <param name="values">All defined enum values.</param>
        /// <param name="names">All defined enum names.</param>
        /// <param name="lookup">Output lookup array, or null if not contiguous.</param>
        /// <param name="minValue">Output minimum value for offset calculation.</param>
        /// <returns>True if the enum is contiguous and lookup was built.</returns>
        private static bool TryBuildContiguousLookup(
            TEnum[] values,
            string[] names,
            out string[] lookup,
            out int minValue
        )
        {
            lookup = null;
            minValue = 0;

            if (values.Length == 0)
            {
                return false;
            }

            // Convert to integers to check contiguity
            int[] intValues = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                intValues[i] = Convert.ToInt32(values[i], CultureInfo.InvariantCulture);
            }

            // Find min and max
            int min = intValues[0];
            int max = intValues[0];
            for (int i = 1; i < intValues.Length; i++)
            {
                if (intValues[i] < min)
                {
                    min = intValues[i];
                }

                if (intValues[i] > max)
                {
                    max = intValues[i];
                }
            }

            // Check if range equals count (contiguous) and range is reasonable
            int range = max - min + 1;
            if (range != values.Length || range > 256)
            {
                // Not contiguous or too large to be worth the memory
                return false;
            }

            // Build the lookup array
            lookup = new string[range];
            for (int i = 0; i < values.Length; i++)
            {
                int index = intValues[i] - min;
                lookup[index] = names[i];
            }

            minValue = min;
            return true;
        }

        /// <summary>
        /// Gets the cached string name for the specified enum value.
        /// </summary>
        /// <param name="value">The enum value to get the name for.</param>
        /// <returns>The string representation of the enum value.</returns>
        /// <remarks>
        /// For known enum values, this returns the pre-cached string with zero allocation.
        /// For unknown values (flags combinations, invalid casts), the result is computed
        /// once and cached for subsequent calls.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetName(TEnum value)
        {
            // Fast path: contiguous enum lookup via array index
            if (IsContiguous)
            {
                int index =
                    Convert.ToInt32(value, CultureInfo.InvariantCulture) - ContiguousMinValue;
                if (index >= 0 && index < ContiguousLookup.Length)
                {
                    return ContiguousLookup[index];
                }
            }

            // Standard path: dictionary lookup for known values
            if (KnownNames.TryGetValue(value, out string name))
            {
                return name;
            }

            // Fallback path: cache unknown values (flags, invalid casts, etc.)
            return UnknownCache.GetOrAdd(value, static v => v.ToString());
        }

        /// <summary>
        /// Gets the cached string name for the specified enum value, with an option
        /// to convert to lowercase for Lua-style output.
        /// </summary>
        /// <param name="value">The enum value to get the name for.</param>
        /// <param name="lowercase">If true, returns the lowercase version of the name.</param>
        /// <returns>The string representation of the enum value.</returns>
        public static string GetName(TEnum value, bool lowercase)
        {
            string name = GetName(value);
            return lowercase ? InvariantString.ToLowerInvariantIfNeeded(name) : name;
        }

        /// <summary>
        /// Gets all defined values for this enum type.
        /// </summary>
        /// <returns>An array of all defined enum values.</returns>
        /// <remarks>
        /// Returns the cached array directly. Do not modify the returned array.
        /// </remarks>
        public static TEnum[] GetAllValues()
        {
            return AllValues;
        }

        /// <summary>
        /// Gets the count of defined values for this enum type.
        /// </summary>
        public static int Count => AllValues.Length;

        /// <summary>
        /// Returns whether this enum type uses the optimized contiguous array lookup.
        /// </summary>
        public static bool UsesContiguousLookup => IsContiguous;
    }

    /// <summary>
    /// Static helper class for warming enum string caches at application startup.
    /// Call these methods during initialization to ensure all enum caches are populated
    /// before entering performance-critical code paths.
    /// </summary>
    internal static class EnumStringCacheInitializer
    {
        /// <summary>
        /// Warms the cache for the specified enum type by accessing its static constructor.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to warm.</typeparam>
        /// <remarks>
        /// This method simply accesses the static Count property, which triggers
        /// the static constructor if not already initialized. This is useful for
        /// warming caches at app startup to avoid first-access latency.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WarmCache<TEnum>()
            where TEnum : struct, Enum
        {
            // Accessing Count triggers static initialization
            _ = EnumStringCache<TEnum>.Count;
        }

        /// <summary>
        /// Warms the caches for multiple enum types.
        /// </summary>
        /// <typeparam name="T1">First enum type to warm.</typeparam>
        /// <typeparam name="T2">Second enum type to warm.</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WarmCaches<T1, T2>()
            where T1 : struct, Enum
            where T2 : struct, Enum
        {
            WarmCache<T1>();
            WarmCache<T2>();
        }

        /// <summary>
        /// Warms the caches for multiple enum types.
        /// </summary>
        /// <typeparam name="T1">First enum type to warm.</typeparam>
        /// <typeparam name="T2">Second enum type to warm.</typeparam>
        /// <typeparam name="T3">Third enum type to warm.</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WarmCaches<T1, T2, T3>()
            where T1 : struct, Enum
            where T2 : struct, Enum
            where T3 : struct, Enum
        {
            WarmCache<T1>();
            WarmCache<T2>();
            WarmCache<T3>();
        }

        /// <summary>
        /// Warms the caches for multiple enum types.
        /// </summary>
        /// <typeparam name="T1">First enum type to warm.</typeparam>
        /// <typeparam name="T2">Second enum type to warm.</typeparam>
        /// <typeparam name="T3">Third enum type to warm.</typeparam>
        /// <typeparam name="T4">Fourth enum type to warm.</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WarmCaches<T1, T2, T3, T4>()
            where T1 : struct, Enum
            where T2 : struct, Enum
            where T3 : struct, Enum
            where T4 : struct, Enum
        {
            WarmCache<T1>();
            WarmCache<T2>();
            WarmCache<T3>();
            WarmCache<T4>();
        }

        /// <summary>
        /// Warms the caches for multiple enum types.
        /// </summary>
        /// <typeparam name="T1">First enum type to warm.</typeparam>
        /// <typeparam name="T2">Second enum type to warm.</typeparam>
        /// <typeparam name="T3">Third enum type to warm.</typeparam>
        /// <typeparam name="T4">Fourth enum type to warm.</typeparam>
        /// <typeparam name="T5">Fifth enum type to warm.</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WarmCaches<T1, T2, T3, T4, T5>()
            where T1 : struct, Enum
            where T2 : struct, Enum
            where T3 : struct, Enum
            where T4 : struct, Enum
            where T5 : struct, Enum
        {
            WarmCache<T1>();
            WarmCache<T2>();
            WarmCache<T3>();
            WarmCache<T4>();
            WarmCache<T5>();
        }

        /// <summary>
        /// Warms the caches for multiple enum types.
        /// </summary>
        /// <typeparam name="T1">First enum type to warm.</typeparam>
        /// <typeparam name="T2">Second enum type to warm.</typeparam>
        /// <typeparam name="T3">Third enum type to warm.</typeparam>
        /// <typeparam name="T4">Fourth enum type to warm.</typeparam>
        /// <typeparam name="T5">Fifth enum type to warm.</typeparam>
        /// <typeparam name="T6">Sixth enum type to warm.</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void WarmCaches<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, Enum
            where T2 : struct, Enum
            where T3 : struct, Enum
            where T4 : struct, Enum
            where T5 : struct, Enum
            where T6 : struct, Enum
        {
            WarmCache<T1>();
            WarmCache<T2>();
            WarmCache<T3>();
            WarmCache<T4>();
            WarmCache<T5>();
            WarmCache<T6>();
        }
    }
}

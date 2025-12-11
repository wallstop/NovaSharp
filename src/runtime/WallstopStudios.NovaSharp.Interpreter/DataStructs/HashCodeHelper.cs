namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Provides deterministic hash code composition using FNV-1a mixing.
    /// </summary>
    /// <remarks>
    /// FNV-1a is a non-cryptographic hash function that provides excellent distribution
    /// characteristics for hash tables. This implementation uses the 32-bit variant
    /// with standard FNV parameters (offset basis = 2166136261, prime = 16777619).
    ///
    /// Unlike <see cref="HashCode"/>, this implementation is deterministic across
    /// process boundaries and .NET versions, making it suitable for scenarios
    /// requiring stable hash codes.
    /// </remarks>
    internal static class HashCodeHelper
    {
        /// <summary>
        /// Combines one value into a deterministic hash.
        /// </summary>
        public static int HashCode<T1>(T1 param1)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines two values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2>(T1 param1, T2 param2)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines three values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2, T3>(T1 param1, T2 param2, T3 param3)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines four values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4)
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines five values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2, T3, T4, T5>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines six values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2, T3, T4, T5, T6>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines seven values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2, T3, T4, T5, T6, T7>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines eight values into a deterministic hash.
        /// </summary>
        public static int HashCode<T1, T2, T3, T4, T5, T6, T7, T8>(
            T1 param1,
            T2 param2,
            T3 param3,
            T4 param4,
            T5 param5,
            T6 param6,
            T7 param7,
            T8 param8
        )
        {
            DeterministicHashBuilder hash = default;
            hash.Add(param1);
            hash.Add(param2);
            hash.Add(param3);
            hash.Add(param4);
            hash.Add(param5);
            hash.Add(param6);
            hash.Add(param7);
            hash.Add(param8);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines hash codes for a span of values into a deterministic composite hash.
        /// </summary>
        public static int SpanHashCode<T>(ReadOnlySpan<T> values)
        {
            if (values.IsEmpty)
            {
                return 0;
            }

            DeterministicHashBuilder hash = default;
            foreach (ref readonly T value in values)
            {
                hash.Add(value);
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// Combines hash codes for all elements in an enumerable.
        /// </summary>
        public static int EnumerableHashCode<T>(IEnumerable<T> enumerable)
        {
            if (ReferenceEquals(enumerable, null))
            {
                return 0;
            }

            DeterministicHashBuilder hash = default;
            switch (enumerable)
            {
                case IReadOnlyList<T> list:
                {
                    for (int i = 0; i < list.Count; ++i)
                    {
                        hash.Add(list[i]);
                    }

                    break;
                }
                default:
                {
                    foreach (T item in enumerable)
                    {
                        hash.Add(item);
                    }

                    break;
                }
            }

            return hash.ToHashCode();
        }

        /// <summary>
        /// Creates a new hash builder for incremental hash code computation.
        /// </summary>
        /// <returns>A new <see cref="DeterministicHashBuilder"/> instance.</returns>
        public static DeterministicHashBuilder CreateBuilder()
        {
            return default;
        }
    }

    /// <summary>
    /// Lightweight deterministic hash accumulator using FNV-1a mixing.
    /// </summary>
    /// <remarks>
    /// This struct can be used directly when you need more control over the hashing process,
    /// such as conditional addition of values or integration with existing hash computation logic.
    ///
    /// Usage:
    /// <code>
    /// var hash = new DeterministicHashBuilder();
    /// hash.Add(field1);
    /// hash.Add(field2);
    /// return hash.ToHashCode();
    /// </code>
    /// </remarks>
    internal struct DeterministicHashBuilder
    {
        // FNV-1a 32-bit parameters
        private const uint Seed = 2166136261u;
        private const uint Prime = 16777619u;

        private uint _hash;
        private bool _hasContribution;
        private bool _hasNonNullContribution;

        /// <summary>
        /// Adds a value to the hash computation using cached type information for efficiency.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T value)
        {
            uint valueHash = TypeTraits<T>.GetValueHash(value, out bool hasNonNullValue);

            if (!_hasContribution)
            {
                // Defer seeding until the first value is observed so empty hashes stay at 0.
                _hash = Seed;
                _hasContribution = true;
            }

            _hash ^= valueHash;
            _hash *= Prime;

            if (hasNonNullValue)
            {
                _hasNonNullContribution = true;
            }
        }

        /// <summary>
        /// Adds an integer value directly to the hash computation.
        /// Optimized path that avoids boxing.
        /// </summary>
        /// <param name="value">The integer value to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddInt(int value)
        {
            if (!_hasContribution)
            {
                _hash = Seed;
                _hasContribution = true;
            }

            _hash ^= unchecked((uint)value);
            _hash *= Prime;
            _hasNonNullContribution = true;
        }

        /// <summary>
        /// Adds a long value directly to the hash computation.
        /// Optimized path that avoids boxing.
        /// </summary>
        /// <param name="value">The long value to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLong(long value)
        {
            if (!_hasContribution)
            {
                _hash = Seed;
                _hasContribution = true;
            }

            // Mix both halves of the long value
            _hash ^= unchecked((uint)value);
            _hash *= Prime;
            _hash ^= unchecked((uint)(value >> 32));
            _hash *= Prime;
            _hasNonNullContribution = true;
        }

        /// <summary>
        /// Adds a double value directly to the hash computation.
        /// Optimized path that avoids boxing.
        /// </summary>
        /// <param name="value">The double value to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDouble(double value)
        {
            if (!_hasContribution)
            {
                _hash = Seed;
                _hasContribution = true;
            }

            // Use BitConverter to get the exact bit representation
            long bits = BitConverter.DoubleToInt64Bits(value);
            _hash ^= unchecked((uint)bits);
            _hash *= Prime;
            _hash ^= unchecked((uint)(bits >> 32));
            _hash *= Prime;
            _hasNonNullContribution = true;
        }

        /// <summary>
        /// Computes the final hash code.
        /// </summary>
        /// <returns>The computed hash code, or 0 if no non-null values were added.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToHashCode()
        {
            if (!_hasContribution || !_hasNonNullContribution)
            {
                return 0;
            }

            return unchecked((int)_hash);
        }
    }

    /// <summary>
    /// Caches type metadata for efficient hash code computation.
    /// Static generic class ensures one instance per type T, with fields initialized once per type.
    /// </summary>
    /// <typeparam name="T">The type to cache metadata for.</typeparam>
    internal static class TypeTraits<T>
    {
        // Sentinel value for null references - golden ratio fraction for good bit distribution
        private const uint NullSentinel = 0x9E3779B9u;

        /// <summary>
        /// Cached flag indicating whether T is a value type.
        /// Evaluated once per type instantiation, avoiding repeated typeof checks.
        /// </summary>
        private static readonly bool IsValueType = typeof(T).IsValueType;

        /// <summary>
        /// Cached flag indicating whether T is exactly System.Object.
        /// When true, we need special handling because the value could be a boxed value type
        /// or any reference type, requiring polymorphic dispatch.
        /// </summary>
        private static readonly bool IsObjectType = typeof(T) == typeof(object);

        /// <summary>
        /// Cached equality comparer for type T.
        /// Avoids repeated calls to EqualityComparer&lt;T&gt;.Default which has internal caching
        /// but still incurs method call overhead.
        /// </summary>
        private static readonly EqualityComparer<T> EqualityComparer = EqualityComparer<T>.Default;

        /// <summary>
        /// Gets the hash code for a value using cached type information.
        /// </summary>
        /// <param name="value">The value to hash.</param>
        /// <param name="hasNonNullValue">Set to true if the value is non-null (or a value type).</param>
        /// <returns>The hash code for the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetValueHash(T value, out bool hasNonNullValue)
        {
            // Value types are never null - use cached flag instead of runtime typeof check
            if (IsValueType)
            {
                hasNonNullValue = true;
                return unchecked((uint)EqualityComparer.GetHashCode(value));
            }

            // Handle boxed objects specially - the runtime type may differ from T
            if (IsObjectType)
            {
                return GetBoxedObjectHash(value, out hasNonNullValue);
            }

            // Reference type null check
            if (ReferenceEquals(value, null))
            {
                hasNonNullValue = false;
                return NullSentinel;
            }

            hasNonNullValue = true;
            return unchecked((uint)EqualityComparer.GetHashCode(value));
        }

        /// <summary>
        /// Handles hash code computation for boxed objects where T is System.Object.
        /// Uses EqualityComparer&lt;object&gt;.Default for proper polymorphic dispatch.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetBoxedObjectHash(T value, out bool hasNonNullValue)
        {
            object boxed = value;

            if (boxed is null)
            {
                hasNonNullValue = false;
                return NullSentinel;
            }

            hasNonNullValue = true;
            return unchecked((uint)EqualityComparer<object>.Default.GetHashCode(boxed));
        }
    }
}

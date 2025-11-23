namespace NovaSharp.Interpreter
{
    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// LINQ helper methods
    /// </summary>
    public static class LinqHelpers
    {
        /// <summary>
        /// Converts the specified enumerable dynvalues of a given script type to objects of a given type
        /// </summary>
        /// <typeparam name="T">The desired type</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="type">The type.</param>
        public static IEnumerable<T> Convert<T>(
            this IEnumerable<DynValue> enumerable,
            DataType type
        )
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            foreach (DynValue value in enumerable)
            {
                if (value.Type == type)
                {
                    yield return value.ToObject<T>();
                }
            }
        }

        /// <summary>
        /// Filters an enumeration for items of the given script type
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="type">The script type.</param>
        public static IEnumerable<DynValue> OfDataType(
            this IEnumerable<DynValue> enumerable,
            DataType type
        )
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            foreach (DynValue value in enumerable)
            {
                if (value.Type == type)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Converts the elements to CLR objects
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        public static IEnumerable<object> AsObjects(this IEnumerable<DynValue> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            foreach (DynValue value in enumerable)
            {
                yield return value.ToObject();
            }
        }

        /// <summary>
        /// Converts the elements to CLR objects of the desired type
        /// </summary>
        /// <typeparam name="T">The desired type</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        public static IEnumerable<T> AsObjects<T>(this IEnumerable<DynValue> enumerable)
        {
            if (enumerable == null)
            {
                throw new ArgumentNullException(nameof(enumerable));
            }

            foreach (DynValue value in enumerable)
            {
                yield return value.ToObject<T>();
            }
        }
    }
}

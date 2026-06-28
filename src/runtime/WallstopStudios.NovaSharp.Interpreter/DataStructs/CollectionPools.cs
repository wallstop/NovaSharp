namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides thread-safe pooled access to <see cref="List{T}"/> instances.
    /// Lists are automatically cleared when returned to the pool.
    /// </summary>
    /// <typeparam name="T">The element type for the lists.</typeparam>
    /// <remarks>
    /// Usage pattern:
    /// <code>
    /// using (ListPool&lt;DynValue&gt;.Get(out List&lt;DynValue&gt; list))
    /// {
    ///     list.Add(value1);
    ///     list.Add(value2);
    ///     // Process list...
    /// } // Automatically cleared and returned to pool
    /// </code>
    /// </remarks>
    internal static class ListPool<T>
    {
        private static readonly GenericPool<List<T>> Pool = new(
            producer: () => new List<T>(),
            maxPoolSize: 32,
            onRelease: list => list.Clear()
        );

        /// <summary>
        /// Gets a pooled list. When disposed, the list is cleared and returned to the pool.
        /// </summary>
        /// <returns>A PooledResource wrapping the list.</returns>
        public static PooledResource<List<T>> Get()
        {
            return Pool.Get();
        }

        /// <summary>
        /// Gets a pooled list and outputs it for immediate use.
        /// </summary>
        /// <param name="list">The retrieved list instance.</param>
        /// <returns>A PooledResource wrapping the list.</returns>
        public static PooledResource<List<T>> Get(out List<T> list)
        {
            return Pool.Get(out list);
        }

        /// <summary>
        /// Gets a pooled list with at least the specified capacity.
        /// </summary>
        /// <param name="capacity">The minimum capacity for the list.</param>
        /// <param name="list">The retrieved list instance.</param>
        /// <returns>A PooledResource wrapping the list.</returns>
        public static PooledResource<List<T>> Get(int capacity, out List<T> list)
        {
            PooledResource<List<T>> pooled = Pool.Get(out list);
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
            return pooled;
        }

        /// <summary>
        /// Copies the list contents to a new exact-sized array.
        /// Useful when you need to persist the data beyond the pooled list's lifetime.
        /// </summary>
        /// <param name="list">The list to copy from.</param>
        /// <returns>A new array containing the list's elements, or Array.Empty if the list is empty.</returns>
        public static T[] ToExactArray(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                return Array.Empty<T>();
            }

            T[] result = new T[list.Count];
            list.CopyTo(result);
            return result;
        }

        /// <summary>
        /// Rents a pooled list for manual lifetime management.
        /// The caller is responsible for returning it via <see cref="Return"/>.
        /// </summary>
        /// <returns>A list instance from the pool.</returns>
        public static List<T> Rent()
        {
            Pool.Get(out List<T> list);
            return list;
        }

        /// <summary>
        /// Rents a pooled list with at least the specified capacity for manual lifetime management.
        /// The caller is responsible for returning it via <see cref="Return"/>.
        /// </summary>
        /// <param name="capacity">The minimum capacity for the list.</param>
        /// <returns>A list instance from the pool with at least the specified capacity.</returns>
        public static List<T> Rent(int capacity)
        {
            Pool.Get(out List<T> list);
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
            return list;
        }

        /// <summary>
        /// Returns a list to the pool. The list will be cleared automatically.
        /// </summary>
        /// <param name="list">The list to return to the pool.</param>
        public static void Return(List<T> list)
        {
            if (list != null)
            {
                Pool.Return(list);
            }
        }

        /// <summary>
        /// Copies the list contents to a new exact-sized array and returns the list to the pool.
        /// This is the preferred method when converting a pooled list to a final array.
        /// </summary>
        /// <param name="list">The list to copy from and return to the pool.</param>
        /// <returns>A new array containing the list's elements, or Array.Empty if the list is null or empty.</returns>
        public static T[] ToArrayAndReturn(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                Return(list);
                return Array.Empty<T>();
            }

            T[] result = new T[list.Count];
            list.CopyTo(result);
            Return(list);
            return result;
        }
    }

    /// <summary>
    /// Provides thread-safe pooled access to <see cref="HashSet{T}"/> instances.
    /// Sets are automatically cleared when returned to the pool.
    /// </summary>
    /// <typeparam name="T">The element type for the sets.</typeparam>
    internal static class HashSetPool<T>
    {
        private static readonly GenericPool<HashSet<T>> Pool = new(
            producer: () => new HashSet<T>(),
            maxPoolSize: 16,
            onRelease: set => set.Clear()
        );

        /// <summary>
        /// Gets a pooled hash set. When disposed, the set is cleared and returned to the pool.
        /// </summary>
        /// <returns>A PooledResource wrapping the hash set.</returns>
        public static PooledResource<HashSet<T>> Get()
        {
            return Pool.Get();
        }

        /// <summary>
        /// Gets a pooled hash set and outputs it for immediate use.
        /// </summary>
        /// <param name="set">The retrieved hash set instance.</param>
        /// <returns>A PooledResource wrapping the hash set.</returns>
        public static PooledResource<HashSet<T>> Get(out HashSet<T> set)
        {
            return Pool.Get(out set);
        }

        /// <summary>
        /// Rents a pooled hash set for manual lifetime management.
        /// The caller is responsible for returning it via <see cref="Return"/>.
        /// </summary>
        /// <returns>A hash set instance from the pool.</returns>
        public static HashSet<T> Rent()
        {
            Pool.Get(out HashSet<T> set);
            return set;
        }

        /// <summary>
        /// Returns a hash set to the pool. The set will be cleared automatically.
        /// </summary>
        /// <param name="set">The hash set to return to the pool.</param>
        public static void Return(HashSet<T> set)
        {
            if (set != null)
            {
                Pool.Return(set);
            }
        }
    }

    /// <summary>
    /// Provides thread-safe pooled access to <see cref="Dictionary{TKey, TValue}"/> instances.
    /// Dictionaries are automatically cleared when returned to the pool.
    /// </summary>
    /// <typeparam name="TKey">The key type for the dictionaries.</typeparam>
    /// <typeparam name="TValue">The value type for the dictionaries.</typeparam>
    internal static class DictionaryPool<TKey, TValue>
    {
        private static readonly GenericPool<Dictionary<TKey, TValue>> Pool = new(
            producer: () => new Dictionary<TKey, TValue>(),
            maxPoolSize: 16,
            onRelease: dict => dict.Clear()
        );

        /// <summary>
        /// Gets a pooled dictionary. When disposed, the dictionary is cleared and returned to the pool.
        /// </summary>
        /// <returns>A PooledResource wrapping the dictionary.</returns>
        public static PooledResource<Dictionary<TKey, TValue>> Get()
        {
            return Pool.Get();
        }

        /// <summary>
        /// Gets a pooled dictionary and outputs it for immediate use.
        /// </summary>
        /// <param name="dictionary">The retrieved dictionary instance.</param>
        /// <returns>A PooledResource wrapping the dictionary.</returns>
        public static PooledResource<Dictionary<TKey, TValue>> Get(
            out Dictionary<TKey, TValue> dictionary
        )
        {
            return Pool.Get(out dictionary);
        }
    }

    /// <summary>
    /// Provides thread-safe pooled access to <see cref="Stack{T}"/> instances.
    /// Stacks are automatically cleared when returned to the pool.
    /// </summary>
    /// <typeparam name="T">The element type for the stacks.</typeparam>
    internal static class StackPool<T>
    {
        private static readonly GenericPool<Stack<T>> Pool = new(
            producer: () => new Stack<T>(),
            maxPoolSize: 16,
            onRelease: stack => stack.Clear()
        );

        /// <summary>
        /// Gets a pooled stack. When disposed, the stack is cleared and returned to the pool.
        /// </summary>
        /// <returns>A PooledResource wrapping the stack.</returns>
        public static PooledResource<Stack<T>> Get()
        {
            return Pool.Get();
        }

        /// <summary>
        /// Gets a pooled stack and outputs it for immediate use.
        /// </summary>
        /// <param name="stack">The retrieved stack instance.</param>
        /// <returns>A PooledResource wrapping the stack.</returns>
        public static PooledResource<Stack<T>> Get(out Stack<T> stack)
        {
            return Pool.Get(out stack);
        }
    }

    /// <summary>
    /// Provides thread-safe pooled access to <see cref="Queue{T}"/> instances.
    /// Queues are automatically cleared when returned to the pool.
    /// </summary>
    /// <typeparam name="T">The element type for the queues.</typeparam>
    internal static class QueuePool<T>
    {
        private static readonly GenericPool<Queue<T>> Pool = new(
            producer: () => new Queue<T>(),
            maxPoolSize: 16,
            onRelease: queue => queue.Clear()
        );

        /// <summary>
        /// Gets a pooled queue. When disposed, the queue is cleared and returned to the pool.
        /// </summary>
        /// <returns>A PooledResource wrapping the queue.</returns>
        public static PooledResource<Queue<T>> Get()
        {
            return Pool.Get();
        }

        /// <summary>
        /// Gets a pooled queue and outputs it for immediate use.
        /// </summary>
        /// <param name="queue">The retrieved queue instance.</param>
        /// <returns>A PooledResource wrapping the queue.</returns>
        public static PooledResource<Queue<T>> Get(out Queue<T> queue)
        {
            return Pool.Get(out queue);
        }
    }
}

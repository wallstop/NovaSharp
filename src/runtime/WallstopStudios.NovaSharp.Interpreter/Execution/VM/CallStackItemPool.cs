namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;

    /// <summary>
    /// Provides pooled <see cref="CallStackItem"/> instances to reduce allocations in the VM hot path.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every function call in the interpreter creates a new <see cref="CallStackItem"/> for tracking
    /// locals, closures, error handlers, and debugging information. This pool reuses these objects
    /// to avoid heap allocations on each call.
    /// </para>
    /// <para>
    /// Usage pattern with automatic cleanup:
    /// </para>
    /// <code>
    /// using (PooledResource&lt;CallStackItem&gt; pooled = CallStackItemPool.Get(out CallStackItem item))
    /// {
    ///     item.BasePointer = currentBase;
    ///     item.ReturnAddress = returnAddr;
    ///     // ...use item...
    /// } // Automatically returned to pool here
    /// </code>
    /// </remarks>
    internal static class CallStackItemPool
    {
        private const int MaxPoolSize = 64;

        [ThreadStatic]
        private static Stack<CallStackItem> ThreadLocalPool;

        private static readonly Action<CallStackItem> ReturnToPool = item => Return(item);

        private static Stack<CallStackItem> GetPool()
        {
            Stack<CallStackItem> pool = ThreadLocalPool;
            if (pool == null)
            {
                pool = new Stack<CallStackItem>(16);
                ThreadLocalPool = pool;
            }
            return pool;
        }

        /// <summary>
        /// Gets a pooled <see cref="CallStackItem"/> and outputs it for immediate use.
        /// </summary>
        /// <param name="item">The rented call stack item, reset to default state.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that returns the item to the pool when disposed.</returns>
        public static PooledResource<CallStackItem> Get(out CallStackItem item)
        {
            Stack<CallStackItem> pool = GetPool();
            if (pool.Count > 0)
            {
                item = pool.Pop();
            }
            else
            {
                item = new CallStackItem();
            }
            return new PooledResource<CallStackItem>(item, ReturnToPool);
        }

        /// <summary>
        /// Rents a <see cref="CallStackItem"/> from the pool.
        /// </summary>
        /// <returns>A pooled or new call stack item, reset to default state.</returns>
        /// <remarks>
        /// Always pair with <see cref="Return"/> to avoid memory leaks.
        /// Prefer using the <see cref="Get(out CallStackItem)"/> method with a using statement instead.
        /// </remarks>
        public static CallStackItem Rent()
        {
            Stack<CallStackItem> pool = GetPool();
            if (pool.Count > 0)
            {
                return pool.Pop();
            }
            return new CallStackItem();
        }

        /// <summary>
        /// Returns a rented <see cref="CallStackItem"/> to the pool for reuse.
        /// </summary>
        /// <param name="item">The item to return. May be null (no-op).</param>
        public static void Return(CallStackItem item)
        {
            if (item == null)
            {
                return;
            }

            item.Reset();

            Stack<CallStackItem> pool = GetPool();
            if (pool.Count < MaxPoolSize)
            {
                pool.Push(item);
            }
            // Items beyond MaxPoolSize are discarded to prevent unbounded growth
        }
    }
}

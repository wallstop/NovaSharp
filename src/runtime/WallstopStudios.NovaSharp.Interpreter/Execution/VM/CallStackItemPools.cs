namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides pooled resources for <see cref="CallStackItem"/> to reduce allocations during function calls.
    /// </summary>
    internal static class CallStackItemPools
    {
        /// <summary>
        /// Gets a pooled list for storing blocks to close.
        /// </summary>
        public static PooledResource<List<List<SymbolRef>>> GetBlocksToCloseList(
            out List<List<SymbolRef>> list
        )
        {
            return ListPool<List<SymbolRef>>.Get(out list);
        }

        /// <summary>
        /// Gets a pooled list of SymbolRef for a single block's closers.
        /// </summary>
        public static PooledResource<List<SymbolRef>> GetClosersList(out List<SymbolRef> list)
        {
            return ListPool<SymbolRef>.Get(out list);
        }

        /// <summary>
        /// Gets a pooled list of SymbolRef with initial capacity.
        /// </summary>
        public static PooledResource<List<SymbolRef>> GetClosersList(
            int capacity,
            out List<SymbolRef> list
        )
        {
            return ListPool<SymbolRef>.Get(capacity, out list);
        }

        /// <summary>
        /// Gets a pooled hash set for tracking to-be-closed indices.
        /// </summary>
        public static PooledResource<HashSet<int>> GetToBeClosedSet(out HashSet<int> set)
        {
            return HashSetPool<int>.Get(out set);
        }

        /// <summary>
        /// Creates a pooled list containing the elements from the source array.
        /// </summary>
        /// <param name="source">The source array to copy from.</param>
        /// <param name="list">The pooled list containing the copied elements.</param>
        /// <returns>A <see cref="PooledResource{T}"/> that returns the list to the pool when disposed.</returns>
        public static PooledResource<List<SymbolRef>> GetClosersListFrom(
            SymbolRef[] source,
            out List<SymbolRef> list
        )
        {
            PooledResource<List<SymbolRef>> pooled = ListPool<SymbolRef>.Get(
                source.Length,
                out list
            );
            for (int i = 0; i < source.Length; i++)
            {
                list.Add(source[i]);
            }
            return pooled;
        }
    }
}

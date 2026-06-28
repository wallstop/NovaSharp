namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

#if USE_DYNAMIC_STACKS
    /// <summary>
    /// Wrapper that maps the fixed-stack API to the dynamic implementation when dynamic stacks are enabled.
    /// </summary>
    internal class FastStack<T> : FastStackDynamic<T>
    {
        public FastStack(int startingCapacity)
            : base(startingCapacity) { }
    }
#endif

    /// <summary>
    /// Dynamically growing stack used when fixed-capacity stacks are disabled (e.g., profiling builds).
    /// </summary>
    /// <typeparam name="T">Element type stored in the stack.</typeparam>
    internal class FastStackDynamic<T> : List<T>
    {
        public FastStackDynamic(int startingCapacity)
            : base(startingCapacity) { }

        /// <summary>
        /// Replaces the value at the given offset from the top of the stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int idxofs, T item)
        {
            this[Count - 1 - idxofs] = item;
        }

        /// <summary>
        /// Pushes an item onto the stack, returning the value for convenience.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Push(T item)
        {
            Add(item);
            return item;
        }

        /// <summary>
        /// Grows the stack by appending default values.
        /// </summary>
        public void Expand(int size)
        {
            for (int i = 0; i < size; i++)
            {
                Add(default(T));
            }
        }

        /// <summary>
        /// Zeros the slot at the specified index.
        /// </summary>
        public void Zero(int index)
        {
            this[index] = default(T);
        }

        /// <summary>
        /// Peeks at the element offset from the top without removal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek(int idxofs = 0)
        {
            return this[Count - 1 - idxofs];
        }

        /// <summary>
        /// Attempts to peek the top element.
        /// </summary>
        public bool TryPeek(out T item)
        {
            return TryPeek(0, out item);
        }

        /// <summary>
        /// Attempts to peek at the specified offset from the top.
        /// </summary>
        public bool TryPeek(int idxofs, out T item)
        {
            if (idxofs < 0 || idxofs >= Count)
            {
                item = default;
                return false;
            }

            item = this[Count - 1 - idxofs];
            return true;
        }

        /// <summary>
        /// Trims the stack so only the first <paramref name="p"/> entries remain.
        /// </summary>
        public void CropAtCount(int p)
        {
            RemoveLast(Count - p);
        }

        /// <summary>
        /// Removes the specified number of elements from the top.
        /// </summary>
        public void RemoveLast(int cnt = 1)
        {
            if (cnt == 1)
            {
                RemoveAt(Count - 1);
            }
            else
            {
                RemoveRange(Count - cnt, cnt);
            }
        }

        /// <summary>
        /// Pops the top element and returns it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            T retval = this[Count - 1];
            RemoveAt(Count - 1);
            return retval;
        }

        /// <summary>
        /// Attempts to pop the top element, returning <see langword="false"/> when empty.
        /// </summary>
        public bool TryPop(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = this[Count - 1];
            RemoveAt(Count - 1);
            return true;
        }
    }
}

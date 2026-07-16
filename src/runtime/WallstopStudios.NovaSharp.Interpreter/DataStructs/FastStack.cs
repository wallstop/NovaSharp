#if !USE_DYNAMIC_STACKS
namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using WallstopStudios.NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Array-backed stack used by the VM for hot paths. The stack grows on demand while keeping contiguous storage,
    /// up to an optional ceiling beyond which growth raises a deterministic Lua stack overflow error.
    /// </summary>
    /// <typeparam name="T">Element type stored in the stack.</typeparam>
    internal class FastStack<T> : IList<T>
    {
        private T[] _storage;
        private int _headIdx;
        private readonly int _maxCapacity;

        /// <summary>
        /// Creates an array-backed stack.
        /// </summary>
        /// <param name="startingCapacity">Initial backing-array capacity.</param>
        /// <param name="maxCapacity">
        /// Maximum backing-array capacity the stack may grow to. A value of <c>0</c> or less means unbounded growth.
        /// </param>
        public FastStack(int startingCapacity, int maxCapacity = 0)
        {
            if (maxCapacity > 0 && startingCapacity > maxCapacity)
            {
                startingCapacity = maxCapacity;
            }

            _storage = new T[startingCapacity];
            _maxCapacity = maxCapacity;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _storage[index]; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { _storage[index] = value; }
        }

        /// <summary>
        /// Pushes a value onto the stack, returning the same value for fluent usage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Push(T item)
        {
            EnsureCapacity(_headIdx + 1);
            _storage[_headIdx++] = item;
            return item;
        }

        /// <summary>
        /// Advances the head by a raw amount (used when the caller knows the slots are already initialized).
        /// </summary>
        public void Expand(int size)
        {
            if (size <= 0)
            {
                return;
            }

            EnsureCapacity(_headIdx + size);
            _headIdx += size;
        }

        private void EnsureCapacity(int requiredCapacity)
        {
            if (requiredCapacity <= _storage.Length)
            {
                return;
            }

            if (_maxCapacity > 0 && requiredCapacity > _maxCapacity)
            {
                throw ScriptRuntimeException.StackOverflow();
            }

            // Grow geometrically for amortized O(1) appends. Guard the doubling against int overflow:
            // a very large ceiling can push _storage.Length past Int32.MaxValue / 2, so fall back to the
            // exact required size (bounded by the ceiling above) instead of looping on a wrapped value.
            int newCapacity = _storage.Length == 0 ? 4 : _storage.Length * 2;
            if (newCapacity < requiredCapacity)
            {
                newCapacity = requiredCapacity;
            }

            if (_maxCapacity > 0 && newCapacity > _maxCapacity)
            {
                newCapacity = _maxCapacity;
            }

            Array.Resize(ref _storage, newCapacity);
        }

        private void Zero(int index)
        {
            _storage[index] = default(T);
        }

        /// <summary>
        /// Returns the element at the given offset from the top without removing it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek(int idxofs = 0)
        {
            return _storage[_headIdx - 1 - idxofs];
        }

        /// <summary>
        /// Tries to expose a contiguous read-only span over a range currently stored in the stack.
        /// </summary>
        public bool TryGetSpan(int from, int length, out ReadOnlySpan<T> span)
        {
            if (from < 0 || length < 0 || from > _headIdx - length)
            {
                span = default;
                return false;
            }

            span = new ReadOnlySpan<T>(_storage, from, length);
            return true;
        }

        /// <summary>
        /// Overwrites the element at the given offset from the top.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int idxofs, T item)
        {
            _storage[_headIdx - 1 - idxofs] = item;
        }

        /// <summary>
        /// Trims the stack so only the first <paramref name="p"/> entries remain.
        /// </summary>
        public void CropAtCount(int p)
        {
            RemoveLast(Count - p);
        }

        /// <summary>
        /// Removes a number of elements from the top, zeroing the slots for GC friendliness.
        /// </summary>
        public void RemoveLast(int cnt = 1)
        {
            if (cnt <= 0)
            {
                return;
            }

            if (cnt == 1)
            {
                --_headIdx;
                _storage[_headIdx] = default(T);
            }
            else
            {
                int oldhead = _headIdx;
                if (cnt > oldhead)
                {
                    throw new ArgumentOutOfRangeException(nameof(cnt));
                }

                _headIdx -= cnt;
                Array.Clear(_storage, _headIdx, cnt);
            }
        }

        /// <summary>
        /// Pops the top element off the stack and returns it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            --_headIdx;
            T retval = _storage[_headIdx];
            _storage[_headIdx] = default(T);
            return retval;
        }

        /// <summary>
        /// Clears the entire storage backing array and resets the head index.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_storage, 0, _storage.Length);
            _headIdx = 0;
        }

        /// <summary>
        /// Clears only the used portion of the storage, leaving spare capacity untouched.
        /// </summary>
        public void ClearUsed()
        {
            Array.Clear(_storage, 0, _headIdx);
            _headIdx = 0;
        }

        /// <summary>
        /// Exposes internal helpers for unit tests without relying on reflection.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Internal hook so tests can zero a specific slot without reflection.
            /// </summary>
            public static void ZeroSlot(FastStack<T> stack, int index)
            {
                stack.Zero(index);
            }
        }

        /// <summary>
        /// Gets the number of items currently stored on the stack.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _headIdx; }
        }

        /// <summary>
        /// Gets the current backing storage capacity.
        /// </summary>
        internal int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _storage.Length; }
        }

        /// <summary>
        /// Gets the maximum backing storage capacity, or <c>0</c> when growth is unbounded.
        /// </summary>
        internal int MaxCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _maxCapacity; }
        }

        int IList<T>.IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        T IList<T>.this[int index]
        {
            get { return this[index]; }
            set { this[index] = value; }
        }

        void ICollection<T>.Add(T item)
        {
            Push(item);
        }

        void ICollection<T>.Clear()
        {
            Clear();
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        int ICollection<T>.Count
        {
            get { return Count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}

#endif

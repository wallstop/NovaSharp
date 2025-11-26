namespace NovaSharp.Interpreter.DataStructs
{
#if !USE_DYNAMIC_STACKS

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Fixed-capacity, array-backed stack used by the VM for hot paths where dynamic allocation is undesirable.
    /// </summary>
    /// <typeparam name="T">Element type stored in the stack.</typeparam>
    internal class FastStack<T> : IList<T>
    {
        private readonly T[] _storage;
        private int _headIdx;

        public FastStack(int maxCapacity)
        {
            _storage = new T[maxCapacity];
        }

        public T this[int index]
        {
            get { return _storage[index]; }
            set { _storage[index] = value; }
        }

        /// <summary>
        /// Pushes a value onto the stack, returning the same value for fluent usage.
        /// </summary>
        public T Push(T item)
        {
            _storage[_headIdx++] = item;
            return item;
        }

        /// <summary>
        /// Advances the head by a raw amount (used when the caller knows the slots are already initialized).
        /// </summary>
        public void Expand(int size)
        {
            _headIdx += size;
        }

        private void Zero(int index)
        {
            _storage[index] = default(T);
        }

        /// <summary>
        /// Returns the element at the given offset from the top without removing it.
        /// </summary>
        public T Peek(int idxofs = 0)
        {
            T item = _storage[_headIdx - 1 - idxofs];
            return item;
        }

        /// <summary>
        /// Overwrites the element at the given offset from the top.
        /// </summary>
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
            get { return _headIdx; }
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

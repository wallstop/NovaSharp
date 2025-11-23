namespace NovaSharp.Interpreter.DataStructs
{
#if !USE_DYNAMIC_STACKS

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A preallocated, non-resizable, stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
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

        public T Push(T item)
        {
            _storage[_headIdx++] = item;
            return item;
        }

        public void Expand(int size)
        {
            _headIdx += size;
        }

        private void Zero(int index)
        {
            _storage[index] = default(T);
        }

        public T Peek(int idxofs = 0)
        {
            T item = _storage[_headIdx - 1 - idxofs];
            return item;
        }

        public void Set(int idxofs, T item)
        {
            _storage[_headIdx - 1 - idxofs] = item;
        }

        public void CropAtCount(int p)
        {
            RemoveLast(Count - p);
        }

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

        public T Pop()
        {
            --_headIdx;
            T retval = _storage[_headIdx];
            _storage[_headIdx] = default(T);
            return retval;
        }

        public void Clear()
        {
            Array.Clear(_storage, 0, _storage.Length);
            _headIdx = 0;
        }

        public void ClearUsed()
        {
            Array.Clear(_storage, 0, _headIdx);
            _headIdx = 0;
        }

        internal static class TestHooks
        {
            // Internal hook so tests can exercise the private clearing path without reflection.
            public static void ZeroSlot(FastStack<T> stack, int index)
            {
                stack.Zero(index);
            }
        }

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

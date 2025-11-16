namespace NovaSharp.Interpreter.DataStructs
{
    using System.Collections.Generic;

#if USE_DYNAMIC_STACKS
    internal class FastStack<T> : FastStackDynamic<T>
    {
        public FastStack(int startingCapacity)
            : base(startingCapacity) { }
    }
#endif

    /// <summary>
    /// A non preallocated, non_fixed size stack
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class FastStackDynamic<T> : List<T>
    {
        public FastStackDynamic(int startingCapacity)
            : base(startingCapacity) { }

        public void Set(int idxofs, T item)
        {
            this[Count - 1 - idxofs] = item;
        }

        public T Push(T item)
        {
            Add(item);
            return item;
        }

        public void Expand(int size)
        {
            for (int i = 0; i < size; i++)
            {
                Add(default(T));
            }
        }

        public void Zero(int index)
        {
            this[index] = default(T);
        }

        public T Peek(int idxofs = 0)
        {
            T item = this[Count - 1 - idxofs];
            return item;
        }

        public bool TryPeek(out T item)
        {
            return TryPeek(0, out item);
        }

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

        public void CropAtCount(int p)
        {
            RemoveLast(Count - p);
        }

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

        public T Pop()
        {
            T retval = this[Count - 1];
            RemoveAt(Count - 1);
            return retval;
        }

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

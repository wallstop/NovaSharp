namespace WallstopStudios.NovaSharp.Interpreter.DataStructs
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A struct-based enumerator for <see cref="Slice{T}"/> to avoid heap allocation during foreach iteration.
    /// </summary>
    /// <typeparam name="T">The type of elements in the slice.</typeparam>
    public struct SliceEnumerator<T> : IEquatable<SliceEnumerator<T>>
    {
        private readonly Slice<T> _slice;
        private int _index;

        internal SliceEnumerator(Slice<T> slice)
        {
            _slice = slice;
            _index = -1;
        }

        /// <summary>
        /// Gets the current element.
        /// </summary>
        public T Current => _slice[_index];

        /// <summary>
        /// Advances the enumerator to the next element.
        /// </summary>
        /// <returns>True if there is another element; false otherwise.</returns>
        public bool MoveNext()
        {
            _index++;
            return _index < _slice.Count;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SliceEnumerator<T> other && Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(SliceEnumerator<T> other)
        {
            return ReferenceEquals(_slice, other._slice) && _index == other._index;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCodeHelper.HashCode(_slice, _index);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        public static bool operator ==(SliceEnumerator<T> left, SliceEnumerator<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(SliceEnumerator<T> left, SliceEnumerator<T> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// Provides facility to create a "sliced" view over an existing IList<typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the items contained in the collection</typeparam>
    internal class Slice<T> : IEnumerable<T>, IList<T>
    {
        private readonly IList<T> _sourceList;

        private readonly int _from;

        private readonly int _length;

        private readonly bool _reversed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Slice{T}"/> class.
        /// </summary>
        /// <param name="list">The list to apply the Slice view on</param>
        /// <param name="from">From which index</param>
        /// <param name="length">The length of the slice</param>
        /// <param name="reversed">if set to <c>true</c> the view is in reversed order.</param>
        public Slice(IList<T> list, int from, int length, bool reversed)
        {
            _sourceList = list;
            _from = from;
            _length = length;
            _reversed = reversed;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return _sourceList[CalcRealIndex(index)]; }
            set { _sourceList[CalcRealIndex(index)] = value; }
        }

        /// <summary>
        /// Gets the index from which the slice starts
        /// </summary>
        public int From
        {
            get { return _from; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public int Count
        {
            get { return _length; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Slice{T}"/> operates in a reversed direction.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this <see cref="Slice{T}"/> operates in a reversed direction; otherwise, <c>false</c>.
        /// </value>
        public bool Reversed
        {
            get { return _reversed; }
        }

        /// <summary>
        /// Calculates the real index in the underlying collection
        /// </summary>
        private int CalcRealIndex(int index)
        {
            if (index < 0 || index >= _length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (_reversed)
            {
                return _from + _length - index - 1;
            }
            else
            {
                return _from + index;
            }
        }

        /// <summary>
        /// Returns a struct-based enumerator that iterates through the collection without heap allocation.
        /// This method is preferred by the C# compiler for foreach loops due to duck typing.
        /// </summary>
        /// <returns>A <see cref="SliceEnumerator{T}"/> struct enumerator.</returns>
        public SliceEnumerator<T> GetEnumerator()
        {
            return new SliceEnumerator<T>(this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// This explicit interface implementation boxes the struct enumerator for interface compatibility.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int i = 0; i < _length; i++)
            {
                yield return _sourceList[CalcRealIndex(i)];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < _length; i++)
            {
                yield return _sourceList[CalcRealIndex(i)];
            }
        }

        /// <summary>
        /// Converts to an array.
        /// </summary>
        public T[] ToArray()
        {
            T[] array = new T[_length];

            for (int i = 0; i < _length; i++)
            {
                array[i] = _sourceList[CalcRealIndex(i)];
            }

            return array;
        }

        /// <summary>
        /// Converts to an list.
        /// </summary>
        public List<T> ToList()
        {
            List<T> list = new(_length);

            for (int i = 0; i < _length; i++)
            {
                list.Add(_sourceList[CalcRealIndex(i)]);
            }

            return list;
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>
        /// The index of <paramref name="item" /> if found in the list; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <exception cref="System.InvalidOperationException">Slices are readonly</exception>
        public void Insert(int index, T item)
        {
            throw new InvalidOperationException("Slices are readonly");
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="System.InvalidOperationException">Slices are readonly</exception>
        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Slices are readonly");
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <exception cref="System.InvalidOperationException">Slices are readonly</exception>
        public void Add(T item)
        {
            throw new InvalidOperationException("Slices are readonly");
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Slices are readonly</exception>
        public void Clear()
        {
            throw new InvalidOperationException("Slices are readonly");
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
        /// </returns>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
            {
                array[i + arrayIndex] = this[i];
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <returns>true if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get { return true; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>
        /// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">Slices are readonly</exception>
        public bool Remove(T item)
        {
            throw new InvalidOperationException("Slices are readonly");
        }
    }
}

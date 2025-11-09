namespace NovaSharp.Interpreter.DataStructs
{
    using System.Collections.Generic;

    /// <summary>
    /// A Dictionary where multiple values can be associated to the same key
    /// </summary>
    /// <typeparam name="TK">The key type</typeparam>
    /// <typeparam name="TV">The value type</typeparam>
    internal class MultiDictionary<TK, TV>
    {
        private readonly Dictionary<TK, List<TV>> _map;
        private readonly TV[] _defaultRet = new TV[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDictionary{K, V}"/> class.
        /// </summary>
        public MultiDictionary()
        {
            _map = new Dictionary<TK, List<TV>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDictionary{K, V}"/> class.
        /// </summary>
        /// <param name="eqComparer">The equality comparer to use in the underlying dictionary.</param>
        public MultiDictionary(IEqualityComparer<TK> eqComparer)
        {
            _map = new Dictionary<TK, List<TV>>(eqComparer);
        }

        /// <summary>
        /// Adds the specified key. Returns true if this is the first value for a given key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool Add(TK key, TV value)
        {
            if (_map.TryGetValue(key, out List<TV> list))
            {
                list.Add(value);
                return false;
            }
            else
            {
                list = new List<TV>();
                list.Add(value);
                _map.Add(key, list);
                return true;
            }
        }

        /// <summary>
        /// Finds all the values associated with the specified key.
        /// An empty collection is returned if not found.
        /// </summary>
        /// <param name="key">The key.</param>
        public IEnumerable<TV> Find(TK key)
        {
            if (_map.TryGetValue(key, out List<TV> list))
            {
                return list;
            }
            else
            {
                return _defaultRet;
            }
        }

        /// <summary>
        /// Determines whether this contains the specified key
        /// </summary>
        /// <param name="key">The key.</param>
        public bool ContainsKey(TK key)
        {
            return _map.ContainsKey(key);
        }

        /// <summary>
        /// Gets the keys.
        /// </summary>
        public IEnumerable<TK> Keys
        {
            get { return _map.Keys; }
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            _map.Clear();
        }

        /// <summary>
        /// Removes the specified key and all its associated values from the multidictionary
        /// </summary>
        /// <param name="key">The key.</param>
        public void Remove(TK key)
        {
            _map.Remove(key);
        }

        /// <summary>
        /// Removes the value. Returns true if the removed value was the last of a given key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool RemoveValue(TK key, TV value)
        {
            if (_map.TryGetValue(key, out List<TV> list))
            {
                list.Remove(value);

                if (list.Count == 0)
                {
                    Remove(key);
                    return true;
                }
            }

            return false;
        }
    }
}

namespace NovaSharp.Interpreter.DataTypes
{
    /// <summary>
    /// A class representing a key/value pair for Table use
    /// </summary>
    public struct TablePair : System.IEquatable<TablePair>
    {
        private static readonly TablePair SNilNode = new(DynValue.Nil, DynValue.Nil);
        private readonly DynValue _key;

        private readonly DynValue _value;

        /// <summary>
        /// Gets the key.
        /// </summary>
        public DynValue Key => _key;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public DynValue Value => _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TablePair"/> struct.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="val">The value.</param>
        public TablePair(DynValue key, DynValue val)
        {
            _key = key;
            _value = val;
        }

        /// <summary>
        /// Gets the nil pair
        /// </summary>
        public static TablePair Nil
        {
            get { return SNilNode; }
        }

        public bool Equals(TablePair other)
        {
            return Equals(_key, other._key) && Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is TablePair pair && Equals(pair);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + (_key?.GetHashCode() ?? 0);
                hash = (hash * 31) + (_value?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static bool operator ==(TablePair left, TablePair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TablePair left, TablePair right)
        {
            return !left.Equals(right);
        }
    }
}

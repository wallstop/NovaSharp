namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using DataStructs;

    /// <summary>
    /// A class representing a key/value pair for Table use
    /// </summary>
    public struct TablePair : System.IEquatable<TablePair>
    {
        private static readonly TablePair NilNode = new(DynValue.Nil, DynValue.Nil);
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
            get { return NilNode; }
        }

        /// <summary>
        /// Determines whether two table pairs contain the same key and value.
        /// </summary>
        public bool Equals(TablePair other)
        {
            return Equals(_key, other._key) && Equals(_value, other._value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is TablePair pair && Equals(pair);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCodeHelper.HashCode(_key, _value);
        }

        /// <summary>
        /// Equality operator; delegates to <see cref="Equals(TablePair)"/>.
        /// </summary>
        public static bool operator ==(TablePair left, TablePair right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        public static bool operator !=(TablePair left, TablePair right)
        {
            return !left.Equals(right);
        }
    }
}

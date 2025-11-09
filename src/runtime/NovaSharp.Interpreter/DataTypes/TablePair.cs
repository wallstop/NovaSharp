namespace NovaSharp.Interpreter
{
    /// <summary>
    /// A class representing a key/value pair for Table use
    /// </summary>
    public struct TablePair
    {
        private static readonly TablePair SNilNode = new(DynValue.Nil, DynValue.Nil);
        private readonly DynValue _key;

        private readonly DynValue _value;

        /// <summary>
        /// Gets the key.
        /// </summary>
        public DynValue Key
        {
            get { return _key; }
            private set { Key = _key; }
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public DynValue Value
        {
            get { return _value; }
            set
            {
                if (_key.IsNotNil())
                {
                    Value = value;
                }
            }
        }

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
    }
}

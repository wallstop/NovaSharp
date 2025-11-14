namespace NovaSharp.Interpreter.DataTypes
{
    /// <summary>
    /// Class wrapping a request to yield a coroutine
    /// </summary>
    public class YieldRequest
    {
        /// <summary>
        /// The return values of the coroutine
        /// </summary>
        private DynValue[] _returnValues;

        public DynValue[] ReturnValues
        {
            get { return _returnValues; }
            internal set { _returnValues = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="YieldRequest"/> is a forced yield.
        /// </summary>
        public bool Forced { get; internal set; }
    }
}

namespace NovaSharp.Interpreter.DataTypes
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Class wrapping a request to yield a coroutine
    /// </summary>
    public class YieldRequest
    {
        /// <summary>
        /// The return values of the coroutine
        /// </summary>
        private DynValue[] _returnValues;

        [SuppressMessage(
            "Performance",
            "CA1819:Properties should not return arrays",
            Justification = "Coroutine yields must share the return tuple backing array without copying."
        )]
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

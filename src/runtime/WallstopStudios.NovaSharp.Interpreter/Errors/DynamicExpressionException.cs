namespace WallstopStudios.NovaSharp.Interpreter.Errors
{
    using System;

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Exception thrown when NovaSharp detects invalid dynamic expression syntax or unsupported constructs.
    /// </summary>
    public class DynamicExpressionException : ScriptRuntimeException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicExpressionException"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public DynamicExpressionException(string format, params object[] args)
            : base("<dynamic>: " + format, args) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicExpressionException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public DynamicExpressionException(string message)
            : base("<dynamic>: " + message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicExpressionException"/> class.
        /// </summary>
        public DynamicExpressionException()
            : base("<dynamic>: dynamic expression error") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicExpressionException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DynamicExpressionException(string message, Exception innerException)
            : base("<dynamic>: " + message, innerException) { }
    }
}

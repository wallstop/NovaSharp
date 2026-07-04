namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    using System.Runtime.Serialization;
#endif

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Exception raised when Lua source cannot be lexed or parsed.
    /// </summary>
    public class LuaSyntaxException : LuaException
    {
        /// <summary>
        /// Initializes a new <see cref="LuaSyntaxException"/> without a message.
        /// </summary>
        public LuaSyntaxException() { }

        /// <summary>
        /// Initializes a new <see cref="LuaSyntaxException"/> with a message.
        /// </summary>
        public LuaSyntaxException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="LuaSyntaxException"/> with a message and inner exception.
        /// </summary>
        public LuaSyntaxException(string message, Exception innerException)
            : base(message, innerException) { }

        internal LuaSyntaxException(SyntaxErrorException innerException)
            : base(CreateMessage(innerException), innerException, innerException)
        {
            IsIncompleteInput = innerException?.IsPrematureStreamTermination ?? false;
        }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        /// <summary>
        /// Initializes a new instance from serialized data.
        /// </summary>
        protected LuaSyntaxException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif

        /// <summary>
        /// Gets whether parsing stopped because the input ended before the chunk was complete.
        /// </summary>
        public bool IsIncompleteInput { get; }

        private static string CreateMessage(SyntaxErrorException exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(exception.DecoratedMessage)
                ? exception.Message
                : exception.DecoratedMessage;
        }
    }
}

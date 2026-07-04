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
    /// Exception raised when Lua code fails at runtime.
    /// </summary>
    public class LuaRuntimeException : LuaException
    {
        /// <summary>
        /// Initializes a new <see cref="LuaRuntimeException"/> without a message.
        /// </summary>
        public LuaRuntimeException() { }

        /// <summary>
        /// Initializes a new <see cref="LuaRuntimeException"/> with a message.
        /// </summary>
        public LuaRuntimeException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="LuaRuntimeException"/> with a message and inner exception.
        /// </summary>
        public LuaRuntimeException(string message, Exception innerException)
            : base(message, innerException) { }

        internal LuaRuntimeException(ScriptRuntimeException innerException)
            : base(CreateMessage(innerException), innerException, innerException) { }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        /// <summary>
        /// Initializes a new instance from serialized data.
        /// </summary>
        protected LuaRuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif

        private static string CreateMessage(ScriptRuntimeException exception)
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

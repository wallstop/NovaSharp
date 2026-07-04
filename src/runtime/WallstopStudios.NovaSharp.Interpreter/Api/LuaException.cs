namespace NovaSharp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    using System.Runtime.Serialization;
#endif

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Base exception for errors raised while compiling or executing Lua through the facade API.
    /// </summary>
    public class LuaException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="LuaException"/> without a message.
        /// </summary>
        public LuaException()
            : this(null, null, null) { }

        /// <summary>
        /// Initializes a new <see cref="LuaException"/> with a message.
        /// </summary>
        public LuaException(string message)
            : this(message, null, null) { }

        /// <summary>
        /// Initializes a new <see cref="LuaException"/> with a message and inner exception.
        /// </summary>
        public LuaException(string message, Exception innerException)
            : this(message, innerException, null) { }

        internal LuaException(InterpreterException innerException)
            : this(CreateMessage(innerException), innerException, innerException) { }

        internal LuaException(string message, Exception innerException, InterpreterException source)
            : base(message, innerException)
        {
            DecoratedMessage = source?.DecoratedMessage ?? message ?? string.Empty;
            InstructionPointer = source?.InstructionPtr ?? -1;
            CallStack = CopyCallStack(source?.CallStack);
        }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        /// <summary>
        /// Initializes a new instance from serialized data.
        /// </summary>
        protected LuaException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            DecoratedMessage = string.Empty;
            InstructionPointer = -1;
            CallStack = Array.AsReadOnly(Array.Empty<string>());
        }
#endif

        /// <summary>
        /// Gets the interpreter-decorated message, including chunk/source context when available.
        /// </summary>
        public string DecoratedMessage { get; }

        /// <summary>
        /// Gets the VM instruction pointer recorded for the failure, or -1 when unavailable.
        /// </summary>
        public int InstructionPointer { get; }

        /// <summary>
        /// Gets a text snapshot of the Lua call stack captured during failure handling.
        /// </summary>
        public IReadOnlyList<string> CallStack { get; }

        /// <summary>
        /// Converts internal interpreter exception types to the root facade exception hierarchy.
        /// </summary>
        internal static LuaException Wrap(InterpreterException exception)
        {
            if (exception is SandboxViolationException sandboxViolationException)
            {
                return new LuaSandboxException(sandboxViolationException);
            }

            if (exception is SyntaxErrorException syntaxErrorException)
            {
                return new LuaSyntaxException(syntaxErrorException);
            }

            if (exception is ScriptRuntimeException scriptRuntimeException)
            {
                return new LuaRuntimeException(scriptRuntimeException);
            }

            if (exception != null)
            {
                return new LuaException(exception);
            }

            throw new ArgumentNullException(nameof(exception));
        }

        private static string CreateMessage(InterpreterException exception)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            return string.IsNullOrEmpty(exception.DecoratedMessage)
                ? exception.Message
                : exception.DecoratedMessage;
        }

        private static ReadOnlyCollection<string> CopyCallStack(IList<WatchItem> callStack)
        {
            if (callStack == null || callStack.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<string>());
            }

            string[] snapshot = new string[callStack.Count];
            for (int i = 0; i < callStack.Count; i++)
            {
                snapshot[i] = callStack[i]?.ToString() ?? string.Empty;
            }

            return new ReadOnlyCollection<string>(snapshot);
        }
    }
}

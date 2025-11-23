namespace NovaSharp.Interpreter.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Debugging;
    using NovaSharp.Interpreter.Compatibility;
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// Base type of all exceptions thrown in NovaSharp
    /// </summary>
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    public class InterpreterException : Exception
    {
        public InterpreterException() { }

        public InterpreterException(string message)
            : base(message) { }

        public InterpreterException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpreterException"/> class.
        /// </summary>
        /// <param name="ex">The ex.</param>
        protected InterpreterException(Exception ex, string message)
            : base(message, EnsureException(ex)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpreterException"/> class.
        /// </summary>
        /// <param name="ex">The ex.</param>
        protected InterpreterException(Exception ex)
            : base(GetExceptionMessage(ex), EnsureException(ex)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpreterException"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        protected InterpreterException(string format, params object[] args)
            : base(FormatInvariant(format, args)) { }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        /// <summary>
        /// Initializes a new instance of the <see cref="InterpreterException" /> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected InterpreterException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif

        /// <summary>
        /// Gets the instruction pointer of the execution (if it makes sense)
        /// </summary>
        public int InstructionPtr { get; internal set; }

        /// <summary>
        /// Gets the interpreter call stack.
        /// </summary>
        public IList<WatchItem> CallStack { get; internal set; }

        /// <summary>
        /// Gets the decorated message (error message plus error location in script) if possible.
        /// </summary>
        public string DecoratedMessage { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message should not be decorated
        /// </summary>
        public bool DoNotDecorateMessage { get; set; }

        internal void DecorateMessage(Script script, SourceRef sref, int ip = -1)
        {
            if (string.IsNullOrEmpty(DecoratedMessage))
            {
                if (DoNotDecorateMessage)
                {
                    DecoratedMessage = Message;
                }
                else if (sref != null)
                {
                    DecoratedMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}: {1}",
                        sref.FormatLocation(script),
                        Message
                    );
                }
                else
                {
                    DecoratedMessage = $"bytecode:{ip}: {Message}";
                }
            }
        }

        /// <summary>
        /// Rethrows this instance if
        /// </summary>
        /// <returns></returns>
        public virtual void Rethrow() { }

        internal void AppendCompatibilityContext(Script script)
        {
            if (
                script == null
                || string.IsNullOrEmpty(DecoratedMessage)
                || DecoratedMessage.Contains("[compatibility:", StringComparison.Ordinal)
            )
            {
                return;
            }

            LuaCompatibilityProfile profile = script.CompatibilityProfile;
            DecoratedMessage = $"{DecoratedMessage} [compatibility: {profile.DisplayName}]";
        }

        private static string FormatInvariant(string format, object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        private static Exception EnsureException(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            return ex;
        }

        private static string GetExceptionMessage(Exception ex)
        {
            return EnsureException(ex).Message ?? string.Empty;
        }
    }
}

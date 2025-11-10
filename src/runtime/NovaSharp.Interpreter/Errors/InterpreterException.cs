namespace NovaSharp.Interpreter.Errors
{
    using System;
    using System.Collections.Generic;
    using Debugging;
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
            : base(message, ex) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpreterException"/> class.
        /// </summary>
        /// <param name="ex">The ex.</param>
        protected InterpreterException(Exception ex)
            : base(ex.Message, ex) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpreterException"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        protected InterpreterException(string format, params object[] args)
            : base(string.Format(format, args)) { }

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
                    return;
                }
                else if (sref != null)
                {
                    DecoratedMessage = string.Format(
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
    }
}

namespace WallstopStudios.NovaSharp.Interpreter.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    using System.Runtime.Serialization;
#endif

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Base type for every exception surfaced by the NovaSharp interpreter, providing call-stack metadata
    /// and helpers to decorate messages with script locations and compatibility context.
    /// </summary>
    public class InterpreterException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="InterpreterException"/> with no message or inner exception; hosts typically
        /// populate details later through <see cref="DecoratedMessage"/>.
        /// </summary>
        public InterpreterException() { }

        /// <summary>
        /// Initializes a new <see cref="InterpreterException"/> using the provided <paramref name="message"/> as the base error text.
        /// </summary>
        /// <param name="message">Human-readable detail describing the interpreter failure.</param>
        public InterpreterException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="InterpreterException"/> that wraps an underlying CLR <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">Human-readable detail describing the interpreter failure.</param>
        /// <param name="innerException">Original CLR exception captured while executing the script.</param>
        public InterpreterException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Initializes a new <see cref="InterpreterException"/> that wraps the supplied CLR exception and overrides the message.
        /// </summary>
        /// <param name="ex">CLR exception raised by the interpreter or host integration layer.</param>
        /// <param name="message">Human-readable message that should replace the wrapped exception text.</param>
        protected InterpreterException(Exception ex, string message)
            : base(message, EnsureException(ex)) { }

        /// <summary>
        /// Initializes a new <see cref="InterpreterException"/> that forwards the original CLR exception message to the script host.
        /// </summary>
        /// <param name="ex">CLR exception raised by the interpreter or host integration layer.</param>
        protected InterpreterException(Exception ex)
            : base(GetExceptionMessage(ex), EnsureException(ex)) { }

        /// <summary>
        /// Initializes a new <see cref="InterpreterException"/> using an invariant composite message built
        /// from <paramref name="format"/> and <paramref name="args"/>.
        /// </summary>
        /// <param name="format">Composite format string interpreted with <see cref="CultureInfo.InvariantCulture"/>.</param>
        /// <param name="args">Arguments applied to <paramref name="format"/>.</param>
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
        /// Gets or sets a value indicating whether the message should skip decoration.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, <see cref="DecorateMessage(Script, SourceRef, int)"/> leaves
        /// <see cref="DecoratedMessage"/> untouched so callers can preserve custom formatting.
        /// </remarks>
        public bool DoNotDecorateMessage { get; set; }

        /// <summary>
        /// Formats <see cref="DecoratedMessage"/> with the best available script context.
        /// </summary>
        /// <param name="script">Script owning the currently executing chunk, used to format source references.</param>
        /// <param name="sref">Source location associated with the failure; if <c>null</c>, the bytecode IP is used.</param>
        /// <param name="ip">Instruction pointer recorded during execution; used only when <paramref name="sref"/> is <c>null</c>.</param>
        public void DecorateMessage(Script script, SourceRef sref, int ip = -1)
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
        /// Rethrows this instance according to the configured <see cref="Script.GlobalOptions"/> policy.
        /// </summary>
        public virtual void Rethrow() { }

        /// <summary>
        /// Appends the active <see cref="LuaCompatibilityProfile"/> to <see cref="DecoratedMessage"/> so host logs
        /// record which Lua baseline caused the failure.
        /// </summary>
        /// <param name="script">Script owning the error; supplies the compatibility profile metadata.</param>
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

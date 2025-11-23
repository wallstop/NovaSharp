namespace NovaSharp.Interpreter.Errors
{
    using System;
    using Debugging;
    using NovaSharp.Interpreter.Tree.Lexer;
    using Tree;
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    using System.Runtime.Serialization;
#endif

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Exception for parser and lexer errors surfaced while compiling Lua chunks.
    /// </summary>
    public class SyntaxErrorException : InterpreterException
    {
        /// <summary>
        /// Gets the lexer token that triggered the syntax failure (not serialized to keep payloads small).
        /// </summary>
        [field: NonSerialized]
        internal Token Token { get; private set; }

        public SyntaxErrorException() { }

        public SyntaxErrorException(string message)
            : base(message) { }

        public SyntaxErrorException(string message, Exception innerException)
            : base(message, innerException) { }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        protected SyntaxErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether the parser stopped because the input stream terminated early (unexpected EOF).
        /// </summary>
        /// <remarks>
        /// REPL hosts can use this flag to differentiate between recoverable errors (supply more text) and fatal ones.
        /// </remarks>
        public bool IsPrematureStreamTermination { get; set; }

        internal SyntaxErrorException(Token t, string format, params object[] args)
            : base(format, args)
        {
            Token = t;
        }

        internal SyntaxErrorException(Token t, string message)
            : base(message)
        {
            Token = t;
        }

        internal SyntaxErrorException(
            Script script,
            SourceRef sref,
            string format,
            params object[] args
        )
            : base(format, args)
        {
            DecorateMessage(script, sref);
        }

        internal SyntaxErrorException(Script script, SourceRef sref, string message)
            : base(message)
        {
            DecorateMessage(script, sref);
        }

        private SyntaxErrorException(SyntaxErrorException syntaxErrorException)
            : base(syntaxErrorException, syntaxErrorException.DecoratedMessage)
        {
            Token = syntaxErrorException.Token;
            DecoratedMessage = Message;
        }

        /// <summary>
        /// Decorates the error message using the token that caused the syntax failure, if any.
        /// </summary>
        /// <param name="script">Script owning the current chunk, required for formatting source references.</param>
        internal void DecorateMessage(Script script)
        {
            if (Token != null)
            {
                DecorateMessage(script, Token.GetSourceRef(false));
            }
        }

        /// <summary>
        /// Rethrows this instance when <see cref="Script.GlobalOptions.RethrowExceptionNested"/> requests nested propagation.
        /// </summary>
        public override void Rethrow()
        {
            if (Script.GlobalOptions.RethrowExceptionNested)
            {
                throw new SyntaxErrorException(this);
            }
        }
    }
}

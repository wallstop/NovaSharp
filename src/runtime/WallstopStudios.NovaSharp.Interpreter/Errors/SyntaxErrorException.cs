namespace WallstopStudios.NovaSharp.Interpreter.Errors
{
    using System;
    using Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;
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

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> with no message; primarily used by serializers/reflection.
        /// </summary>
        public SyntaxErrorException() { }

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> using the provided <paramref name="message"/>.
        /// </summary>
        /// <param name="message">Human-readable detail describing the parser/lexer error.</param>
        public SyntaxErrorException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> wrapping the supplied CLR <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">Human-readable detail describing the parser/lexer error.</param>
        /// <param name="innerException">CLR exception encountered while tokenizing or parsing.</param>
        public SyntaxErrorException(string message, Exception innerException)
            : base(message, innerException) { }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> from serialized data.
        /// </summary>
        /// <param name="info">Serialized data describing the exception.</param>
        /// <param name="context">Streaming context describing the serialization target/source.</param>
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

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> associated with the specified lexer token, formatting the message with invariant culture.
        /// </summary>
        /// <param name="t">Token that triggered the error.</param>
        /// <param name="format">Composite format string describing the failure.</param>
        /// <param name="args">Arguments applied to <paramref name="format"/>.</param>
        internal SyntaxErrorException(Token t, string format, params object[] args)
            : base(format, args)
        {
            Token = t;
        }

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> associated with the specified lexer token.
        /// </summary>
        /// <param name="t">Token that triggered the error.</param>
        /// <param name="message">Human-readable detail describing the parser/lexer error.</param>
        internal SyntaxErrorException(Token t, string message)
            : base(message)
        {
            Token = t;
        }

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> decorated with a source reference produced from <paramref name="sref"/>.
        /// </summary>
        /// <param name="script">Script owning the current chunk, used to format source locations.</param>
        /// <param name="sref">Source reference highlighting the failure.</param>
        /// <param name="format">Composite format string describing the failure.</param>
        /// <param name="args">Arguments applied to <paramref name="format"/>.</param>
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

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> decorated with a source reference produced from <paramref name="sref"/>.
        /// </summary>
        /// <param name="script">Script owning the current chunk, used to format source locations.</param>
        /// <param name="sref">Source reference highlighting the failure.</param>
        /// <param name="message">Human-readable detail describing the parser/lexer error.</param>
        internal SyntaxErrorException(Script script, SourceRef sref, string message)
            : base(message)
        {
            DecorateMessage(script, sref);
        }

        /// <summary>
        /// Initializes a new <see cref="SyntaxErrorException"/> that clones another syntax error, preserving token metadata.
        /// </summary>
        /// <param name="syntaxErrorException">Existing syntax error to clone.</param>
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

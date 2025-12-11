namespace WallstopStudios.NovaSharp.Interpreter.Tree
{
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Base type for every AST node emitted by the NovaSharp parser; exposes shared helpers for token validation and code generation.
    /// </summary>
    internal abstract class NodeBase
    {
        /// <summary>
        /// Script currently being compiled; used for source decoration and diagnostics.
        /// </summary>
        public Script Script { get; private set; }

        /// <summary>
        /// Provides access to the lexer/reader that built this node.
        /// </summary>
        protected ScriptLoadingContext LoadingContext { get; private set; }

        /// <summary>
        /// Initializes a new node and captures the owning script/loading context.
        /// </summary>
        /// <param name="lcontext">Parser context providing the script and lexer.</param>
        public NodeBase(ScriptLoadingContext lcontext)
        {
            Script = lcontext.Script;
            LoadingContext = lcontext;
        }

        /// <summary>
        /// Emits bytecode for this AST node.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the compiled instructions.</param>
        public abstract void Compile(ByteCode bc);

        /// <summary>
        /// Throws a syntax error indicating that <paramref name="t"/> does not match the expected token type.
        /// </summary>
        protected static Token UnexpectedTokenType(Token t)
        {
            throw new SyntaxErrorException(t, "unexpected symbol near '{0}'", t.Text)
            {
                IsPrematureStreamTermination = (t.Type == TokenType.Eof),
            };
        }

        /// <summary>
        /// Ensures the current token matches <paramref name="tokenType"/> and advances the lexer.
        /// </summary>
        /// <param name="lcontext">Source parser context.</param>
        /// <param name="tokenType">Expected token type.</param>
        /// <returns>The consumed token.</returns>
        protected static Token CheckTokenType(ScriptLoadingContext lcontext, TokenType tokenType)
        {
            Token t = lcontext.Lexer.Current;
            if (t.Type != tokenType)
            {
                return UnexpectedTokenType(t);
            }

            lcontext.Lexer.Next();

            return t;
        }

        /// <summary>
        /// Ensures the current token matches one of the supplied types and advances the lexer.
        /// </summary>
        /// <param name="lcontext">Source parser context.</param>
        /// <param name="tokenType1">First permissible token type.</param>
        /// <param name="tokenType2">Second permissible token type.</param>
        /// <returns>The consumed token.</returns>
        protected static Token CheckTokenType(
            ScriptLoadingContext lcontext,
            TokenType tokenType1,
            TokenType tokenType2
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.Type != tokenType1 && t.Type != tokenType2)
            {
                return UnexpectedTokenType(t);
            }

            lcontext.Lexer.Next();

            return t;
        }

        /// <summary>
        /// Ensures the current token matches one of the supplied types and advances the lexer.
        /// </summary>
        /// <param name="lcontext">Source parser context.</param>
        /// <param name="tokenType1">First permissible token type.</param>
        /// <param name="tokenType2">Second permissible token type.</param>
        /// <param name="tokenType3">Third permissible token type.</param>
        /// <returns>The consumed token.</returns>
        protected static Token CheckTokenType(
            ScriptLoadingContext lcontext,
            TokenType tokenType1,
            TokenType tokenType2,
            TokenType tokenType3
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.Type != tokenType1 && t.Type != tokenType2 && t.Type != tokenType3)
            {
                return UnexpectedTokenType(t);
            }

            lcontext.Lexer.Next();

            return t;
        }

        /// <summary>
        /// Verifies that the current token matches <paramref name="tokenType"/> without consuming it.
        /// </summary>
        /// <param name="lcontext">Source parser context.</param>
        /// <param name="tokenType">Token type that must be present.</param>
        protected static void CheckTokenTypeNotNext(
            ScriptLoadingContext lcontext,
            TokenType tokenType
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.Type != tokenType)
            {
                UnexpectedTokenType(t);
            }
        }

        /// <summary>
        /// Validates that the next token closes a previously opened symbol and surfaces a helpful error if it does not.
        /// </summary>
        /// <param name="lcontext">Source parser context.</param>
        /// <param name="originalToken">Original opening token (used for diagnostics).</param>
        /// <param name="expectedTokenType">Expected closing token type.</param>
        /// <param name="expectedTokenText">Text that should be shown for the expected token.</param>
        /// <returns>The consumed closing token.</returns>
        protected static Token CheckMatch(
            ScriptLoadingContext lcontext,
            Token originalToken,
            TokenType expectedTokenType,
            string expectedTokenText
        )
        {
            Token t = lcontext.Lexer.Current;
            if (t.Type != expectedTokenType)
            {
                throw new SyntaxErrorException(
                    lcontext.Lexer.Current,
                    "'{0}' expected (to close '{1}' at line {2}) near '{3}'",
                    expectedTokenText,
                    originalToken.Text,
                    originalToken.FromLine,
                    t.Text
                )
                {
                    IsPrematureStreamTermination = (t.Type == TokenType.Eof),
                };
            }

            lcontext.Lexer.Next();

            return t;
        }
    }
}

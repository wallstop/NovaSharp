namespace WallstopStudios.NovaSharp.Interpreter.Tree
{
    using Expressions;
    using Statements;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Base class for every Lua statement node; handles dispatching to specific statement implementations.
    /// </summary>
    internal abstract class Statement : NodeBase
    {
        /// <summary>
        /// Initializes a statement with the supplied parser context.
        /// </summary>
        /// <param name="lcontext">Parser context providing the current lexer/token stream.</param>
        public Statement(ScriptLoadingContext lcontext)
            : base(lcontext) { }

        /// <summary>
        /// Indicates whether this is a "void statement" per Lua 5.4 §3.5.
        /// </summary>
        /// <remarks>
        /// Void statements are labels and empty statements (semicolons).
        /// They do not affect local variable scope endings.
        /// </remarks>
        public virtual bool IsVoidStatement => false;

        /// <summary>
        /// Parses the next statement, returning the appropriate concrete node and flagging whether Lua requires it to be the final statement in the block.
        /// </summary>
        /// <param name="lcontext">Parser context providing the lexer/token stream.</param>
        /// <param name="forceLast">Set to <c>true</c> when the returned statement must be the last in the current block (Lua `return`).</param>
        /// <returns>The parsed statement node.</returns>
        protected static Statement CreateStatement(
            ScriptLoadingContext lcontext,
            out bool forceLast
        )
        {
            Token tkn = lcontext.Lexer.Current;

            forceLast = false;

            switch (tkn.type)
            {
                case TokenType.DoubleColon:
                    return new LabelStatement(lcontext);
                case TokenType.Goto:
                    return new GotoStatement(lcontext);
                case TokenType.SemiColon:
                    lcontext.Lexer.Next();
                    return new EmptyStatement(lcontext);
                case TokenType.If:
                    return new IfStatement(lcontext);
                case TokenType.While:
                    return new WhileStatement(lcontext);
                case TokenType.Do:
                    return new ScopeBlockStatement(lcontext);
                case TokenType.For:
                    return DispatchForLoopStatement(lcontext);
                case TokenType.Repeat:
                    return new RepeatStatement(lcontext);
                case TokenType.Function:
                    return new FunctionDefinitionStatement(lcontext, false, null);
                case TokenType.Local:
                    Token localToken = lcontext.Lexer.Current;
                    lcontext.Lexer.Next();
                    if (lcontext.Lexer.Current.type == TokenType.Function)
                    {
                        return new FunctionDefinitionStatement(lcontext, true, localToken);
                    }
                    else
                    {
                        return new AssignmentStatement(lcontext, localToken);
                    }
                case TokenType.Return:
                    forceLast = true;
                    return new ReturnStatement(lcontext);
                case TokenType.Break:
                    return new BreakStatement(lcontext);
                default:
                {
                    Token l = lcontext.Lexer.Current;
                    Expression exp = Expression.PrimaryExp(lcontext);

                    if (exp is FunctionCallExpression fnexp)
                    {
                        return new FunctionCallStatement(lcontext, fnexp);
                    }
                    else
                    {
                        return new AssignmentStatement(lcontext, exp, l);
                    }
                }
            }
        }

        /// <summary>
        /// Parses the `for` family of statements and returns either a numeric `for` or `for-in` node.
        /// </summary>
        /// <param name="lcontext">Parser context providing the lexer/token stream.</param>
        /// <returns>The parsed `for` statement node.</returns>
        private static Statement DispatchForLoopStatement(ScriptLoadingContext lcontext)
        {
            //	for Name ‘=’ exp ‘,’ exp [‘,’ exp] do block end |
            //	for namelist in explist do block end |

            Token forTkn = CheckTokenType(lcontext, TokenType.For);

            Token name = CheckTokenType(lcontext, TokenType.Name);

            if (lcontext.Lexer.Current.type == TokenType.OpAssignment)
            {
                return new ForLoopStatement(lcontext, name, forTkn);
            }
            else
            {
                return new ForEachLoopStatement(lcontext, name, forTkn);
            }
        }
    }
}

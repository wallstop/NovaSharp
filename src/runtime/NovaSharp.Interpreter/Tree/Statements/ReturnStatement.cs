namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Expressions;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a Lua <c>return</c> statement.
    /// </summary>
    internal class ReturnStatement : Statement
    {
        private readonly Expression _expression;
        private readonly SourceRef _ref;

        /// <summary>
        /// Creates a return statement with an already parsed expression list (used by parser helpers).
        /// </summary>
        /// <param name="lcontext">Parser context providing the lexer/token stream.</param>
        /// <param name="e">Expression or tuple representing the return values.</param>
        /// <param name="sref">Source span for the statement.</param>
        public ReturnStatement(ScriptLoadingContext lcontext, Expression e, SourceRef sref)
            : base(lcontext)
        {
            _expression = e;
            _ref = sref;
            lcontext.Source.Refs.Add(sref);
        }

        /// <summary>
        /// Parses a return statement directly from the lexer, handling optional value lists.
        /// </summary>
        public ReturnStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            Token ret = lcontext.Lexer.Current;

            lcontext.Lexer.Next();

            Token cur = lcontext.Lexer.Current;

            if (cur.IsEndOfBlock() || cur.Type == TokenType.SemiColon)
            {
                _expression = null;
                _ref = ret.GetSourceRef();
            }
            else
            {
                _expression = new ExprListExpression(Expression.ExprList(lcontext), lcontext);
                _ref = ret.GetSourceRefUpTo(lcontext.Lexer.Current);
            }
            lcontext.Source.Refs.Add(_ref);
        }

        /// <summary>
        /// Emits the return instruction, optionally compiling the value tuple first.
        /// </summary>
        public override void Compile(Execution.VM.ByteCode bc)
        {
            using (bc.EnterSource(_ref))
            {
                if (_expression != null)
                {
                    _expression.Compile(bc);
                    bc.EmitRet(1);
                }
                else
                {
                    bc.EmitRet(0);
                }
            }
        }
    }
}

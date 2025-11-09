namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution;
    using Expressions;

    internal class ReturnStatement : Statement
    {
        private readonly Expression _expression = null;
        private readonly SourceRef _ref;

        public ReturnStatement(ScriptLoadingContext lcontext, Expression e, SourceRef sref)
            : base(lcontext)
        {
            _expression = e;
            _ref = sref;
            lcontext.Source.Refs.Add(sref);
        }

        public ReturnStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            Token ret = lcontext.Lexer.Current;

            lcontext.Lexer.Next();

            Token cur = lcontext.Lexer.Current;

            if (cur.IsEndOfBlock() || cur.type == TokenType.SemiColon)
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

        public override void Compile(Execution.VM.ByteCode bc)
        {
            using (bc.EnterSource(_ref))
            {
                if (_expression != null)
                {
                    _expression.Compile(bc);
                    bc.Emit_Ret(1);
                }
                else
                {
                    bc.Emit_Ret(0);
                }
            }
        }
    }
}

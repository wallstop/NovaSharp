namespace NovaSharp.Interpreter.Tree.Expressions
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class TableConstructor : Expression
    {
        private readonly bool _shared;
        private readonly List<Expression> _positionalValues = new();
        private readonly List<KeyValuePair<Expression, Expression>> _ctorArgs = new();

        public TableConstructor(ScriptLoadingContext lcontext, bool shared)
            : base(lcontext)
        {
            _shared = shared;

            // here lexer is at the '{', go on
            CheckTokenType(lcontext, TokenType.BrkOpenCurly, TokenType.BrkOpenCurlyShared);

            while (lcontext.Lexer.Current.Type != TokenType.BrkCloseCurly)
            {
                switch (lcontext.Lexer.Current.Type)
                {
                    case TokenType.Name:
                        {
                            Token assign = lcontext.Lexer.PeekNext();

                            if (assign.Type == TokenType.OpAssignment)
                            {
                                StructField(lcontext);
                            }
                            else
                            {
                                ArrayField(lcontext);
                            }
                        }
                        break;
                    case TokenType.BrkOpenSquare:
                        MapField(lcontext);
                        break;
                    default:
                        ArrayField(lcontext);
                        break;
                }

                Token curr = lcontext.Lexer.Current;

                if (curr.Type == TokenType.Comma || curr.Type == TokenType.SemiColon)
                {
                    lcontext.Lexer.Next();
                }
                else
                {
                    break;
                }
            }

            CheckTokenType(lcontext, TokenType.BrkCloseCurly);
        }

        private void MapField(ScriptLoadingContext lcontext)
        {
            lcontext.Lexer.Next(); // skip '['

            Expression key = Expr(lcontext);

            CheckTokenType(lcontext, TokenType.BrkCloseSquare);

            CheckTokenType(lcontext, TokenType.OpAssignment);

            Expression value = Expr(lcontext);

            _ctorArgs.Add(new KeyValuePair<Expression, Expression>(key, value));
        }

        private void StructField(ScriptLoadingContext lcontext)
        {
            Expression key = new LiteralExpression(
                lcontext,
                DynValue.NewString(lcontext.Lexer.Current.Text)
            );
            lcontext.Lexer.Next();

            CheckTokenType(lcontext, TokenType.OpAssignment);

            Expression value = Expr(lcontext);

            _ctorArgs.Add(new KeyValuePair<Expression, Expression>(key, value));
        }

        private void ArrayField(ScriptLoadingContext lcontext)
        {
            Expression e = Expr(lcontext);
            _positionalValues.Add(e);
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.Emit_NewTable(_shared);

            foreach (KeyValuePair<Expression, Expression> kvp in _ctorArgs)
            {
                kvp.Key.Compile(bc);
                kvp.Value.Compile(bc);
                bc.Emit_TblInitN();
            }

            for (int i = 0; i < _positionalValues.Count; i++)
            {
                _positionalValues[i].Compile(bc);
                bc.Emit_TblInitI(i == _positionalValues.Count - 1);
            }
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            if (!_shared)
            {
                throw new DynamicExpressionException(
                    "Dynamic Expressions cannot define new non-prime tables."
                );
            }

            DynValue tval = DynValue.NewPrimeTable();
            Table t = tval.Table;

            int idx = 0;
            foreach (Expression e in _positionalValues)
            {
                t.Set(++idx, e.Eval(context));
            }

            foreach (KeyValuePair<Expression, Expression> kvp in _ctorArgs)
            {
                t.Set(kvp.Key.Eval(context), kvp.Value.Eval(context));
            }

            return tval;
        }
    }
}

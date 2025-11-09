namespace NovaSharp.Interpreter.Tree.Expressions
{
    using System.Collections.Generic;
    using Debugging;
    using Execution;

    internal class FunctionCallExpression : Expression
    {
        private readonly List<Expression> _arguments;
        private readonly Expression _function;
        private readonly string _name;
        private readonly string _debugErr;

        internal SourceRef SourceRef { get; private set; }

        public FunctionCallExpression(
            ScriptLoadingContext lcontext,
            Expression function,
            Token thisCallName
        )
            : base(lcontext)
        {
            Token callToken = thisCallName ?? lcontext.Lexer.Current;

            _name = thisCallName != null ? thisCallName.Text : null;
            _debugErr = function.GetFriendlyDebugName();
            _function = function;

            switch (lcontext.Lexer.Current.type)
            {
                case TokenType.BrkOpenRound:
                    Token openBrk = lcontext.Lexer.Current;
                    lcontext.Lexer.Next();
                    Token t = lcontext.Lexer.Current;
                    if (t.type == TokenType.BrkCloseRound)
                    {
                        _arguments = new List<Expression>();
                        SourceRef = callToken.GetSourceRef(t);
                        lcontext.Lexer.Next();
                    }
                    else
                    {
                        _arguments = ExprList(lcontext);
                        SourceRef = callToken.GetSourceRef(
                            CheckMatch(lcontext, openBrk, TokenType.BrkCloseRound, ")")
                        );
                    }
                    break;
                case TokenType.String:
                case TokenType.StringLong:
                    {
                        _arguments = new List<Expression>();
                        Expression le = new LiteralExpression(lcontext, lcontext.Lexer.Current);
                        _arguments.Add(le);
                        SourceRef = callToken.GetSourceRef(lcontext.Lexer.Current);
                    }
                    break;
                case TokenType.BrkOpenCurly:
                case TokenType.BrkOpenCurlyShared:
                    {
                        _arguments = new List<Expression>();
                        _arguments.Add(
                            new TableConstructor(
                                lcontext,
                                lcontext.Lexer.Current.type == TokenType.BrkOpenCurlyShared
                            )
                        );
                        SourceRef = callToken.GetSourceRefUpTo(lcontext.Lexer.Current);
                    }
                    break;
                default:
                    throw new SyntaxErrorException(
                        lcontext.Lexer.Current,
                        "function arguments expected"
                    )
                    {
                        IsPrematureStreamTermination = (
                            lcontext.Lexer.Current.type == TokenType.Eof
                        ),
                    };
            }
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            _function.Compile(bc);

            int argslen = _arguments.Count;

            if (!string.IsNullOrEmpty(_name))
            {
                bc.Emit_Copy(0);
                bc.Emit_Index(DynValue.NewString(_name), true);
                bc.Emit_Swap(0, 1);
                ++argslen;
            }

            for (int i = 0; i < _arguments.Count; i++)
            {
                _arguments[i].Compile(bc);
            }

            if (!string.IsNullOrEmpty(_name))
            {
                bc.Emit_ThisCall(argslen, _debugErr);
            }
            else
            {
                bc.Emit_Call(argslen, _debugErr);
            }
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            throw new DynamicExpressionException("Dynamic Expressions cannot call functions.");
        }
    }
}

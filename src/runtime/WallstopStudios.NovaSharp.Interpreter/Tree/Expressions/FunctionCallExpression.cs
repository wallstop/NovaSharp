namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using System.Collections.Generic;
    using Debugging;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a Lua function invocation, including method-call sugar (colon syntax).
    /// </summary>
    internal class FunctionCallExpression : Expression
    {
        private readonly List<Expression> _arguments;
        private readonly Expression _function;
        private readonly string _name;
        private readonly string _debugErr;

        /// <summary>
        /// Gets the source range that spans the invocation site.
        /// </summary>
        internal SourceRef SourceRef { get; private set; }

        /// <summary>
        /// Initializes a new function-call node using the supplied callee/arguments.
        /// </summary>
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

            switch (lcontext.Lexer.Current.Type)
            {
                case TokenType.BrkOpenRound:
                    Token openBrk = lcontext.Lexer.Current;
                    lcontext.Lexer.Next();
                    Token t = lcontext.Lexer.Current;
                    if (t.Type == TokenType.BrkCloseRound)
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
                                lcontext.Lexer.Current.Type == TokenType.BrkOpenCurlyShared
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
                            lcontext.Lexer.Current.Type == TokenType.Eof
                        ),
                    };
            }
        }

        /// <inheritdoc/>
        public override void Compile(Execution.VM.ByteCode bc)
        {
            _function.Compile(bc);

            int argslen = _arguments.Count;

            if (!string.IsNullOrEmpty(_name))
            {
                bc.EmitCopy(0);
                bc.EmitIndex(DynValue.NewString(_name), true);
                bc.EmitSwap(0, 1);
                ++argslen;
            }

            for (int i = 0; i < _arguments.Count; i++)
            {
                _arguments[i].Compile(bc);
            }

            if (!string.IsNullOrEmpty(_name))
            {
                bc.EmitThisCall(argslen, _debugErr);
            }
            else
            {
                bc.EmitCall(argslen, _debugErr);
            }
        }

        /// <inheritdoc/>
        public override DynValue Eval(ScriptExecutionContext context)
        {
            throw new DynamicExpressionException("Dynamic Expressions cannot call functions.");
        }
    }
}

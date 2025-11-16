namespace NovaSharp.Interpreter.Tree.Expressions
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class LiteralExpression : Expression
    {
        private readonly DynValue _value;

        public DynValue Value
        {
            get { return _value; }
        }

        public LiteralExpression(ScriptLoadingContext lcontext, DynValue value)
            : base(lcontext)
        {
            _value = value;
        }

        public LiteralExpression(ScriptLoadingContext lcontext, Token t)
            : base(lcontext)
        {
            switch (t.type)
            {
                case TokenType.Number:
                case TokenType.NumberHex:
                case TokenType.NumberHexFloat:
                    _value = DynValue.NewNumber(t.GetNumberValue()).AsReadOnly();
                    break;
                case TokenType.String:
                case TokenType.StringLong:
                    _value = DynValue.NewString(t.Text).AsReadOnly();
                    break;
                case TokenType.True:
                    _value = DynValue.True;
                    break;
                case TokenType.False:
                    _value = DynValue.False;
                    break;
                case TokenType.Nil:
                    _value = DynValue.Nil;
                    break;
                default:
                    throw new InternalErrorException("type mismatch");
            }

            if (_value == null)
            {
                throw new SyntaxErrorException(t, "unknown literal format near '{0}'", t.Text);
            }

            lcontext.Lexer.Next();
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.Emit_Literal(_value);
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            return _value;
        }
    }
}

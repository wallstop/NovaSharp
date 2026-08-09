namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a literal token (number, string, boolean, or nil) in the AST.
    /// </summary>
    internal class LiteralExpression : Expression
    {
        private readonly LuaValue _value;

        /// <summary>
        /// Gets the constant value represented by this literal.
        /// </summary>
        public LuaValue Value
        {
            get { return _value; }
        }

        public LiteralExpression(ScriptLoadingContext lcontext, LuaValue value)
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
                    // For Lua 5.3+ compliance: integer literals become integers,
                    // float literals (with decimal point or exponent) become floats
                    if (t.IsFloatLiteralSyntax())
                    {
                        // Float literal syntax (contains . or e/E) - always create float subtype
                        _value = LuaValue.NewFloat(t.GetNumberValue());
                    }
                    else if (t.TryGetIntegerValue(out long intVal))
                    {
                        // Successfully parsed as integer directly (without double intermediate)
                        _value = LuaValue.NewInteger(intVal);
                    }
                    else
                    {
                        // Integer syntax but too large for long - use float
                        _value = LuaValue.NewFloat(t.GetNumberValue());
                    }
                    break;
                case TokenType.String:
                case TokenType.StringLong:
                    _value = LuaValue.NewString(t.text);
                    break;
                case TokenType.True:
                    _value = LuaValue.True;
                    break;
                case TokenType.False:
                    _value = LuaValue.False;
                    break;
                case TokenType.Nil:
                    _value = LuaValue.Nil;
                    break;
                default:
                    throw new InternalErrorException("type mismatch");
            }
            lcontext.Lexer.Next();
        }

        /// <summary>
        /// Emits a literal load so the constant value is pushed on the stack.
        /// </summary>
        /// <param name="bc">Bytecode builder that receives the literal instruction.</param>
        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.EmitLiteral(_value);
        }

        /// <summary>
        /// Returns the literal value when executing a dynamic expression.
        /// </summary>
        /// <param name="context">Execution context (unused).</param>
        /// <returns>The constant <see cref="LuaValue" /> backing this expression.</returns>
        public override LuaValue Eval(ScriptExecutionContext context)
        {
            return _value;
        }
    }
}

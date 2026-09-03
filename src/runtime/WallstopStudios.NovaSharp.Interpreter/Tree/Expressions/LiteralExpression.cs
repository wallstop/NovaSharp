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
                    // Materialize the numeral with the script's compatibility profile:
                    // Lua 5.1/5.2 have a single double number type, so integer-syntax
                    // literals (decimal or hex) round to IEEE 754 floats, while Lua 5.3+
                    // keeps integer and float subtypes (hex accumulates modulo 2^64).
                    if (
                        !LuaNumber.TryParse(
                            t.text,
                            lcontext.Script.CompatibilityVersion,
                            out LuaNumber number
                        )
                    )
                    {
                        throw new SyntaxErrorException(t, "malformed number near '{0}'", t.text);
                    }

                    _value = LuaValue.NewNumber(number);
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

namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents Lua unary expressions such as <c>not</c>, length, arithmetic negation, and bit-not.
    /// </summary>
    internal class UnaryOperatorExpression : Expression
    {
        private readonly Expression _exp;
        private readonly string _opText;

        public UnaryOperatorExpression(
            ScriptLoadingContext lcontext,
            Expression subExpression,
            Token unaryOpToken
        )
            : base(lcontext)
        {
            _opText = unaryOpToken.text;
            _exp = subExpression;
        }

        /// <summary>
        /// Emits the opcode that performs the requested unary operation at runtime.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the emitted operator.</param>
        public override void Compile(ByteCode bc)
        {
            _exp.Compile(bc);

            switch (_opText)
            {
                case LuaKeywords.Not:
                    bc.EmitOperator(OpCode.Not);
                    break;
                case "#":
                    bc.EmitOperator(OpCode.Len);
                    break;
                case "-":
                    bc.EmitOperator(OpCode.Neg);
                    break;
                case "~":
                    bc.EmitOperator(OpCode.BitNot);
                    break;
                default:
                    throw new InternalErrorException("Unexpected unary operator '{0}'", _opText);
            }
        }

        /// <summary>
        /// Evaluates the unary operator inside a dynamic expression.
        /// </summary>
        /// <param name="context">Execution context used to evaluate the operand.</param>
        /// <returns>The result of the unary operation.</returns>
        /// <exception cref="DynamicExpressionException">
        /// Thrown when the operand type is incompatible with the requested operator.
        /// </exception>
        public override DynValue Eval(ScriptExecutionContext context)
        {
            DynValue v = _exp.Eval(context).ToScalar();

            switch (_opText)
            {
                case LuaKeywords.Not:
                    return DynValue.FromBoolean(!v.CastToBool());
                case "#":
                    return v.GetLength();
                case "-":
                {
                    double? d = v.CastToNumber();

                    if (d.HasValue)
                    {
                        return DynValue.NewNumber(-d.Value);
                    }

                    throw new DynamicExpressionException(
                        "Attempt to perform arithmetic on non-numbers."
                    );
                }
                case "~":
                    if (LuaIntegerHelper.TryGetInteger(v, out long operand))
                    {
                        return DynValue.NewNumber(~operand);
                    }

                    throw new DynamicExpressionException(
                        "Attempt to perform bitwise operation on non-integers."
                    );
                default:
                    throw new DynamicExpressionException(
                        "Unexpected unary operator '{0}'",
                        _opText
                    );
            }
        }
    }
}

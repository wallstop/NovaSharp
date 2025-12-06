namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a parenthesized expression that enforces scalar semantics (Lua tuple → single value).
    /// </summary>
    internal class AdjustmentExpression : Expression
    {
        private readonly Expression _expression;

        /// <summary>
        /// Initializes a new scalar adjustment expression wrapping the supplied expression.
        /// </summary>
        public AdjustmentExpression(ScriptLoadingContext lcontext, Expression exp)
            : base(lcontext)
        {
            _expression = exp;
        }

        /// <inheritdoc />
        public override void Compile(Execution.VM.ByteCode bc)
        {
            _expression.Compile(bc);
            bc.EmitScalar();
        }

        /// <inheritdoc />
        public override DynValue Eval(ScriptExecutionContext context)
        {
            return _expression.Eval(context).ToScalar();
        }
    }
}

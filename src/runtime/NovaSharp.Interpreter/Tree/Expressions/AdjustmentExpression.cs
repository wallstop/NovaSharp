namespace NovaSharp.Interpreter.Tree.Expressions
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class AdjustmentExpression : Expression
    {
        private readonly Expression _expression;

        public AdjustmentExpression(ScriptLoadingContext lcontext, Expression exp)
            : base(lcontext)
        {
            _expression = exp;
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            _expression.Compile(bc);
            bc.EmitScalar();
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            return _expression.Eval(context).ToScalar();
        }
    }
}

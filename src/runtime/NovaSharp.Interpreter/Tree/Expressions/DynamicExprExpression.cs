namespace NovaSharp.Interpreter.Tree.Expressions
{
    using System;
    using Execution;
    using NovaSharp.Interpreter.DataTypes;

    internal class DynamicExprExpression : Expression
    {
        private readonly Expression _exp;

        public DynamicExprExpression(Expression exp, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            lcontext.Anonymous = true;
            _exp = exp;
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            return _exp.Eval(context);
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            throw new InvalidOperationException();
        }

        public override SymbolRef FindDynamic(ScriptExecutionContext context)
        {
            return _exp.FindDynamic(context);
        }
    }
}

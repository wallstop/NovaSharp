namespace NovaSharp.Interpreter.Tree.Expressions
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class ExprListExpression : Expression
    {
        private readonly List<Expression> _expressions;

        public ExprListExpression(List<Expression> exps, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _expressions = exps;
        }

        public Expression[] GetExpressions()
        {
            return _expressions.ToArray();
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            foreach (Expression exp in _expressions)
            {
                exp.Compile(bc);
            }

            if (_expressions.Count > 1)
            {
                bc.EmitMkTuple(_expressions.Count);
            }
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            if (_expressions.Count >= 1)
            {
                return _expressions[0].Eval(context);
            }

            return DynValue.Void;
        }
    }
}

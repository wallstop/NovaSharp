namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    /// Represents a comma-separated expression list (used for table constructors, argument lists, etc.).
    /// </summary>
    internal class ExprListExpression : Expression
    {
        private readonly List<Expression> _expressions;

        /// <summary>
        /// Initializes a new expression list node with the supplied expressions.
        /// </summary>
        public ExprListExpression(List<Expression> exps, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _expressions = exps;
        }

        /// <summary>
        /// Gets the underlying expressions in parsing order.
        /// </summary>
        public Expression[] GetExpressions()
        {
            return _expressions.ToArray();
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

namespace NovaSharp.Interpreter.Tree.Expressions
{
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents a dynamic expression placeholder (e.g., created via <c>?</c> prefix in the REPL).
    /// </summary>
    internal class DynamicExprExpression : Expression
    {
        private readonly Expression _exp;

        /// <summary>
        /// Initializes a new dynamic expression wrapper.
        /// </summary>
        public DynamicExprExpression(Expression exp, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            lcontext.Anonymous = true;
            _exp = exp;
        }

        /// <inheritdoc/>
        public override DynValue Eval(ScriptExecutionContext context)
        {
            return _exp.Eval(context);
        }

        /// <inheritdoc/>
        public override void Compile(Execution.VM.ByteCode bc)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc/>
        public override SymbolRef FindDynamic(ScriptExecutionContext context)
        {
            return _exp.FindDynamic(context);
        }
    }
}

namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using Expressions;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Represents a statement-level function call (where return values are discarded).
    /// </summary>
    internal class FunctionCallStatement : Statement
    {
        private readonly FunctionCallExpression _functionCallExpression;

        /// <summary>
        /// Wraps a previously parsed function-call expression so it can be emitted as a statement.
        /// </summary>
        /// <param name="lcontext">Parser context providing the lexer/token stream.</param>
        /// <param name="functionCallExpression">Expression node describing the call.</param>
        public FunctionCallStatement(
            ScriptLoadingContext lcontext,
            FunctionCallExpression functionCallExpression
        )
            : base(lcontext)
        {
            _functionCallExpression = functionCallExpression;
            lcontext.Source.Refs.Add(_functionCallExpression.SourceRef);
        }

        /// <summary>
        /// Emits the call and pops the discarded results.
        /// </summary>
        public override void Compile(ByteCode bc)
        {
            using (bc.EnterSource(_functionCallExpression.SourceRef))
            {
                _functionCallExpression.Compile(bc);
                RemoveBreakpointStop(bc.EmitPop());
            }
        }

        /// <summary>
        /// Ensures post-call pops do not create redundant breakpoints in the debugger.
        /// </summary>
        private static void RemoveBreakpointStop(Instruction instruction)
        {
            instruction.SourceCodeRef = null;
        }
    }
}

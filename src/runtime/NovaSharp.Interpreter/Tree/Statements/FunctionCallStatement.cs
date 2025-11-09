namespace NovaSharp.Interpreter.Tree.Statements
{
    using Execution;
    using Execution.VM;
    using Expressions;

    internal class FunctionCallStatement : Statement
    {
        private readonly FunctionCallExpression _functionCallExpression;

        public FunctionCallStatement(
            ScriptLoadingContext lcontext,
            FunctionCallExpression functionCallExpression
        )
            : base(lcontext)
        {
            _functionCallExpression = functionCallExpression;
            lcontext.Source.Refs.Add(_functionCallExpression.SourceRef);
        }

        public override void Compile(ByteCode bc)
        {
            using (bc.EnterSource(_functionCallExpression.SourceRef))
            {
                _functionCallExpression.Compile(bc);
                RemoveBreakpointStop(bc.Emit_Pop());
            }
        }

        private void RemoveBreakpointStop(Instruction instruction)
        {
            instruction.SourceCodeRef = null;
        }
    }
}

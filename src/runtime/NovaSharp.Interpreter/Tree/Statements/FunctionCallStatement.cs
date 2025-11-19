namespace NovaSharp.Interpreter.Tree.Statements
{
    using Expressions;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

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
                RemoveBreakpointStop(bc.EmitPop());
            }
        }

        private void RemoveBreakpointStop(Instruction instruction)
        {
            instruction.SourceCodeRef = null;
        }
    }
}

namespace NovaSharp.Interpreter.Tree.Statements
{
    using Debugging;
    using Execution;
    using Execution.Scopes;

    internal class ScopeBlockStatement : Statement
    {
        private readonly Statement _block;
        private readonly RuntimeScopeBlock _stackFrame;

        private readonly SourceRef _do;

        private readonly SourceRef _end;

        public ScopeBlockStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            lcontext.Scope.PushBlock();

            _do = CheckTokenType(lcontext, TokenType.Do).GetSourceRef();

            _block = new CompositeStatement(lcontext);

            _end = CheckTokenType(lcontext, TokenType.End).GetSourceRef();

            _stackFrame = lcontext.Scope.PopBlock();
            lcontext.Source.Refs.Add(_do);
            lcontext.Source.Refs.Add(_end);
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            using (bc.EnterSource(_do))
            {
                bc.Emit_Enter(_stackFrame);
            }

            _block.Compile(bc);

            using (bc.EnterSource(_end))
            {
                bc.Emit_Leave(_stackFrame);
            }
        }
    }
}

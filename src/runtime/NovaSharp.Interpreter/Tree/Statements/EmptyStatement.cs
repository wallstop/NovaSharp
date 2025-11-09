namespace NovaSharp.Interpreter.Tree.Statements
{
    using Execution;

    internal class EmptyStatement : Statement
    {
        public EmptyStatement(ScriptLoadingContext lcontext)
            : base(lcontext) { }

        public override void Compile(Execution.VM.ByteCode bc) { }
    }
}

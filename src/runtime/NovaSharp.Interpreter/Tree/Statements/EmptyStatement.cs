namespace NovaSharp.Interpreter.Tree.Statements
{
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class EmptyStatement : Statement
    {
        public EmptyStatement(ScriptLoadingContext lcontext)
            : base(lcontext) { }

        public override void Compile(Execution.VM.ByteCode bc) { }
    }
}

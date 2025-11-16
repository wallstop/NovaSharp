namespace NovaSharp.Interpreter.Execution
{
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class ScriptLoadingContext
    {
        public Script Script { get; private set; }
        public BuildTimeScope Scope { get; set; }
        public SourceCode Source { get; set; }
        public bool Anonymous { get; set; }
        public bool IsDynamicExpression { get; set; }
        public Lexer Lexer { get; set; }

        public ScriptLoadingContext(Script s)
        {
            Script = s;
        }
    }
}

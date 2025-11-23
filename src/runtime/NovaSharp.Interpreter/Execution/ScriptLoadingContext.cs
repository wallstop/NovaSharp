namespace NovaSharp.Interpreter.Execution
{
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Carries the shared state required while parsing/compiling a Lua chunk.
    /// </summary>
    internal class ScriptLoadingContext
    {
        /// <summary>
        /// Gets the script being compiled.
        /// </summary>
        public Script Script { get; private set; }

        /// <summary>
        /// Gets or sets the scope builder used to resolve locals and upvalues.
        /// </summary>
        public BuildTimeScope Scope { get; set; }

        /// <summary>
        /// Gets or sets the source information for diagnostics.
        /// </summary>
        public SourceCode Source { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the chunk has no explicit name (used for debugger labels).
        /// </summary>
        public bool Anonymous { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the context represents a dynamic expression (loadstring).
        /// </summary>
        public bool IsDynamicExpression { get; set; }

        /// <summary>
        /// Gets or sets the lexer delivering tokens to the parser.
        /// </summary>
        public Lexer Lexer { get; set; }

        /// <summary>
        /// Initializes a new loading context for the specified script.
        /// </summary>
        /// <param name="s">Script being compiled.</param>
        public ScriptLoadingContext(Script s)
        {
            Script = s;
        }
    }
}

namespace WallstopStudios.NovaSharp.Interpreter.Tree.Statements
{
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    /// Represents a no-op statement introduced by stray semicolons.
    /// </summary>
    internal class EmptyStatement : Statement
    {
        /// <inheritdoc />
        public override bool IsVoidStatement => true;

        /// <summary>
        /// Initializes a no-op statement; nothing is parsed beyond the triggering semicolon.
        /// </summary>
        public EmptyStatement(ScriptLoadingContext lcontext)
            : base(lcontext) { }

        /// <summary>
        /// Empty statements emit no bytecode.
        /// </summary>
        public override void Compile(Execution.VM.ByteCode bc) { }
    }
}

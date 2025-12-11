namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides the compiler hooks that materialize upvalues captured by nested functions.
    /// </summary>
    internal interface IClosureBuilder
    {
        /// <summary>
        /// Creates an upvalue pointing to the specified symbol when it needs to escape the current frame.
        /// </summary>
        /// <param name="scope">The scope requesting the capture.</param>
        /// <param name="symbol">The symbol being captured.</param>
        /// <returns>The upvalue symbol reference visible to the nested function.</returns>
        public SymbolRef CreateUpValue(BuildTimeScope scope, SymbolRef symbol);
    }
}

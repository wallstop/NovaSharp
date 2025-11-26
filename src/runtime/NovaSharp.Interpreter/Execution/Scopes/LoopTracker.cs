namespace NovaSharp.Interpreter.Execution.Scopes
{
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.Execution.VM;

    /// <summary>
    /// Represents a loop construct that can compile <c>break</c> statements into bytecode.
    /// </summary>
    internal interface ILoop
    {
        /// <summary>
        /// Emits the bytecode required to exit the loop when a <c>break</c> is parsed.
        /// </summary>
        /// <param name="bc">The bytecode buffer currently being built.</param>
        public void CompileBreak(ByteCode bc);

        /// <summary>
        /// Gets a value indicating whether the current loop is a boundary (breaks cannot escape past it).
        /// </summary>
        public bool IsBoundary();
    }

    /// <summary>
    /// Maintains a stack of active loops so <c>break</c> statements can target the correct block.
    /// </summary>
    internal class LoopTracker
    {
        /// <summary>
        /// Gets the stack of loop descriptors, pre-sized large enough for complex Lua chunks.
        /// </summary>
        public FastStack<ILoop> Loops { get; } = new(16384);
    }
}

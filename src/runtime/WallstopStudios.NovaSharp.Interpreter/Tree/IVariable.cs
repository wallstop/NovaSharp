namespace WallstopStudios.NovaSharp.Interpreter.Tree
{
    /// <summary>
    /// Marker for expressions that can participate in assignments (variables, table fields, etc.).
    /// </summary>
    internal interface IVariable
    {
        /// <summary>
        /// Emits the bytecode sequence that stores a value into the variable represented by the node.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the store instructions.</param>
        /// <param name="stackofs">Stack offset that points to the source value.</param>
        /// <param name="tupleidx">
        /// Tuple index consumed when the source expression returns multiple values.
        /// </param>
        public void CompileAssignment(Execution.VM.ByteCode bc, int stackofs, int tupleidx);
    }
}

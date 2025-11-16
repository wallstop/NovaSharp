namespace NovaSharp.Interpreter.Tree
{
    using NovaSharp.Interpreter.Tree.Lexer;

    internal interface IVariable
    {
        public void CompileAssignment(Execution.VM.ByteCode bc, int stackofs, int tupleidx);
    }
}

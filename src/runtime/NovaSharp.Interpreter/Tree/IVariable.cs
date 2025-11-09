namespace NovaSharp.Interpreter.Tree
{
    internal interface IVariable
    {
        public void CompileAssignment(Execution.VM.ByteCode bc, int stackofs, int tupleidx);
    }
}

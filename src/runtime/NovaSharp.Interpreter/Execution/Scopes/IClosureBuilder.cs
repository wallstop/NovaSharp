namespace NovaSharp.Interpreter.Execution
{
    internal interface IClosureBuilder
    {
        public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol);
    }
}

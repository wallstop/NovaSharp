namespace NovaSharp.Interpreter.Execution.Scopes
{
    using NovaSharp.Interpreter.DataTypes;

    internal interface IClosureBuilder
    {
        public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol);
    }
}

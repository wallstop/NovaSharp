namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    internal sealed partial class Processor
    {
        private void ClearBlockData(Instruction i)
        {
            int from = i.NumVal;
            int to = i.NumVal2;

            DynValue[] array = _executionStack.Peek().localScope;

            if (to >= 0 && from >= 0 && to >= from)
            {
                Array.Clear(array, from, to - from + 1);
            }
        }

        public DynValue GetGenericSymbol(SymbolRef symref)
        {
            switch (symref.i_Type)
            {
                case SymbolRefType.DefaultEnv:
                    return DynValue.NewTable(GetScript().Globals);
                case SymbolRefType.Global:
                    return GetGlobalSymbol(GetGenericSymbol(symref.i_Env), symref.i_Name);
                case SymbolRefType.Local:
                    return GetTopNonClrFunction().localScope[symref.i_Index];
                case SymbolRefType.Upvalue:
                    return GetTopNonClrFunction().closureScope[symref.i_Index];
                default:
                    throw new InternalErrorException(
                        "Unexpected {0} LRef at resolution: {1}",
                        symref.i_Type,
                        symref.i_Name
                    );
            }
        }

        private DynValue GetGlobalSymbol(DynValue dynValue, string name)
        {
            if (dynValue.Type != DataType.Table)
            {
                throw new InvalidOperationException($"_ENV is not a table but a {dynValue.Type}");
            }

            return dynValue.Table.Get(name);
        }

        private void SetGlobalSymbol(DynValue dynValue, string name, DynValue value)
        {
            if (dynValue.Type != DataType.Table)
            {
                throw new InvalidOperationException($"_ENV is not a table but a {dynValue.Type}");
            }

            dynValue.Table.Set(name, value ?? DynValue.Nil);
        }

        public void AssignGenericSymbol(SymbolRef symref, DynValue value)
        {
            switch (symref.i_Type)
            {
                case SymbolRefType.Global:
                    SetGlobalSymbol(GetGenericSymbol(symref.i_Env), symref.i_Name, value);
                    break;
                case SymbolRefType.Local:
                    {
                        CallStackItem stackframe = GetTopNonClrFunction();

                        DynValue v = stackframe.localScope[symref.i_Index];
                        if (v == null)
                        {
                            stackframe.localScope[symref.i_Index] = v = DynValue.NewNil();
                        }

                        v.Assign(value);
                    }
                    break;
                case SymbolRefType.Upvalue:
                    {
                        CallStackItem stackframe = GetTopNonClrFunction();

                        DynValue v = stackframe.closureScope[symref.i_Index];
                        if (v == null)
                        {
                            stackframe.closureScope[symref.i_Index] = v = DynValue.NewNil();
                        }

                        v.Assign(value);
                    }
                    break;
                case SymbolRefType.DefaultEnv:
                {
                    throw new ArgumentException("Can't AssignGenericSymbol on a DefaultEnv symbol");
                }
                default:
                    throw new InternalErrorException(
                        "Unexpected {0} LRef at resolution: {1}",
                        symref.i_Type,
                        symref.i_Name
                    );
            }
        }

        private CallStackItem GetTopNonClrFunction()
        {
            CallStackItem stackframe = null;

            for (int i = 0; i < _executionStack.Count; i++)
            {
                stackframe = _executionStack.Peek(i);

                if (stackframe.clrFunction == null)
                {
                    break;
                }
            }

            return stackframe;
        }

        public SymbolRef FindSymbolByName(string name)
        {
            if (_executionStack.Count > 0)
            {
                CallStackItem stackframe = GetTopNonClrFunction();

                if (stackframe != null)
                {
                    if (stackframe.debugSymbols != null)
                    {
                        for (int i = stackframe.debugSymbols.Length - 1; i >= 0; i--)
                        {
                            SymbolRef l = stackframe.debugSymbols[i];

                            if (l.i_Name == name && stackframe.localScope[i] != null)
                            {
                                return l;
                            }
                        }
                    }

                    ClosureContext closure = stackframe.closureScope;

                    if (closure != null)
                    {
                        for (int i = 0; i < closure.Symbols.Length; i++)
                        {
                            if (closure.Symbols[i] == name)
                            {
                                return SymbolRef.Upvalue(name, i);
                            }
                        }
                    }
                }
            }

            if (name != WellKnownSymbols.ENV)
            {
                SymbolRef env = FindSymbolByName(WellKnownSymbols.ENV);
                return SymbolRef.Global(name, env);
            }
            else
            {
                return SymbolRef.DefaultEnv;
            }
        }
    }
}

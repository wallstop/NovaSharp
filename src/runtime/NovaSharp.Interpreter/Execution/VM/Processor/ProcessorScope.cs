namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    /// <content>
    /// Implements symbol resolution and scope-management helpers.
    /// </content>
    internal sealed partial class Processor
    {
        private void ClearBlockData(Instruction i)
        {
            CallStackItem stackframe = _executionStack.Peek();

            if (i.SymbolList != null && i.SymbolList.Length > 0)
            {
                CloseSymbolsSubset(stackframe, i.SymbolList, DynValue.Nil);
            }

            int from = i.NumVal;
            int to = i.NumVal2;

            DynValue[] array = stackframe.LocalScope;

            if (to >= 0 && from >= 0 && to >= from)
            {
                Array.Clear(array, from, to - from + 1);
            }
        }

        private void CloseSymbolsSubset(
            CallStackItem stackframe,
            SymbolRef[] symbols,
            DynValue error
        )
        {
            if (symbols == null || symbols.Length == 0)
            {
                return;
            }

            foreach (SymbolRef sym in symbols)
            {
                stackframe.ToBeClosedIndices?.Remove(sym.IndexValue);

                if (stackframe.BlocksToClose != null)
                {
                    for (
                        int listIndex = stackframe.BlocksToClose.Count - 1;
                        listIndex >= 0;
                        listIndex--
                    )
                    {
                        List<SymbolRef> list = stackframe.BlocksToClose[listIndex];
                        int foundIndex = list.FindIndex(s => s.IndexValue == sym.IndexValue);
                        if (foundIndex >= 0)
                        {
                            list.RemoveAt(foundIndex);
                            break;
                        }
                    }
                }

                DynValue slot = stackframe.LocalScope[sym.IndexValue];

                if (slot != null && !slot.IsNil())
                {
                    DynValue previous = slot.Clone();
                    CloseValue(sym, previous, error);
                    slot.Assign(DynValue.Nil);
                }
            }
        }

        /// <summary>
        /// Evaluates the value represented by the specified symbol reference (locals, upvalues, globals, _ENV).
        /// </summary>
        public DynValue GetGenericSymbol(SymbolRef symref)
        {
            switch (symref.SymbolType)
            {
                case SymbolRefType.DefaultEnv:
                    return DynValue.NewTable(GetScript().Globals);
                case SymbolRefType.Global:
                    return GetGlobalSymbol(
                        GetGenericSymbol(symref.EnvironmentRef),
                        symref.NameValue
                    );
                case SymbolRefType.Local:
                    return GetTopNonClrFunction().LocalScope[symref.IndexValue];
                case SymbolRefType.UpValue:
                    return GetTopNonClrFunction().ClosureScope[symref.IndexValue];
                default:
                    throw new InternalErrorException(
                        "Unexpected {0} LRef at resolution: {1}",
                        symref.SymbolType,
                        symref.NameValue
                    );
            }
        }

        private static DynValue GetGlobalSymbol(DynValue dynValue, string name)
        {
            if (dynValue.Type != DataType.Table)
            {
                throw new InvalidOperationException($"_ENV is not a table but a {dynValue.Type}");
            }

            return dynValue.Table.Get(name);
        }

        private static void SetGlobalSymbol(DynValue dynValue, string name, DynValue value)
        {
            if (dynValue.Type != DataType.Table)
            {
                throw new InvalidOperationException($"_ENV is not a table but a {dynValue.Type}");
            }

            dynValue.Table.Set(name, value ?? DynValue.Nil);
        }

        /// <summary>
        /// Assigns the specified value to a symbol, honoring locals, upvalues, and globals.
        /// </summary>
        public void AssignGenericSymbol(SymbolRef symref, DynValue value)
        {
            switch (symref.SymbolType)
            {
                case SymbolRefType.Global:
                    SetGlobalSymbol(
                        GetGenericSymbol(symref.EnvironmentRef),
                        symref.NameValue,
                        value
                    );
                    break;
                case SymbolRefType.Local:
                    AssignLocal(symref, value);
                    break;
                case SymbolRefType.UpValue:
                    {
                        CallStackItem stackframe = GetTopNonClrFunction();

                        DynValue v = stackframe.ClosureScope[symref.IndexValue];
                        if (v == null)
                        {
                            stackframe.ClosureScope[symref.IndexValue] = v = DynValue.NewNil();
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
                        symref.SymbolType,
                        symref.NameValue
                    );
            }
        }

        private CallStackItem GetTopNonClrFunction()
        {
            CallStackItem stackframe = null;

            for (int i = 0; i < _executionStack.Count; i++)
            {
                stackframe = _executionStack.Peek(i);

                if (stackframe.ClrFunction == null)
                {
                    break;
                }
            }

            return stackframe;
        }

        /// <summary>
        /// Resolves a symbol reference by name, searching locals, closures, and finally the global environment.
        /// </summary>
        public SymbolRef FindSymbolByName(string name)
        {
            if (_executionStack.Count > 0)
            {
                CallStackItem stackframe = GetTopNonClrFunction();

                if (stackframe != null)
                {
                    if (stackframe.DebugSymbols != null)
                    {
                        for (int i = stackframe.DebugSymbols.Length - 1; i >= 0; i--)
                        {
                            SymbolRef l = stackframe.DebugSymbols[i];

                            if (l.NameValue == name && stackframe.LocalScope[i] != null)
                            {
                                return l;
                            }
                        }
                    }

                    ClosureContext closure = stackframe.ClosureScope;

                    if (closure != null)
                    {
                        for (int i = 0; i < closure.Symbols.Length; i++)
                        {
                            if (closure.Symbols[i] == name)
                            {
                                return SymbolRef.UpValue(name, i);
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

namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing table Lua iterators (pairs, ipairs, next)
    /// </summary>
    [NovaSharpModule]
    public static class TableIteratorsModule
    {
        // Cached callback DynValues to avoid allocation on every ipairs/pairs call
        private static readonly DynValue CachedNextArrayCallback = DynValue.NewCallback(NextArray);
        private static readonly DynValue CachedNextCallback = DynValue.NewCallback(Next);

        // ipairs (t)
        // -------------------------------------------------------------------------------------------------------------------
        // If t has a metamethod __ipairs, calls it with t as argument and returns the first three results from the call.
        // Otherwise, returns three values: an iterator function, the table t, and 0, so that the construction
        //	  for i,v in ipairs(t) do body end
        // will iterate over the pairs (1,t[1]), (2,t[2]), ..., up to the first integer key absent from the table.
        /// <summary>
        /// Implements Lua `ipairs`, respecting `__ipairs` metamethods and otherwise yielding the array iterator triple.
        /// </summary>
        [NovaSharpModuleMethod(Name = "ipairs")]
        public static DynValue Ipairs(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue table = args[0];

            DynValue meta = executionContext.GetMetamethodTailCall(
                table,
                Metamethods.IPairs,
                args.GetArray()
            );

            if (meta != null)
            {
                return meta;
            }

            // Per Lua spec: ipairs requires a table argument when no __ipairs metamethod exists
            if (table.Type != DataType.Table)
            {
                throw ScriptRuntimeException.BadArgument(
                    0,
                    "ipairs",
                    DataType.Table,
                    table.Type,
                    false
                );
            }

            return DynValue.NewTuple(CachedNextArrayCallback, table, DynValue.FromNumber(0));
        }

        // pairs (t)
        // -------------------------------------------------------------------------------------------------------------------
        // If t has a metamethod __pairs, calls it with t as argument and returns the first three results from the call.
        // Otherwise, returns three values: the next function, the table t, and nil, so that the construction
        //     for k,v in pairs(t) do body end
        // will iterate over all key–value pairs of table t.
        // See function next for the caveats of modifying the table during its traversal.
        /// <summary>
        /// Implements Lua `pairs`, honoring `__pairs` metamethods or returning the default `next` iterator triple.
        /// </summary>
        [NovaSharpModuleMethod(Name = "pairs")]
        public static DynValue Pairs(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue table = args[0];

            DynValue meta = executionContext.GetMetamethodTailCall(
                table,
                Metamethods.Pairs,
                args.GetArray()
            );

            if (meta != null)
            {
                return meta;
            }

            // Per Lua spec: pairs requires a table argument when no __pairs metamethod exists
            if (table.Type != DataType.Table)
            {
                throw ScriptRuntimeException.BadArgument(
                    0,
                    "pairs",
                    DataType.Table,
                    table.Type,
                    false
                );
            }

            return DynValue.NewTuple(CachedNextCallback, table);
        }

        // next (table [, index])
        // -------------------------------------------------------------------------------------------------------------------
        // Allows a program to traverse all fields of a table. Its first argument is a table and its second argument is an
        // index in this table. next returns the next index of the table and its associated value.
        // When called with nil as its second argument, next returns an initial index and its associated value.
        // When called with the last index, or with nil in an empty table, next returns nil. If the second argument is absent,
        // then it is interpreted as nil. In particular, you can use next(t) to check whether a table is empty.
        // The order in which the indices are enumerated is not specified, even for numeric indices.
        // (To traverse a table in numeric order, use a numerical for.)
        // The behavior of next is undefined if, during the traversal, you assign any value to a non-existent field in the table.
        // You may however modify existing fields. In particular, you may clear existing fields.
        /// <summary>
        /// Implements Lua `next`, returning successive key/value pairs for a table (§3.3.6).
        /// </summary>
        [NovaSharpModuleMethod(Name = "next")]
        public static DynValue Next(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue table = args.AsType(0, "next", DataType.Table);
            DynValue index = args[1];

            TablePair? pair = table.Table.NextKey(index);

            if (pair.HasValue)
            {
                return DynValue.NewTuple(pair.Value.Key, pair.Value.Value);
            }
            else
            {
                throw new ScriptRuntimeException("invalid key to 'next'");
            }
        }

        // __next_i (table [, index])
        // -------------------------------------------------------------------------------------------------------------------
        // Allows a program to traverse all fields of an array. index is an integer number
        // Lua 5.3+: ipairs respects __index metamethod (uses metamethod-aware indexing)
        // Lua 5.1/5.2: ipairs uses raw access (ignores __index metamethod)
        /// <summary>
        /// Internal helper that drives the array-style iterator used by `ipairs`.
        /// </summary>
        public static DynValue NextArray(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));
            DynValue table = args.AsType(0, "!!next_i!!", DataType.Table);
            DynValue index = args.AsType(1, "!!next_i!!", DataType.Number);

            int idx = ((int)index.Number) + 1;

            // Lua 5.3+: ipairs respects __index metamethod
            // Lua 5.1/5.2: ipairs uses raw access (ignores __index)
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            DynValue val;
            if (version >= LuaCompatibilityVersion.Lua53)
            {
                val = GetTableValueWithMetamethods(executionContext, table, idx);
            }
            else
            {
                val = table.Table.Get(idx);
            }

            if (val.Type != DataType.Nil)
            {
                return DynValue.NewTuple(DynValue.FromNumber(idx), val);
            }
            else
            {
                return DynValue.Nil;
            }
        }

        /// <summary>
        /// Gets a value from a table, respecting __index metamethods.
        /// This mimics the VM's index operation for metamethod-aware access.
        /// </summary>
        /// <param name="executionContext">The execution context for calling metamethods.</param>
        /// <param name="table">The table DynValue to index.</param>
        /// <param name="key">The integer key to look up.</param>
        /// <returns>The value at the given key, or DynValue.Nil if not found.</returns>
        private static DynValue GetTableValueWithMetamethods(
            ScriptExecutionContext executionContext,
            DynValue table,
            int key
        )
        {
            const int MaxMetamethodDepth = 10;
            DynValue current = table;
            DynValue keyValue = DynValue.FromNumber(key);

            for (int depth = 0; depth < MaxMetamethodDepth; depth++)
            {
                if (current.Type == DataType.Table)
                {
                    // First try raw access
                    DynValue rawVal = current.Table.Get(key);
                    if (!rawVal.IsNil())
                    {
                        return rawVal;
                    }

                    // Raw value is nil, check for __index metamethod
                    DynValue indexMeta = executionContext.GetMetamethod(current, Metamethods.Index);
                    if (indexMeta == null || indexMeta.IsNil())
                    {
                        // No __index metamethod, return nil
                        return DynValue.Nil;
                    }

                    if (
                        indexMeta.Type == DataType.Function
                        || indexMeta.Type == DataType.ClrFunction
                    )
                    {
                        // __index is a function: call it with (table, key)
                        return executionContext.Call(indexMeta, current, keyValue);
                    }
                    else
                    {
                        // __index is a table (or other value): continue lookup in that table
                        current = indexMeta;
                    }
                }
                else
                {
                    // Non-table value encountered in chain, return nil
                    return DynValue.Nil;
                }
            }

            // Max depth exceeded, prevent infinite loops
            throw new ScriptRuntimeException("'__index' chain too long; possible loop");
        }
    }
}

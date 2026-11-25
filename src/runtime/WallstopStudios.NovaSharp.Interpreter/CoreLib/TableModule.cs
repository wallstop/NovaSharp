namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using global::NovaSharp;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing table Lua functions
    /// </summary>
    [NovaSharpModule(Namespace = "table")]
    internal static class TableModule
    {
        /// <summary>
        /// Struct-based comparer for table.sort to avoid closure allocations (Initiative 12 Phase 4).
        /// </summary>
        private readonly struct LuaSortComparer : IComparer<LuaValue>
        {
            private readonly ScriptExecutionContext _ctx;
            private readonly LuaValue _lt;
            private readonly bool _supportsIntegerSubtype;

            public LuaSortComparer(
                ScriptExecutionContext ctx,
                LuaValue lt,
                bool supportsIntegerSubtype
            )
            {
                _ctx = ctx;
                _lt = lt;
                _supportsIntegerSubtype = supportsIntegerSubtype;
            }

            public int Compare(LuaValue a, LuaValue b) =>
                SortComparer(_ctx, a, b, _lt, _supportsIntegerSubtype);
        }

        /// <summary>
        /// Implements Lua `table.unpack`, returning a tuple of array elements between the provided indices (§6.6).
        /// This function was added in Lua 5.2, replacing the global <c>unpack</c> function from Lua 5.1.
        /// </summary>
        [LuaCompatibility(LuaCompatibilityVersion.Lua52)]
        [Preserve]
        [NovaSharpModuleMethod("unpack")]
        private static LuaValue Unpack(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaValue s = args.AsType(0, "unpack", DataType.Table, false);
            LuaValue vi = args.AsType(1, "unpack", DataType.Number, true);
            LuaValue vj = args.AsType(2, "unpack", DataType.Number, true);

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            // Lua 5.3+: require integer representation; Lua 5.1/5.2: silently truncate
            int ii = vi.IsNil
                ? 1
                : (int)Utilities.LuaNumberHelpers.ToLongWithValidation(version, vi, "unpack", 2);
            int ij = vj.IsNil
                ? GetTableLength(executionContext, s)
                : (int)Utilities.LuaNumberHelpers.ToLongWithValidation(version, vj, "unpack", 3);

            Table t = s.Table;
            int count = ij - ii + 1;

            // Fast path for empty range
            if (count <= 0)
            {
                return LuaValue.Void;
            }

            // Fast path for single element - avoid array allocation
            if (count == 1)
            {
                return t.Get(ii);
            }

            LuaValue[] v = new LuaValue[count];

            int tidx = 0;
            for (int i = ii; i <= ij; i++)
            {
                v[tidx++] = t.Get(i);
            }

            return LuaValue.NewTuple(v);
        }

        /// <summary>
        /// Implements Lua 5.1's `table.maxn`, returning the largest positive numeric key in a table (§5.5).
        /// </summary>
        /// <remarks>
        /// This function was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// It returns 0 if the table has no positive numeric keys.
        /// Unlike #t, this function scans all keys, not just the array portion.
        /// </remarks>
        /// <param name="args">Arguments (the table to scan).</param>
        /// <returns>The largest positive numeric key, or 0 if none exist.</returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua52)]
        [Preserve]
        [NovaSharpModuleMethod("maxn")]
        private static LuaValue MaxN(CallbackArgumentsView args)
        {
            LuaValue vTable = args.AsType(0, "maxn", DataType.Table, false);
            Table table = vTable.Table;

            double maxKey = 0;

            foreach (TablePair pair in table.GetNonNilPairsEnumerator())
            {
                if (pair.Key.Type == DataType.Number)
                {
                    double key = pair.Key.Number;
                    if (key > maxKey)
                    {
                        maxKey = key;
                    }
                }
            }

            return LuaValue.NewNumber(maxKey);
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [Preserve]
        [NovaSharpModuleMethod("getn")]
        private static LuaValue GetN(CallbackArgumentsView args)
        {
            Table table = args.AsType(0, "getn", DataType.Table).Table;
            return LuaValue.NewNumber(table.Length);
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [Preserve]
        [NovaSharpModuleMethod("setn")]
        private static LuaValue SetN(CallbackArgumentsView args)
        {
            args.AsType(0, "setn", DataType.Table);
            // Stock Lua 5.1 leaves LUA_COMPAT_GETN disabled.
            throw new ScriptRuntimeException("'setn' is obsolete");
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [Preserve]
        [NovaSharpModuleMethod("foreach")]
        private static LuaValue ForEach(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            Table table = args.AsType(0, "foreach", DataType.Table).Table;
            LuaValue action = GetIterationCallback(args, "foreach");
            LuaValue key = LuaValue.Nil;
            using Processor.NativeInstructionBudgetScope budget =
                executionContext.EnterNativeInstructionBudget();
            bool chargeInstruction = executionContext.HasActiveSandboxInstructionBudget;

            while (true)
            {
                if (chargeInstruction)
                {
                    executionContext.ChargeSandboxInstruction();
                }

                TablePair? next = table.NextKey(key);
                if (!next.HasValue)
                {
                    throw new ScriptRuntimeException("invalid key to 'next'");
                }

                TablePair pair = next.Value;
                key = pair.Key;
                if (key.IsNil)
                {
                    return LuaValue.Void;
                }

                LuaValue result = executionContext
                    .Script.CallLibraryCallback(action, key, pair.Value)
                    .ToScalar();
                if (result.IsNotNil())
                {
                    return result;
                }
            }
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [Preserve]
        [NovaSharpModuleMethod("foreachi")]
        private static LuaValue ForEachI(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            Table table = args.AsType(0, "foreachi", DataType.Table).Table;
            int length = table.Length;
            LuaValue action = GetIterationCallback(args, "foreachi");
            using Processor.NativeInstructionBudgetScope budget =
                executionContext.EnterNativeInstructionBudget();
            bool chargeInstruction = executionContext.HasActiveSandboxInstructionBudget;
            for (int i = 1; i <= length; i++)
            {
                if (chargeInstruction)
                {
                    executionContext.ChargeSandboxInstruction();
                }

                LuaValue result = executionContext
                    .Script.CallLibraryCallback(action, LuaValue.NewNumber(i), table.Get(i))
                    .ToScalar();
                if (result.IsNotNil())
                {
                    return result;
                }
            }

            return LuaValue.Void;
        }

        private static LuaValue GetIterationCallback(CallbackArgumentsView args, string function)
        {
            LuaValue action = args[1];
            return action.Type == DataType.ClrFunction
                ? action
                : args.AsType(1, function, DataType.Function);
        }

        [LuaCompatibility(LuaCompatibilityVersion.Lua55)]
        [Preserve]
        [NovaSharpModuleMethod("create")]
        private static LuaValue Create(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            long sequence = CheckCapacityInteger(args[0], 0);
            long records = args[1].IsNil ? 0 : CheckCapacityInteger(args[1], 1);
            if (sequence < 0 || sequence > int.MaxValue)
            {
                throw ScriptRuntimeException.BadArgument(0, "create", "out of range");
            }

            if (records < 0 || records > int.MaxValue)
            {
                throw ScriptRuntimeException.BadArgument(1, "create", "out of range");
            }

            // Reference Lua's hash size must fit a positive C int after rounding up.
            if (records > 1 << 30)
            {
                throw new ScriptRuntimeException("table overflow");
            }

            return LuaValue.NewTable(
                new Table(executionContext.Script, (int)sequence, (int)records)
            );
        }

        private static long CheckCapacityInteger(LuaValue argument, int index)
        {
            const string function = "create";
            LuaNumber? number = GetTableArgumentNumber(argument, LuaCompatibilityVersion.Lua55);
            if (number.HasValue)
            {
                return Utilities.LuaNumberHelpers.ToLongWithValidation(
                    LuaCompatibilityVersion.Lua55,
                    LuaValue.NewNumber(number.Value),
                    function,
                    index + 1
                );
            }

            throw argument.IsVoid()
                ? ScriptRuntimeException.BadArgumentNoValue(index, function, DataType.Number)
                : ScriptRuntimeException.BadArgument(
                    index,
                    function,
                    DataType.Number,
                    argument.Type,
                    allowNil: false
                );
        }

        /// <summary>
        /// Implements Lua `table.pack`, wrapping arbitrary arguments into a table with field `n` (§6.6).
        /// This function was added in Lua 5.2.
        /// </summary>
        [LuaCompatibility(LuaCompatibilityVersion.Lua52)]
        [Preserve]
        [NovaSharpModuleMethod("pack")]
        private static LuaValue Pack(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            Table t = new(executionContext.Script);
            LuaValue v = LuaValue.NewTable(t);

            for (int i = 0; i < args.Count; i++)
            {
                t.Set(i + 1, args[i]);
            }

            t.Set("n", LuaValue.FromNumber(args.Count));

            return v;
        }

        /// <summary>
        /// Implements Lua `table.sort`, sorting in place with an optional comparator and returning no values.
        /// </summary>
        [Preserve]
        [NovaSharpModuleMethod("sort")]
        private static LuaValue Sort(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaValue vlist = args.AsType(0, "sort", DataType.Table, false);
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool supportsIntegerSubtype = version >= LuaCompatibilityVersion.Lua53;
            long length = GetLibrarySequenceLength(executionContext, vlist, version);

            // Lua 5.3 moved comparator validation inside the nontrivial sort path.
            if (supportsIntegerSubtype)
            {
                if (length <= 1)
                {
                    return LuaValue.Void;
                }

                if (length >= int.MaxValue)
                {
                    throw ScriptRuntimeException.BadArgument(0, "sort", "array too big");
                }
            }

            LuaValue lt = args[1];

            if (lt.Type != DataType.Function && lt.Type != DataType.ClrFunction && lt.IsNotNil())
            {
                _ = args.AsType(1, "sort", DataType.Function);
            }

            if (length <= 1)
            {
                return LuaValue.Void;
            }

            int end = (int)length;
            using (ListPool<LuaValue>.Get(end, out List<LuaValue> values))
            {
                for (int i = 1; i <= end; i++)
                {
                    values.Add(vlist.Table.Get(i));
                }

                try
                {
                    // Use struct comparer with boxing-free pdqsort (Initiative 16)
                    values.Sort<LuaValue, LuaSortComparer>(
                        new LuaSortComparer(executionContext, lt, supportsIntegerSubtype)
                    );
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.InnerException is ScriptRuntimeException)
                    {
                        throw ex.InnerException;
                    }
                }

                for (int i = 0; i < values.Count; i++)
                {
                    vlist.Table.Set(i + 1, values[i]);
                }

                return LuaValue.Void;
            }
        }

        private static int SortComparer(
            ScriptExecutionContext executionContext,
            LuaValue a,
            LuaValue b,
            LuaValue lt,
            bool supportsIntegerSubtype
        )
        {
            if (lt.IsNil)
            {
                if (a.Type == DataType.Number && b.Type == DataType.Number)
                {
                    return a.LuaNumber.CompareForSort(b.LuaNumber, supportsIntegerSubtype);
                }

                if (a.Type == DataType.String && b.Type == DataType.String)
                {
                    return string.Compare(a.String, b.String, StringComparison.Ordinal);
                }

                if (
                    !executionContext.TryGetBinaryMetamethod(a, b, Metamethods.Lt, out lt)
                    || lt.IsNil
                )
                {
                    throw ScriptRuntimeException.CompareInvalidType(a, b);
                }
            }

            return LuaComparerToClrComparer(
                executionContext.Script.CallLibraryCallback(lt, a, b),
                executionContext.Script.CallLibraryCallback(lt, b, a)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LuaComparerToClrComparer(LuaValue dynValue1, LuaValue dynValue2)
        {
            bool v1 = dynValue1.ToScalar().CastToBool();
            bool v2 = dynValue2.ToScalar().CastToBool();

            if (v1 && !v2)
            {
                return -1;
            }

            if (v2 && !v1)
            {
                return 1;
            }

            if (v1 || v2)
            {
                throw new ScriptRuntimeException("invalid order function for sorting");
            }

            return 0;
        }

        /// <summary>
        /// Implements Lua `table.insert`, inserting a value at the specified position (§6.6).
        /// </summary>
        [Preserve]
        [NovaSharpModuleMethod("insert")]
        private static LuaValue Insert(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            LuaValue tableValue = args[0];
            Table list = tableValue.Type == DataType.Table ? tableValue.Table : null;
            if (
                list == null
                && (
                    version <= LuaCompatibilityVersion.Lua52
                    || !HasInsertProxyMetatable(executionContext.GetMetatable(tableValue))
                )
            )
            {
                _ = args.AsType(0, "table.insert", DataType.Table);
            }

            // A length hook and the indexed access it enables share the caller's budget.
            using Processor.NativeInstructionBudgetScope budget =
                version >= LuaCompatibilityVersion.Lua52 && (list == null || list.MetaTable != null)
                    ? executionContext.EnterNativeInstructionBudget()
                    : default;

            long length = GetLibrarySequenceLength(executionContext, tableValue, version);
            long end =
                version <= LuaCompatibilityVersion.Lua52
                    ? unchecked((int)length + 1)
                    : unchecked(length + 1);
            // The length hook can replace the metatable or install an indexing hook.
            Table metatable = list?.MetaTable;
            bool needsIndexedAccess =
                version >= LuaCompatibilityVersion.Lua53
                && (
                    list == null
                    || (
                        metatable != null
                        && (
                            !metatable.Get(Metamethods.Index).IsNil
                            || !metatable.Get(Metamethods.NewIndex).IsNil
                        )
                    )
                );

            if (args.Count == 2)
            {
                if (needsIndexedAccess)
                {
                    AccessTableIntegerIndex(executionContext, tableValue, end, args[1]);
                }
                else
                {
                    SetInsertValue(list, end, args[1]);
                }

                return LuaValue.Void;
            }

            if (args.Count != 3)
            {
                throw new ScriptRuntimeException("wrong number of arguments to 'insert'");
            }

            LuaValue positionArgument = args[1];
            LuaNumber? number = GetTableArgumentNumber(positionArgument, version);
            if (!number.HasValue)
            {
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "table.insert",
                    DataType.Number,
                    positionArgument.Type,
                    false
                );
            }

            LuaNumber numericPosition = number.Value;
            long position;
            if (version <= LuaCompatibilityVersion.Lua52)
            {
                position = NarrowInsertNumber(numericPosition);
            }
            else
            {
                position = Utilities.LuaNumberHelpers.ToLongWithValidation(
                    version,
                    LuaValue.NewNumber(numericPosition),
                    "insert",
                    2
                );
            }

            // Lua 5.4 and 5.5 compare pos - 1 and end as unsigned Lua integers.
            bool outOfBounds =
                version >= LuaCompatibilityVersion.Lua54
                    ? unchecked((ulong)position - 1) >= unchecked((ulong)end)
                    : position < 1 || position > end;
            if (version != LuaCompatibilityVersion.Lua51 && outOfBounds)
            {
                throw ScriptRuntimeException.BadArgument(1, "insert", "position out of bounds");
            }

            if (needsIndexedAccess)
            {
                for (long i = end; i > position; i--)
                {
                    LuaValue value = AccessTableIntegerIndex(executionContext, tableValue, i - 1);
                    AccessTableIntegerIndex(executionContext, tableValue, i, value);
                }

                AccessTableIntegerIndex(executionContext, tableValue, position, args[2]);
                return LuaValue.Void;
            }

            if (end > position)
            {
                // Sparse ranges can span billions of absent keys. Visit occupied entries instead.
                if (unchecked((ulong)(end - position)) > (ulong)list.Count)
                {
                    ShiftSparseInsertRange(list, position, end);
                }
                else if (end <= int.MaxValue && position >= int.MinValue)
                {
                    for (int i = (int)end; i > (int)position; i--)
                    {
                        list.Set(i, list.Get(i - 1));
                    }
                }
                else
                {
                    for (long i = end; i > position; i--)
                    {
                        SetInsertValue(list, i, list.GetValue(LuaValue.NewInteger(i - 1)));
                    }
                }
            }

            SetInsertValue(list, position, args[2]);
            return LuaValue.Void;
        }

        private static bool HasInsertProxyMetatable(Table metatable)
        {
            // Lua checks for raw non-nil fields, even if the hooks will fail when used.
            return metatable != null
                && !metatable.Get(Metamethods.Index).IsNil
                && !metatable.Get(Metamethods.NewIndex).IsNil
                && !metatable.Get(Metamethods.Len).IsNil;
        }

        private static LuaValue AccessTableIntegerIndex(
            ScriptExecutionContext executionContext,
            LuaValue target,
            long index,
            LuaValue? assignedValue = null
        )
        {
            LuaValue key = LuaValue.NewInteger(index);
            bool isWrite = assignedValue.HasValue;
            string eventName = isWrite ? Metamethods.NewIndex : Metamethods.Index;
            bool chargeInstruction = executionContext.HasActiveSandboxInstructionBudget;
            const int maxHookHops = 2000;
            for (int followed = 0; ; followed++)
            {
                if (chargeInstruction)
                {
                    executionContext.ChargeSandboxInstruction();
                }

                Table table = target.Type == DataType.Table ? target.Table : null;
                LuaValue stored = table?.GetValue(key) ?? LuaValue.Nil;
                // Lua checks the raw slot after following the last permitted hook.
                if (followed == maxHookHops && stored.IsNil)
                {
                    throw new ScriptRuntimeException(
                        "'{0}' chain too long; possible loop",
                        eventName
                    );
                }

                LuaValue hook = stored.IsNil
                    ? executionContext.GetMetatable(target)?.Get(eventName) ?? LuaValue.Nil
                    : LuaValue.Nil;
                if (hook.IsNil)
                {
                    if (table == null)
                    {
                        throw ScriptRuntimeException.IndexType(target);
                    }

                    if (isWrite)
                    {
                        table.SetValue(key, assignedValue.GetValueOrDefault());
                    }

                    return stored;
                }

                if (hook.Type == DataType.Function || hook.Type == DataType.ClrFunction)
                {
                    if (isWrite)
                    {
                        executionContext.Script.CallLibraryCallback(
                            hook,
                            target,
                            key,
                            assignedValue
                        );
                        return LuaValue.Void;
                    }

                    LuaValue result = executionContext
                        .Script.CallLibraryCallback(hook, target, key)
                        .ToScalar();
                    return result.IsNil ? LuaValue.Nil : result;
                }

                target = hook;
            }
        }

        private static void SetInsertValue(Table list, long index, LuaValue value)
        {
            if (index >= int.MinValue && index <= int.MaxValue)
            {
                list.Set((int)index, value);
            }
            else
            {
                list.SetValue(LuaValue.NewInteger(index), value);
            }
        }

        private static long GetLibrarySequenceLength(
            ScriptExecutionContext executionContext,
            LuaValue tableValue,
            LuaCompatibilityVersion version
        )
        {
            if (tableValue.Type == DataType.String)
            {
                return tableValue.String.Length;
            }

            if (
                version == LuaCompatibilityVersion.Lua51
                || !executionContext.TryGetMetamethod(
                    tableValue,
                    Metamethods.Len,
                    out LuaValue lengthMetamethod
                )
            )
            {
                return tableValue.Table.Length;
            }

            LuaValue length = executionContext
                .Script.CallLibraryCallback(lengthMetamethod, tableValue, tableValue)
                .ToScalar();
            LuaNumber? number = GetTableArgumentNumber(length, version);
            if (version == LuaCompatibilityVersion.Lua52)
            {
                if (!number.HasValue)
                {
                    throw new ScriptRuntimeException("object length is not a number");
                }

                return NarrowInsertNumber(number.Value);
            }

            if (!number.HasValue || !number.Value.TryToInteger(out long integerLength))
            {
                throw new ScriptRuntimeException("object length is not an integer");
            }

            return integerLength;
        }

        private static int NarrowInsertNumber(LuaNumber number)
        {
            // Lua 5.1 and 5.2 truncate toward zero before narrowing to C int.
            double value = number.AsFloat;
            return value >= long.MinValue && value < -(double)long.MinValue
                ? unchecked((int)(long)value)
                : 0;
        }

        private static LuaNumber? GetTableArgumentNumber(
            LuaValue argument,
            LuaCompatibilityVersion version
        )
        {
            if (argument.Type == DataType.Number)
            {
                return argument.LuaNumber;
            }

            if (argument.Type != DataType.String)
            {
                return null;
            }

            string text = argument.String;
            ReadOnlySpan<char> numeral = text.AsSpan();
            int terminator = numeral.IndexOf('\0');
            if (terminator >= 0)
            {
                // Lua 5.1 converts a C string. Later versions require the full Lua string.
                if (version != LuaCompatibilityVersion.Lua51)
                {
                    return null;
                }

                numeral = numeral.Slice(0, terminator);
            }

            // The shared parser also trims Unicode whitespace, which Lua numerals reject.
            foreach (char character in numeral)
            {
                if (character > 0x7f)
                {
                    return null;
                }
            }

            string numericText = terminator >= 0 ? text.Substring(0, terminator) : text;
            return LuaNumber.TryParse(numericText, version, out LuaNumber number) ? number : null;
        }

        private static void ShiftSparseInsertRange(Table list, long position, long end)
        {
            using PooledResource<List<TablePair>> lease = ListPool<TablePair>.Get(
                out List<TablePair> entries
            );
            foreach (TablePair pair in list.GetNonNilPairsEnumerator())
            {
                if (
                    pair.Key.Type == DataType.Number
                    && pair.Key.LuaNumber.TryToInteger(out long key)
                    && key >= position
                    && key <= end
                )
                {
                    entries.Add(pair);
                }
            }

            // Keep the source values before clearing overwritten slots, including the old end.
            foreach (TablePair pair in entries)
            {
                list.SetValue(pair.Key, LuaValue.Nil);
            }

            foreach (TablePair pair in entries)
            {
                long key = pair.Key.LuaNumber.AsInteger;
                if (key < end)
                {
                    SetInsertValue(list, key + 1, pair.Value);
                }
            }
        }

        /// <summary>
        /// Implements Lua `table.remove`, removing and returning a value at the given position (§6.6).
        /// </summary>
        [Preserve]
        [NovaSharpModuleMethod("remove")]
        private static LuaValue Remove(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaValue vlist = args.AsType(0, "table.remove", DataType.Table, false);
            LuaValue vpos = args.AsType(1, "table.remove", DataType.Number, true);
            LuaValue ret = LuaValue.Nil;

            // Note: Lua silently ignores extra arguments (does NOT throw an error)
            // This behavior is consistent across Lua 5.1, 5.2, 5.3, and 5.4

            int len = GetTableLength(executionContext, vlist);
            Table list = vlist.Table;
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            // Lua 5.3+: require integer representation; Lua 5.1/5.2: silently truncate
            int pos = vpos.IsNil
                ? len
                : (int)Utilities.LuaNumberHelpers.ToLongWithValidation(version, vpos, "remove", 2);

            if (pos >= len + 1 || (pos < 1 && len > 0))
            {
                throw new ScriptRuntimeException(
                    "bad argument #1 to 'remove' (position out of bounds)"
                );
            }

            for (int i = pos; i <= len; i++)
            {
                if (i == pos)
                {
                    ret = list.Get(i);
                }

                list.Set(i, list.Get(i + 1));
            }

            return ret;
        }

        //table.concat (list [, sep [, i [, j]]])
        //Given a list where all elements are strings or numbers, returns the string list[i]..sep..list[i+1] (...) sep..list[j].
        //The default value for sep is the empty string, the default for i is 1, and the default for j is #list. If i is greater
        //than j, returns the empty string.
        /// <summary>
        /// Implements Lua `table.concat`, concatenating array elements with an optional separator (§6.6).
        /// </summary>
        [Preserve]
        [NovaSharpModuleMethod("concat")]
        private static LuaValue Concat(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            LuaValue vlist = args.AsType(0, "concat", DataType.Table, false);
            LuaValue vsep = args.AsType(executionContext, 1, "concat", DataType.String, true);
            LuaValue vstart = args.AsType(2, "concat", DataType.Number, true);
            LuaValue vend = args.AsType(3, "concat", DataType.Number, true);

            Table list = vlist.Table;
            string sep = vsep.IsNil ? "" : vsep.String;
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            // Lua 5.3+: require integer representation; Lua 5.1/5.2: silently truncate
            int start = vstart.IsNilOrNan()
                ? 1
                : (int)
                    Utilities.LuaNumberHelpers.ToLongWithValidation(version, vstart, "concat", 3);
            int end;

            if (vend.IsNilOrNan())
            {
                end = GetTableLength(executionContext, vlist);
            }
            else
            {
                // Lua 5.3+: require integer representation; Lua 5.1/5.2: silently truncate
                end = (int)
                    Utilities.LuaNumberHelpers.ToLongWithValidation(version, vend, "concat", 4);
            }

            if (end < start)
            {
                return LuaValue.NewString(string.Empty);
            }

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            for (int i = start; i <= end; i++)
            {
                LuaValue v = list.Get(i);

                if (v.Type != DataType.Number && v.Type != DataType.String)
                {
                    throw new ScriptRuntimeException(
                        "invalid value ({1}) at index {0} in table for 'concat'",
                        i,
                        v.Type.ToLuaTypeString()
                    );
                }

                string s = v.ToPrintString(version);

                if (i != start)
                {
                    sb.Append(sep);
                }

                sb.Append(s);
            }

            return LuaValue.NewString(sb.ToString());
        }

        /// <summary>
        /// Implements Lua 5.3 `table.move`, copying values between tables with overlap handling (§6.6).
        /// </summary>
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        [Preserve]
        [NovaSharpModuleMethod("move")]
        private static LuaValue Move(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            const string func = "move";
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            Table source = args.AsType(0, func, DataType.Table, false).Table;
            // table.move is Lua 5.3+ only, so always require integer representation
            LuaValue vFrom = args.AsType(1, func, DataType.Number, false);
            LuaValue vTo = args.AsType(2, func, DataType.Number, false);
            LuaValue vTarget = args.AsType(3, func, DataType.Number, false);
            int from = (int)
                Utilities.LuaNumberHelpers.ToLongWithValidation(version, vFrom, func, 2);
            int to = (int)Utilities.LuaNumberHelpers.ToLongWithValidation(version, vTo, func, 3);
            int target = (int)
                Utilities.LuaNumberHelpers.ToLongWithValidation(version, vTarget, func, 4);
            Table destination =
                (args.Count >= 5 && !args[4].IsNil)
                    ? args.AsType(4, func, DataType.Table, false).Table
                    : source;

            int elementsToCopy = to - from;

            if (elementsToCopy >= 0)
            {
                int offset = target - from;

                if (destination == source && offset > 0 && target <= to)
                {
                    for (int i = elementsToCopy; i >= 0; i--)
                    {
                        int srcIndex = from + i;
                        int destIndex = srcIndex + offset;
                        LuaValue value = source.Get(srcIndex);
                        destination.Set(destIndex, value);
                    }
                }
                else
                {
                    for (int i = 0; i <= elementsToCopy; i++)
                    {
                        int srcIndex = from + i;
                        int destIndex = srcIndex + offset;
                        LuaValue value = source.Get(srcIndex);
                        destination.Set(destIndex, value);
                    }
                }
            }

            return LuaValue.NewTable(destination);
        }

        private static int GetTableLength(ScriptExecutionContext executionContext, LuaValue vlist)
        {
            if (
                executionContext.TryGetMetamethod(
                    vlist,
                    Metamethods.Len,
                    out LuaValue lengthMetamethod
                )
            )
            {
                LuaValue lenv = executionContext.Script.CallValues(lengthMetamethod, vlist);

                double? lengthValue = lenv.CastToNumber();

                if (lengthValue == null)
                {
                    throw new ScriptRuntimeException("object length is not a number");
                }

                return (int)lengthValue;
            }
            return (int)vlist.Table.Length;
        }

        /// <summary>
        /// Registers the Lua 5.1 global <c>unpack</c> entry point.
        /// </summary>
        [NovaSharpModule]
        internal static class Globals
        {
            [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
            [Preserve]
            [NovaSharpModuleMethod("unpack")]
            private static LuaValue Unpack(
                ScriptExecutionContext executionContext,
                CallbackArgumentsView args
            )
            {
                return TableModule.Unpack(executionContext, args);
            }
        }
    }
}

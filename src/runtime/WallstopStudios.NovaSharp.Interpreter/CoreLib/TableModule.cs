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
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing table Lua functions
    /// </summary>
    [NovaSharpModule(Namespace = "table")]
    internal static partial class TableModule
    {
        /// <summary>
        /// Struct-based comparer for table.sort to avoid closure allocations (Initiative 12 Phase 4).
        /// </summary>
        private readonly struct LuaSortComparer : IComparer<LuaValue>
        {
            private readonly ScriptExecutionContext _ctx;
            private readonly LuaValue _lt;

            public LuaSortComparer(ScriptExecutionContext ctx, LuaValue lt)
            {
                _ctx = ctx;
                _lt = lt;
            }

            public int Compare(LuaValue a, LuaValue b) => SortComparer(_ctx, a, b, _lt);
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

            const string function = "unpack";
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool raw = version <= LuaCompatibilityVersion.Lua52;
            LuaValue value = raw ? args.AsType(0, function, DataType.Table) : args[0];
            long first = args[1].IsNil ? 1 : CheckTableInteger(args[1], version, function, 1);
            Processor processor = executionContext.NativeInstructionBudgetProcessor;
            using Processor.NativeInstructionBudgetScope budget =
                processor.EnterNativeInstructionBudget();
            long last = args[2].IsNil
                ? GetLibrarySequenceLength(executionContext, value, version)
                : CheckTableInteger(args[2], version, function, 2);
            if (first > last)
            {
                return LuaValue.Void;
            }

            // Unsigned subtraction preserves the range even across the signed endpoints.
            ulong distance = unchecked((ulong)last - (ulong)first);
            const int lua51MaxCStack = 8000;
            int referenceCapacity =
                version == LuaCompatibilityVersion.Lua51
                    ? lua51MaxCStack - args.Count
                    : VmStackDefaults.ValueStackMaxCapacity;
            int capacity = Math.Min(referenceCapacity, processor.AvailableValueStackSlots);

            if (capacity <= 0 || distance >= (ulong)capacity)
            {
                throw new ScriptRuntimeException("too many results to unpack");
            }

            TableSequence sequence = new(executionContext, processor, value, raw);
            if (distance == 0)
            {
                return sequence.Get(first);
            }

            LuaValue[] results = new LuaValue[(int)distance + 1];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = sequence.Get(first + i);
            }

            return LuaValue.NewTuple(results);
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
            Processor processor = executionContext.NativeInstructionBudgetProcessor;
            using Processor.NativeInstructionBudgetScope budget =
                processor.EnterNativeInstructionBudget();
            bool chargeInstruction = processor.HasActiveSandboxInstructionBudget;

            while (true)
            {
                if (chargeInstruction)
                {
                    processor.ChargeSandboxInstruction();
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
            Processor processor = executionContext.NativeInstructionBudgetProcessor;
            using Processor.NativeInstructionBudgetScope budget =
                processor.EnterNativeInstructionBudget();
            bool chargeInstruction = processor.HasActiveSandboxInstructionBudget;
            for (int i = 1; i <= length; i++)
            {
                if (chargeInstruction)
                {
                    processor.ChargeSandboxInstruction();
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
            const LuaCompatibilityVersion version = LuaCompatibilityVersion.Lua55;
            long sequence = CheckTableInteger(args[0], version, "create", 0);
            long records = args[1].IsNil ? 0 : CheckTableInteger(args[1], version, "create", 1);
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

            const string function = "sort";
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool indexed = version >= LuaCompatibilityVersion.Lua53;
            LuaValue vlist = CheckTableSequence(
                executionContext,
                args,
                0,
                function,
                version,
                SequenceAccess.Read | SequenceAccess.Write | SequenceAccess.Length
            );
            Processor processor = executionContext.NativeInstructionBudgetProcessor;
            using Processor.NativeInstructionBudgetScope budget =
                processor.EnterNativeInstructionBudget();
            long length = GetLibrarySequenceLength(executionContext, vlist, version);

            // Lua 5.3 moved comparator validation inside the nontrivial sort path.
            if (indexed)
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
            if (indexed)
            {
                TableSequence sequence = new(executionContext, processor, vlist, raw: false);
                new IndexedTableSort(executionContext, sequence, lt).Sort(1, end, 0);
                return LuaValue.Void;
            }

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
                        new LuaSortComparer(executionContext, lt)
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
            LuaValue lt
        )
        {
            if (lt.IsNil)
            {
                if (a.Type == DataType.Number && b.Type == DataType.Number)
                {
                    return a.LuaNumber.CompareForSort(b.LuaNumber, supportsIntegerSubtype: false);
                }

                if (a.Type == DataType.String && b.Type == DataType.String)
                {
                    return string.Compare(a.String, b.String, StringComparison.Ordinal);
                }

                if (
                    !executionContext.TryGetOrderingMetamethod(a, b, Metamethods.Lt, out lt)
                    || lt.IsNil
                )
                {
                    throw executionContext.CreateOrderingError(a, b);
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
            LuaValue tableValue = CheckTableSequence(
                executionContext,
                args,
                0,
                "table.insert",
                version,
                SequenceAccess.Read | SequenceAccess.Write | SequenceAccess.Length
            );
            Table list = tableValue.Type == DataType.Table ? tableValue.Table : null;

            // A length hook and the indexed access it enables share the caller's budget.
            Processor processor =
                version >= LuaCompatibilityVersion.Lua52 && (list == null || list.MetaTable != null)
                    ? executionContext.NativeInstructionBudgetProcessor
                    : null;
            using Processor.NativeInstructionBudgetScope budget =
                processor?.EnterNativeInstructionBudget() ?? default;

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
                    AccessTableIntegerIndex(executionContext, processor, tableValue, end, args[1]);
                }
                else
                {
                    SetTableIntegerIndex(list, end, args[1]);
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
                position = NarrowLegacyTableInteger(numericPosition);
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
                    LuaValue value = AccessTableIntegerIndex(
                        executionContext,
                        processor,
                        tableValue,
                        i - 1
                    );
                    AccessTableIntegerIndex(executionContext, processor, tableValue, i, value);
                }

                AccessTableIntegerIndex(executionContext, processor, tableValue, position, args[2]);
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
                        SetTableIntegerIndex(list, i, list.GetValue(LuaValue.NewInteger(i - 1)));
                    }
                }
            }

            SetTableIntegerIndex(list, position, args[2]);
            return LuaValue.Void;
        }

        [Flags]
        private enum SequenceAccess
        {
            Read = 1,
            Write = 2,
            Length = 4,
        }

        private static LuaValue CheckTableSequence(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args,
            int position,
            string function,
            LuaCompatibilityVersion version,
            SequenceAccess access
        )
        {
            LuaValue value = args[position];
            if (value.Type == DataType.Table)
            {
                return value;
            }

            if (version >= LuaCompatibilityVersion.Lua53)
            {
                // Stock file userdata exposes __index through its file method table. NovaSharp
                // handles the same reads through the file descriptor, without length or writes.
                if (
                    access == SequenceAccess.Read
                    && value.Type == DataType.UserData
                    && value.UserData.Descriptor is IO.FileUserDataDescriptor
                )
                {
                    return value;
                }

                // Lua checks raw non-nil fields even when a hook will fail when used.
                Table metatable = executionContext.GetMetatable(value);
                if (
                    metatable != null
                    && (
                        (access & SequenceAccess.Read) == 0
                        || !metatable.Get(Metamethods.Index).IsNil
                    )
                    && (
                        (access & SequenceAccess.Write) == 0
                        || !metatable.Get(Metamethods.NewIndex).IsNil
                    )
                    && (
                        (access & SequenceAccess.Length) == 0
                        || !metatable.Get(Metamethods.Len).IsNil
                    )
                )
                {
                    return value;
                }
            }

            return args.AsType(position, function, DataType.Table);
        }

        private readonly struct TableSequence
        {
            private readonly ScriptExecutionContext _context;
            private readonly Processor _processor;
            private readonly LuaValue _value;
            private readonly Table _table;
            private readonly bool _raw;
            private readonly bool _chargeInstruction;

            public TableSequence(
                ScriptExecutionContext context,
                Processor processor,
                LuaValue value,
                bool raw
            )
            {
                _context = context;
                _processor = processor;
                _value = value;
                _table = value.Type == DataType.Table ? value.Table : null;
                _raw = raw;
                _chargeInstruction = processor.HasActiveSandboxInstructionBudget;
            }

            public LuaValue Get(long index)
            {
                if (!_raw && (_chargeInstruction || _table == null || _table.MetaTable != null))
                {
                    return AccessTableIntegerIndex(_context, _processor, _value, index);
                }

                if (_chargeInstruction)
                {
                    _processor.ChargeSandboxInstruction();
                }

                return index >= int.MinValue && index <= int.MaxValue
                    ? _table.Get((int)index)
                    : _table.GetValue(LuaValue.NewInteger(index));
            }

            public void Set(long index, LuaValue value)
            {
                if (!_raw && (_chargeInstruction || _table == null || _table.MetaTable != null))
                {
                    AccessTableIntegerIndex(_context, _processor, _value, index, value);
                    return;
                }

                if (_chargeInstruction)
                {
                    _processor.ChargeSandboxInstruction();
                }

                SetTableIntegerIndex(_table, index, value);
            }
        }

        private static LuaValue AccessTableIntegerIndex(
            ScriptExecutionContext executionContext,
            Processor processor,
            LuaValue target,
            long index,
            LuaValue? assignedValue = null
        )
        {
            LuaValue key = LuaValue.NewInteger(index);
            bool isWrite = assignedValue.HasValue;
            string eventName = isWrite ? Metamethods.NewIndex : Metamethods.Index;
            bool chargeInstruction = processor.HasActiveSandboxInstructionBudget;
            const int maxHookHops = 2000;
            for (int followed = 0; ; followed++)
            {
                if (chargeInstruction)
                {
                    processor.ChargeSandboxInstruction();
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

                if (target.Type == DataType.UserData)
                {
                    UserData userData = target.UserData;
                    if (isWrite)
                    {
                        if (
                            userData.Descriptor.SetIndex(
                                executionContext.Script,
                                userData.Object,
                                key,
                                assignedValue.GetValueOrDefault(),
                                isDirectIndexing: false
                            )
                        )
                        {
                            return LuaValue.Void;
                        }
                    }
                    else if (
                        UserDataAccess.TryIndex(
                            userData.Descriptor,
                            executionContext.Script,
                            userData.Object,
                            key,
                            isDirectIndexing: false,
                            out LuaValue value
                        )
                    )
                    {
                        return value.IsNil ? LuaValue.Nil : value;
                    }

                    throw ScriptRuntimeException.UserDataMissingField(
                        userData.Descriptor.Name,
                        key.String
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

        private static void SetTableIntegerIndex(Table list, long index, LuaValue value)
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
                if (tableValue.Type != DataType.Table)
                {
                    throw ScriptRuntimeException.LenOnInvalidType(tableValue);
                }

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

                return NarrowLegacyTableInteger(number.Value);
            }

            if (!number.HasValue || !number.Value.TryToInteger(out long integerLength))
            {
                throw new ScriptRuntimeException("object length is not an integer");
            }

            return integerLength;
        }

        private static long CheckTableInteger(
            LuaValue argument,
            LuaCompatibilityVersion version,
            string function,
            int position
        )
        {
            LuaNumber? number = GetTableArgumentNumber(argument, version);
            if (!number.HasValue)
            {
                throw argument.IsVoid()
                    ? ScriptRuntimeException.BadArgumentNoValue(position, function, DataType.Number)
                    : ScriptRuntimeException.BadArgument(
                        position,
                        function,
                        DataType.Number,
                        argument.Type,
                        allowNil: false
                    );
            }

            return version <= LuaCompatibilityVersion.Lua52
                ? NarrowLegacyTableInteger(number.Value)
                : Utilities.LuaNumberHelpers.ToLongWithValidation(
                    version,
                    LuaValue.NewNumber(number.Value),
                    function,
                    position + 1
                );
        }

        private static int NarrowLegacyTableInteger(LuaNumber number)
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
                    SetTableIntegerIndex(list, key + 1, pair.Value);
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

            const string function = "remove";
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            LuaValue target = CheckTableSequence(
                executionContext,
                args,
                0,
                function,
                version,
                SequenceAccess.Read | SequenceAccess.Write | SequenceAccess.Length
            );
            Processor processor = executionContext.NativeInstructionBudgetProcessor;
            using Processor.NativeInstructionBudgetScope budget =
                processor.EnterNativeInstructionBudget();
            long length = GetLibrarySequenceLength(executionContext, target, version);
            long position = args[1].IsNil
                ? length
                : CheckTableInteger(args[1], version, function, 1);

            if (version == LuaCompatibilityVersion.Lua51)
            {
                if (position < 1 || position > length)
                {
                    return LuaValue.Void;
                }
            }
            else if (position != length)
            {
                long lastPosition =
                    version == LuaCompatibilityVersion.Lua52
                        ? unchecked((int)length + 1)
                        : unchecked(length + 1);
                bool outOfBounds =
                    version >= LuaCompatibilityVersion.Lua54
                        ? unchecked((ulong)position - 1) > unchecked((ulong)length)
                        : position < 1 || position > lastPosition;
                if (outOfBounds)
                {
                    int argument = version >= LuaCompatibilityVersion.Lua54 ? 1 : 0;
                    throw ScriptRuntimeException.BadArgument(
                        argument,
                        function,
                        "position out of bounds"
                    );
                }
            }

            TableSequence sequence = new(
                executionContext,
                processor,
                target,
                raw: version <= LuaCompatibilityVersion.Lua52
            );
            LuaValue result = sequence.Get(position);
            for (; position < length; position++)
            {
                sequence.Set(position, sequence.Get(position + 1));
            }

            sequence.Set(position, LuaValue.Nil);
            return result;
        }

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

            const string function = "concat";
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool raw = version <= LuaCompatibilityVersion.Lua52;
            Processor processor = executionContext.NativeInstructionBudgetProcessor;
            using Processor.NativeInstructionBudgetScope budget =
                processor.EnterNativeInstructionBudget();
            LuaValue target;
            LuaValue separator;
            long last = 0;

            if (raw)
            {
                separator = args.AsType(executionContext, 1, function, DataType.String, true);
                target = args.AsType(0, function, DataType.Table);
            }
            else
            {
                target = CheckTableSequence(
                    executionContext,
                    args,
                    0,
                    function,
                    version,
                    SequenceAccess.Read | SequenceAccess.Length
                );
                last = GetLibrarySequenceLength(executionContext, target, version);
                separator = args.AsType(executionContext, 1, function, DataType.String, true);
            }

            long first = args[2].IsNil ? 1 : CheckTableInteger(args[2], version, function, 2);
            if (!args[3].IsNil)
            {
                last = CheckTableInteger(args[3], version, function, 3);
            }
            else if (raw)
            {
                last = GetLibrarySequenceLength(executionContext, target, version);
            }

            if (first > last)
            {
                return LuaValue.NewString(string.Empty);
            }

            string sep = separator.IsNil ? string.Empty : separator.String;
            TableSequence sequence = new(executionContext, processor, target, raw);
            using Utf16ValueStringBuilder result = ZStringBuilder.Create();

            for (long i = first; ; i++)
            {
                LuaValue value = sequence.Get(i);
                if (value.Type != DataType.Number && value.Type != DataType.String)
                {
                    throw new ScriptRuntimeException(
                        "invalid value ({1}) at index {0} in table for 'concat'",
                        version <= LuaCompatibilityVersion.Lua53 ? unchecked((int)i) : i,
                        value.Type.ToLuaTypeString()
                    );
                }

                result.Append(value.ToPrintString(version));
                if (i == last)
                {
                    break;
                }

                result.Append(sep);
            }

            return LuaValue.NewString(result.ToString());
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

            const string function = "move";
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            long from = CheckTableInteger(args[1], version, function, 1);
            long to = CheckTableInteger(args[2], version, function, 2);
            long target = CheckTableInteger(args[3], version, function, 3);
            bool hasDestination = !args[4].IsNil;
            LuaValue source = CheckTableSequence(
                executionContext,
                args,
                0,
                function,
                version,
                SequenceAccess.Read
            );
            LuaValue destination = CheckTableSequence(
                executionContext,
                args,
                hasDestination ? 4 : 0,
                function,
                version,
                SequenceAccess.Write
            );

            if (to < from)
            {
                return destination;
            }

            if (from <= 0 && to >= long.MaxValue + from)
            {
                throw ScriptRuntimeException.BadArgument(2, function, "too many elements to move");
            }

            long distance = to - from;
            if (target > long.MaxValue - distance)
            {
                throw ScriptRuntimeException.BadArgument(3, function, "destination wrap around");
            }

            Processor processor = executionContext.NativeInstructionBudgetProcessor;
            using Processor.NativeInstructionBudgetScope budget =
                processor.EnterNativeInstructionBudget();
            TableSequence read = new(executionContext, processor, source, raw: false);
            TableSequence write = new(executionContext, processor, destination, raw: false);
            bool moveBackward =
                target > from
                && target <= to
                && (
                    !hasDestination
                    || MoveTargetsCompareEqual(executionContext, source, destination, version)
                );

            if (moveBackward)
            {
                for (long i = distance; i >= 0; i--)
                {
                    write.Set(target + i, read.Get(from + i));
                }
            }
            else
            {
                for (long i = 0; i <= distance; i++)
                {
                    write.Set(target + i, read.Get(from + i));
                }
            }

            return destination;
        }

        private static bool MoveTargetsCompareEqual(
            ScriptExecutionContext executionContext,
            LuaValue source,
            LuaValue destination,
            LuaCompatibilityVersion version
        )
        {
            if (source.HasSameReferenceIdentity(destination))
            {
                return true;
            }

            if (
                source.Type == destination.Type
                && source.Type is DataType.Table or DataType.UserData
                && executionContext.TryGetBinaryMetamethod(
                    source,
                    destination,
                    Metamethods.Eq,
                    out LuaValue equal
                )
            )
            {
                return executionContext
                    .Script.CallLibraryCallback(equal, source, destination)
                    .ToScalar()
                    .CastToBool();
            }

            return source.Equals(destination, version);
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

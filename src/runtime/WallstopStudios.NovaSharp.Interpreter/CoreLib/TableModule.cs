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
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing table Lua functions
    /// </summary>
    [NovaSharpModule(Namespace = "table")]
    public static class TableModule
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
        [NovaSharpModuleMethod(Name = "unpack")]
        public static LuaValue Unpack(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments (the table to scan).</param>
        /// <returns>The largest positive numeric key, or 0 if none exist.</returns>
        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua52)]
        [NovaSharpModuleMethod(Name = "maxn")]
        public static LuaValue MaxN(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue vTable = args.AsType(0, "maxn", DataType.Table, false);
            Table table = vTable.Table;

            double maxKey = 0;

            foreach (TablePair pair in table.GetPairsEnumerator())
            {
                if (pair.Key.Type == DataType.Number)
                {
                    double key = pair.Key.Number;
                    if (key > maxKey && key == Math.Floor(key))
                    {
                        maxKey = key;
                    }
                }
            }

            return LuaValue.NewNumber(maxKey);
        }

        /// <summary>
        /// Implements Lua `table.pack`, wrapping arbitrary arguments into a table with field `n` (§6.6).
        /// This function was added in Lua 5.2.
        /// </summary>
        [LuaCompatibility(LuaCompatibilityVersion.Lua52)]
        [NovaSharpModuleMethod(Name = "pack")]
        public static LuaValue Pack(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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
        /// Implements Lua `table.sort`, sorting the array portion with an optional comparator (§6.6).
        /// </summary>
        [NovaSharpModuleMethod(Name = "sort")]
        public static LuaValue Sort(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue vlist = args.AsType(0, "sort", DataType.Table, false);
            LuaValue lt = args[1];

            if (lt.Type != DataType.Function && lt.Type != DataType.ClrFunction && lt.IsNotNil())
            {
                args.AsType(1, "sort", DataType.Function, true); // this throws
            }

            int end = GetTableLength(executionContext, vlist);

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

                return vlist;
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
                if (
                    !executionContext.TryGetBinaryMetamethod(a, b, Metamethods.Lt, out lt)
                    || lt.IsNil
                )
                {
                    if (a.Type == DataType.Number && b.Type == DataType.Number)
                    {
                        return a.Number.CompareTo(b.Number);
                    }

                    if (a.Type == DataType.String && b.Type == DataType.String)
                    {
                        return string.Compare(a.String, b.String, StringComparison.Ordinal);
                    }

                    throw ScriptRuntimeException.CompareInvalidType(a, b);
                }
                else
                {
                    return LuaComparerToClrComparer(
                        executionContext.Script.CallValues(lt, a, b),
                        executionContext.Script.CallValues(lt, b, a)
                    );
                }
            }
            else
            {
                return LuaComparerToClrComparer(
                    executionContext.Script.CallValues(lt, a, b),
                    executionContext.Script.CallValues(lt, b, a)
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LuaComparerToClrComparer(LuaValue dynValue1, LuaValue dynValue2)
        {
            bool v1 = dynValue1.CastToBool();
            bool v2 = dynValue2.CastToBool();

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
        [NovaSharpModuleMethod(Name = "insert")]
        public static LuaValue Insert(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue vlist = args.AsType(0, "table.insert", DataType.Table, false);
            LuaValue vpos = args[1];
            LuaValue vvalue = args[2];

            if (args.Count > 3)
            {
                throw new ScriptRuntimeException("wrong number of arguments to 'insert'");
            }

            int len = GetTableLength(executionContext, vlist);
            Table list = vlist.Table;
            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;

            if (vvalue.IsNil)
            {
                vvalue = vpos;
                vpos = LuaValue.FromNumber(len + 1);
            }

            if (vpos.Type != DataType.Number)
            {
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "table.insert",
                    DataType.Number,
                    vpos.Type,
                    false
                );
            }

            // Lua 5.3+: require integer representation; Lua 5.1/5.2: silently truncate
            int pos = (int)
                Utilities.LuaNumberHelpers.ToLongWithValidation(version, vpos, "insert", 2);

            if (pos > len + 1 || pos < 1)
            {
                throw new ScriptRuntimeException(
                    "bad argument #2 to 'insert' (position out of bounds)"
                );
            }

            for (int i = len; i >= pos; i--)
            {
                list.Set(i + 1, list.Get(i));
            }

            list.Set(pos, vvalue);

            return vlist;
        }

        /// <summary>
        /// Implements Lua `table.remove`, removing and returning a value at the given position (§6.6).
        /// </summary>
        [NovaSharpModuleMethod(Name = "remove")]
        public static LuaValue Remove(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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
        [NovaSharpModuleMethod(Name = "concat")]
        public static LuaValue Concat(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue vlist = args.AsType(0, "concat", DataType.Table, false);
            LuaValue vsep = args.AsType(1, "concat", DataType.String, true);
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
        [NovaSharpModuleMethod(Name = "move")]
        public static LuaValue Move(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

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
    }

    /// <summary>
    /// Class exposing <c>unpack</c> in the global namespace for Lua 5.1 compatibility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Lua 5.1, <c>unpack</c> was a global function.
    /// In Lua 5.2+, it was moved to the <c>table</c> library as <c>table.unpack</c>.
    /// </para>
    /// <para>
    /// Note: Unlike <c>unpack</c>, <c>table.pack</c> was introduced NEW in Lua 5.2;
    /// there was no global <c>pack</c> function in Lua 5.1 or any other version.
    /// </para>
    /// </remarks>
    [NovaSharpModule]
    public static class TableModuleGlobals
    {
        /// <summary>
        /// Global <c>unpack</c> function for Lua 5.1 compatibility.
        /// This function was moved to <c>table.unpack</c> in Lua 5.2 and removed from the global namespace.
        /// </summary>
        [LuaCompatibility(LuaCompatibilityVersion.Lua51, LuaCompatibilityVersion.Lua51)]
        [NovaSharpModuleMethod(Name = "unpack")]
        public static LuaValue Unpack(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return TableModule.Unpack(executionContext, args);
        }
    }
}

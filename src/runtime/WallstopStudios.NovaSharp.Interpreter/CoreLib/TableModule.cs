namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
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
        /// Implements Lua `table.unpack`, returning a tuple of array elements between the provided indices (§6.6).
        /// </summary>
        [NovaSharpModuleMethod(Name = "unpack")]
        public static DynValue Unpack(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue s = args.AsType(0, "unpack", DataType.Table, false);
            DynValue vi = args.AsType(1, "unpack", DataType.Number, true);
            DynValue vj = args.AsType(2, "unpack", DataType.Number, true);

            int ii = vi.IsNil() ? 1 : (int)vi.Number;
            int ij = vj.IsNil() ? GetTableLength(executionContext, s) : (int)vj.Number;

            Table t = s.Table;
            int count = ij - ii + 1;

            // Fast path for empty range
            if (count <= 0)
            {
                return DynValue.Void;
            }

            // Fast path for single element - avoid array allocation
            if (count == 1)
            {
                return t.Get(ii);
            }

            DynValue[] v = new DynValue[count];

            int tidx = 0;
            for (int i = ii; i <= ij; i++)
            {
                v[tidx++] = t.Get(i);
            }

            return DynValue.NewTuple(v);
        }

        /// <summary>
        /// Implements Lua `table.pack`, wrapping arbitrary arguments into a table with field `n` (§6.6).
        /// </summary>
        [NovaSharpModuleMethod(Name = "pack")]
        public static DynValue Pack(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            Table t = new(executionContext.Script);
            DynValue v = DynValue.NewTable(t);

            for (int i = 0; i < args.Count; i++)
            {
                t.Set(i + 1, args[i]);
            }

            t.Set("n", DynValue.FromNumber(args.Count));

            return v;
        }

        /// <summary>
        /// Implements Lua `table.sort`, sorting the array portion with an optional comparator (§6.6).
        /// </summary>
        [NovaSharpModuleMethod(Name = "sort")]
        public static DynValue Sort(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vlist = args.AsType(0, "sort", DataType.Table, false);
            DynValue lt = args[1];

            if (lt.Type != DataType.Function && lt.Type != DataType.ClrFunction && lt.IsNotNil())
            {
                args.AsType(1, "sort", DataType.Function, true); // this throws
            }

            int end = GetTableLength(executionContext, vlist);

            using (ListPool<DynValue>.Get(end, out List<DynValue> values))
            {
                for (int i = 1; i <= end; i++)
                {
                    values.Add(vlist.Table.Get(i));
                }

                try
                {
                    values.Sort((a, b) => SortComparer(executionContext, a, b, lt));
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
            DynValue a,
            DynValue b,
            DynValue lt
        )
        {
            if (lt == null || lt.IsNil())
            {
                lt = executionContext.GetBinaryMetamethod(a, b, "__lt");

                if (lt == null || lt.IsNil())
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
                        executionContext.Script.Call(lt, a, b),
                        executionContext.Script.Call(lt, b, a)
                    );
                }
            }
            else
            {
                return LuaComparerToClrComparer(
                    executionContext.Script.Call(lt, a, b),
                    executionContext.Script.Call(lt, b, a)
                );
            }
        }

        private static int LuaComparerToClrComparer(DynValue dynValue1, DynValue dynValue2)
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
        public static DynValue Insert(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vlist = args.AsType(0, "table.insert", DataType.Table, false);
            DynValue vpos = args[1];
            DynValue vvalue = args[2];

            if (args.Count > 3)
            {
                throw new ScriptRuntimeException("wrong number of arguments to 'insert'");
            }

            int len = GetTableLength(executionContext, vlist);
            Table list = vlist.Table;

            if (vvalue.IsNil())
            {
                vvalue = vpos;
                vpos = DynValue.FromNumber(len + 1);
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

            int pos = (int)vpos.Number;

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
        public static DynValue Remove(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vlist = args.AsType(0, "table.remove", DataType.Table, false);
            DynValue vpos = args.AsType(1, "table.remove", DataType.Number, true);
            DynValue ret = DynValue.Nil;

            if (args.Count > 2)
            {
                throw new ScriptRuntimeException("wrong number of arguments to 'remove'");
            }

            int len = GetTableLength(executionContext, vlist);
            Table list = vlist.Table;

            int pos = vpos.IsNil() ? len : (int)vpos.Number;

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
        public static DynValue Concat(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue vlist = args.AsType(0, "concat", DataType.Table, false);
            DynValue vsep = args.AsType(1, "concat", DataType.String, true);
            DynValue vstart = args.AsType(2, "concat", DataType.Number, true);
            DynValue vend = args.AsType(3, "concat", DataType.Number, true);

            Table list = vlist.Table;
            string sep = vsep.IsNil() ? "" : vsep.String;
            int start = vstart.IsNilOrNan() ? 1 : (int)vstart.Number;
            int end;

            if (vend.IsNilOrNan())
            {
                end = GetTableLength(executionContext, vlist);
            }
            else
            {
                end = (int)vend.Number;
            }

            if (end < start)
            {
                return DynValue.NewString(string.Empty);
            }

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            for (int i = start; i <= end; i++)
            {
                DynValue v = list.Get(i);

                if (v.Type != DataType.Number && v.Type != DataType.String)
                {
                    throw new ScriptRuntimeException(
                        "invalid value ({1}) at index {0} in table for 'concat'",
                        i,
                        v.Type.ToLuaTypeString()
                    );
                }

                string s = v.ToPrintString();

                if (i != start)
                {
                    sb.Append(sep);
                }

                sb.Append(s);
            }

            return DynValue.NewString(sb.ToString());
        }

        /// <summary>
        /// Implements Lua 5.3 `table.move`, copying values between tables with overlap handling (§6.6).
        /// </summary>
        [LuaCompatibility(LuaCompatibilityVersion.Lua53)]
        [NovaSharpModuleMethod(Name = "move")]
        public static DynValue Move(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            const string func = "move";

            Table source = args.AsType(0, func, DataType.Table, false).Table;
            int from = args.AsInt(1, func);
            int to = args.AsInt(2, func);
            int target = args.AsInt(3, func);
            Table destination =
                (args.Count >= 5 && !args[4].IsNil())
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
                        DynValue value = source.Get(srcIndex);
                        destination.Set(destIndex, value ?? DynValue.Nil);
                    }
                }
                else
                {
                    for (int i = 0; i <= elementsToCopy; i++)
                    {
                        int srcIndex = from + i;
                        int destIndex = srcIndex + offset;
                        DynValue value = source.Get(srcIndex);
                        destination.Set(destIndex, value ?? DynValue.Nil);
                    }
                }
            }

            return DynValue.NewTable(destination);
        }

        private static int GetTableLength(ScriptExecutionContext executionContext, DynValue vlist)
        {
            DynValue len = executionContext.GetMetamethod(vlist, "__len");

            if (len != null)
            {
                DynValue lenv = executionContext.Script.Call(len, vlist);

                double? lengthValue = lenv.CastToNumber();

                if (lengthValue == null)
                {
                    throw new ScriptRuntimeException("object length is not a number");
                }

                return (int)lengthValue;
            }
            else
            {
                return (int)vlist.Table.Length;
            }
        }
    }

    /// <summary>
    /// Class exposing table.unpack and table.pack in the global namespace (to work around the most common Lua 5.1 compatibility issue).
    /// </summary>
    [NovaSharpModule]
    public static class TableModuleGlobals
    {
        /// <summary>
        /// Global alias for `table.unpack` to maintain Lua 5.1 compatibility.
        /// </summary>
        [NovaSharpModuleMethod(Name = "unpack")]
        public static DynValue Unpack(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return TableModule.Unpack(executionContext, args);
        }

        /// <summary>
        /// Global alias for `table.pack` to maintain Lua 5.1 compatibility.
        /// </summary>
        [NovaSharpModuleMethod(Name = "pack")]
        public static DynValue Pack(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            return TableModule.Pack(executionContext, args);
        }
    }
}

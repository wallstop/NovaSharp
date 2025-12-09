namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using Cysharp.Text;
    using Debugging;
    using Execution.Scopes;
    using REPL;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// Class implementing debug Lua functions. Support for the debug module is partial.
    /// </summary>
    [NovaSharpModule(Namespace = "debug")]
    public static class DebugModule
    {
        private static readonly ConditionalWeakTable<object, DebugHookState> HookStates = new();
        private static readonly object DefaultHookKey = new();
        private static readonly ConditionalWeakTable<DynValue, DynValue> UpvalueIdentifiers = new();
        private static readonly IUserDataDescriptor UpvalueIdentifierDescriptorInstance =
            new UpvalueIdentifierDescriptor();

        /// <summary>
        /// Implements Lua's interactive <c>debug.debug</c> helper by launching the REPL and allowing the host to inspect state.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Unused but validated per Lua semantics.</param>
        /// <returns><see cref="DynValue.Nil"/> after the user exits the REPL.</returns>
        [NovaSharpModuleMethod(Name = "debug")]
        public static DynValue Debug(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            Script script = executionContext.Script;

            if (script.Options.DebugInput == null)
            {
                throw new ScriptRuntimeException(
                    "debug.debug not supported on this platform/configuration"
                );
            }

            ReplInterpreter interpreter = new(script)
            {
                HandleDynamicExprs = false,
                HandleClassicExprsSyntax = true,
            };

            while (true)
            {
                string input = script.Options.DebugInput(interpreter.ClassicPrompt + " ");

                if (input == null)
                {
                    break;
                }

                ReadOnlySpan<char> trimmedInput = input.AsSpan().TrimWhitespace();

                if (trimmedInput.Equals("return".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                try
                {
                    DynValue result = interpreter.Evaluate(input);

                    if (result != null && result.Type != DataType.Void)
                    {
                        script.Options.DebugPrint($"{result}");
                    }
                }
                catch (InterpreterException ex)
                {
                    script.Options.DebugPrint($"{ex.DecoratedMessage ?? ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    script.Options.DebugPrint($"{ex.Message}");
                }
                catch (ArgumentException ex)
                {
                    script.Options.DebugPrint($"{ex.Message}");
                }
            }

            return DynValue.Nil;
        }

        /// <summary>
        /// Implements Lua's <c>debug.getinfo</c> helper (§6.10) by returning metadata about a function or stack level.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments describing the target function/level.</param>
        /// <returns>A table describing the requested stack frame or function; <c>nil</c> when the level exceeds the stack depth.</returns>
        [NovaSharpModuleMethod(Name = "getinfo")]
        public static DynValue GetInfo(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue target = args[0];
            string what = ResolveWhatOption(args, 1);

            switch (target.Type)
            {
                case DataType.Number:
                    int level = args.AsInt(0, "getinfo");
                    if (level < 0)
                    {
                        return DynValue.Nil;
                    }

                    return BuildStackInfo(executionContext, level, what);
                case DataType.Function:
                case DataType.ClrFunction:
                    return BuildFunctionInfo(executionContext.Script, target, what);
                default:
                    throw ScriptRuntimeException.BadArgument(
                        0,
                        "getinfo",
                        "function or level expected"
                    );
            }
        }

        /// <summary>
        /// Implements <c>debug.getuservalue</c>, returning the user value associated with userdata or nil otherwise.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (userdata whose user value should be returned).</param>
        /// <returns>The stored user value or <see cref="DynValue.Nil"/>.</returns>
        [NovaSharpModuleMethod(Name = "getuservalue")]
        public static DynValue GetUserValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue v = args[0];

            if (v.Type != DataType.UserData)
            {
                return DynValue.Nil;
            }

            return v.UserData.UserValue ?? DynValue.Nil;
        }

        /// <summary>
        /// Implements <c>debug.setuservalue</c>, assigning a new table to the supplied userdata's user value slot.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (userdata and optional table).</param>
        /// <returns>The table that was assigned.</returns>
        [NovaSharpModuleMethod(Name = "setuservalue")]
        public static DynValue SetUserValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue v = args.AsType(0, "setuservalue", DataType.UserData, false);
            DynValue valueArgument = args.Count > 1 ? args[1] : DynValue.Void;

            if (valueArgument.Type == DataType.Void)
            {
                throw ScriptRuntimeException.BadArgumentNoValue(1, "setuservalue", DataType.Table);
            }

            if (valueArgument.IsNotNil() && valueArgument.Type != DataType.Table)
            {
                string got = valueArgument.Type.ToErrorTypeString();
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "setuservalue",
                    FormattableString.Invariant($"table expected, got {got}")
                );
            }

            DynValue userValue = valueArgument.IsNil() ? DynValue.Nil : valueArgument;
            v.UserData.UserValue = userValue;
            return v;
        }

        /// <summary>
        /// Implements <c>debug.getregistry</c>, returning the script registry table.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Unused but validated per Lua semantics.</param>
        /// <returns>The registry table.</returns>
        [NovaSharpModuleMethod(Name = "getregistry")]
        public static DynValue GetRegistry(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return DynValue.NewTable(executionContext.Script.Registry);
        }

        /// <summary>
        /// Implements <c>debug.getmetatable</c>, returning the metatable for the supplied value.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value whose metatable is requested).</param>
        /// <returns>The metatable or <see cref="DynValue.Nil"/>.</returns>
        [NovaSharpModuleMethod(Name = "getmetatable")]
        public static DynValue GetMetatable(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue v = args[0];
            Script s = executionContext.Script;

            if (v.Type.CanHaveTypeMetatables())
            {
                Table typeMetatable = s.GetTypeMetatable(v.Type);
                return typeMetatable != null ? DynValue.NewTable(typeMetatable) : DynValue.Nil;
            }
            else if (v.Type == DataType.Table)
            {
                Table tableMetatable = v.Table.MetaTable;
                return tableMetatable != null ? DynValue.NewTable(tableMetatable) : DynValue.Nil;
            }
            else
            {
                return DynValue.Nil;
            }
        }

        /// <summary>
        /// Implements <c>debug.setmetatable</c>, assigning a new metatable to a type or table.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value and optional metatable).</param>
        /// <returns>The original value after mutation.</returns>
        [NovaSharpModuleMethod(Name = "setmetatable")]
        public static DynValue SetMetatable(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue v = args[0];
            DynValue metaArgument = args.Count > 1 ? args[1] : DynValue.Void;

            if (metaArgument.Type == DataType.Void)
            {
                throw ScriptRuntimeException.BadArgumentNoValue(1, "setmetatable", DataType.Table);
            }

            if (metaArgument.IsNotNil() && metaArgument.Type != DataType.Table)
            {
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "setmetatable",
                    "nil or table expected"
                );
            }

            Table m = metaArgument.IsNil() ? null : metaArgument.Table;
            Script s = executionContext.Script;

            if (v.Type.CanHaveTypeMetatables())
            {
                s.SetTypeMetatable(v.Type, m);
            }
            else if (v.Type == DataType.Table)
            {
                v.Table.MetaTable = m;
            }
            else
            {
                throw new ScriptRuntimeException(
                    "cannot debug.setmetatable on type {0}",
                    v.Type.ToErrorTypeString()
                );
            }

            return v;
        }

        /// <summary>
        /// Implements <c>debug.getupvalue</c>, returning the name and value of the specified closure upvalue.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (Lua closure and upvalue index).</param>
        /// <returns>A tuple containing the upvalue name and value, or nil when unavailable.</returns>
        [NovaSharpModuleMethod(Name = "getupvalue")]
        public static DynValue GetUpValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            int index = (int)args.AsType(1, "getupvalue", DataType.Number, false).Number - 1;

            if (args[0].Type == DataType.ClrFunction)
            {
                return DynValue.Nil;
            }

            Closure fn = args.AsType(0, "getupvalue", DataType.Function, false).Function;

            ClosureContext closure = fn.ClosureContext;

            if (index < 0 || index >= closure.Count)
            {
                return DynValue.Nil;
            }

            return DynValue.NewTuple(DynValue.NewString(closure.Symbols[index]), closure[index]);
        }

        /// <summary>
        /// Implements <c>debug.upvalueid</c>, returning an identifier for the specified upvalue reference.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (closure and upvalue index).</param>
        /// <returns>An identifier suitable for comparison or nil.</returns>
        [NovaSharpModuleMethod(Name = "upvalueid")]
        public static DynValue UpValueId(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            int index = (int)args.AsType(1, "getupvalue", DataType.Number, false).Number - 1;

            if (args[0].Type == DataType.ClrFunction)
            {
                return DynValue.Nil;
            }

            Closure fn = args.AsType(0, "getupvalue", DataType.Function, false).Function;

            ClosureContext closure = fn.ClosureContext;

            if (index < 0 || index >= closure.Count)
            {
                return DynValue.Nil;
            }

            DynValue slot = closure[index];

            if (slot == null)
            {
                return DynValue.Nil;
            }

            return GetUpvalueIdentifier(slot);
        }

        /// <summary>
        /// Implements <c>debug.setupvalue</c>, assigning a new value to the specified closure upvalue.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (closure, index, new value).</param>
        /// <returns>The upvalue name or nil if the index is invalid.</returns>
        [NovaSharpModuleMethod(Name = "setupvalue")]
        public static DynValue SetUpValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            int index = (int)args.AsType(1, "setupvalue", DataType.Number, false).Number - 1;

            if (args[0].Type == DataType.ClrFunction)
            {
                return DynValue.Nil;
            }

            Closure fn = args.AsType(0, "setupvalue", DataType.Function, false).Function;

            ClosureContext closure = fn.ClosureContext;

            if (index < 0 || index >= closure.Count)
            {
                return DynValue.Nil;
            }

            closure[index].Assign(args[2]);

            return DynValue.NewString(closure.Symbols[index]);
        }

        /// <summary>
        /// Implements <c>debug.upvaluejoin</c>, making two closures share the same upvalue reference.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (closure A/index, closure B/index).</param>
        /// <returns><see cref="DynValue.Void"/> after the join completes.</returns>
        [NovaSharpModuleMethod(Name = "upvaluejoin")]
        public static DynValue UpValueJoin(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue f1 = args.AsType(0, "upvaluejoin", DataType.Function, false);
            DynValue f2 = args.AsType(2, "upvaluejoin", DataType.Function, false);
            int n1 = args.AsInt(1, "upvaluejoin") - 1;
            int n2 = args.AsInt(3, "upvaluejoin") - 1;

            Closure c1 = f1.Function;
            Closure c2 = f2.Function;

            if (n1 < 0 || n1 >= c1.ClosureContext.Count)
            {
                throw ScriptRuntimeException.BadArgument(1, "upvaluejoin", "invalid upvalue index");
            }

            if (n2 < 0 || n2 >= c2.ClosureContext.Count)
            {
                throw ScriptRuntimeException.BadArgument(3, "upvaluejoin", "invalid upvalue index");
            }

            c2.ClosureContext[n2] = c1.ClosureContext[n1];

            return DynValue.Void;
        }

        /// <summary>
        /// Implements <c>debug.traceback</c>, formatting a stack trace for the current or supplied coroutine.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (optional thread, message, and level).</param>
        /// <returns>A string containing the formatted traceback or the original message value.</returns>
        [NovaSharpModuleMethod(Name = "traceback")]
        public static DynValue Traceback(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();

            DynValue vmessage = args[0];
            DynValue vlevel = args[1];

            double defaultSkip = 1.0;

            Coroutine cor = executionContext.CallingCoroutine;

            if (vmessage.Type == DataType.Thread)
            {
                cor = vmessage.Coroutine;
                vmessage = args[1];
                vlevel = args[2];
                defaultSkip = 0.0;
            }

            if (
                vmessage.IsNotNil()
                && vmessage.Type != DataType.String
                && vmessage.Type != DataType.Number
            )
            {
                return vmessage;
            }

            string message = vmessage.CastToString();

            int skip = (int)((vlevel.CastToNumber()) ?? defaultSkip);

            WatchItem[] stacktrace = cor.GetStackTrace(Math.Max(0, skip));

            if (message != null)
            {
                sb.Append(message);
                sb.Append('\n');
            }

            sb.Append("stack traceback:\n");

            foreach (WatchItem wi in stacktrace)
            {
                string name;

                if (wi.Name == null)
                {
                    if (wi.RetAddress < 0)
                    {
                        name = "main chunk";
                    }
                    else
                    {
                        name = "?";
                    }
                }
                else
                {
                    name = "function '" + wi.Name + "'";
                }

                string loc =
                    wi.Location != null
                        ? wi.Location.FormatLocation(executionContext.Script)
                        : "[clr]";
                sb.Append('\t');
                sb.Append(loc);
                sb.Append(": in ");
                sb.Append(name);
                sb.Append('\n');
            }

            return DynValue.NewString(sb.ToString());
        }

        /// <summary>
        /// Implements Lua's <c>debug.sethook</c>, registering a hook function for the current coroutine.
        /// </summary>
        [NovaSharpModuleMethod(Name = "sethook")]
        public static DynValue SetHook(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            int argIndex = 0;
            Coroutine targetCoroutine = null;
            int argCount = args?.Count ?? 0;

            if (argCount > 0 && args[0].Type == DataType.Thread)
            {
                targetCoroutine = args[0].Coroutine;
                argIndex++;
            }

            object hookKey = GetHookKey(executionContext, targetCoroutine);

            if (argCount <= argIndex)
            {
                HookStates.Remove(hookKey);
                return DynValue.Nil;
            }

            DynValue hookFunction = args[argIndex];

            string mask = string.Empty;
            int count = 0;

            if (argCount > argIndex + 1 && args[argIndex + 1].IsNotNil())
            {
                mask = args.AsType(argIndex + 1, "sethook", DataType.String, false).String;
            }

            if (argCount > argIndex + 2 && args[argIndex + 2].IsNotNil())
            {
                count = args.AsInt(argIndex + 2, "sethook");
            }

            if (hookFunction.IsNil())
            {
                HookStates.Remove(hookKey);
                return DynValue.Nil;
            }

            if (hookFunction.Type != DataType.Function && hookFunction.Type != DataType.ClrFunction)
            {
                throw ScriptRuntimeException.BadArgument(argIndex, "sethook", "function expected");
            }

            DebugHookState state = HookStates.GetValue(hookKey, _ => new DebugHookState());
            state.Function = hookFunction;
            state.Mask = mask ?? string.Empty;
            state.Count = Math.Max(0, count);

            return DynValue.Nil;
        }

        /// <summary>
        /// Implements Lua's <c>debug.gethook</c>, returning the previously registered hook function.
        /// </summary>
        [NovaSharpModuleMethod(Name = "gethook")]
        public static DynValue GetHook(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );

            Coroutine targetCoroutine = null;

            if (args != null && args.Count > 0 && args[0].Type == DataType.Thread)
            {
                targetCoroutine = args[0].Coroutine;
            }

            object hookKey = GetHookKey(executionContext, targetCoroutine);

            if (!HookStates.TryGetValue(hookKey, out DebugHookState state))
            {
                return DynValue.NewTuple(
                    DynValue.Nil,
                    DynValue.NewString(string.Empty),
                    DynValue.FromNumber(0)
                );
            }

            return DynValue.NewTuple(
                state.Function,
                DynValue.NewString(state.Mask ?? string.Empty),
                DynValue.NewNumber(state.Count)
            );
        }

        /// <summary>
        /// Implements Lua's <c>debug.getlocal</c>, returning the name and value of the specified local.
        /// </summary>
        [NovaSharpModuleMethod(Name = "getlocal")]
        public static DynValue GetLocal(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            int argIndex = 0;
            DynValue target = args[argIndex];

            if (target.Type == DataType.Function || target.Type == DataType.ClrFunction)
            {
                int funcLocalIndex = args.AsInt(argIndex + 1, "getlocal");
                return GetLocalFromFunction(target, funcLocalIndex);
            }

            int level = args.AsInt(argIndex, "getlocal");
            int locationIndex = args.AsInt(argIndex + 1, "getlocal");

            if (level < 0)
            {
                return DynValue.Nil;
            }

            if (level == 0)
            {
                return GetClrDebugLocalTuple(locationIndex, args, argIndex);
            }

            if (!TryGetLuaStackFrame(executionContext, level, out CallStackItem frame))
            {
                throw ScriptRuntimeException.BadArgument(
                    argIndex,
                    "getlocal",
                    "level out of range"
                );
            }

            return GetLocalFromFrame(frame, locationIndex);
        }

        /// <summary>
        /// Implements Lua's <c>debug.setlocal</c>, assigning a new value to the specified local.
        /// </summary>
        [NovaSharpModuleMethod(Name = "setlocal")]
        public static DynValue SetLocal(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args.Count < 3)
            {
                throw ScriptRuntimeException.BadArgument(2, "setlocal", "value expected");
            }

            int level = args.AsInt(0, "setlocal");
            int locationIndex = args.AsInt(1, "setlocal");
            DynValue newValue = args[2];

            if (level < 0)
            {
                return DynValue.Nil;
            }

            if (level == 0)
            {
                return GetClrDebugLocalName(locationIndex);
            }

            bool frameFound = TryGetLuaStackFrame(executionContext, level, out CallStackItem frame);

            if (!frameFound)
            {
                throw ScriptRuntimeException.BadArgument(0, "setlocal", "level out of range");
            }

            return SetLocalOnFrame(frame, locationIndex, newValue);
        }

        private static DynValue BuildStackInfo(
            ScriptExecutionContext executionContext,
            int level,
            string what
        )
        {
            IReadOnlyList<WatchItem> frames = executionContext.GetCallStackSnapshot(
                executionContext.CallingLocation
            );

            if (frames.Count == 0 || level >= frames.Count)
            {
                return DynValue.Nil;
            }

            Table info = new(executionContext.Script);
            PopulateInfoFromFrame(executionContext.Script, info, frames[level], what);
            return DynValue.NewTable(info);
        }

        private static DynValue BuildFunctionInfo(Script script, DynValue function, string what)
        {
            Table info = new(script);

            if (ContainsWhatFlag(what, 'f'))
            {
                info.Set("func", function);
            }

            PopulateFunctionMetadata(script, info, function, what);
            return DynValue.NewTable(info);
        }

        private static void PopulateFunctionMetadata(
            Script script,
            Table info,
            DynValue function,
            string what
        )
        {
            bool isClr = function.Type == DataType.ClrFunction;

            if (ContainsWhatFlag(what, 'S'))
            {
                info.Set("what", DynValue.NewString(isClr ? "C" : "Lua"));
                SetSourceFields(script, info, null, isClr);
            }

            if (ContainsWhatFlag(what, 'l'))
            {
                info.Set("currentline", DynValue.NewNumber(-1));
            }

            if (ContainsWhatFlag(what, 'n'))
            {
                info.Set("name", DynValue.Nil);
                info.Set("namewhat", DynValue.NewString(string.Empty));
            }

            if (ContainsWhatFlag(what, 'u'))
            {
                int upvalues =
                    function.Type == DataType.Function ? function.Function.UpValuesCount : 0;
                info.Set("nups", DynValue.FromNumber(upvalues));
                info.Set("nparams", DynValue.FromNumber(0));
                info.Set("isvararg", DynValue.False);
            }

            if (ContainsWhatFlag(what, 'L'))
            {
                info.Set("activelines", DynValue.NewTable(new Table(script)));
            }
        }

        private static void PopulateInfoFromFrame(
            Script script,
            Table info,
            WatchItem frame,
            string what
        )
        {
            bool isClrFrame =
                frame.Address < 0 || frame.Location == null || frame.Location.IsClrLocation;

            if (ContainsWhatFlag(what, 'f'))
            {
                info.Set("func", BuildFunctionPlaceholder(frame));
            }

            if (ContainsWhatFlag(what, 'S'))
            {
                info.Set("what", DynValue.NewString(isClrFrame ? "C" : "Lua"));
                SetSourceFields(script, info, frame.Location, isClrFrame);
            }

            if (ContainsWhatFlag(what, 'l'))
            {
                int currentLine = frame.Location?.FromLine ?? -1;
                info.Set("currentline", DynValue.NewNumber(currentLine));
            }

            if (ContainsWhatFlag(what, 'n'))
            {
                if (frame.Name != null)
                {
                    info.Set("name", DynValue.NewString(frame.Name));
                    info.Set("namewhat", DynValue.NewString("global"));
                }
                else
                {
                    info.Set("name", DynValue.Nil);
                    info.Set("namewhat", DynValue.NewString(string.Empty));
                }
            }

            if (ContainsWhatFlag(what, 'u'))
            {
                info.Set("nups", DynValue.FromNumber(0));
                info.Set("nparams", DynValue.FromNumber(0));
                info.Set("isvararg", DynValue.False);
            }

            if (ContainsWhatFlag(what, 'L'))
            {
                info.Set("activelines", DynValue.NewTable(new Table(script)));
            }
        }

        private static DynValue BuildFunctionPlaceholder(WatchItem frame)
        {
            if (frame.Address >= 0)
            {
                return DynValue.NewString(
                    FormattableString.Invariant($"function: 0x{frame.Address:X}")
                );
            }

            string name = frame.Name ?? "function";
            return DynValue.NewString($"function: {name}");
        }

        private static void SetSourceFields(
            Script script,
            Table info,
            SourceRef location,
            bool isClrFrame
        )
        {
            if (isClrFrame || location == null)
            {
                info.Set("source", DynValue.NewString("=[C]"));
                info.Set("short_src", DynValue.NewString("[C]"));
                info.Set("linedefined", DynValue.NewNumber(-1));
                info.Set("lastlinedefined", DynValue.NewNumber(-1));
                return;
            }

            SourceCode source = script.GetSourceCode(location.SourceIdx);
            string sourceName = source?.Name ?? string.Empty;
            string chunkName = "@" + sourceName;

            info.Set("source", DynValue.NewString(chunkName));
            info.Set("short_src", DynValue.NewString(ShortenSource(sourceName)));
            info.Set("linedefined", DynValue.NewNumber(location.FromLine));
            info.Set("lastlinedefined", DynValue.NewNumber(location.ToLine));
        }

        private static string ShortenSource(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName) || sourceName.Length <= 60)
            {
                return sourceName;
            }

            return sourceName.Substring(0, 57) + "...";
        }

        private static string ResolveWhatOption(CallbackArguments args, int optionIndex)
        {
            string what = "nSluf";
            if (args.Count > optionIndex && args[optionIndex].IsNotNil())
            {
                what = args.AsType(optionIndex, "getinfo", DataType.String, false).String;
            }

            ValidateWhatOption(what, optionIndex);
            return what;
        }

        private static void ValidateWhatOption(string what, int optionIndex)
        {
            if (string.IsNullOrEmpty(what))
            {
                return;
            }

            foreach (char flag in what)
            {
                if (!ContainsWhatFlag("nSlufL", flag))
                {
                    throw ScriptRuntimeException.BadArgument(
                        optionIndex,
                        "getinfo",
                        "invalid option"
                    );
                }
            }
        }

        private static bool ContainsWhatFlag(string what, char flag)
        {
            if (string.IsNullOrEmpty(what))
            {
                return false;
            }

            return what.Contains(
                flag.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal
            );
        }

        private static DynValue GetLocalFromFrame(CallStackItem frame, int index)
        {
            if (
                frame == null
                || frame.DebugSymbols == null
                || frame.LocalScope == null
                || index <= 0
            )
            {
                return DynValue.Nil;
            }

            int zeroBased = index - 1;
            int max = Math.Min(frame.DebugSymbols.Length, frame.LocalScope.Length);

            if (zeroBased >= max)
            {
                return DynValue.Nil;
            }

            SymbolRef symbol = frame.DebugSymbols[zeroBased];
            DynValue value = frame.LocalScope[zeroBased] ?? DynValue.Nil;
            string name = symbol?.Name ?? string.Empty;

            return DynValue.NewTuple(DynValue.NewString(name), value);
        }

        private static DynValue SetLocalOnFrame(CallStackItem frame, int index, DynValue newValue)
        {
            if (
                frame == null
                || frame.DebugSymbols == null
                || frame.LocalScope == null
                || index <= 0
            )
            {
                return DynValue.Nil;
            }

            int zeroBased = index - 1;
            int max = Math.Min(frame.DebugSymbols.Length, frame.LocalScope.Length);

            if (zeroBased >= max)
            {
                return DynValue.Nil;
            }

            SymbolRef symbol = frame.DebugSymbols[zeroBased];
            DynValue slot = frame.LocalScope[zeroBased];

            if (slot == null)
            {
                slot = DynValue.NewNil();
                frame.LocalScope[zeroBased] = slot;
            }

            slot.Assign(newValue);

            string name = symbol?.Name ?? string.Empty;
            return DynValue.NewString(name);
        }

        private static DynValue GetLocalFromFunction(DynValue function, int index)
        {
            if (index <= 0)
            {
                return DynValue.Nil;
            }

            string placeholderName = FormattableString.Invariant($"(*function-local {index})");
            return DynValue.NewTuple(DynValue.NewString(placeholderName), DynValue.Nil);
        }

        private static DynValue GetClrDebugLocalTuple(
            int index,
            CallbackArguments args,
            int levelArgIndex
        )
        {
            return index switch
            {
                1 => DynValue.NewTuple(
                    DynValue.NewString("(*level)"),
                    GetArgumentOrNil(args, levelArgIndex)
                ),
                2 => DynValue.NewTuple(
                    DynValue.NewString("(*index)"),
                    GetArgumentOrNil(args, levelArgIndex + 1)
                ),
                3 => DynValue.NewTuple(
                    DynValue.NewString("(*value)"),
                    GetArgumentOrNil(args, levelArgIndex + 2)
                ),
                _ => DynValue.Nil,
            };
        }

        private static DynValue GetClrDebugLocalName(int index)
        {
            return index switch
            {
                1 => DynValue.NewString("(*level)"),
                2 => DynValue.NewString("(*index)"),
                3 => DynValue.NewString("(*value)"),
                _ => DynValue.Nil,
            };
        }

        private static DynValue GetArgumentOrNil(CallbackArguments args, int index)
        {
            if (args == null || index < 0 || index >= args.Count)
            {
                return DynValue.Nil;
            }

            return args[index];
        }

        private static object GetHookKey(
            ScriptExecutionContext executionContext,
            Coroutine coroutine
        )
        {
            if (coroutine != null)
            {
                return coroutine;
            }

            Coroutine current = executionContext?.CallingCoroutine;
            if (current != null)
            {
                return current;
            }

            if (executionContext?.Script != null)
            {
                return executionContext.Script;
            }

            return DefaultHookKey;
        }

        private static bool TryGetLuaStackFrame(
            ScriptExecutionContext executionContext,
            int luaLevel,
            out CallStackItem frame
        )
        {
            frame = null;

            if (executionContext == null || luaLevel <= 0)
            {
                return false;
            }

            int matched = 0;

            for (
                int depth = 0;
                executionContext.TryGetStackFrame(depth, out CallStackItem candidate);
                depth++
            )
            {
                if (candidate.ClrFunction != null)
                {
                    continue;
                }

                matched++;

                if (matched == luaLevel)
                {
                    frame = candidate;
                    return true;
                }
            }

            return false;
        }

        private static DynValue GetUpvalueIdentifier(DynValue upvalueSlot)
        {
            return UpvalueIdentifiers.GetValue(
                upvalueSlot,
                static slot =>
                    UserData.Create(
                        new UpvalueIdentifier(slot),
                        UpvalueIdentifierDescriptorInstance
                    )
            );
        }

        private sealed class DebugHookState
        {
            public DynValue Function { get; set; } = DynValue.Nil;
            public string Mask { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        private sealed class UpvalueIdentifier
        {
            public UpvalueIdentifier(DynValue slot)
            {
                Upvalue = slot ?? throw new ArgumentNullException(nameof(slot));
                ReferenceId = slot.ReferenceId;
            }

            public DynValue Upvalue { get; }

            public int ReferenceId { get; }

            public override string ToString()
            {
                return FormattableString.Invariant($"upvalue: 0x{ReferenceId:X}");
            }
        }

        private sealed class UpvalueIdentifierDescriptor : IUserDataDescriptor
        {
            public string Name => "upvalue";

            public Type Type => typeof(UpvalueIdentifier);

            public DynValue Index(Script script, object obj, DynValue index, bool isDirectIndexing)
            {
                return DynValue.Nil;
            }

            public bool SetIndex(
                Script script,
                object obj,
                DynValue index,
                DynValue value,
                bool isDirectIndexing
            )
            {
                return false;
            }

            public string AsString(object obj)
            {
                if (obj is UpvalueIdentifier identifier)
                {
                    return identifier.ToString();
                }

                return "userdata: upvalue";
            }

            public DynValue MetaIndex(Script script, object obj, string metaname)
            {
                return null;
            }

            public bool IsTypeCompatible(Type type, object obj)
            {
                if (type == null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                return obj is UpvalueIdentifier && type.IsAssignableFrom(Type);
            }
        }
    }
}

namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using global::NovaSharp;
    using Cysharp.Text;
    using Debugging;
    using Execution.Scopes;
    using REPL;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
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
        private static readonly ConditionalWeakTable<
            ValueSlot,
            UpvalueIdentifierValue
        > UpvalueIdentifiers = new();
        private static readonly IUserDataDescriptor UpvalueIdentifierDescriptorInstance =
            new UpvalueIdentifierDescriptor();

        /// <summary>
        /// Implements Lua's interactive <c>debug.debug</c> helper by launching the REPL and allowing the host to inspect state.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Unused but validated per Lua semantics.</param>
        /// <returns><see cref="LuaValue.Nil"/> after the user exits the REPL.</returns>
        [NovaSharpModuleMethod(Name = "debug")]
        public static LuaValue Debug(
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

            // Reference Lua uses "lua_debug> " as the fixed prompt for debug.debug(),
            // unlike the main REPL which uses ">" and ">>" for continuation.
            const string DebugPrompt = "lua_debug> ";

            while (true)
            {
                string input = script.Options.DebugInput(DebugPrompt);

                if (input == null)
                {
                    break;
                }

                ReadOnlySpan<char> trimmedInput = input.AsSpan().TrimWhitespace();

                if (
                    trimmedInput.Equals(
                        LuaKeywords.Return.AsSpan(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    break;
                }

                try
                {
                    LuaValue? result = interpreter.Evaluate(input);

                    if (result.HasValue && result.Value.Type != DataType.Void)
                    {
                        script.Options.DebugPrint(result.Value.ToRawString());
                    }
                }
                catch (InterpreterException ex)
                {
                    script.Options.DebugPrint(ex.DecoratedMessage ?? ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    script.Options.DebugPrint(ex.Message);
                }
                catch (ArgumentException ex)
                {
                    script.Options.DebugPrint(ex.Message);
                }
            }

            return LuaValue.Nil;
        }

        /// <summary>
        /// Implements Lua's <c>debug.getinfo</c> helper (§6.10) by returning metadata about a function or stack level.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments describing the target function/level.</param>
        /// <returns>A table describing the requested stack frame or function; <c>nil</c> when the level exceeds the stack depth.</returns>
        [NovaSharpModuleMethod(Name = "getinfo")]
        public static LuaValue GetInfo(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue target = args[0];
            string what = ResolveWhatOption(executionContext.Script, args, 1);

            switch (target.Type)
            {
                case DataType.Number:
                    int level = args.AsInt(0, "getinfo");
                    if (level < 0)
                    {
                        return LuaValue.Nil;
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
        /// In Lua 5.4+, accepts an optional second argument <c>n</c> specifying which user value slot (1-based).
        /// In Lua 5.4+, returns two values: the user value and a boolean (false if the userdata doesn't have that value).
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (userdata [, n]).</param>
        /// <returns>The stored user value (and boolean in 5.4+) or <see cref="LuaValue.Nil"/>.</returns>
        [NovaSharpModuleMethod(Name = "getuservalue")]
        public static LuaValue GetUserValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            bool isLua54OrLater =
                LuaVersionDefaults.Resolve(version) >= LuaCompatibilityVersion.Lua54;

            // Lua 5.4 parses n before checking whether the first argument is userdata.
            int n = isLua54OrLater
                ? GetOptionalUserValueIndex(args, 1, "getuservalue", version)
                : 1;
            LuaValue v = args[0];

            if (v.Type != DataType.UserData)
            {
                return LuaValue.Nil;
            }

            // NovaSharp only supports a single user value (slot 1)
            // Any n != 1 means the userdata doesn't have that value
            if (n != 1)
            {
                // Lua 5.4+: return nil, false for invalid slot
                return isLua54OrLater
                    ? LuaValue.NewTuple(LuaValue.Nil, LuaValue.False)
                    : LuaValue.Nil;
            }

            LuaValue userValue = v.UserData.UserValue;

            // Lua 5.4+: return value, true (indicating the userdata has this value slot)
            return isLua54OrLater ? LuaValue.NewTuple(userValue, LuaValue.True) : userValue;
        }

        /// <summary>
        /// Implements <c>debug.setuservalue</c>, assigning a value to the supplied userdata's user value slot.
        /// In Lua 5.4+, accepts an optional third argument <c>n</c> specifying which user value slot (1-based).
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (userdata, value [, n]).</param>
        /// <returns>The userdata (or nil/fail if the userdata doesn't have that slot in 5.4+).</returns>
        [NovaSharpModuleMethod(Name = "setuservalue")]
        public static LuaValue SetUserValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaCompatibilityVersion version = executionContext.Script.CompatibilityVersion;
            LuaCompatibilityVersion resolvedVersion = LuaVersionDefaults.Resolve(version);
            bool isLua54OrLater = resolvedVersion >= LuaCompatibilityVersion.Lua54;

            // Lua 5.4 parses n before validating the userdata and value arguments.
            int n = isLua54OrLater
                ? GetOptionalUserValueIndex(args, 2, "setuservalue", version)
                : 1;

            LuaValue v = args.AsType(0, "setuservalue", DataType.UserData, false);
            LuaValue valueArgument = args.Count > 1 ? args[1] : LuaValue.Void;

            if (valueArgument.Type == DataType.Void)
            {
                if (resolvedVersion <= LuaCompatibilityVersion.Lua52)
                {
                    valueArgument = LuaValue.Nil;
                }
                else
                {
                    throw ScriptRuntimeException.BadArgumentValueExpected(1, "setuservalue");
                }
            }

            // Lua 5.2 requires nil or table; Lua 5.3 broadens the user value to any Lua value.
            // NovaSharp exposes this API in 5.1 compatibility mode with the 5.2 contract.
            if (
                resolvedVersion <= LuaCompatibilityVersion.Lua52
                && valueArgument.IsNotNil()
                && valueArgument.Type != DataType.Table
            )
            {
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "setuservalue",
                    ZString.Concat("table expected, got ", valueArgument.Type.ToErrorTypeString())
                );
            }

            // NovaSharp only supports a single user value (slot 1)
            // Any n != 1 means the userdata doesn't have that slot, return nil (fail)
            if (n != 1)
            {
                return LuaValue.Nil;
            }

            v.UserData.UserValue = valueArgument;
            return v;
        }

        private static int GetOptionalUserValueIndex(
            CallbackArguments args,
            int argumentIndex,
            string functionName,
            LuaCompatibilityVersion version
        )
        {
            if (args.Count <= argumentIndex || args[argumentIndex].IsNil)
            {
                return 1;
            }

            LuaValue suppliedValue = args[argumentIndex];
            LuaValue value;
            if (suppliedValue.Type == DataType.String)
            {
                value = LuaNumber.TryParse(
                    suppliedValue.String,
                    version,
                    out LuaNumber parsedNumber
                )
                    ? LuaValue.NewNumber(parsedNumber)
                    : suppliedValue.CheckType(
                        functionName,
                        DataType.Number,
                        argumentIndex,
                        TypeValidationOptions.None
                    );
            }
            else
            {
                value = args.AsType(argumentIndex, functionName, DataType.Number, false);
            }
            long index = LuaNumberHelpers.ToLongWithValidation(
                version,
                value,
                functionName,
                argumentIndex + 1
            );

            // Lua's implementation explicitly narrows lua_Integer to C int here.
            return unchecked((int)index);
        }

        /// <summary>
        /// Implements <c>debug.getregistry</c>, returning the script registry table.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Unused but validated per Lua semantics.</param>
        /// <returns>The registry table.</returns>
        [NovaSharpModuleMethod(Name = "getregistry")]
        public static LuaValue GetRegistry(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return LuaValue.NewTable(executionContext.Script.Registry);
        }

        /// <summary>
        /// Implements <c>debug.getmetatable</c>, returning the metatable for the supplied value.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value whose metatable is requested).</param>
        /// <returns>The metatable or <see cref="LuaValue.Nil"/>.</returns>
        [NovaSharpModuleMethod(Name = "getmetatable")]
        public static LuaValue GetMetatable(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue v = args[0];
            Script s = executionContext.Script;

            if (v.Type.CanHaveTypeMetatables())
            {
                Table typeMetatable = s.GetTypeMetatable(v.Type);
                return typeMetatable != null ? LuaValue.NewTable(typeMetatable) : LuaValue.Nil;
            }
            else if (v.Type == DataType.Table)
            {
                Table tableMetatable = v.Table.MetaTable;
                return tableMetatable != null ? LuaValue.NewTable(tableMetatable) : LuaValue.Nil;
            }
            else
            {
                return LuaValue.Nil;
            }
        }

        /// <summary>
        /// Implements <c>debug.setmetatable</c>, assigning a new metatable to a type or table.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (value and optional metatable).</param>
        /// <returns>The original value after mutation.</returns>
        [NovaSharpModuleMethod(Name = "setmetatable")]
        public static LuaValue SetMetatable(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue v = args[0];
            LuaValue metaArgument = args.Count > 1 ? args[1] : LuaValue.Void;

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

            Table m = metaArgument.IsNil ? null : metaArgument.Table;
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
        public static LuaValue GetUpValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue indexArg = args.AsType(1, "getupvalue", DataType.Number, false);

            // Lua 5.3+: index must have integer representation
            LuaNumberHelpers.ValidateIntegerArgument(
                executionContext.Script.CompatibilityVersion,
                indexArg,
                "getupvalue",
                2
            );

            // Use LuaNumber for proper integer extraction
            LuaNumber indexNum = indexArg.LuaNumber;
            int index =
                (indexNum.IsInteger ? (int)indexNum.AsInteger : (int)Math.Floor(indexNum.AsFloat))
                - 1;

            if (args[0].Type == DataType.ClrFunction)
            {
                return LuaValue.Nil;
            }

            Closure fn = args.AsType(0, "getupvalue", DataType.Function, false).Function;

            ClosureContext closure = fn.ClosureContext;

            if (index < 0 || index >= closure.Count)
            {
                return LuaValue.Nil;
            }

            return LuaValue.NewTuple(LuaValue.NewString(closure.Symbols[index]), closure[index]);
        }

        /// <summary>
        /// Implements <c>debug.upvalueid</c>, returning an identifier for the specified upvalue reference.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (closure and upvalue index).</param>
        /// <returns>An identifier suitable for comparison or nil.</returns>
        [NovaSharpModuleMethod(Name = "upvalueid")]
        public static LuaValue UpValueId(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue indexArg = args.AsType(1, "upvalueid", DataType.Number, false);

            // Lua 5.3+: index must have integer representation
            LuaNumberHelpers.ValidateIntegerArgument(
                executionContext.Script.CompatibilityVersion,
                indexArg,
                "upvalueid",
                2
            );

            // Use LuaNumber for proper integer extraction
            LuaNumber indexNum = indexArg.LuaNumber;
            int index =
                (indexNum.IsInteger ? (int)indexNum.AsInteger : (int)Math.Floor(indexNum.AsFloat))
                - 1;

            // Version-conditional behavior:
            // - Lua 5.4+: Return nil for invalid indices, CLR functions, or null slots
            // - Lua 5.3: Throw "bad argument #2 to 'upvalueid' (invalid upvalue index)"
            Compatibility.LuaCompatibilityVersion resolvedVersion =
                Compatibility.LuaVersionDefaults.Resolve(
                    executionContext.Script.CompatibilityVersion
                );
            bool useLua54Behavior = resolvedVersion >= Compatibility.LuaCompatibilityVersion.Lua54;

            if (args[0].Type == DataType.ClrFunction)
            {
                // CLR functions have no accessible upvalues
                if (useLua54Behavior)
                {
                    return LuaValue.Nil;
                }
                throw new ScriptRuntimeException(
                    "bad argument #2 to 'upvalueid' (invalid upvalue index)"
                );
            }

            Closure fn = args.AsType(0, "upvalueid", DataType.Function, false).Function;

            ClosureContext closure = fn.ClosureContext;

            if (index < 0 || index >= closure.Count)
            {
                // Invalid index
                if (useLua54Behavior)
                {
                    return LuaValue.Nil;
                }
                throw new ScriptRuntimeException(
                    "bad argument #2 to 'upvalueid' (invalid upvalue index)"
                );
            }

            ValueSlot slot = closure.GetSlot(index);

            if (slot == null)
            {
                // Null slot is also invalid
                if (useLua54Behavior)
                {
                    return LuaValue.Nil;
                }
                throw new ScriptRuntimeException(
                    "bad argument #2 to 'upvalueid' (invalid upvalue index)"
                );
            }

            return GetUpvalueIdentifier(executionContext.Script, slot);
        }

        /// <summary>
        /// Implements <c>debug.setupvalue</c>, assigning a new value to the specified closure upvalue.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (closure, index, new value).</param>
        /// <returns>The upvalue name or nil if the index is invalid.</returns>
        [NovaSharpModuleMethod(Name = "setupvalue")]
        public static LuaValue SetUpValue(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue indexArg = args.AsType(1, "setupvalue", DataType.Number, false);

            // Lua 5.3+: index must have integer representation
            LuaNumberHelpers.ValidateIntegerArgument(
                executionContext.Script.CompatibilityVersion,
                indexArg,
                "setupvalue",
                2
            );

            // Use LuaNumber for proper integer extraction
            LuaNumber indexNum = indexArg.LuaNumber;
            int index =
                (indexNum.IsInteger ? (int)indexNum.AsInteger : (int)Math.Floor(indexNum.AsFloat))
                - 1;

            if (args[0].Type == DataType.ClrFunction)
            {
                return LuaValue.Nil;
            }

            Closure fn = args.AsType(0, "setupvalue", DataType.Function, false).Function;

            ClosureContext closure = fn.ClosureContext;

            if (index < 0 || index >= closure.Count)
            {
                return LuaValue.Nil;
            }

            closure.GetSlot(index).Value = args[2];

            return LuaValue.NewString(closure.Symbols[index]);
        }

        /// <summary>
        /// Implements <c>debug.upvaluejoin</c>, making two closures share the same upvalue reference.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (closure A/index, closure B/index).</param>
        /// <returns><see cref="LuaValue.Void"/> after the join completes.</returns>
        [NovaSharpModuleMethod(Name = "upvaluejoin")]
        public static LuaValue UpValueJoin(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue f1 = args.AsType(0, "upvaluejoin", DataType.Function, false);
            LuaValue f2 = args.AsType(2, "upvaluejoin", DataType.Function, false);
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

            // Make f1's n1-th upvalue refer to f2's n2-th upvalue (per Lua 5.2+ spec)
            c1.ClosureContext.SetSlot(n1, c2.ClosureContext.GetSlot(n2));

            return LuaValue.Void;
        }

        /// <summary>
        /// Implements <c>debug.traceback</c>, formatting a stack trace for the current or supplied coroutine.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (optional thread, message, and level).</param>
        /// <returns>A string containing the formatted traceback or the original message value.</returns>
        [NovaSharpModuleMethod(Name = "traceback")]
        public static LuaValue Traceback(
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

            LuaValue vmessage = args[0];
            LuaValue vlevel = args[1];

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

            return LuaValue.NewString(sb.ToString());
        }

        /// <summary>
        /// Implements Lua's <c>debug.sethook</c>, registering a hook function for the current coroutine.
        /// </summary>
        [NovaSharpModuleMethod(Name = "sethook")]
        public static LuaValue SetHook(
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
                return LuaValue.Nil;
            }

            LuaValue hookFunction = args[argIndex];

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

            if (hookFunction.IsNil)
            {
                HookStates.Remove(hookKey);
                return LuaValue.Nil;
            }

            if (hookFunction.Type != DataType.Function && hookFunction.Type != DataType.ClrFunction)
            {
                throw ScriptRuntimeException.BadArgument(argIndex, "sethook", "function expected");
            }

            DebugHookState state = HookStates.GetValue(hookKey, _ => new DebugHookState());
            state.Function = hookFunction;
            state.Mask = mask ?? string.Empty;
            state.Count = Math.Max(0, count);

            return LuaValue.Nil;
        }

        /// <summary>
        /// Implements Lua's <c>debug.gethook</c>, returning the previously registered hook function.
        /// </summary>
        [NovaSharpModuleMethod(Name = "gethook")]
        public static LuaValue GetHook(
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
                return LuaValue.NewTuple(
                    LuaValue.Nil,
                    LuaValue.NewString(string.Empty),
                    LuaValue.FromNumber(0)
                );
            }

            return LuaValue.NewTuple(
                state.Function,
                LuaValue.NewString(state.Mask ?? string.Empty),
                LuaValue.NewNumber(state.Count)
            );
        }

        /// <summary>
        /// Implements Lua's <c>debug.getlocal</c>, returning the name and value of the specified local.
        /// </summary>
        [NovaSharpModuleMethod(Name = "getlocal")]
        public static LuaValue GetLocal(
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
            LuaValue target = args[argIndex];

            if (target.Type == DataType.Function || target.Type == DataType.ClrFunction)
            {
                int funcLocalIndex = args.AsInt(argIndex + 1, "getlocal");
                return GetLocalFromFunction(target, funcLocalIndex);
            }

            int level = args.AsInt(argIndex, "getlocal");
            int locationIndex = args.AsInt(argIndex + 1, "getlocal");

            if (level < 0)
            {
                return LuaValue.Nil;
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
        public static LuaValue SetLocal(
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
            LuaValue newValue = args[2];

            if (level < 0)
            {
                return LuaValue.Nil;
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

        private static LuaValue BuildStackInfo(
            ScriptExecutionContext executionContext,
            int level,
            string what
        )
        {
            bool includeFunctions = ContainsWhatFlag(what, 'f') || ContainsWhatFlag(what, 'u');
            IReadOnlyList<WatchItem> frames = executionContext.GetCallStackSnapshot(
                executionContext.CallingLocation,
                includeFunctions
            );

            if (frames.Count == 0 || level >= frames.Count)
            {
                return LuaValue.Nil;
            }

            Table info = new(executionContext.Script);
            PopulateInfoFromFrame(executionContext.Script, info, frames[level], what);
            return LuaValue.NewTable(info);
        }

        private static LuaValue BuildFunctionInfo(Script script, LuaValue function, string what)
        {
            Table info = new(script);

            if (ContainsWhatFlag(what, 'f'))
            {
                info.Set("func", function);
            }

            PopulateFunctionMetadata(script, info, function, what);
            return LuaValue.NewTable(info);
        }

        private static void PopulateFunctionMetadata(
            Script script,
            Table info,
            LuaValue function,
            string what
        )
        {
            bool isClr = function.Type == DataType.ClrFunction;

            if (ContainsWhatFlag(what, 'S'))
            {
                SourceRef sourceRef =
                    function.Type == DataType.Function
                        ? script.GetFunctionSourceRef(function.Function)
                        : null;
                info.Set("what", LuaValue.NewString(isClr ? "C" : "Lua"));
                SetSourceFields(script, info, sourceRef, isClr);
            }

            if (ContainsWhatFlag(what, 'l'))
            {
                info.Set("currentline", LuaValue.NewNumber(-1));
            }

            if (ContainsWhatFlag(what, 'n'))
            {
                info.Set("name", LuaValue.Nil);
                info.Set("namewhat", LuaValue.NewString(string.Empty));
            }

            if (ContainsWhatFlag(what, 'u'))
            {
                SetUpvalueFields(script, info, function);
            }

            if (ContainsWhatFlag(what, 'L'))
            {
                info.Set("activelines", LuaValue.NewTable(new Table(script)));
            }

            if (ContainsWhatFlag(what, 't'))
            {
                info.Set("istailcall", LuaValue.False);
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
                info.Set("func", frame.Value ?? BuildFunctionPlaceholder(frame));
            }

            if (ContainsWhatFlag(what, 'S'))
            {
                info.Set("what", LuaValue.NewString(isClrFrame ? "C" : "Lua"));
                SetSourceFields(script, info, frame.Location, isClrFrame);
            }

            if (ContainsWhatFlag(what, 'l'))
            {
                int currentLine = frame.Location?.FromLine ?? -1;
                info.Set("currentline", LuaValue.NewNumber(currentLine));
            }

            if (ContainsWhatFlag(what, 'n'))
            {
                if (frame.IsTailCall)
                {
                    info.Set("name", LuaValue.Nil);
                    info.Set("namewhat", LuaValue.NewString(string.Empty));
                }
                else if (frame.Name != null)
                {
                    info.Set("name", LuaValue.NewString(frame.Name));
                    info.Set("namewhat", LuaValue.NewString("global"));
                }
                else
                {
                    info.Set("name", LuaValue.Nil);
                    info.Set("namewhat", LuaValue.NewString(string.Empty));
                }
            }

            if (ContainsWhatFlag(what, 'u'))
            {
                SetUpvalueFields(script, info, frame.Value);
            }

            if (ContainsWhatFlag(what, 'L'))
            {
                info.Set("activelines", LuaValue.NewTable(new Table(script)));
            }

            if (ContainsWhatFlag(what, 't'))
            {
                info.Set("istailcall", LuaValue.FromBoolean(frame.IsTailCall));
            }
        }

        private static LuaValue BuildFunctionPlaceholder(WatchItem frame)
        {
            if (frame.Address >= 0)
            {
                using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
                sb.Append("function: 0x");
                sb.Append(frame.Address.ToString("x", CultureInfo.InvariantCulture));
                return LuaValue.NewString(sb.ToString());
            }

            string name = frame.Name ?? LuaKeywords.Function;
            return LuaValue.NewString(ZString.Concat("function: ", name));
        }

        private static void SetUpvalueFields(Script script, Table info, LuaValue? function)
        {
            int upvalues =
                function.HasValue && function.Value.Type == DataType.Function
                    ? function.Value.Function.UpValuesCount
                    : 0;
            info.Set("nups", LuaValue.FromNumber(upvalues));

            if (
                LuaVersionDefaults.Resolve(script.CompatibilityVersion)
                < LuaCompatibilityVersion.Lua52
            )
            {
                return;
            }

            int parameterCount = 0;
            bool isVarArg = function.HasValue && function.Value.Type == DataType.ClrFunction;

            if (function.HasValue && function.Value.Type == DataType.Function)
            {
                script.GetFunctionArgumentInfo(
                    function.Value.Function,
                    out parameterCount,
                    out isVarArg
                );
            }

            info.Set("nparams", LuaValue.FromNumber(parameterCount));
            info.Set("isvararg", LuaValue.FromBoolean(isVarArg));
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
                info.Set("source", LuaValue.NewString("=[C]"));
                info.Set("short_src", LuaValue.NewString("[C]"));
                info.Set("linedefined", LuaValue.NewNumber(-1));
                info.Set("lastlinedefined", LuaValue.NewNumber(-1));
                return;
            }

            SourceCode source = script.GetSourceCode(location.SourceIdx);
            string sourceName = source?.Name ?? string.Empty;
            string chunkName = "@" + sourceName;

            info.Set("source", LuaValue.NewString(chunkName));
            info.Set("short_src", LuaValue.NewString(ShortenSource(sourceName)));
            info.Set("linedefined", LuaValue.NewNumber(location.FromLine));
            info.Set("lastlinedefined", LuaValue.NewNumber(location.ToLine));
        }

        private static string ShortenSource(string sourceName)
        {
            if (string.IsNullOrEmpty(sourceName) || sourceName.Length <= 60)
            {
                return sourceName;
            }

            return sourceName.Substring(0, 57) + "...";
        }

        private static string ResolveWhatOption(
            Script script,
            CallbackArguments args,
            int optionIndex
        )
        {
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                script.CompatibilityVersion
            );
            string what = version >= LuaCompatibilityVersion.Lua52 ? "nSltuf" : "nSluf";
            if (args.Count > optionIndex && args[optionIndex].IsNotNil())
            {
                what = args.AsType(optionIndex, "getinfo", DataType.String, false).String;
            }

            ValidateWhatOption(script, what, optionIndex);
            return what;
        }

        private static void ValidateWhatOption(Script script, string what, int optionIndex)
        {
            if (string.IsNullOrEmpty(what))
            {
                return;
            }

            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                script.CompatibilityVersion
            );
            string validFlags = version >= LuaCompatibilityVersion.Lua52 ? "nSlufLt" : "nSlufL";

            foreach (char flag in what)
            {
                if (!ContainsWhatFlag(validFlags, flag))
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

        private static LuaValue GetLocalFromFrame(CallStackItem frame, int index)
        {
            if (
                frame == null
                || frame.DebugSymbols == null
                || frame.LocalScope == null
                || index <= 0
            )
            {
                return LuaValue.Nil;
            }

            int zeroBased = index - 1;
            int max = Math.Min(frame.DebugSymbols.Length, frame.LocalScope.Length);

            if (zeroBased >= max)
            {
                return LuaValue.Nil;
            }

            SymbolRef symbol = frame.DebugSymbols[zeroBased];
            LuaValue value = frame.LocalScope[zeroBased]?.Value ?? LuaValue.Nil;
            string name = symbol?.Name ?? string.Empty;

            return LuaValue.NewTuple(LuaValue.NewString(name), value);
        }

        private static LuaValue SetLocalOnFrame(CallStackItem frame, int index, LuaValue newValue)
        {
            if (
                frame == null
                || frame.DebugSymbols == null
                || frame.LocalScope == null
                || index <= 0
            )
            {
                return LuaValue.Nil;
            }

            int zeroBased = index - 1;
            int max = Math.Min(frame.DebugSymbols.Length, frame.LocalScope.Length);

            if (zeroBased >= max)
            {
                return LuaValue.Nil;
            }

            SymbolRef symbol = frame.DebugSymbols[zeroBased];
            ValueSlot slot = frame.LocalScope[zeroBased];

            if (slot == null)
            {
                slot = new ValueSlot();
                frame.LocalScope[zeroBased] = slot;
            }

            slot.Value = newValue;

            string name = symbol?.Name ?? string.Empty;
            return LuaValue.NewString(name);
        }

        private static LuaValue GetLocalFromFunction(LuaValue function, int index)
        {
            if (index <= 0)
            {
                return LuaValue.Nil;
            }

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append("(*function-local ");
            sb.Append(index);
            sb.Append(')');
            string placeholderName = sb.ToString();
            return LuaValue.NewTuple(LuaValue.NewString(placeholderName), LuaValue.Nil);
        }

        private static LuaValue GetClrDebugLocalTuple(
            int index,
            CallbackArguments args,
            int levelArgIndex
        )
        {
            return index switch
            {
                1 => LuaValue.NewTuple(
                    LuaValue.NewString("(*level)"),
                    GetArgumentOrNil(args, levelArgIndex)
                ),
                2 => LuaValue.NewTuple(
                    LuaValue.NewString("(*index)"),
                    GetArgumentOrNil(args, levelArgIndex + 1)
                ),
                3 => LuaValue.NewTuple(
                    LuaValue.NewString("(*value)"),
                    GetArgumentOrNil(args, levelArgIndex + 2)
                ),
                _ => LuaValue.Nil,
            };
        }

        private static LuaValue GetClrDebugLocalName(int index)
        {
            return index switch
            {
                1 => LuaValue.NewString("(*level)"),
                2 => LuaValue.NewString("(*index)"),
                3 => LuaValue.NewString("(*value)"),
                _ => LuaValue.Nil,
            };
        }

        private static LuaValue GetArgumentOrNil(CallbackArguments args, int index)
        {
            if (args == null || index < 0 || index >= args.Count)
            {
                return LuaValue.Nil;
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

        /// <summary>
        /// Mints (or reuses) the stable identity handle for an upvalue.
        /// </summary>
        /// <param name="script">The script owning the upvalue.</param>
        /// <param name="upvalueSlot">The mutable cell backing the upvalue.</param>
        /// <remarks>
        /// Keyed by the <see cref="ValueSlot"/> cell rather than the value it currently holds.
        /// <c>debug.upvalueid</c> exists so a program can tell whether two closures share the same
        /// variable, so the identity must track the variable: keying by value would both collide
        /// unrelated upvalues that happen to hold the same shared instance (nil, true, a cached
        /// small integer) and change the identity of one upvalue whenever it is assigned.
        /// </remarks>
        private static LuaValue GetUpvalueIdentifier(Script script, ValueSlot upvalueSlot)
        {
            return UpvalueIdentifiers
                .GetValue(
                    upvalueSlot,
                    slot => new UpvalueIdentifierValue(
                        UserData.Create(
                            script,
                            new UpvalueIdentifier(slot),
                            UpvalueIdentifierDescriptorInstance
                        )
                    )
                )
                .Value;
        }

        private sealed class UpvalueIdentifierValue
        {
            internal UpvalueIdentifierValue(LuaValue value)
            {
                Value = value;
            }

            internal LuaValue Value { get; }
        }

        private sealed class DebugHookState
        {
            public LuaValue Function { get; set; } = LuaValue.Nil;
            public string Mask { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        private sealed class UpvalueIdentifier
        {
            private static int ReferenceIdCounter;

            public UpvalueIdentifier(ValueSlot slot)
            {
                Upvalue = slot ?? throw new ArgumentNullException(nameof(slot));
                ReferenceId = Interlocked.Increment(ref ReferenceIdCounter);
            }

            public ValueSlot Upvalue { get; }

            public int ReferenceId { get; }

            public override string ToString()
            {
                using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
                sb.Append("upvalue: 0x");
                sb.Append(ReferenceId.ToString("X", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        private sealed class UpvalueIdentifierDescriptor : IUserDataDescriptorTryAccess
        {
            public string Name => "upvalue";

            public Type Type => typeof(UpvalueIdentifier);

            public bool TryIndex(
                Script script,
                object obj,
                LuaValue index,
                bool isDirectIndexing,
                out LuaValue value
            )
            {
                value = LuaValue.Nil;
                return true;
            }

            public bool SetIndex(
                Script script,
                object obj,
                LuaValue index,
                LuaValue value,
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

            public bool TryMetaIndex(Script script, object obj, string metaname, out LuaValue value)
            {
                value = LuaValue.Nil;
                return false;
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

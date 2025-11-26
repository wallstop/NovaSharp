namespace NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Text;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Utilities;
    using REPL;

    /// <summary>
    /// Class implementing debug Lua functions. Support for the debug module is partial.
    /// </summary>
    [NovaSharpModule(Namespace = "debug")]
    public static class DebugModule
    {
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
            DynValue t = args.AsType(1, "setuservalue", DataType.Table, true);

            return v.UserData.UserValue = t;
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
                return DynValue.NewTable(s.GetTypeMetatable(v.Type));
            }
            else if (v.Type == DataType.Table)
            {
                return DynValue.NewTable(v.Table.MetaTable);
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
            DynValue t = args.AsType(1, "setmetatable", DataType.Table, true);
            Table m = (t.IsNil()) ? null : t.Table;
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

            return DynValue.NewNumber(closure[index].ReferenceId);
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

            StringBuilder sb = new();

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
                sb.AppendLine(message);
            }

            sb.AppendLine("stack traceback:");

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
                sb.Append('\t').Append(loc).Append(": in ").Append(name).Append('\n');
            }

            return DynValue.NewString(sb);
        }

        //[NovaSharpModuleMethod(Name = "getlocal")]
        //public static DynValue getlocal(ScriptExecutionContext executionContext, CallbackArguments args)
        //{
        //	Coroutine c;
        //	int funcIdx;
        //	Closure f;
        //	int nextArg = ParseComplexArgs("getlocal", executionContext, args, out c, out f, out funcIdx);

        //	int localIdx = args.AsInt(nextArg, "getlocal");

        //	if (f != null)
        //	{

        //	}
        //	else
        //	{

        //	}

        //}

        //private static int ParseComplexArgs(string funcname, ScriptExecutionContext executionContext, CallbackArguments args, out Coroutine c, out Closure f, out int funcIdx)
        //{
        //	DynValue arg1 = args[0];
        //	int argbase = 0;
        //	c = null;

        //	if (arg1.Type == DataType.Thread)
        //	{
        //		c = arg1.Coroutine;
        //		argbase = 1;
        //	}

        //	if (args[argbase].Type == DataType.Number)
        //	{
        //		funcIdx = (int)args[argbase].Number;
        //		f = null;
        //	}
        //	else
        //	{
        //		funcIdx = -1;
        //		f = args.AsType(argbase, funcname, DataType.Function, false).Function;
        //	}

        //	return argbase + 1;
        //}

        //[NovaSharpMethod]
        //public static DynValue getinfo(ScriptExecutionContext executionContext, CallbackArguments args)
        //{
        //	Coroutine cor = executionContext.CallingCoroutine();
        //	int vfArgIdx = 0;

        //	if (args[0].Type == DataType.Thread)
        //		cor = args[0].Coroutine;

        //	DynValue vf = args[vfArgIdx+0];
        //	DynValue vwhat = args[vfArgIdx+1];

        //	args.AsType(vfArgIdx + 1, "getinfo", DataType.String, true);

        //	string what = vwhat.CastToString() ?? "nfSlu";

        //	DynValue vt = DynValue.NewTable(executionContext.GetScript());
        //	Table t = vt.Table;

        //	if (vf.Type == DataType.Function)
        //	{
        //		Closure f = vf.Function;
        //		executionContext.GetInfoForFunction
        //	}
        //	else if (vf.Type == DataType.ClrFunction)
        //	{

        //	}
        //	else if (vf.Type == DataType.Number || vf.Type == DataType.String)
        //	{

        //	}
        //	else
        //	{
        //		args.AsType(vfArgIdx + 0, "getinfo", DataType.Number, true);
        //	}

        //	return vt;

        //}
    }
}

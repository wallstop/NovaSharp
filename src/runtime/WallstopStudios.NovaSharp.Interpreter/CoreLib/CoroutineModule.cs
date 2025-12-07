namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Implements Lua's coroutine library (§11.3) for creating, scheduling, and querying coroutines.
    /// </summary>
    [NovaSharpModule(Namespace = "coroutine")]
    public static class CoroutineModule
    {
        /// <summary>
        /// Implements <c>coroutine.create</c>, producing a new coroutine that wraps the supplied function.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments where index 0 is the function to wrap.</param>
        /// <returns>A thread <see cref="DynValue"/> representing the created coroutine.</returns>
        [NovaSharpModuleMethod(Name = "create")]
        public static DynValue Create(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args[0].Type != DataType.Function && args[0].Type != DataType.ClrFunction)
            {
                args.AsType(0, "create", DataType.Function); // this throws
            }

            return executionContext.Script.CreateCoroutine(args[0]);
        }

        /// <summary>
        /// Implements <c>coroutine.wrap</c>, returning a function that resumes a coroutine and propagates errors as exceptions.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments where index 0 is the function to wrap.</param>
        /// <returns>A CLR callback that resumes the wrapped coroutine.</returns>
        [NovaSharpModuleMethod(Name = "wrap")]
        public static DynValue Wrap(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            if (args[0].Type != DataType.Function && args[0].Type != DataType.ClrFunction)
            {
                args.AsType(0, "wrap", DataType.Function); // this throws
            }

            DynValue handle = Create(executionContext, args);
            return DynValue.NewCallback(
                (ctx, callArgs) => handle.Coroutine.Resume(callArgs.GetArray())
            );
        }

        /// <summary>
        /// Implements <c>coroutine.resume</c>, resuming a coroutine and returning Lua-style status + values.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (handle plus optional resume parameters).</param>
        /// <returns>A tuple beginning with <c>true</c>/<c>false</c> followed by yielded results or an error message.</returns>
        [NovaSharpModuleMethod(Name = "resume")]
        public static DynValue Resume(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue handle = args.AsType(0, "resume", DataType.Thread);

            try
            {
                DynValue ret = handle.Coroutine.Resume(args.GetArray(1));

                using (ListPool<DynValue>.Get(out List<DynValue> retval))
                {
                    retval.Add(DynValue.True);

                    if (ret.Type == DataType.Tuple)
                    {
                        for (int i = 0; i < ret.Tuple.Length; i++)
                        {
                            DynValue v = ret.Tuple[i];

                            if ((i == ret.Tuple.Length - 1) && (v.Type == DataType.Tuple))
                            {
                                retval.AddRange(v.Tuple);
                            }
                            else
                            {
                                retval.Add(v);
                            }
                        }
                    }
                    else
                    {
                        retval.Add(ret);
                    }

                    return DynValue.NewTuple(ListPool<DynValue>.ToExactArray(retval));
                }
            }
            catch (ScriptRuntimeException ex)
            {
                return DynValue.NewTuple(DynValue.False, DynValue.NewString(ex.Message));
            }
        }

        /// <summary>
        /// Implements Lua 5.4's <c>coroutine.close</c>, closing the supplied coroutine.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (handle to close).</param>
        /// <returns>The close result tuple provided by the coroutine.</returns>
        [NovaSharpModuleMethod(Name = "close")]
        public static DynValue Close(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue handle = args.AsType(0, "close", DataType.Thread);
            return handle.Coroutine.Close();
        }

        /// <summary>
        /// Implements <c>coroutine.yield</c>, propagating a yield request with the supplied arguments.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments passed to the resuming caller.</param>
        /// <returns>A yield request <see cref="DynValue"/>.</returns>
        [NovaSharpModuleMethod(Name = "yield")]
        public static DynValue Yield(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return DynValue.NewYieldReq(args.GetArray());
        }

        /// <summary>
        /// Implements <c>coroutine.running</c>, returning the currently running coroutine and whether it is the main thread.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Ignored but validated per Lua semantics.</param>
        /// <returns>A tuple of the running coroutine and a boolean indicating main status.</returns>
        [NovaSharpModuleMethod(Name = "running")]
        public static DynValue Running(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            Coroutine c = executionContext.CallingCoroutine;
            return DynValue.NewTuple(
                DynValue.NewCoroutine(c),
                DynValue.FromBoolean(c.State == CoroutineState.Main)
            );
        }

        /// <summary>
        /// Implements <c>coroutine.status</c>, reporting the textual state of the supplied coroutine.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Arguments (handle whose state is queried).</param>
        /// <returns>A string <see cref="DynValue"/> (running, normal, suspended, or dead).</returns>
        [NovaSharpModuleMethod(Name = "status")]
        public static DynValue Status(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue handle = args.AsType(0, "status", DataType.Thread);
            Coroutine running = executionContext.CallingCoroutine;
            CoroutineState cs = handle.Coroutine.State;

            switch (cs)
            {
                case CoroutineState.Main:
                case CoroutineState.Running:
                    return (handle.Coroutine == running)
                        ? DynValue.NewString("running")
                        : DynValue.NewString("normal");
                case CoroutineState.NotStarted:
                case CoroutineState.Suspended:
                case CoroutineState.ForceSuspended:
                    return DynValue.NewString("suspended");
                case CoroutineState.Dead:
                    return DynValue.NewString("dead");
                default:
                    throw new InternalErrorException("Unexpected coroutine state {0}", cs);
            }
        }

        /// <summary>
        /// Implements <c>coroutine.isyieldable</c>, returning true when the current execution context can yield.
        /// </summary>
        /// <param name="executionContext">Current execution context.</param>
        /// <param name="args">Ignored but validated per Lua semantics.</param>
        /// <returns><c>true</c> when yielding is allowed; otherwise <c>false</c>.</returns>
        [NovaSharpModuleMethod(Name = "isyieldable")]
        public static DynValue IsYieldable(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return DynValue.FromBoolean(executionContext.IsYieldable());
        }
    }
}

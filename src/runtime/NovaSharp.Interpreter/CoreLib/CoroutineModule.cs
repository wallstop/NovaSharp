// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing coroutine Lua functions
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule(Namespace = "coroutine")]
    public class CoroutineModule
    {
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

            return executionContext.GetScript().CreateCoroutine(args[0]);
        }

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

                List<DynValue> retval = new();
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

                return DynValue.NewTuple(retval.ToArray());
            }
            catch (ScriptRuntimeException ex)
            {
                return DynValue.NewTuple(DynValue.False, DynValue.NewString(ex.Message));
            }
        }

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

            Coroutine c = executionContext.GetCallingCoroutine();
            return DynValue.NewTuple(
                DynValue.NewCoroutine(c),
                DynValue.NewBoolean(c.State == CoroutineState.Main)
            );
        }

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
            Coroutine running = executionContext.GetCallingCoroutine();
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

            return DynValue.NewBoolean(executionContext.IsYieldable());
        }
    }
}

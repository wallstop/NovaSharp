// Disable warnings about XML documentation
namespace NovaSharp.Interpreter.CoreLib
{
#pragma warning disable 1591

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing error handling Lua functions (pcall and xpcall)
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1052:Static holder types should be static or not inheritable",
        Justification = "Module types participate in generic registration requiring instance types."
    )]
    [NovaSharpModule]
    public class ErrorHandlingModule
    {
        [NovaSharpModuleMethod(Name = "pcall")]
        public static DynValue Pcall(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return SetErrorHandlerStrategy("pcall", executionContext, args, null);
        }

        private static DynValue SetErrorHandlerStrategy(
            string funcName,
            ScriptExecutionContext executionContext,
            CallbackArguments args,
            DynValue handlerBeforeUnwind
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            DynValue v = args[0];
            DynValue[] a = new DynValue[args.Count - 1];

            for (int i = 1; i < args.Count; i++)
            {
                a[i - 1] = args[i];
            }

            if (args[0].Type == DataType.ClrFunction)
            {
                try
                {
                    DynValue ret = args[0].Callback.Invoke(executionContext, a);
                    if (ret.Type == DataType.TailCallRequest)
                    {
                        if (
                            ret.TailCallData.Continuation != null
                            || ret.TailCallData.ErrorHandler != null
                        )
                        {
                            throw new ScriptRuntimeException(
                                "the function passed to {0} cannot be called directly by {0}. wrap in a script function instead.",
                                funcName
                            );
                        }

                        return DynValue.NewTailCallReq(
                            new TailCallData()
                            {
                                Args = ret.TailCallData.Args,
                                Function = ret.TailCallData.Function,
                                Continuation = new CallbackFunction(PcallContinuation, funcName),
                                ErrorHandler = new CallbackFunction(PcallOnError, funcName),
                                ErrorHandlerBeforeUnwind = handlerBeforeUnwind,
                            }
                        );
                    }
                    else if (ret.Type == DataType.YieldRequest)
                    {
                        throw new ScriptRuntimeException(
                            "the function passed to {0} cannot be called directly by {0}. wrap in a script function instead.",
                            funcName
                        );
                    }
                    else
                    {
                        return DynValue.NewTupleNested(DynValue.True, ret);
                    }
                }
                catch (ScriptRuntimeException ex)
                {
                    executionContext.PerformMessageDecorationBeforeUnwind(handlerBeforeUnwind, ex);
                    return DynValue.NewTupleNested(
                        DynValue.False,
                        DynValue.NewString(ex.DecoratedMessage)
                    );
                }
            }
            else if (args[0].Type != DataType.Function)
            {
                return DynValue.NewTupleNested(
                    DynValue.False,
                    DynValue.NewString("attempt to " + funcName + " a non-function")
                );
            }
            else
            {
                return DynValue.NewTailCallReq(
                    new TailCallData()
                    {
                        Args = a,
                        Function = v,
                        Continuation = new CallbackFunction(PcallContinuation, funcName),
                        ErrorHandler = new CallbackFunction(PcallOnError, funcName),
                        ErrorHandlerBeforeUnwind = handlerBeforeUnwind,
                    }
                );
            }
        }

        private static DynValue MakeReturnTuple(bool retstatus, CallbackArguments args)
        {
            DynValue[] rets = new DynValue[args.Count + 1];

            for (int i = 0; i < args.Count; i++)
            {
                rets[i + 1] = args[i];
            }

            rets[0] = DynValue.NewBoolean(retstatus);

            return DynValue.NewTuple(rets);
        }

        public static DynValue PcallContinuation(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return MakeReturnTuple(true, args);
        }

        public static DynValue PcallOnError(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return MakeReturnTuple(false, args);
        }

        [NovaSharpModuleMethod(Name = "xpcall")]
        public static DynValue Xpcall(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            List<DynValue> a = new();

            for (int i = 0; i < args.Count; i++)
            {
                if (i != 1)
                {
                    a.Add(args[i]);
                }
            }

            DynValue handler = null;
            if (args[1].Type == DataType.Function || args[1].Type == DataType.ClrFunction)
            {
                handler = args[1];
            }
            else if (args[1].Type != DataType.Nil)
            {
                args.AsType(1, "xpcall", DataType.Function, false);
            }

            return SetErrorHandlerStrategy(
                "xpcall",
                executionContext,
                new CallbackArguments(a, false),
                handler
            );
        }
    }
}

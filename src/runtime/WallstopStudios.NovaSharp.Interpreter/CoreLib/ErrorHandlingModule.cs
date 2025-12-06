namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing error handling Lua functions (pcall and xpcall)
    /// </summary>
    [NovaSharpModule]
    public static class ErrorHandlingModule
    {
        /// <summary>
        /// Implements Lua's <c>pcall</c>, wrapping a function invocation in protected mode.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 is the function to call and the rest flow into it.</param>
        /// <returns>A tuple beginning with <c>true</c>/<c>false</c> followed by the function results or error message.</returns>
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

            rets[0] = DynValue.FromBoolean(retstatus);

            return DynValue.NewTuple(rets);
        }

        /// <summary>
        /// Continuation invoked after a protected call completes successfully; it prepends
        /// <c>true</c> to the callee's return values to match Lua's <c>pcall</c>/<c>xpcall</c>
        /// contract.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments representing the protected call's return values.</param>
        /// <returns>Tuple with <c>true</c> followed by the original return values.</returns>
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

        /// <summary>
        /// Continuation invoked when a protected call fails; it prepends <c>false</c> and the error
        /// object to mimic Lua's <c>pcall</c>/<c>xpcall</c> failure contract.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments containing the error object/message.</param>
        /// <returns>Tuple beginning with <c>false</c> followed by the error payload.</returns>
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

        /// <summary>
        /// Implements Lua's <c>xpcall</c>, invoking a function with a custom error handler when failures occur.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 is the function and index 1 is the error handler.</param>
        /// <returns>A tuple matching <c>pcall</c>'s result contract.</returns>
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

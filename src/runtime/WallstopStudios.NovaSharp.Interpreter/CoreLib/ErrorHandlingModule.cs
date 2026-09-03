namespace WallstopStudios.NovaSharp.Interpreter.CoreLib
{
    using System;
    using System.Collections.Generic;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class implementing error handling Lua functions (pcall and xpcall)
    /// </summary>
    [NovaSharpModule]
    public static class ErrorHandlingModule
    {
        [ThreadStatic]
        private static CallbackFunction PcallContinuationCallback;

        [ThreadStatic]
        private static CallbackFunction PcallOnErrorCallback;

        [ThreadStatic]
        private static CallbackFunction XpcallContinuationCallback;

        [ThreadStatic]
        private static CallbackFunction XpcallOnErrorCallback;

        /// <summary>
        /// Implements Lua's <c>pcall</c>, wrapping a function invocation in protected mode.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 is the function to call and the rest flow into it.</param>
        /// <returns>A tuple beginning with <c>true</c>/<c>false</c> followed by the function results or error message.</returns>
        [NovaSharpModuleMethod(Name = "pcall")]
        public static LuaValue Pcall(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            return SetErrorHandlerStrategy(
                "pcall",
                executionContext,
                args,
                LuaValue.Nil,
                hasHandlerBeforeUnwind: false
            );
        }

        private static LuaValue SetErrorHandlerStrategy(
            string funcName,
            ScriptExecutionContext executionContext,
            CallbackArguments args,
            LuaValue handlerBeforeUnwind,
            bool hasHandlerBeforeUnwind
        )
        {
            CallbackFunction continuationCallback;
            CallbackFunction errorCallback;
            if (funcName == "xpcall")
            {
                continuationCallback = GetXpcallContinuationCallback();
                errorCallback = GetXpcallOnErrorCallback();
            }
            else
            {
                continuationCallback = GetPcallContinuationCallback();
                errorCallback = GetPcallOnErrorCallback();
            }

            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            LuaValue v = args[0];
            LuaValue[] a = new LuaValue[args.Count - 1];

            for (int i = 1; i < args.Count; i++)
            {
                a[i - 1] = args[i];
            }

            if (args[0].Type == DataType.ClrFunction)
            {
                try
                {
                    LuaValue ret = args[0].Callback.Invoke(executionContext, a);
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

                        TailCallData tailCallData = new()
                        {
                            Args = ret.TailCallData.Args,
                            Function = ret.TailCallData.Function,
                            Continuation = continuationCallback,
                            ErrorHandler = errorCallback,
                        };
                        if (hasHandlerBeforeUnwind)
                        {
                            tailCallData.ErrorHandlerBeforeUnwind = handlerBeforeUnwind;
                        }

                        return LuaValue.NewTailCallReq(tailCallData);
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
                        return LuaValue.NewTupleNested(LuaValue.True, ret);
                    }
                }
                catch (ScriptRuntimeException ex)
                {
                    if (hasHandlerBeforeUnwind)
                    {
                        executionContext.PerformMessageDecorationBeforeUnwind(
                            handlerBeforeUnwind,
                            ex
                        );
                    }
                    else
                    {
                        ex.DecoratedMessage = ex.Message;
                    }
                    return LuaValue.NewTupleNested(
                        LuaValue.False,
                        LuaValue.NewString(ex.DecoratedMessage)
                    );
                }
            }
            else if (args[0].Type != DataType.Function)
            {
                return LuaValue.NewTupleNested(
                    LuaValue.False,
                    LuaValue.NewString("attempt to " + funcName + " a non-function")
                );
            }
            else
            {
                TailCallData tailCallData = new()
                {
                    Args = a,
                    Function = v,
                    Continuation = continuationCallback,
                    ErrorHandler = errorCallback,
                };
                if (hasHandlerBeforeUnwind)
                {
                    tailCallData.ErrorHandlerBeforeUnwind = handlerBeforeUnwind;
                }

                return LuaValue.NewTailCallReq(tailCallData);
            }
        }

        private static CallbackFunction GetPcallContinuationCallback()
        {
            CallbackFunction callback = PcallContinuationCallback;
            if (callback == null)
            {
                callback = new CallbackFunction(PcallContinuation, "pcall");
                PcallContinuationCallback = callback;
            }

            return PrepareCachedCallback(callback);
        }

        private static CallbackFunction GetPcallOnErrorCallback()
        {
            CallbackFunction callback = PcallOnErrorCallback;
            if (callback == null)
            {
                callback = new CallbackFunction(PcallOnError, "pcall");
                PcallOnErrorCallback = callback;
            }

            return PrepareCachedCallback(callback);
        }

        private static CallbackFunction GetXpcallContinuationCallback()
        {
            CallbackFunction callback = XpcallContinuationCallback;
            if (callback == null)
            {
                callback = new CallbackFunction(PcallContinuation, "xpcall");
                XpcallContinuationCallback = callback;
            }

            return PrepareCachedCallback(callback);
        }

        private static CallbackFunction GetXpcallOnErrorCallback()
        {
            CallbackFunction callback = XpcallOnErrorCallback;
            if (callback == null)
            {
                callback = new CallbackFunction(PcallOnError, "xpcall");
                XpcallOnErrorCallback = callback;
            }

            return PrepareCachedCallback(callback);
        }

        private static CallbackFunction PrepareCachedCallback(CallbackFunction callback)
        {
            callback.AdditionalData = null;
            return callback;
        }

        private static LuaValue MakeReturnTuple(bool retstatus, CallbackArguments args)
        {
            LuaValue[] rets = new LuaValue[args.Count + 1];

            for (int i = 0; i < args.Count; i++)
            {
                rets[i + 1] = args[i];
            }

            rets[0] = LuaValue.FromBoolean(retstatus);

            return LuaValue.NewTuple(rets);
        }

        /// <summary>
        /// Continuation invoked after a protected call completes successfully; it prepends
        /// <c>true</c> to the callee's return values to match Lua's <c>pcall</c>/<c>xpcall</c>
        /// contract.
        /// </summary>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments representing the protected call's return values.</param>
        /// <returns>Tuple with <c>true</c> followed by the original return values.</returns>
        public static LuaValue PcallContinuation(
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
        public static LuaValue PcallOnError(
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
        /// <remarks>
        /// <para>Version-specific behavior for extra arguments:</para>
        /// <list type="bullet">
        /// <item><description><b>Lua 5.1</b>: <c>xpcall(f, err)</c> — Only 2 arguments supported. Extra arguments are ignored.</description></item>
        /// <item><description><b>Lua 5.2+</b>: <c>xpcall(f, msgh [,arg1, ...])</c> — Extra arguments are passed to the function <c>f</c>.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="executionContext">Current script execution context.</param>
        /// <param name="args">Arguments where index 0 is the function and index 1 is the error handler.</param>
        /// <returns>A tuple matching <c>pcall</c>'s result contract.</returns>
        [NovaSharpModuleMethod(Name = "xpcall")]
        public static LuaValue Xpcall(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            executionContext = ModuleArgumentValidation.RequireExecutionContext(
                executionContext,
                nameof(executionContext)
            );
            args = ModuleArgumentValidation.RequireArguments(args, nameof(args));

            // Get version early as it affects both extra args and handler validation
            LuaCompatibilityVersion version = LuaVersionDefaults.Resolve(
                executionContext.Script.CompatibilityVersion
            );
            bool isLua51 = version == LuaCompatibilityVersion.Lua51;
            bool isLua51Or52 = isLua51 || version == LuaCompatibilityVersion.Lua52;

            // Build argument array for the function call.
            // Version-specific behavior for extra arguments:
            // - Lua 5.1: xpcall(f, err) — Only 2 arguments supported. Extra arguments are IGNORED.
            // - Lua 5.2+: xpcall(f, msgh [,arg1, ...]) — Extra arguments are passed to f.
            // Note: SetErrorHandlerStrategy expects args[0] to be the function, and args[1+] to be args to pass.
            LuaValue[] a;
            if (isLua51)
            {
                // Lua 5.1: Only pass the function itself, no extra args
                a = new LuaValue[] { args[0] };
            }
            else
            {
                // Lua 5.2+: Pass function (index 0) and all extra args (index 2+), skip handler (index 1)
                using (ListPool<LuaValue>.Get(out List<LuaValue> tempList))
                {
                    for (int i = 0; i < args.Count; i++)
                    {
                        if (i != 1) // Skip the handler at index 1
                        {
                            tempList.Add(args[i]);
                        }
                    }
                    a = ListPool<LuaValue>.ToExactArray(tempList);
                }
            }

            // Version-specific handler validation:
            // Lua 5.1 & 5.2: Do NOT pre-validate the error handler type.
            //   Non-callable handlers are effectively ignored for successful calls,
            //   and cause secondary errors on failure.
            // Lua 5.3+: Pre-validate and throw "bad argument #2" if handler is not a function.
            // NOTE: All versions throw if no handler argument is provided at all.

            // Missing handler argument throws in all versions
            if (!args.TryRawGet(1, translateVoids: false, out LuaValue handlerArg))
            {
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "xpcall",
                    LuaKeywords.Function,
                    "no value",
                    false
                );
            }

            LuaValue handler = LuaValue.Nil;
            if (handlerArg.Type == DataType.Function || handlerArg.Type == DataType.ClrFunction)
            {
                handler = handlerArg;
            }
            else if (!isLua51Or52)
            {
                // Lua 5.3+ validates handler type upfront (including nil)
                throw ScriptRuntimeException.BadArgument(
                    1,
                    "xpcall",
                    LuaKeywords.Function,
                    handlerArg.Type.ToLuaTypeString(),
                    false
                );
            }
            else
            {
                // For Lua 5.1/5.2 with non-function values (including nil), we still pass
                // the handler argument so that when an error occurs, we can attempt to call it
                // and produce "error in error handling" if it fails.
                handler = handlerArg;
            }

            return SetErrorHandlerStrategy(
                "xpcall",
                executionContext,
                new CallbackArguments(a, false) { OwnerScript = executionContext.Script },
                handler,
                hasHandlerBeforeUnwind: true
            );
        }
    }
}

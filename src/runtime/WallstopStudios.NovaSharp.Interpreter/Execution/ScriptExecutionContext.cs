namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.LuaPort.LuaStateInterop;

    /// <summary>
    /// Class giving access to details of the environment where the script is executing
    /// </summary>
    public class ScriptExecutionContext : IScriptPrivateResource
    {
        private readonly Processor _processor;
        private readonly CallbackFunction _callback;

        private readonly struct FixedCallArguments
        {
            private readonly DynValue _arg0;
            private readonly DynValue _arg1;
            private readonly DynValue _arg2;
            private readonly DynValue _arg3;
            private readonly DynValue _arg4;
            private readonly int _count;

            internal FixedCallArguments(DynValue arg)
            {
                _arg0 = arg;
                _arg1 = null;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _count = 1;
            }

            internal FixedCallArguments(DynValue arg1, DynValue arg2)
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _count = 2;
            }

            internal FixedCallArguments(DynValue arg1, DynValue arg2, DynValue arg3)
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = null;
                _arg4 = null;
                _count = 3;
            }

            internal FixedCallArguments(DynValue arg1, DynValue arg2, DynValue arg3, DynValue arg4)
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = null;
                _count = 4;
            }

            internal FixedCallArguments(
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4,
                DynValue arg5
            )
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _count = 5;
            }

            /// <summary>
            /// Invokes the specified callback with the stored fixed arguments.
            /// </summary>
            internal DynValue InvokeCallback(
                ScriptExecutionContext context,
                CallbackFunction callback
            )
            {
                if (callback.HasArgumentViewCallback)
                {
                    return _count switch
                    {
                        1 => callback.InvokeArgumentViewFixed(context, _arg0),
                        2 => callback.InvokeArgumentViewFixed(context, _arg0, _arg1),
                        3 => callback.InvokeArgumentViewFixed(context, _arg0, _arg1, _arg2),
                        4 => callback.InvokeArgumentViewFixed(context, _arg0, _arg1, _arg2, _arg3),
                        5 => callback.InvokeArgumentViewFixed(
                            context,
                            _arg0,
                            _arg1,
                            _arg2,
                            _arg3,
                            _arg4
                        ),
                        _ => throw new InvalidOperationException("Invalid fixed argument count."),
                    };
                }

                return _count switch
                {
                    1 => callback.InvokeLegacyFixed(context, _arg0),
                    2 => callback.InvokeLegacyFixed(context, _arg0, _arg1),
                    3 => callback.InvokeLegacyFixed(context, _arg0, _arg1, _arg2),
                    4 => callback.InvokeLegacyFixed(context, _arg0, _arg1, _arg2, _arg3),
                    5 => callback.InvokeLegacyFixed(context, _arg0, _arg1, _arg2, _arg3, _arg4),
                    _ => throw new InvalidOperationException("Invalid fixed argument count."),
                };
            }

            /// <summary>
            /// Materializes the stored fixed arguments for fallback paths that require an array.
            /// </summary>
            internal DynValue[] ToArray()
            {
                return _count switch
                {
                    1 => new[] { _arg0 },
                    2 => new[] { _arg0, _arg1 },
                    3 => new[] { _arg0, _arg1, _arg2 },
                    4 => new[] { _arg0, _arg1, _arg2, _arg3 },
                    5 => new[] { _arg0, _arg1, _arg2, _arg3, _arg4 },
                    _ => throw new InvalidOperationException("Invalid fixed argument count."),
                };
            }
        }

        internal ScriptExecutionContext(
            Processor p,
            CallbackFunction callBackFunction,
            SourceRef sourceRef,
            bool isDynamic = false
        )
        {
            IsDynamicExecution = isDynamic;
            _processor = p;
            _callback = callBackFunction;
            CallingLocation = sourceRef;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running a dynamic execution.
        /// Under a dynamic execution, most methods of ScriptExecutionContext are not reliable as the
        /// processing engine of the script is not "really" running or is not available.
        /// </summary>
        public bool IsDynamicExecution { get; private set; }

        /// <summary>
        /// Gets the location of the code calling back
        /// </summary>
        public SourceRef CallingLocation { get; private set; }

        /// <summary>
        /// Gets or sets the additional data associated to this CLR function call.
        /// </summary>
        public object AdditionalData
        {
            get { return (_callback != null) ? _callback.AdditionalData : null; }
            set
            {
                if (_callback == null)
                {
                    throw new InvalidOperationException(
                        "Cannot set additional data on a context which has no callback"
                    );
                }

                _callback.AdditionalData = value;
            }
        }

        /// <summary>
        /// Gets the metatable associated with the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public Table GetMetatable(DynValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return _processor.GetMetatable(value);
        }

        /// <summary>
        /// Gets the specified metamethod associated with the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="metamethod">The metamethod name.</param>
        /// <returns></returns>
        public DynValue GetMetamethod(DynValue value, string metamethod)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (metamethod == null)
            {
                throw new ArgumentNullException(nameof(metamethod));
            }

            return _processor.GetMetamethod(value, metamethod);
        }

        /// <summary>
        /// prepares a tail call request for the specified metamethod, or null if no metamethod is found.
        /// </summary>
        public DynValue GetMetamethodTailCall(
            DynValue value,
            string metamethod,
            params DynValue[] args
        )
        {
            DynValue meta = GetMetamethod(value, metamethod);
            if (meta == null)
            {
                return null;
            }

            return DynValue.NewTailCallReq(meta, args);
        }

        /// <summary>
        /// Gets the metamethod to be used for a binary operation using op1 and op2.
        /// </summary>
        public DynValue GetBinaryMetamethod(DynValue op1, DynValue op2, string eventName)
        {
            if (op1 == null)
            {
                throw new ArgumentNullException(nameof(op1));
            }

            if (op2 == null)
            {
                throw new ArgumentNullException(nameof(op2));
            }

            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            return _processor.GetBinaryMetamethod(op1, op2, eventName);
        }

        /// <summary>
        /// Gets the script object associated with this request.
        /// </summary>
        public Script Script => _processor.GetScript();

        /// <summary>
        /// Gets the coroutine currently performing the call.
        /// </summary>
        public Coroutine CallingCoroutine => _processor.AssociatedCoroutine;

        /// <summary>
        /// Determines whether the current CLR callback is allowed to yield back into Lua (Lua 5.4 §3.3.4 coroutines).
        /// </summary>
        /// <returns><c>true</c> when the call originated from a resumable coroutine and the VM is prepared to yield.</returns>
        internal bool IsYieldable()
        {
            if (_processor == null || IsDynamicExecution)
            {
                return false;
            }

            Coroutine coroutine = _processor.AssociatedCoroutine;

            if (coroutine == null || coroutine.State == CoroutineState.Main)
            {
                return false;
            }

            return _processor.CanYield;
        }

        /// <summary>
        /// Calls a callback function implemented in "classic way".
        /// Useful to port C code from Lua, or C# code from UniLua and KopiLua.
        /// Lua : http://www.lua.org/
        /// UniLua : http://github.com/xebecnan/UniLua
        /// KopiLua : http://github.com/NLua/KopiLua
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="functionName">Name of the function - for error messages.</param>
        /// <param name="callback">The callback.</param>
        /// <returns></returns>
        public DynValue EmulateClassicCall(
            CallbackArguments args,
            string functionName,
            Func<LuaState, int> callback
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            LuaState l = new(this, args, functionName);
            int retvals = callback(l);
            return l.GetReturnValue(retvals);
        }

        /// <summary>
        /// Calls the specified function, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(DynValue func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (func.Type == DataType.Function)
            {
                return Script.Call(func);
            }

            if (func.Type == DataType.ClrFunction && func.Callback.HasArgumentViewCallback)
            {
                return CompleteDirectClrCall(func.Callback.InvokeArgumentViewFixed(this));
            }

            if (func.Type == DataType.ClrFunction)
            {
                return CompleteDirectClrCall(func.Callback.InvokeLegacyFixed(this));
            }

            return Call(func, Array.Empty<DynValue>());
        }

        /// <summary>
        /// Calls the specified function with one argument, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg">The argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(DynValue func, DynValue arg)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            FixedCallArguments callArgs = new(arg);

            if (func.Type == DataType.Function)
            {
                return Script.Call(func, arg);
            }

            if (func.Type == DataType.ClrFunction)
            {
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return Call(func, callArgs.ToArray());
        }

        /// <summary>
        /// Calls the specified function with two arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(DynValue func, DynValue arg1, DynValue arg2)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            FixedCallArguments callArgs = new(arg1, arg2);

            if (func.Type == DataType.Function)
            {
                return Script.Call(func, arg1, arg2);
            }

            if (func.Type == DataType.ClrFunction)
            {
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return Call(func, callArgs.ToArray());
        }

        /// <summary>
        /// Calls the specified function with three arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(DynValue func, DynValue arg1, DynValue arg2, DynValue arg3)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            FixedCallArguments callArgs = new(arg1, arg2, arg3);

            if (func.Type == DataType.Function)
            {
                return Script.Call(func, arg1, arg2, arg3);
            }

            if (func.Type == DataType.ClrFunction)
            {
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return Call(func, callArgs.ToArray());
        }

        /// <summary>
        /// Calls the specified function with four arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(
            DynValue func,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4
        )
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            FixedCallArguments callArgs = new(arg1, arg2, arg3, arg4);

            if (func.Type == DataType.Function)
            {
                return Script.Call(func, arg1, arg2, arg3, arg4);
            }

            if (func.Type == DataType.ClrFunction)
            {
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return Call(func, callArgs.ToArray());
        }

        /// <summary>
        /// Calls the specified function with five arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(
            DynValue func,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            FixedCallArguments callArgs = new(arg1, arg2, arg3, arg4, arg5);

            if (func.Type == DataType.Function)
            {
                return Script.Call(func, arg1, arg2, arg3, arg4, arg5);
            }

            if (func.Type == DataType.ClrFunction)
            {
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return Call(func, callArgs.ToArray());
        }

        /// <summary>
        /// Calls the specified function with caller-owned contiguous arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(DynValue func, ReadOnlySpan<DynValue> args)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (func.Type == DataType.ClrFunction)
            {
                DynValue ret = func.Callback.HasArgumentViewCallback
                    ? func.Callback.InvokeArgumentViewSpan(this, args)
                    : func.Callback.InvokeLegacySpan(this, args);
                return CompleteDirectClrCall(ret);
            }

            if (func.Type == DataType.Function)
            {
                switch (args.Length)
                {
                    case 0:
                        return Call(func);
                    case 1:
                        return Call(func, args[0]);
                    case 2:
                        return Call(func, args[0], args[1]);
                    case 3:
                        return Call(func, args[0], args[1], args[2]);
                    case 4:
                        return Call(func, args[0], args[1], args[2], args[3]);
                    case 5:
                        return Call(func, args[0], args[1], args[2], args[3], args[4]);
                }

                return Script.Call(func, args);
            }

            int maxloops = 10;

            while (maxloops > 0)
            {
                DynValue v = GetMetamethod(func, Metamethods.Call);

                if (v == null || v.IsNil() || !CanCallMetamethod(v))
                {
                    throw ScriptRuntimeException.AttemptToCallNonFunc(func.Type);
                }

                DynValue previousFunc = func;
                func = v;

                DynValue[] nextArgs = CreateCallMetamethodArguments(previousFunc, args);
                if (func.Type == DataType.Function || func.Type == DataType.ClrFunction)
                {
                    return Call(func, nextArgs.AsSpan());
                }

                args = nextArgs;
                maxloops--;
            }

            throw ScriptRuntimeException.LoopInCall();
        }

        /// <summary>
        /// Calls the specified function, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public DynValue Call(DynValue func, params DynValue[] args)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (func.Type == DataType.Function)
            {
                return Script.Call(func, args);
            }
            else if (func.Type == DataType.ClrFunction)
            {
                while (true)
                {
                    DynValue ret = func.Callback.Invoke(this, args, false);

                    if (ret.Type == DataType.YieldRequest)
                    {
                        throw ScriptRuntimeException.CannotYield();
                    }
                    else if (ret.Type == DataType.TailCallRequest)
                    {
                        TailCallData tail = ret.TailCallData;

                        if (tail.Continuation != null || tail.ErrorHandler != null)
                        {
                            throw new ScriptRuntimeException(
                                "the function passed cannot be called directly. wrap in a script function instead."
                            );
                        }
                        else
                        {
                            args = tail.BorrowArgsBuffer();
                            func = tail.Function;
                        }
                    }
                    else
                    {
                        return ret;
                    }
                }
            }
            else
            {
                int maxloops = 10;

                while (maxloops > 0)
                {
                    DynValue v = GetMetamethod(func, Metamethods.Call);

                    if (v == null || v.IsNil() || !CanCallMetamethod(v))
                    {
                        throw ScriptRuntimeException.AttemptToCallNonFunc(func.Type);
                    }

                    DynValue previousFunc = func;
                    func = v;

                    if (func.Type == DataType.Function || func.Type == DataType.ClrFunction)
                    {
                        DynValue[] metaargs = new DynValue[args.Length + 1];
                        metaargs[0] = previousFunc;
                        Array.Copy(args, 0, metaargs, 1, args.Length);
                        return Call(func, metaargs);
                    }

                    DynValue[] nextArgs = new DynValue[args.Length + 1];
                    nextArgs[0] = previousFunc;
                    Array.Copy(args, 0, nextArgs, 1, args.Length);
                    args = nextArgs;

                    maxloops--;
                }

                throw ScriptRuntimeException.LoopInCall();
            }
        }

        private static DynValue[] CreateCallMetamethodArguments(
            DynValue function,
            ReadOnlySpan<DynValue> args
        )
        {
            DynValue[] metaargs = new DynValue[args.Length + 1];
            metaargs[0] = function;
            for (int i = 0; i < args.Length; i++)
            {
                metaargs[i + 1] = args[i];
            }

            return metaargs;
        }

        private bool CanCallMetamethod(DynValue metafunction)
        {
            return LuaVersionDefaults.Resolve(Script.Options.CompatibilityVersion)
                    >= LuaCompatibilityVersion.Lua54
                || metafunction.Type == DataType.Function
                || metafunction.Type == DataType.ClrFunction;
        }

        private DynValue CompleteDirectClrCall(DynValue ret)
        {
            while (true)
            {
                if (ret.Type == DataType.YieldRequest)
                {
                    throw ScriptRuntimeException.CannotYield();
                }

                if (ret.Type != DataType.TailCallRequest)
                {
                    return ret;
                }

                TailCallData tail = ret.TailCallData;

                if (tail.Continuation != null || tail.ErrorHandler != null)
                {
                    throw new ScriptRuntimeException(
                        "the function passed cannot be called directly. wrap in a script function instead."
                    );
                }

                ret = Call(tail.Function, tail.BorrowArgsBuffer());
            }
        }

        /// <summary>
        /// Tries to get the reference of a symbol in the current execution state
        /// </summary>
        public DynValue EvaluateSymbol(SymbolRef symref)
        {
            if (symref == null)
            {
                return DynValue.Nil;
            }

            return _processor.GetGenericSymbol(symref);
        }

        /// <summary>
        /// Tries to get the value of a symbol in the current execution state
        /// </summary>
        public DynValue EvaluateSymbolByName(string symbol)
        {
            return EvaluateSymbol(FindSymbolByName(symbol));
        }

        /// <summary>
        /// Finds a symbol by name in the current execution state
        /// </summary>
        public SymbolRef FindSymbolByName(string symbol)
        {
            return _processor.FindSymbolByName(symbol);
        }

        /// <summary>
        /// Gets the current global env, or null if not found.
        /// </summary>
        public Table CurrentGlobalEnv
        {
            get
            {
                DynValue env = EvaluateSymbolByName(WellKnownSymbols.ENV);

                if (env == null || env.Type != DataType.Table)
                {
                    return null;
                }
                else
                {
                    return env.Table;
                }
            }
        }

        /// <summary>
        /// Performs a message decoration before unwinding after an error. To be used in the implementation of xpcall like functions.
        /// </summary>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="exception">The exception.</param>
        public void PerformMessageDecorationBeforeUnwind(
            DynValue messageHandler,
            ScriptRuntimeException exception
        )
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (messageHandler != null)
            {
                exception.DecoratedMessage = _processor.PerformMessageDecorationBeforeUnwind(
                    messageHandler,
                    exception.Message,
                    CallingLocation
                );
            }
            else
            {
                exception.DecoratedMessage = exception.Message;
            }
        }

        /// <summary>
        /// Gets the script owning this resource.
        /// </summary>
        /// <value>
        /// The script owning this resource.
        /// </value>
        public Script OwnerScript
        {
            get { return Script; }
        }

        /// <summary>
        /// Captures the current Lua call stack for debugger-facing helpers.
        /// </summary>
        /// <param name="startingLocation">
        /// Source reference representing the instruction that invoked the current CLR callback.
        /// </param>
        /// <returns>An immutable snapshot of the active call stack.</returns>
        internal IReadOnlyList<WatchItem> GetCallStackSnapshot(SourceRef startingLocation)
        {
            if (_processor == null || IsDynamicExecution)
            {
                return Array.Empty<WatchItem>();
            }

            return _processor.GetDebuggerCallStack(startingLocation ?? CallingLocation);
        }

        /// <summary>
        /// Attempts to resolve the call stack frame at the supplied stack depth (0 = current frame).
        /// </summary>
        internal bool TryGetStackFrame(int level, out CallStackItem frame)
        {
            if (_processor == null || level < 0)
            {
                frame = null;
                return false;
            }

            return _processor.TryGetStackFrame(level, out frame);
        }
    }
}

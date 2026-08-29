namespace WallstopStudios.NovaSharp.Interpreter.Execution
{
    using System;
    using System.Collections.Generic;
    using global::NovaSharp;
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
            private readonly LuaValue _arg0;
            private readonly LuaValue _arg1;
            private readonly LuaValue _arg2;
            private readonly LuaValue _arg3;
            private readonly LuaValue _arg4;
            private readonly LuaValue _arg5;
            private readonly LuaValue _arg6;
            private readonly int _count;

            internal FixedCallArguments(LuaValue arg)
            {
                _arg0 = arg;
                _arg1 = default;
                _arg2 = default;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 1;
            }

            internal FixedCallArguments(LuaValue arg1, LuaValue arg2)
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = default;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 2;
            }

            internal FixedCallArguments(LuaValue arg1, LuaValue arg2, LuaValue arg3)
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = default;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 3;
            }

            internal FixedCallArguments(LuaValue arg1, LuaValue arg2, LuaValue arg3, LuaValue arg4)
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = default;
                _arg5 = default;
                _arg6 = default;
                _count = 4;
            }

            internal FixedCallArguments(
                LuaValue arg1,
                LuaValue arg2,
                LuaValue arg3,
                LuaValue arg4,
                LuaValue arg5
            )
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _arg5 = default;
                _arg6 = default;
                _count = 5;
            }

            internal FixedCallArguments(
                LuaValue arg1,
                LuaValue arg2,
                LuaValue arg3,
                LuaValue arg4,
                LuaValue arg5,
                LuaValue arg6
            )
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _arg5 = arg6;
                _arg6 = default;
                _count = 6;
            }

            internal FixedCallArguments(
                LuaValue arg1,
                LuaValue arg2,
                LuaValue arg3,
                LuaValue arg4,
                LuaValue arg5,
                LuaValue arg6,
                LuaValue arg7
            )
            {
                _arg0 = arg1;
                _arg1 = arg2;
                _arg2 = arg3;
                _arg3 = arg4;
                _arg4 = arg5;
                _arg5 = arg6;
                _arg6 = arg7;
                _count = 7;
            }

            /// <summary>
            /// Gets the number of fixed arguments currently stored.
            /// </summary>
            internal int Count
            {
                get { return _count; }
            }

            /// <summary>
            /// Gets a fixed argument by zero-based index.
            /// </summary>
            internal LuaValue this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => _arg0,
                        1 => _arg1,
                        2 => _arg2,
                        3 => _arg3,
                        4 => _arg4,
                        5 => _arg5,
                        6 => _arg6,
                        _ => throw new ArgumentOutOfRangeException(nameof(index)),
                    };
                }
            }

            /// <summary>
            /// Prepends a callable self value when the fixed argument buffer has capacity.
            /// </summary>
            internal bool TryPrepend(LuaValue value, out FixedCallArguments args)
            {
                switch (_count)
                {
                    case 1:
                        args = new FixedCallArguments(value, _arg0);
                        return true;
                    case 2:
                        args = new FixedCallArguments(value, _arg0, _arg1);
                        return true;
                    case 3:
                        args = new FixedCallArguments(value, _arg0, _arg1, _arg2);
                        return true;
                    case 4:
                        args = new FixedCallArguments(value, _arg0, _arg1, _arg2, _arg3);
                        return true;
                    case 5:
                        args = new FixedCallArguments(value, _arg0, _arg1, _arg2, _arg3, _arg4);
                        return true;
                    case 6:
                        args = new FixedCallArguments(
                            value,
                            _arg0,
                            _arg1,
                            _arg2,
                            _arg3,
                            _arg4,
                            _arg5
                        );
                        return true;
                    default:
                        args = default;
                        return false;
                }
            }

            /// <summary>
            /// Copies the fixed arguments into an existing argument buffer.
            /// </summary>
            internal void CopyTo(LuaValue[] destination, int destinationIndex)
            {
                for (int i = 0; i < Count; i++)
                {
                    destination[destinationIndex + i] = this[i];
                }
            }

            /// <summary>
            /// Invokes the specified callback with the stored fixed arguments.
            /// </summary>
            internal LuaValue InvokeCallback(
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
                        6 => callback.InvokeArgumentViewFixed(
                            context,
                            _arg0,
                            _arg1,
                            _arg2,
                            _arg3,
                            _arg4,
                            _arg5
                        ),
                        7 => callback.InvokeArgumentViewFixed(
                            context,
                            _arg0,
                            _arg1,
                            _arg2,
                            _arg3,
                            _arg4,
                            _arg5,
                            _arg6
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
                    6 => callback.InvokeLegacyFixed(
                        context,
                        _arg0,
                        _arg1,
                        _arg2,
                        _arg3,
                        _arg4,
                        _arg5
                    ),
                    7 => callback.InvokeLegacyFixed(
                        context,
                        _arg0,
                        _arg1,
                        _arg2,
                        _arg3,
                        _arg4,
                        _arg5,
                        _arg6
                    ),
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
        public Table GetMetatable(LuaValue value)
        {
            return _processor.GetMetatable(value);
        }

        /// <summary>
        /// Gets the specified metamethod associated with the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="metamethod">The metamethod name.</param>
        /// <returns>The metamethod, or <see cref="LuaValue.Nil"/> when none is available.</returns>
        public LuaValue GetMetamethod(LuaValue value, string metamethod)
        {
            return TryGetMetamethod(value, metamethod, out LuaValue resolvedMetamethod)
                ? resolvedMetamethod
                : LuaValue.Nil;
        }

        /// <summary>
        /// Attempts to get the specified metamethod associated with the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="metamethod">The metamethod name.</param>
        /// <param name="resolvedMetamethod">
        /// The resolved metamethod, or <see cref="LuaValue.Nil"/> when none is available.
        /// </param>
        /// <returns><see langword="true"/> when a metamethod was resolved; otherwise, <see langword="false"/>.</returns>
        public bool TryGetMetamethod(
            LuaValue value,
            string metamethod,
            out LuaValue resolvedMetamethod
        )
        {
            if (metamethod == null)
            {
                throw new ArgumentNullException(nameof(metamethod));
            }

            return _processor.TryGetMetamethod(value, metamethod, out resolvedMetamethod);
        }

        /// <summary>
        /// Prepares a tail call request for the specified metamethod, or nil if no metamethod is found.
        /// </summary>
        public LuaValue GetMetamethodTailCall(
            LuaValue value,
            string metamethod,
            params LuaValue[] args
        )
        {
            return TryGetMetamethodTailCall(value, metamethod, out LuaValue tailCall, args)
                ? tailCall
                : LuaValue.Nil;
        }

        /// <summary>
        /// Attempts to prepare a tail call request for the specified metamethod.
        /// </summary>
        public bool TryGetMetamethodTailCall(
            LuaValue value,
            string metamethod,
            out LuaValue tailCall,
            params LuaValue[] args
        )
        {
            if (!TryGetMetamethod(value, metamethod, out LuaValue meta))
            {
                tailCall = LuaValue.Nil;
                return false;
            }

            tailCall = LuaValue.NewTailCallReq(meta, args);
            return true;
        }

        /// <summary>
        /// Gets the metamethod to be used for a binary operation using op1 and op2.
        /// </summary>
        /// <returns>The metamethod, or <see cref="LuaValue.Nil"/> when none is available.</returns>
        public LuaValue GetBinaryMetamethod(LuaValue op1, LuaValue op2, string eventName)
        {
            return TryGetBinaryMetamethod(op1, op2, eventName, out LuaValue resolvedMetamethod)
                ? resolvedMetamethod
                : LuaValue.Nil;
        }

        /// <summary>
        /// Attempts to get the metamethod used for a binary operation on <paramref name="op1"/> and
        /// <paramref name="op2"/>.
        /// </summary>
        /// <param name="op1">The left operand.</param>
        /// <param name="op2">The right operand.</param>
        /// <param name="eventName">The metamethod name.</param>
        /// <param name="resolvedMetamethod">
        /// The resolved metamethod, or <see cref="LuaValue.Nil"/> when none is available.
        /// </param>
        /// <returns><see langword="true"/> when a metamethod was resolved; otherwise, <see langword="false"/>.</returns>
        public bool TryGetBinaryMetamethod(
            LuaValue op1,
            LuaValue op2,
            string eventName,
            out LuaValue resolvedMetamethod
        )
        {
            if (eventName == null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            return _processor.TryGetBinaryMetamethod(op1, op2, eventName, out resolvedMetamethod);
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
        /// Test-only hook exposing the calling processor's current value-stack depth, used to assert the VM
        /// leaves no orphaned value slots when a stack overflow is thrown during CLR-to-Lua call setup.
        /// </summary>
        internal int GetCallingProcessorValueStackDepthForTests()
        {
            return _processor.GetValueStackForTests().Count;
        }

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
        public LuaValue EmulateClassicCall(
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
        /// Invokes a classic LuaState callback from a stack-only argument view.
        /// </summary>
        /// <param name="args">Arguments visible to the callback.</param>
        /// <param name="functionName">Function name used in diagnostics.</param>
        /// <param name="callback">Classic callback to invoke synchronously.</param>
        /// <returns>The values left on the emulated LuaState stack.</returns>
        internal LuaValue EmulateClassicCall(
            CallbackArgumentsView args,
            string functionName,
            Func<LuaState, int> callback
        )
        {
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
        public LuaValue Call(LuaValue func)
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func);
            }

            if (func.Type == DataType.ClrFunction && func.Callback.HasArgumentViewCallback)
            {
                return CompleteDirectClrCall(func.Callback.InvokeArgumentViewFixed(this));
            }

            if (func.Type == DataType.ClrFunction)
            {
                return CompleteDirectClrCall(func.Callback.InvokeLegacyFixed(this));
            }

            return CallNonFunction(func);
        }

        /// <summary>
        /// Calls the specified function with one argument, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg">The argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public LuaValue Call(LuaValue func, LuaValue arg)
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, arg);
            }

            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments callArgs = new(arg);
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return CallNonFunction(func, arg);
        }

        /// <summary>
        /// Calls the specified function with two arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public LuaValue Call(LuaValue func, LuaValue arg1, LuaValue arg2)
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, arg1, arg2);
            }

            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments callArgs = new(arg1, arg2);
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return CallNonFunction(func, arg1, arg2);
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
        public LuaValue Call(LuaValue func, LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, arg1, arg2, arg3);
            }

            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments callArgs = new(arg1, arg2, arg3);
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return CallNonFunction(func, arg1, arg2, arg3);
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
        public LuaValue Call(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4
        )
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, arg1, arg2, arg3, arg4);
            }

            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments callArgs = new(arg1, arg2, arg3, arg4);
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return CallNonFunction(func, arg1, arg2, arg3, arg4);
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
        public LuaValue Call(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5
        )
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, arg1, arg2, arg3, arg4, arg5);
            }

            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments callArgs = new(arg1, arg2, arg3, arg4, arg5);
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return CallNonFunction(func, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// Calls the specified function with six arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <param name="arg6">The sixth argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public LuaValue Call(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6
        )
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, arg1, arg2, arg3, arg4, arg5, arg6);
            }

            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments callArgs = new(arg1, arg2, arg3, arg4, arg5, arg6);
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return CallNonFunction(func, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// Calls the specified function with seven arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="arg1">The first argument.</param>
        /// <param name="arg2">The second argument.</param>
        /// <param name="arg3">The third argument.</param>
        /// <param name="arg4">The fourth argument.</param>
        /// <param name="arg5">The fifth argument.</param>
        /// <param name="arg6">The sixth argument.</param>
        /// <param name="arg7">The seventh argument.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public LuaValue Call(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7
        )
        {
            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments callArgs = new(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return CompleteDirectClrCall(callArgs.InvokeCallback(this, func.Callback));
            }

            return CallNonFunction(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        /// <summary>
        /// Calls the specified function with caller-owned contiguous arguments, supporting most cases. The called function must not yield.
        /// </summary>
        /// <param name="func">The function; it must be a Function or ClrFunction or have a call metamethod defined.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>The function result.</returns>
        /// <exception cref="ScriptRuntimeException">If the function yields, returns a tail call request with continuations/handlers or, of course, if it encounters errors.</exception>
        public LuaValue Call(LuaValue func, ReadOnlySpan<LuaValue> args)
        {
            if (func.Type == DataType.ClrFunction)
            {
                LuaValue ret = func.Callback.HasArgumentViewCallback
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
                    case 6:
                        return Call(func, args[0], args[1], args[2], args[3], args[4], args[5]);
                    case 7:
                        return Call(
                            func,
                            args[0],
                            args[1],
                            args[2],
                            args[3],
                            args[4],
                            args[5],
                            args[6]
                        );
                }

                return Script.CallValues(func, args);
            }

            int maxloops = 10;
            bool isFirstCallMetamethodResolution = true;

            while (maxloops > 0)
            {
                if (
                    !TryGetMetamethod(func, Metamethods.Call, out LuaValue v)
                    || v.IsNil
                    || !CanCallMetamethod(v)
                )
                {
                    throw ScriptRuntimeException.AttemptToCallNonFunc(func.Type);
                }

                LuaValue previousFunc = func;
                if (
                    isFirstCallMetamethodResolution
                    && TryCallDirectMetamethod(v, previousFunc, args, out LuaValue directResult)
                )
                {
                    return directResult;
                }

                func = v;
                LuaValue[] nextArgs = CreateCallMetamethodArguments(previousFunc, args);
                if (func.Type == DataType.Function || func.Type == DataType.ClrFunction)
                {
                    return Call(func, nextArgs.AsSpan());
                }

                args = nextArgs;
                isFirstCallMetamethodResolution = false;
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
        public LuaValue Call(LuaValue func, params LuaValue[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (func.Type == DataType.Function)
            {
                return Script.CallValues(func, args);
            }
            else if (func.Type == DataType.ClrFunction)
            {
                while (true)
                {
                    LuaValue ret = func.Callback.Invoke(this, args, false);

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
                bool isFirstCallMetamethodResolution = true;

                while (maxloops > 0)
                {
                    if (
                        !TryGetMetamethod(func, Metamethods.Call, out LuaValue v)
                        || v.IsNil
                        || !CanCallMetamethod(v)
                    )
                    {
                        throw ScriptRuntimeException.AttemptToCallNonFunc(func.Type);
                    }

                    LuaValue previousFunc = func;
                    if (
                        isFirstCallMetamethodResolution
                        && TryCallDirectMetamethod(v, previousFunc, args, out LuaValue directResult)
                    )
                    {
                        return directResult;
                    }

                    func = v;

                    if (func.Type == DataType.Function || func.Type == DataType.ClrFunction)
                    {
                        LuaValue[] metaargs = CreateCallMetamethodArguments(previousFunc, args);
                        return Call(func, metaargs);
                    }

                    LuaValue[] nextArgs = CreateCallMetamethodArguments(previousFunc, args);
                    args = nextArgs;

                    isFirstCallMetamethodResolution = false;
                    maxloops--;
                }

                throw ScriptRuntimeException.LoopInCall();
            }
        }

        private static LuaValue[] CreateCallMetamethodArguments(
            LuaValue function,
            ReadOnlySpan<LuaValue> args
        )
        {
            LuaValue[] metaargs = new LuaValue[args.Length + 1];
            metaargs[0] = function;
            for (int i = 0; i < args.Length; i++)
            {
                metaargs[i + 1] = args[i];
            }

            return metaargs;
        }

        private bool TryCallDirectMetamethod(
            LuaValue metafunction,
            LuaValue self,
            ReadOnlySpan<LuaValue> args,
            out LuaValue result
        )
        {
            if (!IsDirectCallTarget(metafunction))
            {
                result = default;
                return false;
            }

            switch (args.Length)
            {
                case 0:
                    result = Call(metafunction, self);
                    return true;
                case 1:
                    result = Call(metafunction, self, args[0]);
                    return true;
                case 2:
                    result = Call(metafunction, self, args[0], args[1]);
                    return true;
                case 3:
                    result = Call(metafunction, self, args[0], args[1], args[2]);
                    return true;
                case 4:
                    result = Call(metafunction, self, args[0], args[1], args[2], args[3]);
                    return true;
                case 5:
                    result = CallDirectTarget(
                        metafunction,
                        self,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4]
                    );
                    return true;
                case 6:
                    result = CallDirectTarget(
                        metafunction,
                        self,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4],
                        args[5]
                    );
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        private LuaValue CallDirectTarget(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6
        )
        {
            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments args = new(arg1, arg2, arg3, arg4, arg5, arg6);
                return CompleteDirectClrCall(args.InvokeCallback(this, func.Callback));
            }

            return Script.CallDirectLuaFunction(func, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        private LuaValue CallDirectTarget(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7
        )
        {
            if (func.Type == DataType.ClrFunction)
            {
                FixedCallArguments args = new(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                return CompleteDirectClrCall(args.InvokeCallback(this, func.Callback));
            }

            return Script.CallDirectLuaFunction(func, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }

        private LuaValue CallNonFunction(LuaValue func)
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedCallArguments args = new(func);
                return CallChainedNonFunction(metafunction, args);
            }

            return Call(metafunction, func);
        }

        private LuaValue CallNonFunction(LuaValue func, LuaValue arg)
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedCallArguments args = new(func, arg);
                return CallChainedNonFunction(metafunction, args);
            }

            return Call(metafunction, func, arg);
        }

        private LuaValue CallNonFunction(LuaValue func, LuaValue arg1, LuaValue arg2)
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedCallArguments args = new(func, arg1, arg2);
                return CallChainedNonFunction(metafunction, args);
            }

            return Call(metafunction, func, arg1, arg2);
        }

        private LuaValue CallNonFunction(LuaValue func, LuaValue arg1, LuaValue arg2, LuaValue arg3)
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedCallArguments args = new(func, arg1, arg2, arg3);
                return CallChainedNonFunction(metafunction, args);
            }

            return Call(metafunction, func, arg1, arg2, arg3);
        }

        private LuaValue CallNonFunction(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4
        )
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedCallArguments args = new(func, arg1, arg2, arg3, arg4);
                return CallChainedNonFunction(metafunction, args);
            }

            return Call(metafunction, func, arg1, arg2, arg3, arg4);
        }

        private LuaValue CallNonFunction(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5
        )
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedCallArguments args = new(func, arg1, arg2, arg3, arg4, arg5);
                return CallChainedNonFunction(metafunction, args);
            }

            return CallDirectTarget(metafunction, func, arg1, arg2, arg3, arg4, arg5);
        }

        private LuaValue CallNonFunction(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6
        )
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedCallArguments args = new(func, arg1, arg2, arg3, arg4, arg5, arg6);
                return CallChainedNonFunction(metafunction, args);
            }

            return CallDirectTarget(metafunction, func, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        private LuaValue CallNonFunction(
            LuaValue func,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7
        )
        {
            LuaValue metafunction = GetCallableMetamethodOrThrow(func);

            using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                8,
                out LuaValue[] arguments
            );
            arguments[0] = func;
            arguments[1] = arg1;
            arguments[2] = arg2;
            arguments[3] = arg3;
            arguments[4] = arg4;
            arguments[5] = arg5;
            arguments[6] = arg6;
            arguments[7] = arg7;

            return Call(metafunction, arguments.AsSpan(0, 8));
        }

        private LuaValue CallChainedNonFunction(LuaValue func, FixedCallArguments args)
        {
            int maxloops = 9;

            while (func.Type != DataType.Function && func.Type != DataType.ClrFunction)
            {
                if (maxloops <= 0)
                {
                    throw ScriptRuntimeException.LoopInCall();
                }

                LuaValue metafunction = GetCallableMetamethodOrThrow(func);
                if (!args.TryPrepend(func, out FixedCallArguments nextArgs))
                {
                    return CallOverflowChainedNonFunction(func, metafunction, args, maxloops);
                }

                args = nextArgs;
                func = metafunction;
                maxloops--;
            }

            return CallFixed(func, args);
        }

        private LuaValue CallOverflowChainedNonFunction(
            LuaValue func,
            LuaValue metafunction,
            FixedCallArguments args,
            int maxloops
        )
        {
            int count = args.Count + 1;
            int capacity = count + Math.Max(0, maxloops - 1);
            using PooledResource<LuaValue[]> pooled = DynValueArrayPool.Get(
                capacity,
                out LuaValue[] arguments
            );
            arguments[0] = func;
            args.CopyTo(arguments, 1);

            func = metafunction;
            maxloops--;

            while (func.Type != DataType.Function && func.Type != DataType.ClrFunction)
            {
                if (maxloops <= 0)
                {
                    throw ScriptRuntimeException.LoopInCall();
                }

                metafunction = GetCallableMetamethodOrThrow(func);
                Array.Copy(arguments, 0, arguments, 1, count);
                arguments[0] = func;
                count++;
                func = metafunction;
                maxloops--;
            }

            return Call(func, arguments.AsSpan(0, count));
        }

        private LuaValue CallFixed(LuaValue func, FixedCallArguments args)
        {
            return args.Count switch
            {
                1 => Call(func, args[0]),
                2 => Call(func, args[0], args[1]),
                3 => Call(func, args[0], args[1], args[2]),
                4 => Call(func, args[0], args[1], args[2], args[3]),
                5 => Call(func, args[0], args[1], args[2], args[3], args[4]),
                6 => CallDirectTarget(func, args[0], args[1], args[2], args[3], args[4], args[5]),
                7 => CallDirectTarget(
                    func,
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5],
                    args[6]
                ),
                _ => Call(func),
            };
        }

        private static bool IsDirectCallTarget(LuaValue func)
        {
            return func.Type == DataType.Function || func.Type == DataType.ClrFunction;
        }

        private LuaValue GetCallableMetamethodOrThrow(LuaValue func)
        {
            if (
                TryGetMetamethod(func, Metamethods.Call, out LuaValue metafunction)
                && !metafunction.IsNil
                && CanCallMetamethod(metafunction)
            )
            {
                return metafunction;
            }

            throw ScriptRuntimeException.AttemptToCallNonFunc(func.Type);
        }

        private bool CanCallMetamethod(LuaValue metafunction)
        {
            return LuaVersionDefaults.Resolve(Script.Options.CompatibilityVersion)
                    >= LuaCompatibilityVersion.Lua54
                || metafunction.Type == DataType.Function
                || metafunction.Type == DataType.ClrFunction;
        }

        private LuaValue CompleteDirectClrCall(LuaValue ret)
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
        public LuaValue EvaluateSymbol(SymbolRef symref)
        {
            if (symref == null)
            {
                return LuaValue.Nil;
            }

            return _processor.GetGenericSymbol(symref);
        }

        /// <summary>
        /// Tries to get the value of a symbol in the current execution state
        /// </summary>
        public LuaValue EvaluateSymbolByName(string symbol)
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
                LuaValue env = EvaluateSymbolByName(WellKnownSymbols.ENV);

                if (env.Type != DataType.Table)
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
            LuaValue messageHandler,
            ScriptRuntimeException exception
        )
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            exception.DecoratedMessage = _processor.PerformMessageDecorationBeforeUnwind(
                messageHandler,
                exception.Message,
                CallingLocation
            );
        }

        /// <summary>
        /// Preserves the original error message when no pre-unwind handler was supplied.
        /// </summary>
        public static void PerformMessageDecorationBeforeUnwind(ScriptRuntimeException exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            exception.DecoratedMessage = exception.Message;
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
        internal IReadOnlyList<WatchItem> GetCallStackSnapshot(
            SourceRef startingLocation,
            bool includeFunctions = false
        )
        {
            if (_processor == null || IsDynamicExecution)
            {
                return Array.Empty<WatchItem>();
            }

            return _processor.GetDebuggerCallStack(
                startingLocation ?? CallingLocation,
                includeFunctions
            );
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

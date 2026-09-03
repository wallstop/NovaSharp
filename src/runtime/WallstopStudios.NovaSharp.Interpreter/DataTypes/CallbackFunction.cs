namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Options;

    /// <summary>
    /// This class wraps a CLR function
    /// </summary>
    public sealed class CallbackFunction : RefIdObject, IScriptPrivateResource
    {
        private static InteropAccessMode DefaultAccessModeValue = InteropAccessMode.LazyOptimized;
        private readonly ScriptFunctionCallbackView _argumentViewCallback;
        private readonly ScriptFunctionCallbackViewNoContext _argumentViewNoContextCallback;
        private readonly SharedState _sharedState;
        private ConditionalWeakTable<Script, CallbackFunction> _scriptBindings;

        internal sealed class SharedState
        {
            public object AdditionalData { get; set; }
        }

        /// <summary>
        /// Gets the name of the function
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the script owning this callback, or <c>null</c> when the callback is intentionally
        /// shared between scripts.
        /// </summary>
        public Script OwnerScript { get; }

        /// <summary>
        /// Gets the call back.
        /// </summary>
        /// <value>
        /// The call back.
        /// </value>
        public Func<ScriptExecutionContext, CallbackArguments, LuaValue> ClrCallback
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallbackFunction" /> class.
        /// </summary>
        /// <param name="callBack">The callback function to be called.</param>
        /// <param name="name">The callback name, used in stacktraces, debugger, etc..</param>
        public CallbackFunction(
            Func<ScriptExecutionContext, CallbackArguments, LuaValue> callBack,
            string name = null
        )
            : this(null, callBack, name) { }

        internal CallbackFunction(
            Script ownerScript,
            Func<ScriptExecutionContext, CallbackArguments, LuaValue> callBack,
            string name = null,
            SharedState sharedState = null
        )
        {
            if (callBack == null)
            {
                throw new ArgumentNullException(nameof(callBack));
            }

            ClrCallback = callBack;
            Name = name;
            OwnerScript = ownerScript;
            _sharedState = sharedState ?? new SharedState();
        }

        private CallbackFunction(
            Script ownerScript,
            ScriptFunctionCallbackView callBack,
            string name,
            SharedState sharedState = null
        )
        {
            if (callBack == null)
            {
                throw new ArgumentNullException(nameof(callBack));
            }

            _argumentViewCallback = callBack;
            ClrCallback = InvokeArgumentViewCallback;
            Name = name;
            OwnerScript = ownerScript;
            _sharedState = sharedState ?? new SharedState();
        }

        private CallbackFunction(
            Script ownerScript,
            ScriptFunctionCallbackViewNoContext callBack,
            string name,
            SharedState sharedState = null
        )
        {
            if (callBack == null)
            {
                throw new ArgumentNullException(nameof(callBack));
            }

            _argumentViewNoContextCallback = callBack;
            ClrCallback = InvokeArgumentViewCallback;
            Name = name;
            OwnerScript = ownerScript;
            _sharedState = sharedState ?? new SharedState();
        }

        /// <summary>
        /// Creates a callback function that receives a stack-only argument view.
        /// </summary>
        /// <param name="callBack">The callback function to be called.</param>
        /// <param name="name">The callback name, used in stacktraces, debugger, etc..</param>
        /// <returns>The callback function.</returns>
        public static CallbackFunction FromArgumentView(
            ScriptFunctionCallbackView callBack,
            string name = null
        )
        {
            return new CallbackFunction(null, callBack, name);
        }

        /// <summary>
        /// Creates a callback function that receives a stack-only argument view without requiring
        /// a script execution context.
        /// </summary>
        /// <param name="callBack">The callback function to be called.</param>
        /// <param name="name">The callback name, used in stacktraces, debugger, etc..</param>
        /// <returns>The callback function.</returns>
        public static CallbackFunction FromArgumentView(
            ScriptFunctionCallbackViewNoContext callBack,
            string name = null
        )
        {
            return new CallbackFunction(null, callBack, name);
        }

        /// <summary>
        /// Creates a script-owned callback function that receives a stack-only argument view.
        /// </summary>
        internal static CallbackFunction FromArgumentView(
            Script ownerScript,
            ScriptFunctionCallbackView callBack,
            string name = null
        )
        {
            return new CallbackFunction(ownerScript, callBack, name);
        }

        /// <summary>
        /// Creates a script-owned callback function that receives a stack-only argument view
        /// without requiring a script execution context.
        /// </summary>
        internal static CallbackFunction FromArgumentView(
            Script ownerScript,
            ScriptFunctionCallbackViewNoContext callBack,
            string name = null
        )
        {
            return new CallbackFunction(ownerScript, callBack, name);
        }

        /// <summary>
        /// Returns this callback bound to the specified script without mutating a shared callback.
        /// </summary>
        internal CallbackFunction BindToScript(Script ownerScript)
        {
            if (ownerScript == null)
            {
                throw new ArgumentNullException(nameof(ownerScript));
            }

            if (ReferenceEquals(OwnerScript, ownerScript))
            {
                return this;
            }

            if (OwnerScript != null)
            {
                throw new InvalidOperationException(
                    "Callback function belongs to a different Script instance."
                );
            }

            ConditionalWeakTable<Script, CallbackFunction> bindings = Volatile.Read(
                ref _scriptBindings
            );
            if (bindings == null)
            {
                ConditionalWeakTable<Script, CallbackFunction> created = new();
                bindings = Interlocked.CompareExchange(ref _scriptBindings, created, null);
                bindings ??= created;
            }

            return bindings.GetValue(ownerScript, CreateScriptBinding);
        }

        private CallbackFunction CreateScriptBinding(Script ownerScript)
        {
            if (_argumentViewCallback != null)
            {
                return new CallbackFunction(ownerScript, _argumentViewCallback, Name, _sharedState);
            }

            if (_argumentViewNoContextCallback != null)
            {
                return new CallbackFunction(
                    ownerScript,
                    _argumentViewNoContextCallback,
                    Name,
                    _sharedState
                );
            }

            return new CallbackFunction(ownerScript, ClrCallback, Name, _sharedState);
        }

        internal bool HasArgumentViewCallback
        {
            get { return _argumentViewCallback != null || _argumentViewNoContextCallback != null; }
        }

        internal bool HasArgumentViewNoContextCallback
        {
            get { return _argumentViewNoContextCallback != null; }
        }

        /// <summary>
        /// Gets the shared context-aware argument-view delegate, or <c>null</c>. Delegates are
        /// created once per module method and reused across every script registration.
        /// </summary>
        internal ScriptFunctionCallbackView ArgumentViewCallback => _argumentViewCallback;

        /// <summary>
        /// Gets the shared contextless argument-view delegate, or <c>null</c>. Delegates are
        /// created once per module method and reused across every script registration.
        /// </summary>
        internal ScriptFunctionCallbackViewNoContext ArgumentViewNoContextCallback =>
            _argumentViewNoContextCallback;

        /// <summary>
        /// Invokes the callback function
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="isMethodCall">if set to <c>true</c> this is a method call.</param>
        /// <returns></returns>
        public LuaValue Invoke(
            ScriptExecutionContext executionContext,
            IList<LuaValue> args,
            bool isMethodCall = false
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            isMethodCall = NormalizeMethodCall(
                executionContext,
                args.Count,
                args.Count > 0 ? args[0] : LuaValue.Nil,
                isMethodCall
            );

            if (HasArgumentViewCallback)
            {
                return InvokeArgumentViewCallback(
                    executionContext,
                    new CallbackArgumentsView(args, isMethodCall)
                );
            }

            return ClrCallback(executionContext, new CallbackArguments(args, isMethodCall));
        }

        /// <summary>
        /// Invokes the callback function, creating a dynamic context only when the callback
        /// contract requires one.
        /// </summary>
        internal LuaValue Invoke(Script script, IList<LuaValue> args, bool isMethodCall = false)
        {
            if (_argumentViewNoContextCallback == null)
            {
                return Invoke(script.CreateDynamicExecutionContext(this), args, isMethodCall);
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            isMethodCall = NormalizeMethodCall(
                script,
                args.Count,
                args.Count > 0 ? args[0] : LuaValue.Nil,
                isMethodCall
            );

            return _argumentViewNoContextCallback(new CallbackArgumentsView(args, isMethodCall));
        }

        /// <summary>
        /// Invokes an argument-view callback with no arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 0, LuaValue.Nil, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with no arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(Script script, bool isMethodCall = false)
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 0, LuaValue.Nil, isMethodCall);
            return _argumentViewNoContextCallback(new CallbackArgumentsView(isMethodCall));
        }

        /// <summary>
        /// Invokes an argument-view callback with one fixed argument.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 1, arg, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with one fixed argument.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            Script script,
            LuaValue arg,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    arg,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 1, arg, isMethodCall);
            return _argumentViewNoContextCallback(new CallbackArgumentsView(arg, isMethodCall));
        }

        /// <summary>
        /// Invokes an argument-view callback with two fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 2, arg1, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with two fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            Script script,
            LuaValue arg1,
            LuaValue arg2,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    arg1,
                    arg2,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 2, arg1, isMethodCall);
            return _argumentViewNoContextCallback(
                new CallbackArgumentsView(arg1, arg2, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with three fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 3, arg1, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with three fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            Script script,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    arg1,
                    arg2,
                    arg3,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 3, arg1, isMethodCall);
            return _argumentViewNoContextCallback(
                new CallbackArgumentsView(arg1, arg2, arg3, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with four fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 4, arg1, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with four fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            Script script,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 4, arg1, isMethodCall);
            return _argumentViewNoContextCallback(
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with five fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 5, arg1, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, arg5, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with five fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            Script script,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 5, arg1, isMethodCall);
            return _argumentViewNoContextCallback(
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, arg5, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with six fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 6, arg1, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, arg5, arg6, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with six fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            Script script,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 6, arg1, isMethodCall);
            return _argumentViewNoContextCallback(
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, arg5, arg6, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with seven fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 7, arg1, isMethodCall);
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, arg5, arg6, arg7, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with seven fixed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewFixed(
            Script script,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewFixed(
                    script.CreateDynamicExecutionContext(this),
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6,
                    arg7,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(script, 7, arg1, isMethodCall);
            return _argumentViewNoContextCallback(
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, arg5, arg6, arg7, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with a subrange of stack-backed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewStack(
            ScriptExecutionContext executionContext,
            IList<LuaValue> args,
            int offset,
            int count,
            bool isMethodCall = false
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (offset < 0 || offset > args.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > args.Count - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            isMethodCall = NormalizeMethodCall(
                executionContext,
                count,
                count > 0 ? args[offset] : LuaValue.Nil,
                isMethodCall
            );
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(args, offset, count, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with a subrange of stack-backed arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewStack(
            Script script,
            IList<LuaValue> args,
            int offset,
            int count,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewStack(
                    script.CreateDynamicExecutionContext(this),
                    args,
                    offset,
                    count,
                    isMethodCall
                );
            }

            ValidateStackRange(args, offset, count);

            isMethodCall = NormalizeMethodCall(
                script,
                count,
                count > 0 ? args[offset] : LuaValue.Nil,
                isMethodCall
            );
            return _argumentViewNoContextCallback(
                new CallbackArgumentsView(args, offset, count, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with caller-owned contiguous arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewSpan(
            ScriptExecutionContext executionContext,
            ReadOnlySpan<LuaValue> args,
            bool isMethodCall = false
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            isMethodCall = NormalizeMethodCall(
                executionContext,
                args.Length,
                args.Length > 0 ? args[0] : LuaValue.Nil,
                isMethodCall
            );
            return InvokeArgumentViewCallback(
                executionContext,
                new CallbackArgumentsView(args, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with caller-owned contiguous arguments.
        /// </summary>
        internal LuaValue InvokeArgumentViewSpan(
            Script script,
            ReadOnlySpan<LuaValue> args,
            bool isMethodCall = false
        )
        {
            if (_argumentViewNoContextCallback == null)
            {
                return InvokeArgumentViewSpan(
                    script.CreateDynamicExecutionContext(this),
                    args,
                    isMethodCall
                );
            }

            isMethodCall = NormalizeMethodCall(
                script,
                args.Length,
                args.Length > 0 ? args[0] : LuaValue.Nil,
                isMethodCall
            );
            return _argumentViewNoContextCallback(new CallbackArgumentsView(args, isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback that receives materialized <see cref="CallbackArguments"/>.
        /// </summary>
        internal LuaValue InvokeLegacy(
            ScriptExecutionContext executionContext,
            IList<LuaValue> args,
            bool isMethodCall = false
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            isMethodCall = NormalizeMethodCall(
                executionContext,
                args.Count,
                args.Count > 0 ? args[0] : LuaValue.Nil,
                isMethodCall
            );
            return ClrCallback(executionContext, new CallbackArguments(args, isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with no fixed arguments.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 0, LuaValue.Nil, isMethodCall);
            return ClrCallback(executionContext, new CallbackArguments(isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with one fixed argument.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 1, arg, isMethodCall);
            return ClrCallback(executionContext, new CallbackArguments(arg, isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with two fixed arguments.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 2, arg1, isMethodCall);
            return ClrCallback(executionContext, new CallbackArguments(arg1, arg2, isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with three fixed arguments.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 3, arg1, isMethodCall);
            return ClrCallback(
                executionContext,
                new CallbackArguments(arg1, arg2, arg3, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes a legacy callback with four fixed arguments.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 4, arg1, isMethodCall);
            return ClrCallback(
                executionContext,
                new CallbackArguments(arg1, arg2, arg3, arg4, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes a legacy callback with five fixed arguments.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 5, arg1, isMethodCall);
            return ClrCallback(
                executionContext,
                new CallbackArguments(arg1, arg2, arg3, arg4, arg5, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes a legacy callback with six fixed arguments.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 6, arg1, isMethodCall);
            return ClrCallback(
                executionContext,
                new CallbackArguments(arg1, arg2, arg3, arg4, arg5, arg6, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes a legacy callback with seven fixed arguments.
        /// </summary>
        internal LuaValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            LuaValue arg1,
            LuaValue arg2,
            LuaValue arg3,
            LuaValue arg4,
            LuaValue arg5,
            LuaValue arg6,
            LuaValue arg7,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 7, arg1, isMethodCall);
            return ClrCallback(
                executionContext,
                new CallbackArguments(arg1, arg2, arg3, arg4, arg5, arg6, arg7, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes a legacy callback with caller-owned contiguous arguments, materializing only when
        /// the legacy callback contract requires more than fixed storage can carry.
        /// </summary>
        internal LuaValue InvokeLegacySpan(
            ScriptExecutionContext executionContext,
            ReadOnlySpan<LuaValue> args,
            bool isMethodCall = false
        )
        {
            switch (args.Length)
            {
                case 0:
                    return InvokeLegacyFixed(executionContext, isMethodCall);
                case 1:
                    return InvokeLegacyFixed(executionContext, args[0], isMethodCall);
                case 2:
                    return InvokeLegacyFixed(executionContext, args[0], args[1], isMethodCall);
                case 3:
                    return InvokeLegacyFixed(
                        executionContext,
                        args[0],
                        args[1],
                        args[2],
                        isMethodCall
                    );
                case 4:
                    return InvokeLegacyFixed(
                        executionContext,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        isMethodCall
                    );
                case 5:
                    return InvokeLegacyFixed(
                        executionContext,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4],
                        isMethodCall
                    );
                case 6:
                    return InvokeLegacyFixed(
                        executionContext,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4],
                        args[5],
                        isMethodCall
                    );
                case 7:
                    return InvokeLegacyFixed(
                        executionContext,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4],
                        args[5],
                        args[6],
                        isMethodCall
                    );
                default:
                    LuaValue[] copiedArgs = new LuaValue[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        copiedArgs[i] = args[i];
                    }

                    return InvokeLegacy(executionContext, copiedArgs, isMethodCall);
            }
        }

        private LuaValue InvokeArgumentViewCallback(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return InvokeArgumentViewCallback(executionContext, new CallbackArgumentsView(args));
        }

        private LuaValue InvokeArgumentViewCallback(
            ScriptExecutionContext executionContext,
            CallbackArgumentsView args
        )
        {
            if (_argumentViewCallback != null)
            {
                return _argumentViewCallback(executionContext, args);
            }

            return _argumentViewNoContextCallback(args);
        }

        private bool NormalizeMethodCall(
            ScriptExecutionContext executionContext,
            int count,
            LuaValue firstArgument,
            bool isMethodCall
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            this.CheckScriptOwnership(executionContext.Script);

            if (!isMethodCall)
            {
                return false;
            }

            ColonOperatorBehaviour colon = executionContext
                .Script
                .Options
                .ColonOperatorClrCallbackBehaviour;

            return NormalizeMethodCall(colon, count, firstArgument, isMethodCall);
        }

        private bool NormalizeMethodCall(
            Script script,
            int count,
            LuaValue firstArgument,
            bool isMethodCall
        )
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            this.CheckScriptOwnership(script);

            ColonOperatorBehaviour colon = script.Options.ColonOperatorClrCallbackBehaviour;

            return NormalizeMethodCall(colon, count, firstArgument, isMethodCall);
        }

        private static bool NormalizeMethodCall(
            ColonOperatorBehaviour colon,
            int count,
            LuaValue firstArgument,
            bool isMethodCall
        )
        {
            if (!isMethodCall)
            {
                return false;
            }

            if (colon == ColonOperatorBehaviour.TreatAsColon)
            {
                return false;
            }

            if (colon == ColonOperatorBehaviour.TreatAsDotOnUserData)
            {
                return count > 0 && firstArgument.Type == DataType.UserData;
            }

            return isMethodCall;
        }

        private static void ValidateStackRange(IList<LuaValue> args, int offset, int count)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (offset < 0 || offset > args.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > args.Count - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
        }

        /// <summary>
        /// Gets or sets the default access mode used when marshalling delegates
        /// </summary>
        /// <value>
        /// The default access mode. Default, HideMembers and BackgroundOptimized are NOT supported.
        /// </value>
        /// <exception cref="System.ArgumentException">Default, HideMembers and BackgroundOptimized are NOT supported.</exception>
        public static InteropAccessMode DefaultAccessMode
        {
            get { return DefaultAccessModeValue; }
            set
            {
                if (
                    value == InteropAccessMode.Default
                    || value == InteropAccessMode.HideMembers
                    || value == InteropAccessMode.BackgroundOptimized
                )
                {
                    throw new ArgumentException("DefaultAccessMode");
                }

                DefaultAccessModeValue = value;
            }
        }

        /// <summary>
        /// Creates a CallbackFunction from a delegate.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="del">The delegate.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <returns></returns>
        public static CallbackFunction FromDelegate(
            Script script,
            Delegate del,
            InteropAccessMode accessMode = InteropAccessMode.Default
        )
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (del == null)
            {
                throw new ArgumentNullException(nameof(del));
            }

            if (accessMode == InteropAccessMode.Default)
            {
                accessMode = DefaultAccessModeValue;
            }

#if NETFX_CORE
            MethodMemberDescriptor descr = new MethodMemberDescriptor(
                del.GetMethodInfo(),
                accessMode
            );
#else
            MethodMemberDescriptor descr = new(del.Method, accessMode);
#endif
            return descr.GetCallbackFunction(script, del.Target).BindToScript(script);
        }

        /// <summary>
        /// Creates a CallbackFunction from a MethodInfo relative to a function.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="mi">The MethodInfo object.</param>
        /// <param name="obj">The object to which the function applies, or null for static methods.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">The method is not static.</exception>
        public static CallbackFunction FromMethodInfo(
            Script script,
            System.Reflection.MethodInfo mi,
            object obj = null,
            InteropAccessMode accessMode = InteropAccessMode.Default
        )
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (mi == null)
            {
                throw new ArgumentNullException(nameof(mi));
            }

            if (accessMode == InteropAccessMode.Default)
            {
                accessMode = DefaultAccessModeValue;
            }

            MethodMemberDescriptor descr = new(mi, accessMode);
            return descr.GetCallbackFunction(script, obj).BindToScript(script);
        }

        /// <summary>
        /// Gets or sets an object used as additional data to the callback function (available in the execution context).
        /// </summary>
        public object AdditionalData
        {
            get { return _sharedState.AdditionalData; }
            set { _sharedState.AdditionalData = value; }
        }

        /// <summary>
        /// Checks the callback signature of a method is compatible for callbacks
        /// </summary>
        public static bool CheckCallbackSignature(
            System.Reflection.MethodInfo mi,
            bool requirePublicVisibility
        )
        {
            return CheckLegacyCallbackSignature(mi, requirePublicVisibility)
                || CheckArgumentViewCallbackSignature(mi, requirePublicVisibility)
                || CheckArgumentViewNoContextCallbackSignature(mi, requirePublicVisibility);
        }

        /// <summary>
        /// Checks whether a method has the classic callback signature.
        /// </summary>
        internal static bool CheckLegacyCallbackSignature(
            System.Reflection.MethodInfo mi,
            bool requirePublicVisibility
        )
        {
            return CheckCallbackSignatureCore(
                mi,
                requirePublicVisibility,
                typeof(CallbackArguments)
            );
        }

        /// <summary>
        /// Checks whether a method has the argument-view callback signature.
        /// </summary>
        internal static bool CheckArgumentViewCallbackSignature(
            System.Reflection.MethodInfo mi,
            bool requirePublicVisibility
        )
        {
            return CheckCallbackSignatureCore(
                mi,
                requirePublicVisibility,
                typeof(CallbackArgumentsView)
            );
        }

        /// <summary>
        /// Checks whether a method has the contextless argument-view callback signature.
        /// </summary>
        internal static bool CheckArgumentViewNoContextCallbackSignature(
            System.Reflection.MethodInfo mi,
            bool requirePublicVisibility
        )
        {
            if (mi == null)
            {
                throw new ArgumentNullException(nameof(mi));
            }

            System.Reflection.ParameterInfo[] pi = mi.GetParameters();

            return pi.Length == 1
                && pi[0].ParameterType == typeof(CallbackArgumentsView)
                && mi.ReturnType == typeof(LuaValue)
                && (requirePublicVisibility || mi.IsPublic);
        }

        private static bool CheckCallbackSignatureCore(
            System.Reflection.MethodInfo mi,
            bool requirePublicVisibility,
            Type argumentsType
        )
        {
            if (mi == null)
            {
                throw new ArgumentNullException(nameof(mi));
            }

            System.Reflection.ParameterInfo[] pi = mi.GetParameters();

            return (
                pi.Length == 2
                && pi[0].ParameterType == typeof(ScriptExecutionContext)
                && pi[1].ParameterType == argumentsType
                && mi.ReturnType == typeof(LuaValue)
                && (requirePublicVisibility || mi.IsPublic)
            );
        }
    }
}

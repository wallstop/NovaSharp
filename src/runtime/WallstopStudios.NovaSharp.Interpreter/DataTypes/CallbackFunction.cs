namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Options;

    /// <summary>
    /// This class wraps a CLR function
    /// </summary>
    public sealed class CallbackFunction : RefIdObject
    {
        private static InteropAccessMode DefaultAccessModeValue = InteropAccessMode.LazyOptimized;
        private readonly ScriptFunctionCallbackView _argumentViewCallback;

        /// <summary>
        /// Gets the name of the function
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets a cached <see cref="DynValue"/> wrapping this callback.
        /// </summary>
        internal DynValue CachedDynValue { get; set; }

        /// <summary>
        /// Gets the call back.
        /// </summary>
        /// <value>
        /// The call back.
        /// </value>
        public Func<ScriptExecutionContext, CallbackArguments, DynValue> ClrCallback
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
            Func<ScriptExecutionContext, CallbackArguments, DynValue> callBack,
            string name = null
        )
        {
            if (callBack == null)
            {
                throw new ArgumentNullException(nameof(callBack));
            }

            ClrCallback = callBack;
            Name = name;
        }

        private CallbackFunction(ScriptFunctionCallbackView callBack, string name)
        {
            if (callBack == null)
            {
                throw new ArgumentNullException(nameof(callBack));
            }

            _argumentViewCallback = callBack;
            ClrCallback = InvokeArgumentViewCallback;
            Name = name;
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
            return new CallbackFunction(callBack, name);
        }

        internal bool HasArgumentViewCallback
        {
            get { return _argumentViewCallback != null; }
        }

        /// <summary>
        /// Invokes the callback function
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="isMethodCall">if set to <c>true</c> this is a method call.</param>
        /// <returns></returns>
        public DynValue Invoke(
            ScriptExecutionContext executionContext,
            IList<DynValue> args,
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
                args.Count > 0 ? args[0] : null,
                isMethodCall
            );

            if (_argumentViewCallback != null)
            {
                return _argumentViewCallback(
                    executionContext,
                    new CallbackArgumentsView(args, isMethodCall)
                );
            }

            return ClrCallback(executionContext, new CallbackArguments(args, isMethodCall));
        }

        /// <summary>
        /// Invokes an argument-view callback with no arguments.
        /// </summary>
        internal DynValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 0, null, isMethodCall);
            return _argumentViewCallback(executionContext, new CallbackArgumentsView(isMethodCall));
        }

        /// <summary>
        /// Invokes an argument-view callback with one fixed argument.
        /// </summary>
        internal DynValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            DynValue arg,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 1, arg, isMethodCall);
            return _argumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with two fixed arguments.
        /// </summary>
        internal DynValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 2, arg1, isMethodCall);
            return _argumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with three fixed arguments.
        /// </summary>
        internal DynValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 3, arg1, isMethodCall);
            return _argumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with four fixed arguments.
        /// </summary>
        internal DynValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 4, arg1, isMethodCall);
            return _argumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with five fixed arguments.
        /// </summary>
        internal DynValue InvokeArgumentViewFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 5, arg1, isMethodCall);
            return _argumentViewCallback(
                executionContext,
                new CallbackArgumentsView(arg1, arg2, arg3, arg4, arg5, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with a subrange of stack-backed arguments.
        /// </summary>
        internal DynValue InvokeArgumentViewStack(
            ScriptExecutionContext executionContext,
            IList<DynValue> args,
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
                count > 0 ? args[offset] : null,
                isMethodCall
            );
            return _argumentViewCallback(
                executionContext,
                new CallbackArgumentsView(args, offset, count, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes an argument-view callback with caller-owned contiguous arguments.
        /// </summary>
        internal DynValue InvokeArgumentViewSpan(
            ScriptExecutionContext executionContext,
            ReadOnlySpan<DynValue> args,
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
                args.Length > 0 ? args[0] : null,
                isMethodCall
            );
            return _argumentViewCallback(
                executionContext,
                new CallbackArgumentsView(args, isMethodCall)
            );
        }

        /// <summary>
        /// Invokes a legacy callback that receives materialized <see cref="CallbackArguments"/>.
        /// </summary>
        internal DynValue InvokeLegacy(
            ScriptExecutionContext executionContext,
            IList<DynValue> args,
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
                args.Count > 0 ? args[0] : null,
                isMethodCall
            );
            return ClrCallback(executionContext, new CallbackArguments(args, isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with no fixed arguments.
        /// </summary>
        internal DynValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 0, null, isMethodCall);
            return ClrCallback(executionContext, new CallbackArguments(isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with one fixed argument.
        /// </summary>
        internal DynValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            DynValue arg,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 1, arg, isMethodCall);
            return ClrCallback(executionContext, new CallbackArguments(arg, isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with two fixed arguments.
        /// </summary>
        internal DynValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            bool isMethodCall = false
        )
        {
            isMethodCall = NormalizeMethodCall(executionContext, 2, arg1, isMethodCall);
            return ClrCallback(executionContext, new CallbackArguments(arg1, arg2, isMethodCall));
        }

        /// <summary>
        /// Invokes a legacy callback with three fixed arguments.
        /// </summary>
        internal DynValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
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
        internal DynValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
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
        internal DynValue InvokeLegacyFixed(
            ScriptExecutionContext executionContext,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
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
        /// Invokes a legacy callback with caller-owned contiguous arguments, materializing only when
        /// the legacy callback contract requires more than fixed storage can carry.
        /// </summary>
        internal DynValue InvokeLegacySpan(
            ScriptExecutionContext executionContext,
            ReadOnlySpan<DynValue> args,
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
                default:
                    DynValue[] copiedArgs = new DynValue[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        copiedArgs[i] = args[i];
                    }

                    return InvokeLegacy(executionContext, copiedArgs, isMethodCall);
            }
        }

        private DynValue InvokeArgumentViewCallback(
            ScriptExecutionContext executionContext,
            CallbackArguments args
        )
        {
            return _argumentViewCallback(executionContext, new CallbackArgumentsView(args));
        }

        private static bool NormalizeMethodCall(
            ScriptExecutionContext executionContext,
            int count,
            DynValue firstArgument,
            bool isMethodCall
        )
        {
            if (executionContext == null)
            {
                throw new ArgumentNullException(nameof(executionContext));
            }

            if (!isMethodCall)
            {
                return false;
            }

            ColonOperatorBehaviour colon = executionContext
                .Script
                .Options
                .ColonOperatorClrCallbackBehaviour;

            if (colon == ColonOperatorBehaviour.TreatAsColon)
            {
                return false;
            }

            if (colon == ColonOperatorBehaviour.TreatAsDotOnUserData)
            {
                return count > 0 && firstArgument?.Type == DataType.UserData;
            }

            return isMethodCall;
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
            return descr.GetCallbackFunction(script, del.Target);
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
            return descr.GetCallbackFunction(script, obj);
        }

        /// <summary>
        /// Gets or sets an object used as additional data to the callback function (available in the execution context).
        /// </summary>
        public object AdditionalData { get; set; }

        /// <summary>
        /// Checks the callback signature of a method is compatible for callbacks
        /// </summary>
        public static bool CheckCallbackSignature(
            System.Reflection.MethodInfo mi,
            bool requirePublicVisibility
        )
        {
            return CheckLegacyCallbackSignature(mi, requirePublicVisibility)
                || CheckArgumentViewCallbackSignature(mi, requirePublicVisibility);
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
                && mi.ReturnType == typeof(DynValue)
                && (requirePublicVisibility || mi.IsPublic)
            );
        }
    }
}

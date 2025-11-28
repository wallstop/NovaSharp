namespace NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using Diagnostics;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;

    /// <summary>
    /// Class providing easier marshalling of CLR functions
    /// </summary>
    public class MethodMemberDescriptor
        : FunctionMemberDescriptorBase,
            IOptimizableDescriptor,
            IWireableDescriptor
    {
        /// <summary>
        /// Gets the method information (can be a MethodInfo or ConstructorInfo)
        /// </summary>
        public MethodBase MethodInfo { get; private set; }

        /// <summary>
        /// Gets the access mode used for interop
        /// </summary>
        public InteropAccessMode AccessMode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the described method is a constructor
        /// </summary>
        public bool IsConstructor { get; private set; }

        private Func<object, object[], object> _optimizedFunc;
        private Action<object, object[]> _optimizedAction;
        private readonly bool _isAction;
        private readonly bool _isArrayCtor;

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodMemberDescriptor"/> class.
        /// </summary>
        /// <param name="methodBase">The MethodBase (MethodInfo or ConstructorInfo) got through reflection.</param>
        /// <param name="accessMode">The interop access mode.</param>
        /// <exception cref="System.ArgumentException">Invalid accessMode</exception>
        public MethodMemberDescriptor(
            MethodBase methodBase,
            InteropAccessMode accessMode = InteropAccessMode.Default
        )
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            CheckMethodIsCompatible(methodBase, true);

            IsConstructor = (methodBase is ConstructorInfo);
            MethodInfo = methodBase;

            bool isStatic = methodBase.IsStatic || IsConstructor;

            if (IsConstructor)
            {
                _isAction = false;
            }
            else
            {
                _isAction = ((MethodInfo)methodBase).ReturnType == typeof(void);
            }

            ParameterInfo[] reflectionParams = methodBase.GetParameters();
            ParameterDescriptor[] parameters;

            if (MethodInfo.DeclaringType.IsArray)
            {
                _isArrayCtor = true;

                int rank = MethodInfo.DeclaringType.GetArrayRank();

                parameters = new ParameterDescriptor[rank];

                for (int i = 0; i < rank; i++)
                {
                    parameters[i] = new ParameterDescriptor(
                        "idx" + i.ToString(CultureInfo.InvariantCulture),
                        typeof(int)
                    );
                }
            }
            else
            {
                parameters = new ParameterDescriptor[reflectionParams.Length];
                for (int i = 0; i < reflectionParams.Length; i++)
                {
                    parameters[i] = new ParameterDescriptor(reflectionParams[i]);
                }
            }

            bool isExtensionMethod =
                methodBase.IsStatic
                && parameters.Length > 0
                && Attribute.IsDefined(methodBase, typeof(ExtensionAttribute), false);

            Initialize(methodBase.Name, isStatic, parameters, isExtensionMethod);

            // adjust access mode
            if (Script.GlobalOptions.Platform.IsRunningOnAOT())
            {
                accessMode = InteropAccessMode.Reflection;
            }

            if (accessMode == InteropAccessMode.Default)
            {
                accessMode = UserData.DefaultAccessMode;
            }

            if (accessMode == InteropAccessMode.HideMembers)
            {
                throw new ArgumentException("Invalid accessMode");
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Type.IsByRef)
                {
                    accessMode = InteropAccessMode.Reflection;
                    break;
                }
            }

            AccessMode = accessMode;

            if (AccessMode == InteropAccessMode.Preoptimized)
            {
                Optimize();
            }
        }

        /// <summary>
        /// Tries to create a new MethodMemberDescriptor, returning
        /// <c>null</c> in case the method is not
        /// visible to script code.
        /// </summary>
        /// <param name="methodBase">The MethodBase.</param>
        /// <param name="accessMode">The <see cref="InteropAccessMode" /></param>
        /// <param name="forceVisibility">if set to <c>true</c> forces visibility.</param>
        /// <returns>
        /// A new MethodMemberDescriptor or null.
        /// </returns>
        public static MethodMemberDescriptor TryCreateIfVisible(
            MethodBase methodBase,
            InteropAccessMode accessMode,
            bool forceVisibility = false
        )
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            if (!CheckMethodIsCompatible(methodBase, false))
            {
                return null;
            }

            if (
                forceVisibility || (methodBase.GetVisibilityFromAttributes() ?? methodBase.IsPublic)
            )
            {
                return new MethodMemberDescriptor(methodBase, accessMode);
            }

            return null;
        }

        /// <summary>
        /// Checks if the method is compatible with a standard descriptor
        /// </summary>
        /// <param name="methodBase">The MethodBase.</param>
        /// <param name="throwException">if set to <c>true</c> an exception with the proper error message is thrown if not compatible.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">
        /// Thrown if throwException is <c>true</c> and one of this applies:
        /// The method contains unresolved generic parameters, or has an unresolved generic return type
        /// or
        /// The method contains pointer parameters, or has a pointer return type
        /// </exception>
        public static bool CheckMethodIsCompatible(MethodBase methodBase, bool throwException)
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            if (methodBase.ContainsGenericParameters)
            {
                if (throwException)
                {
                    throw new ArgumentException(
                        "Method cannot contain unresolved generic parameters"
                    );
                }

                return false;
            }

            bool hasPointerParameters = false;
            ParameterInfo[] pointerCheckedParameters = methodBase.GetParameters();

            for (int i = 0; i < pointerCheckedParameters.Length; i++)
            {
                if (pointerCheckedParameters[i].ParameterType.IsPointer)
                {
                    hasPointerParameters = true;
                    break;
                }
            }

            if (hasPointerParameters)
            {
                if (throwException)
                {
                    throw new ArgumentException("Method cannot contain pointer parameters");
                }

                return false;
            }

            MethodInfo mi = methodBase as MethodInfo;

            if (mi != null)
            {
                if (mi.ReturnType.IsPointer)
                {
                    if (throwException)
                    {
                        throw new ArgumentException("Method cannot have a pointer return type");
                    }

                    return false;
                }

                if (Framework.Do.IsGenericTypeDefinition(mi.ReturnType))
                {
                    if (throwException)
                    {
                        throw new ArgumentException(
                            "Method cannot have an unresolved generic return type"
                        );
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The internal callback which actually executes the method
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="context">The context.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public override DynValue Execute(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args
        )
        {
            this.CheckAccess(MemberDescriptorAccess.CanExecute, obj);

            if (
                AccessMode == InteropAccessMode.LazyOptimized
                && _optimizedFunc == null
                && _optimizedAction == null
            )
            {
                Optimize();
            }

            try
            {
                object[] pars = base.BuildArgumentList(
                    script,
                    obj,
                    context,
                    args,
                    out IList<int> outParams
                );
                object retv = null;

                if (_optimizedFunc != null)
                {
                    retv = _optimizedFunc(obj, pars);
                }
                else if (_optimizedAction != null)
                {
                    _optimizedAction(obj, pars);
                    retv = DynValue.Void;
                }
                else if (_isAction)
                {
                    MethodInfo.Invoke(obj, pars);
                    retv = DynValue.Void;
                }
                else
                {
                    if (IsConstructor)
                    {
                        retv = ((ConstructorInfo)MethodInfo).Invoke(pars);
                    }
                    else
                    {
                        retv = MethodInfo.Invoke(obj, pars);
                    }
                }

                return BuildReturnValue(script, outParams, pars, retv);
            }
            catch (TargetInvocationException invocationException)
            {
                if (invocationException.InnerException is InterpreterException interpreterException)
                {
                    ExceptionDispatchInfo.Capture(interpreterException).Throw();
                }

                throw;
            }
        }

        /// <summary>
        /// Called by standard descriptors when background optimization or preoptimization needs to be performed.
        /// </summary>
        /// <exception cref="InternalErrorException">Out/Ref params cannot be precompiled.</exception>
        public void Optimize()
        {
            ParameterDescriptor[] parameters = GetParameterArray();

            if (AccessMode == InteropAccessMode.Reflection)
            {
                return;
            }

            MethodInfo methodInfo = MethodInfo as MethodInfo;

            if (methodInfo == null)
            {
                return;
            }

            using (
                PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation)
            )
            {
                ParameterExpression ep = Expression.Parameter(typeof(object[]), "pars");
                ParameterExpression objinst = Expression.Parameter(typeof(object), "instance");
                UnaryExpression inst = Expression.Convert(objinst, MethodInfo.DeclaringType);

                Expression[] args = new Expression[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].OriginalType.IsByRef)
                    {
                        throw new InternalErrorException("Out/Ref params cannot be precompiled.");
                    }
                    else
                    {
                        BinaryExpression x = Expression.ArrayIndex(ep, Expression.Constant(i));
                        args[i] = Expression.Convert(x, parameters[i].OriginalType);
                    }
                }

                Expression fn;

                if (IsStatic)
                {
                    fn = Expression.Call(methodInfo, args);
                }
                else
                {
                    fn = Expression.Call(inst, methodInfo, args);
                }

                if (_isAction)
                {
                    Expression<Action<object, object[]>> lambda = Expression.Lambda<
                        Action<object, object[]>
                    >(fn, objinst, ep);
                    Interlocked.Exchange(ref _optimizedAction, lambda.Compile());
                }
                else
                {
                    UnaryExpression fnc = Expression.Convert(fn, typeof(object));
                    Expression<Func<object, object[], object>> lambda = Expression.Lambda<
                        Func<object, object[], object>
                    >(fnc, objinst, ep);
                    Interlocked.Exchange(ref _optimizedFunc, lambda.Compile());
                }
            }
        }

        /// <summary>
        /// Prepares the descriptor for hard-wiring.
        /// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
        /// </summary>
        /// <param name="t">The table to be filled</param>
        public void PrepareForWiring(Table t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            t.Set("class", DynValue.NewString(GetType().FullName));
            t.Set("name", DynValue.NewString(Name));
            t.Set("ctor", DynValue.NewBoolean(IsConstructor));
            t.Set("special", DynValue.NewBoolean(MethodInfo.IsSpecialName));
            t.Set("visibility", DynValue.NewString(MethodInfo.GetClrVisibility()));

            if (IsConstructor)
            {
                t.Set(
                    "ret",
                    DynValue.NewString(((ConstructorInfo)MethodInfo).DeclaringType.FullName)
                );
            }
            else
            {
                t.Set("ret", DynValue.NewString(((MethodInfo)MethodInfo).ReturnType.FullName));
            }

            if (_isArrayCtor)
            {
                t.Set(
                    "arraytype",
                    DynValue.NewString(MethodInfo.DeclaringType.GetElementType().FullName)
                );
            }

            t.Set("decltype", DynValue.NewString(MethodInfo.DeclaringType.FullName));
            t.Set("static", DynValue.NewBoolean(IsStatic));
            t.Set("extension", DynValue.NewBoolean(ExtensionMethodType != null));

            DynValue pars = DynValue.NewPrimeTable();

            t.Set("params", pars);

            int i = 0;

            foreach (ParameterDescriptor p in Parameters)
            {
                DynValue pt = DynValue.NewPrimeTable();
                pars.Table.Set(++i, pt);
                p.PrepareForWiring(pt.Table);
            }
        }

        /// <summary>
        /// Provides helpers used exclusively by tests to exercise internal optimization paths.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Overrides <see cref="AccessMode"/> so tests can verify eager/lazy optimization behavior.
            /// </summary>
            public static void ForceAccessMode(
                MethodMemberDescriptor descriptor,
                InteropAccessMode accessMode
            )
            {
                descriptor.AccessMode = accessMode;
            }
        }
    }
}

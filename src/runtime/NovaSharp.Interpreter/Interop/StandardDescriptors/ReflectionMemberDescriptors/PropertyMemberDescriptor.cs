namespace NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using Diagnostics;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.Converters;

    /// <summary>
    /// Class providing easier marshalling of CLR properties
    /// </summary>
    public class PropertyMemberDescriptor
        : IMemberDescriptor,
            IOptimizableDescriptor,
            IWireableDescriptor
    {
        /// <summary>
        /// Gets the PropertyInfo got by reflection
        /// </summary>
        public PropertyInfo PropertyInfo { get; private set; }

        /// <summary>
        /// Gets the <see cref="InteropAccessMode" />
        /// </summary>
        public InteropAccessMode AccessMode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the described property is static.
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Gets the name of the property
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance can be read from
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can be read from; otherwise, <c>false</c>.
        /// </value>
        public bool CanRead
        {
            get { return _getter != null; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can be written to.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance can be written to; otherwise, <c>false</c>.
        /// </value>
        public bool CanWrite
        {
            get { return _setter != null; }
        }

        private readonly MethodInfo _getter;

        private readonly MethodInfo _setter;

        private Func<object, object> _optimizedGetter;
        private Action<object, object> _optimizedSetter;

        /// <summary>
        /// Tries to create a new StandardUserDataPropertyDescriptor, returning <c>null</c> in case the property is not
        /// visible to script code.
        /// </summary>
        /// <param name="pi">The PropertyInfo.</param>
        /// <param name="accessMode">The <see cref="InteropAccessMode" /></param>
        /// <returns>A new StandardUserDataPropertyDescriptor or null.</returns>
        public static PropertyMemberDescriptor TryCreateIfVisible(
            PropertyInfo pi,
            InteropAccessMode accessMode
        )
        {
            MethodInfo getter = Framework.Do.GetGetMethod(pi);
            MethodInfo setter = Framework.Do.GetSetMethod(pi);

            bool? pvisible = pi.GetVisibilityFromAttributes();
            bool? gvisible = getter.GetVisibilityFromAttributes();
            bool? svisible = setter.GetVisibilityFromAttributes();

            if (pvisible.HasValue)
            {
                return TryCreate(
                    pi,
                    accessMode,
                    (gvisible ?? pvisible.Value) ? getter : null,
                    (svisible ?? pvisible.Value) ? setter : null
                );
            }
            else
            {
                return TryCreate(
                    pi,
                    accessMode,
                    (gvisible ?? getter.IsPublic) ? getter : null,
                    (svisible ?? setter.IsPublic) ? setter : null
                );
            }
        }

        private static PropertyMemberDescriptor TryCreate(
            PropertyInfo pi,
            InteropAccessMode accessMode,
            MethodInfo getter,
            MethodInfo setter
        )
        {
            if (getter == null && setter == null)
            {
                return null;
            }
            else
            {
                return new PropertyMemberDescriptor(pi, accessMode, getter, setter);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMemberDescriptor"/> class.
        /// NOTE: This constructor gives get/set visibility based exclusively on the CLR visibility of the
        /// getter and setter methods.
        /// </summary>
        /// <param name="pi">The pi.</param>
        /// <param name="accessMode">The access mode.</param>
        public PropertyMemberDescriptor(PropertyInfo pi, InteropAccessMode accessMode)
            : this(pi, accessMode, Framework.Do.GetGetMethod(pi), Framework.Do.GetSetMethod(pi)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMemberDescriptor" /> class.
        /// </summary>
        /// <param name="pi">The PropertyInfo.</param>
        /// <param name="accessMode">The <see cref="InteropAccessMode" /></param>
        /// <param name="getter">The getter method. Use null to make the property writeonly.</param>
        /// <param name="setter">The setter method. Use null to make the property readonly.</param>
        public PropertyMemberDescriptor(
            PropertyInfo pi,
            InteropAccessMode accessMode,
            MethodInfo getter,
            MethodInfo setter
        )
        {
            if (pi == null)
            {
                throw new ArgumentNullException(nameof(pi));
            }

            if (getter == null && setter == null)
            {
                throw new ArgumentNullException("getter and setter cannot both be null");
            }

            if (Script.GlobalOptions.Platform.IsRunningOnAOT())
            {
                accessMode = InteropAccessMode.Reflection;
            }

            PropertyInfo = pi;
            AccessMode = accessMode;
            Name = pi.Name;

            _getter = getter;
            _setter = setter;

            IsStatic = (_getter ?? _setter).IsStatic;

            if (AccessMode == InteropAccessMode.Preoptimized)
            {
                OptimizeGetter();
                OptimizeSetter();
            }
        }

        /// <summary>
        /// Gets the value of the property
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public DynValue GetValue(Script script, object obj)
        {
            this.CheckAccess(MemberDescriptorAccess.CanRead, obj);

            if (_getter == null)
            {
                throw new ScriptRuntimeException(
                    "userdata property '{0}.{1}' cannot be read from.",
                    PropertyInfo.DeclaringType.Name,
                    Name
                );
            }

            if (AccessMode == InteropAccessMode.LazyOptimized && _optimizedGetter == null)
            {
                OptimizeGetter();
            }

            object result = null;

            if (_optimizedGetter != null)
            {
                result = _optimizedGetter(obj);
            }
            else
            {
                result = _getter.Invoke(IsStatic ? null : obj, null); // convoluted workaround for --full-aot Mono execution
            }

            return ClrToScriptConversions.ObjectToDynValue(script, result);
        }

        /// <summary>
        /// Builds (and caches) a delegate for the backing getter when the current access mode allows precompilation.
        /// </summary>
        internal void OptimizeGetter()
        {
            using (
                PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation)
            )
            {
                if (_getter != null)
                {
                    if (IsStatic)
                    {
                        ParameterExpression paramExp = Expression.Parameter(
                            typeof(object),
                            "dummy"
                        );
                        MemberExpression propAccess = Expression.Property(null, PropertyInfo);
                        UnaryExpression castPropAccess = Expression.Convert(
                            propAccess,
                            typeof(object)
                        );
                        Expression<Func<object, object>> lambda = Expression.Lambda<
                            Func<object, object>
                        >(castPropAccess, paramExp);
                        Interlocked.Exchange(ref _optimizedGetter, lambda.Compile());
                    }
                    else
                    {
                        ParameterExpression paramExp = Expression.Parameter(typeof(object), "obj");
                        UnaryExpression castParamExp = Expression.Convert(
                            paramExp,
                            PropertyInfo.DeclaringType
                        );
                        MemberExpression propAccess = Expression.Property(
                            castParamExp,
                            PropertyInfo
                        );
                        UnaryExpression castPropAccess = Expression.Convert(
                            propAccess,
                            typeof(object)
                        );
                        Expression<Func<object, object>> lambda = Expression.Lambda<
                            Func<object, object>
                        >(castPropAccess, paramExp);
                        Interlocked.Exchange(ref _optimizedGetter, lambda.Compile());
                    }
                }
            }
        }

        /// <summary>
        /// Builds (and caches) a delegate for the backing setter when writable and precompilation is allowed.
        /// </summary>
        internal void OptimizeSetter()
        {
            using (
                PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation)
            )
            {
                if (_setter != null && !(Framework.Do.IsValueType(PropertyInfo.DeclaringType)))
                {
                    MethodInfo setterMethod = Framework.Do.GetSetMethod(PropertyInfo);

                    if (IsStatic)
                    {
                        ParameterExpression paramExp = Expression.Parameter(
                            typeof(object),
                            "dummy"
                        );
                        ParameterExpression paramValExp = Expression.Parameter(
                            typeof(object),
                            "val"
                        );
                        UnaryExpression castParamValExp = Expression.Convert(
                            paramValExp,
                            PropertyInfo.PropertyType
                        );
                        MethodCallExpression callExpression = Expression.Call(
                            setterMethod,
                            castParamValExp
                        );
                        Expression<Action<object, object>> lambda = Expression.Lambda<
                            Action<object, object>
                        >(callExpression, paramExp, paramValExp);
                        Interlocked.Exchange(ref _optimizedSetter, lambda.Compile());
                    }
                    else
                    {
                        ParameterExpression paramExp = Expression.Parameter(typeof(object), "obj");
                        ParameterExpression paramValExp = Expression.Parameter(
                            typeof(object),
                            "val"
                        );
                        UnaryExpression castParamExp = Expression.Convert(
                            paramExp,
                            PropertyInfo.DeclaringType
                        );
                        UnaryExpression castParamValExp = Expression.Convert(
                            paramValExp,
                            PropertyInfo.PropertyType
                        );
                        MethodCallExpression callExpression = Expression.Call(
                            castParamExp,
                            setterMethod,
                            castParamValExp
                        );
                        Expression<Action<object, object>> lambda = Expression.Lambda<
                            Action<object, object>
                        >(callExpression, paramExp, paramValExp);
                        Interlocked.Exchange(ref _optimizedSetter, lambda.Compile());
                    }
                }
            }
        }

        /// <summary>
        /// Sets the value of the property
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="v">The value to set.</param>
        public void SetValue(Script script, object obj, DynValue v)
        {
            if (v == null)
            {
                throw new ArgumentNullException(nameof(v));
            }

            this.CheckAccess(MemberDescriptorAccess.CanWrite, obj);

            if (_setter == null)
            {
                throw new ScriptRuntimeException(
                    "userdata property '{0}.{1}' cannot be written to.",
                    PropertyInfo.DeclaringType.Name,
                    Name
                );
            }

            object value = ScriptToClrConversions.DynValueToObjectOfType(
                v,
                PropertyInfo.PropertyType,
                null,
                false
            );

            try
            {
                if (value is double d)
                {
                    value = NumericConversions.DoubleToType(PropertyInfo.PropertyType, d);
                }

                if (AccessMode == InteropAccessMode.LazyOptimized && _optimizedSetter == null)
                {
                    OptimizeSetter();
                }

                if (_optimizedSetter != null)
                {
                    _optimizedSetter(obj, value);
                }
                else
                {
                    _setter.Invoke(IsStatic ? null : obj, new object[] { value }); // convoluted workaround for --full-aot Mono execution
                }
            }
            catch (ArgumentException)
            {
                // non-optimized setters fall here
                throw ScriptRuntimeException.UserDataArgumentTypeMismatch(
                    v.Type,
                    PropertyInfo.PropertyType
                );
            }
            catch (InvalidCastException)
            {
                // optimized setters fall here
                throw ScriptRuntimeException.UserDataArgumentTypeMismatch(
                    v.Type,
                    PropertyInfo.PropertyType
                );
            }
        }

        /// <summary>
        /// Gets the types of access supported by this member
        /// </summary>
        public MemberDescriptorAccess MemberAccess
        {
            get
            {
                MemberDescriptorAccess access = 0;

                if (_setter != null)
                {
                    access |= MemberDescriptorAccess.CanWrite;
                }

                if (_getter != null)
                {
                    access |= MemberDescriptorAccess.CanRead;
                }

                return access;
            }
        }

        /// <summary>
        /// Called by standard descriptors when background optimization or preoptimization needs to be performed.
        /// </summary>
        public virtual void Optimize()
        {
            OptimizeGetter();
            OptimizeSetter();
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
            t.Set("visibility", DynValue.NewString(PropertyInfo.GetClrVisibility()));
            t.Set("name", DynValue.NewString(Name));
            t.Set("static", DynValue.NewBoolean(IsStatic));
            t.Set("read", DynValue.NewBoolean(CanRead));
            t.Set("write", DynValue.NewBoolean(CanWrite));
            t.Set("decltype", DynValue.NewString(PropertyInfo.DeclaringType.FullName));
            t.Set(
                "declvtype",
                DynValue.NewBoolean(Framework.Do.IsValueType(PropertyInfo.DeclaringType))
            );
            t.Set("type", DynValue.NewString(PropertyInfo.PropertyType.FullName));
        }

        /// <summary>
        /// Helpers exposed for tests to override caching state.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Injects a fake optimized setter so tests can simulate compiled delegates.
            /// </summary>
            public static void SetOptimizedSetter(
                PropertyMemberDescriptor descriptor,
                Action<object, object> setter
            )
            {
                descriptor._optimizedSetter = setter;
            }
        }
    }
}

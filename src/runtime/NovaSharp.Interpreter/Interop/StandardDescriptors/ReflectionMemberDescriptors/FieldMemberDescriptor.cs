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
    /// Class providing easier marshalling of CLR fields
    /// </summary>
    public class FieldMemberDescriptor
        : IMemberDescriptor,
            IOptimizableDescriptor,
            IWireableDescriptor
    {
        /// <summary>
        /// Gets the FieldInfo got by reflection
        /// </summary>
        public FieldInfo FieldInfo { get; private set; }

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
        /// Gets a value indicating whether this instance is a constant
        /// </summary>
        public bool IsConst { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is readonly
        /// </summary>
        public bool IsReadonly { get; private set; }

        private readonly object _constValue = null;

        private Func<object, object> _optimizedGetter = null;

        /// <summary>
        /// Tries to create a new StandardUserDataFieldDescriptor, returning <c>null</c> in case the field is not
        /// visible to script code.
        /// </summary>
        /// <param name="fi">The FieldInfo.</param>
        /// <param name="accessMode">The <see cref="InteropAccessMode" /></param>
        /// <returns>A new StandardUserDataFieldDescriptor or null.</returns>
        public static FieldMemberDescriptor TryCreateIfVisible(
            FieldInfo fi,
            InteropAccessMode accessMode
        )
        {
            if (fi.GetVisibilityFromAttributes() ?? fi.IsPublic)
            {
                return new FieldMemberDescriptor(fi, accessMode);
            }

            return null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyMemberDescriptor"/> class.
        /// </summary>
        /// <param name="fi">The FieldInfo.</param>
        /// <param name="accessMode">The <see cref="InteropAccessMode" /> </param>
        public FieldMemberDescriptor(FieldInfo fi, InteropAccessMode accessMode)
        {
            if (Script.GlobalOptions.Platform.IsRunningOnAOT())
            {
                accessMode = InteropAccessMode.Reflection;
            }

            FieldInfo = fi;
            AccessMode = accessMode;
            Name = fi.Name;
            IsStatic = FieldInfo.IsStatic;

            if (FieldInfo.IsLiteral)
            {
                IsConst = true;
                _constValue = FieldInfo.GetValue(null);
            }
            else
            {
                IsReadonly = FieldInfo.IsInitOnly;
            }

            if (AccessMode == InteropAccessMode.Preoptimized)
            {
                OptimizeGetter();
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

            // optimization+workaround of Unity bug..
            if (IsConst)
            {
                return ClrToScriptConversions.ObjectToDynValue(script, _constValue);
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
                result = FieldInfo.GetValue(obj);
            }

            return ClrToScriptConversions.ObjectToDynValue(script, result);
        }

        internal void OptimizeGetter()
        {
            if (IsConst)
            {
                return;
            }

            using (
                PerformanceStatistics.StartGlobalStopwatch(PerformanceCounter.AdaptersCompilation)
            )
            {
                if (IsStatic)
                {
                    ParameterExpression paramExp = Expression.Parameter(typeof(object), "dummy");
                    MemberExpression propAccess = Expression.Field(null, FieldInfo);
                    UnaryExpression castPropAccess = Expression.Convert(propAccess, typeof(object));
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
                        FieldInfo.DeclaringType
                    );
                    MemberExpression propAccess = Expression.Field(castParamExp, FieldInfo);
                    UnaryExpression castPropAccess = Expression.Convert(propAccess, typeof(object));
                    Expression<Func<object, object>> lambda = Expression.Lambda<
                        Func<object, object>
                    >(castPropAccess, paramExp);
                    Interlocked.Exchange(ref _optimizedGetter, lambda.Compile());
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
            this.CheckAccess(MemberDescriptorAccess.CanWrite, obj);

            if (IsReadonly || IsConst)
            {
                throw new ScriptRuntimeException(
                    "userdata field '{0}.{1}' cannot be written to.",
                    FieldInfo.DeclaringType.Name,
                    Name
                );
            }

            object value = ScriptToClrConversions.DynValueToObjectOfType(
                v,
                FieldInfo.FieldType,
                null,
                false
            );

            try
            {
                if (value is double d)
                {
                    value = NumericConversions.DoubleToType(FieldInfo.FieldType, d);
                }

                FieldInfo.SetValue(IsStatic ? null : obj, value);
            }
            catch (ArgumentException)
            {
                // non-optimized setters fall here
                throw ScriptRuntimeException.UserDataArgumentTypeMismatch(
                    v.Type,
                    FieldInfo.FieldType
                );
            }
            catch (InvalidCastException)
            {
                // optimized setters fall here
                throw ScriptRuntimeException.UserDataArgumentTypeMismatch(
                    v.Type,
                    FieldInfo.FieldType
                );
            }
#if !(PCL || ENABLE_DOTNET || NETFX_CORE)
            catch (FieldAccessException ex)
            {
                throw new ScriptRuntimeException(ex);
            }
#endif
        }

        /// <summary>
        /// Gets the types of access supported by this member
        /// </summary>
        public MemberDescriptorAccess MemberAccess
        {
            get
            {
                if (IsReadonly || IsConst)
                {
                    return MemberDescriptorAccess.CanRead;
                }
                else
                {
                    return MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite;
                }
            }
        }

        void IOptimizableDescriptor.Optimize()
        {
            if (_optimizedGetter == null)
            {
                OptimizeGetter();
            }
        }

        /// <summary>
        /// Prepares the descriptor for hard-wiring.
        /// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
        /// </summary>
        /// <param name="t">The table to be filled</param>
        public void PrepareForWiring(Table t)
        {
            t.Set("class", DynValue.NewString(GetType().FullName));
            t.Set("visibility", DynValue.NewString(FieldInfo.GetClrVisibility()));

            t.Set("name", DynValue.NewString(Name));
            t.Set("static", DynValue.NewBoolean(IsStatic));
            t.Set("const", DynValue.NewBoolean(IsConst));
            t.Set("readonly", DynValue.NewBoolean(IsReadonly));
            t.Set("decltype", DynValue.NewString(FieldInfo.DeclaringType.FullName));
            t.Set(
                "declvtype",
                DynValue.NewBoolean(Framework.Do.IsValueType(FieldInfo.DeclaringType))
            );
            t.Set("type", DynValue.NewString(FieldInfo.FieldType.FullName));
            t.Set("read", DynValue.NewBoolean(true));
            t.Set("write", DynValue.NewBoolean(!(IsConst || IsReadonly)));
        }
    }
}

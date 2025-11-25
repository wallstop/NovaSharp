namespace NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.Converters;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Class providing easier marshalling of CLR functions
    /// </summary>
    public abstract class FunctionMemberDescriptorBase : IOverloadableMemberDescriptor
    {
        /// <summary>
        /// Gets a value indicating whether the described method is static.
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Gets the name of the described method
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets a sort discriminant to give consistent overload resolution matching in case of perfectly equal scores
        /// </summary>
        public string SortDiscriminant { get; private set; }

        private ParameterDescriptor[] _parameters = Array.Empty<ParameterDescriptor>();

        /// <summary>
        /// Gets the type of the arguments of the underlying CLR function
        /// </summary>
        public IReadOnlyList<ParameterDescriptor> Parameters => _parameters;

        protected ParameterDescriptor[] GetParameterArray()
        {
            return _parameters;
        }

        /// <summary>
        /// Gets the type which this extension method extends, null if this is not an extension method.
        /// </summary>
        public Type ExtensionMethodType { get; private set; }

        /// <summary>
        /// Gets a value indicating the type of the ParamArray parameter of a var-args function. If the function is not var-args,
        /// null is returned.
        /// </summary>
        public Type VarArgsArrayType { get; private set; }

        /// <summary>
        /// Gets a value indicating the type of the elements of the ParamArray parameter of a var-args function. If the function is not var-args,
        /// null is returned.
        /// </summary>
        public Type VarArgsElementType { get; private set; }

        /// <summary>
        /// Initializes this instance.
        /// This *MUST* be called by the constructors extending this class to complete initialization.
        /// </summary>
        /// <param name="funcName">Name of the function.</param>
        /// <param name="isStatic">if set to <c>true</c> [is static].</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="isExtensionMethod">if set to <c>true</c> [is extension method].</param>
        protected void Initialize(
            string funcName,
            bool isStatic,
            ParameterDescriptor[] parameters,
            bool isExtensionMethod
        )
        {
            Name = funcName;
            IsStatic = isStatic;
            _parameters = parameters ?? Array.Empty<ParameterDescriptor>();

            if (isExtensionMethod)
            {
                ExtensionMethodType = _parameters[0].Type;
            }

            if (_parameters.Length > 0 && _parameters[^1].IsVarArgs)
            {
                VarArgsArrayType = _parameters[^1].Type;
                VarArgsElementType = _parameters[^1].Type.GetElementType();
            }

            if (_parameters.Length == 0)
            {
                SortDiscriminant = string.Empty;
            }
            else
            {
                string[] discriminants = new string[_parameters.Length];
                for (int i = 0; i < _parameters.Length; i++)
                {
                    discriminants[i] = _parameters[i].Type.FullName ?? string.Empty;
                }

                SortDiscriminant = string.Join(":", discriminants);
            }
        }

        /// <summary>
        /// Gets a callback function as a delegate
        /// </summary>
        /// <param name="script">The script for which the callback must be generated.</param>
        /// <param name="obj">The object (null for static).</param>
        /// <returns></returns>
        public Func<ScriptExecutionContext, CallbackArguments, DynValue> GetCallback(
            Script script,
            object obj = null
        )
        {
            return (c, a) => Execute(script, obj, c, a);
        }

        /// <summary>
        /// Gets the callback function.
        /// </summary>
        /// <param name="script">The script for which the callback must be generated.</param>
        /// <param name="obj">The object (null for static).</param>
        /// <returns></returns>
        public CallbackFunction GetCallbackFunction(Script script, object obj = null)
        {
            return new CallbackFunction(GetCallback(script, obj), Name);
        }

        /// <summary>
        /// Gets the callback function as a DynValue.
        /// </summary>
        /// <param name="script">The script for which the callback must be generated.</param>
        /// <param name="obj">The object (null for static).</param>
        /// <returns></returns>
        public DynValue GetCallbackAsDynValue(Script script, object obj = null)
        {
            return DynValue.NewCallback(GetCallbackFunction(script, obj));
        }

        /// <summary>
        /// Creates a callback DynValue starting from a MethodInfo.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="mi">The mi.</param>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static DynValue CreateCallbackDynValue(
            Script script,
            MethodInfo mi,
            object obj = null
        )
        {
            MethodMemberDescriptor desc = new(mi);
            return desc.GetCallbackAsDynValue(script, obj);
        }

        /// <summary>
        /// Builds the argument list.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="context">The context.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="outParams">Output: A list containing the indices of all "out" parameters, or null if no out parameters are specified.</param>
        /// <returns>The arguments, appropriately converted.</returns>
        protected virtual object[] BuildArgumentList(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args,
            out IList<int> outParams
        )
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            ParameterDescriptor[] parameters = _parameters;

            object[] pars = new object[parameters.Length];

            int j = args.IsMethodCall ? 1 : 0;

            outParams = null;

            for (int i = 0; i < pars.Length; i++)
            {
                // keep track of out and ref params
                if (parameters[i].Type.IsByRef)
                {
                    if (outParams == null)
                    {
                        outParams = new List<int>();
                    }

                    outParams.Add(i);
                }

                // if an ext method, we have an obj -> fill the first param
                if (ExtensionMethodType != null && obj != null && i == 0)
                {
                    pars[i] = obj;
                    continue;
                }
                // else, fill types with a supported type
                else if (parameters[i].Type == typeof(Script))
                {
                    pars[i] = script;
                }
                else if (parameters[i].Type == typeof(ScriptExecutionContext))
                {
                    pars[i] = context;
                }
                else if (parameters[i].Type == typeof(CallbackArguments))
                {
                    pars[i] = args.SkipMethodCall();
                }
                // else, ignore out params
                else if (parameters[i].IsOut)
                {
                    pars[i] = null;
                }
                else if (i == parameters.Length - 1 && VarArgsArrayType != null)
                {
                    List<DynValue> extraArgs = new();

                    while (true)
                    {
                        DynValue arg = args.RawGet(j, false);
                        j += 1;
                        if (arg != null)
                        {
                            extraArgs.Add(arg);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // here we have to worry we already have an array.. damn. We only support this for userdata.
                    // remains to be analyzed what's the correct behavior here. For example, let's take a params object[]..
                    // given a single table parameter, should it use it as an array or as an object itself ?
                    if (extraArgs.Count == 1)
                    {
                        DynValue arg = extraArgs[0];

                        if (arg.Type == DataType.UserData && arg.UserData.Object != null)
                        {
                            if (
                                Framework.Do.IsAssignableFrom(
                                    VarArgsArrayType,
                                    arg.UserData.Object.GetType()
                                )
                            )
                            {
                                pars[i] = arg.UserData.Object;
                                continue;
                            }
                        }
                    }

                    // ok let's create an array, and loop
                    Array vararg = Array.CreateInstance(VarArgsElementType, extraArgs.Count);

                    for (int ii = 0; ii < extraArgs.Count; ii++)
                    {
                        vararg.SetValue(
                            ScriptToClrConversions.DynValueToObjectOfType(
                                extraArgs[ii],
                                VarArgsElementType,
                                null,
                                false
                            ),
                            ii
                        );
                    }

                    pars[i] = vararg;
                }
                // else, convert it
                else
                {
                    DynValue arg = args.RawGet(j, false) ?? DynValue.Void;
                    pars[i] = ScriptToClrConversions.DynValueToObjectOfType(
                        arg,
                        parameters[i].Type,
                        parameters[i].DefaultValue,
                        parameters[i].HasDefaultValue
                    );
                    j += 1;
                }
            }

            return pars;
        }

        /// <summary>
        /// Builds the return value of a call
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="outParams">The out parameters indices, or null. See <see cref="BuildArgumentList" />.</param>
        /// <param name="pars">The parameters passed to the function.</param>
        /// <param name="retv">The return value from the function. Use DynValue.Void if the function returned no value.</param>
        /// <returns>A DynValue to be returned to scripts</returns>
        protected static DynValue BuildReturnValue(
            Script script,
            IList<int> outParams,
            object[] pars,
            object retv
        )
        {
            if (pars == null)
            {
                throw new ArgumentNullException(nameof(pars));
            }

            if (outParams == null)
            {
                return ClrToScriptConversions.ObjectToDynValue(script, retv);
            }
            else
            {
                DynValue[] rets = new DynValue[outParams.Count + 1];

                if (retv is DynValue value && value.IsVoid())
                {
                    rets[0] = DynValue.Nil;
                }
                else
                {
                    rets[0] = ClrToScriptConversions.ObjectToDynValue(script, retv);
                }

                for (int i = 0; i < outParams.Count; i++)
                {
                    rets[i + 1] = ClrToScriptConversions.ObjectToDynValue(
                        script,
                        pars[outParams[i]]
                    );
                }

                return DynValue.NewTuple(rets);
            }
        }

        /// <summary>
        /// The internal callback which actually executes the method
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="context">The context.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        public abstract DynValue Execute(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args
        );

        /// <summary>
        /// Gets the types of access supported by this member
        /// </summary>
        public MemberDescriptorAccess MemberAccess
        {
            get { return MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanExecute; }
        }

        /// <summary>
        /// Gets the value of this member as a <see cref="DynValue" /> to be exposed to scripts.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object owning this member, or null if static.</param>
        /// <returns>
        /// The value of this member as a <see cref="DynValue" />.
        /// </returns>
        public virtual DynValue GetValue(Script script, object obj)
        {
            this.CheckAccess(MemberDescriptorAccess.CanRead, obj);
            return GetCallbackAsDynValue(script, obj);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="value">The value to assign.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void SetValue(Script script, object obj, DynValue value)
        {
            this.CheckAccess(MemberDescriptorAccess.CanWrite, obj);
        }
    }
}

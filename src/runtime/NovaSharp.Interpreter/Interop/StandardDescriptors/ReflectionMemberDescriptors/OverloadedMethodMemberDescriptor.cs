//#define DEBUG_OVERLOAD_RESOLVER

namespace NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.Converters;

    /// <summary>
    /// Class providing easier marshalling of overloaded CLR functions
    /// </summary>
    public class OverloadedMethodMemberDescriptor
        : IOptimizableDescriptor,
            IMemberDescriptor,
            IWireableDescriptor
    {
        /// <summary>
        /// Comparer class for IOverloadableMemberDescriptor
        /// </summary>
        private class OverloadableMemberDescriptorComparer
            : IComparer<IOverloadableMemberDescriptor>
        {
            public int Compare(IOverloadableMemberDescriptor x, IOverloadableMemberDescriptor y)
            {
                return x.SortDiscriminant.CompareTo(y.SortDiscriminant);
            }
        }

        private const int CACHE_SIZE = 5;

        private class OverloadCacheItem
        {
            public bool hasObject;
            public IOverloadableMemberDescriptor method;
            public List<DataType> argsDataType;
            public List<Type> argsUserDataType;
            public int hitIndexAtLastHit;
        }

        private readonly List<IOverloadableMemberDescriptor> _overloads = new();
        private List<IOverloadableMemberDescriptor> _extOverloads = new();
        private bool _unsorted = true;
        private OverloadCacheItem[] _cache = new OverloadCacheItem[CACHE_SIZE];
        private int _cacheHits;
        private int _extensionMethodVersion;

        /// <summary>
        /// Gets or sets a value indicating whether this instance ignores extension methods.
        /// </summary>
        public bool IgnoreExtensionMethods { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OverloadedMethodMemberDescriptor"/> class.
        /// </summary>
        public OverloadedMethodMemberDescriptor(string name, Type declaringType)
        {
            Name = name;
            DeclaringType = declaringType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OverloadedMethodMemberDescriptor" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="declaringType">The declaring type.</param>
        /// <param name="descriptor">The descriptor of the first overloaded method.</param>
        public OverloadedMethodMemberDescriptor(
            string name,
            Type declaringType,
            IOverloadableMemberDescriptor descriptor
        )
            : this(name, declaringType)
        {
            _overloads.Add(descriptor);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OverloadedMethodMemberDescriptor" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="declaringType">The declaring type.</param>
        /// <param name="descriptors">The descriptors of the overloaded methods.</param>
        public OverloadedMethodMemberDescriptor(
            string name,
            Type declaringType,
            IEnumerable<IOverloadableMemberDescriptor> descriptors
        )
            : this(name, declaringType)
        {
            _overloads.AddRange(descriptors);
        }

        /// <summary>
        /// Sets the extension methods snapshot.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="extMethods">The ext methods.</param>
        internal void SetExtensionMethodsSnapshot(
            int version,
            List<IOverloadableMemberDescriptor> extMethods
        )
        {
            _extOverloads = extMethods;
            _extensionMethodVersion = version;
        }

        /// <summary>
        /// Gets the name of the first described overload
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the name of the first described overload
        /// </summary>
        public Type DeclaringType { get; private set; }

        /// <summary>
        /// Adds an overload.
        /// </summary>
        /// <param name="overload">The overload.</param>
        public void AddOverload(IOverloadableMemberDescriptor overload)
        {
            _overloads.Add(overload);
            _unsorted = true;
        }

        /// <summary>
        /// Gets the number of overloaded methods contained in this collection
        /// </summary>
        /// <value>
        /// The overload count.
        /// </value>
        public int OverloadCount
        {
            get { return _overloads.Count; }
        }

        /// <summary>
        /// Performs the overloaded call.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="context">The context.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">function call doesn't match any overload</exception>
        private DynValue PerformOverloadedCall(
            Script script,
            object obj,
            ScriptExecutionContext context,
            CallbackArguments args
        )
        {
            bool extMethodCacheNotExpired =
                IgnoreExtensionMethods
                || (obj == null)
                || _extensionMethodVersion == UserData.GetExtensionMethodsChangeVersion();

            // common case, let's optimize for it
            if (_overloads.Count == 1 && _extOverloads.Count == 0 && extMethodCacheNotExpired)
            {
                return _overloads[0].Execute(script, obj, context, args);
            }

            if (_unsorted)
            {
                _overloads.Sort(new OverloadableMemberDescriptorComparer());
                _unsorted = false;
            }

            if (extMethodCacheNotExpired)
            {
                for (int i = 0; i < _cache.Length; i++)
                {
                    if (_cache[i] != null && CheckMatch(obj != null, args, _cache[i]))
                    {
#if DEBUG_OVERLOAD_RESOLVER
                        System.Diagnostics.Debug.WriteLine(
                            string.Format("[OVERLOAD] : CACHED! slot {0}, hits: {1}", i, _CacheHits)
                        );
#endif
                        return _cache[i].method.Execute(script, obj, context, args);
                    }
                }
            }

            // resolve on overloads first
            int maxScore = 0;
            IOverloadableMemberDescriptor bestOverload = null;

            for (int i = 0; i < _overloads.Count; i++)
            {
                if (obj != null || _overloads[i].IsStatic)
                {
                    int score = CalcScoreForOverload(context, args, _overloads[i], false);

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestOverload = _overloads[i];
                    }
                }
            }

            if (!IgnoreExtensionMethods && (obj != null))
            {
                if (!extMethodCacheNotExpired)
                {
                    _extensionMethodVersion = UserData.GetExtensionMethodsChangeVersion();
                    _extOverloads = UserData.GetExtensionMethodsByNameAndType(Name, DeclaringType);
                }

                for (int i = 0; i < _extOverloads.Count; i++)
                {
                    int score = CalcScoreForOverload(context, args, _extOverloads[i], true);

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestOverload = _extOverloads[i];
                    }
                }
            }

            if (bestOverload != null)
            {
                Cache(obj != null, args, bestOverload);
                return bestOverload.Execute(script, obj, context, args);
            }

            throw new ScriptRuntimeException("function call doesn't match any overload");
        }

        private void Cache(
            bool hasObject,
            CallbackArguments args,
            IOverloadableMemberDescriptor bestOverload
        )
        {
            int lowestHits = int.MaxValue;
            OverloadCacheItem found = null;
            for (int i = 0; i < _cache.Length; i++)
            {
                if (_cache[i] == null)
                {
                    found = new OverloadCacheItem()
                    {
                        argsDataType = new List<DataType>(),
                        argsUserDataType = new List<Type>(),
                    };
                    _cache[i] = found;
                    break;
                }
                else if (_cache[i].hitIndexAtLastHit < lowestHits)
                {
                    lowestHits = _cache[i].hitIndexAtLastHit;
                    found = _cache[i];
                }
            }

            if (found == null)
            {
                // overflow..
                _cache = new OverloadCacheItem[CACHE_SIZE];
                found = new OverloadCacheItem()
                {
                    argsDataType = new List<DataType>(),
                    argsUserDataType = new List<Type>(),
                };
                _cache[0] = found;
                _cacheHits = 0;
            }

            found.method = bestOverload;
            found.hitIndexAtLastHit = ++_cacheHits;
            found.argsDataType.Clear();
            found.hasObject = hasObject;

            for (int i = 0; i < args.Count; i++)
            {
                found.argsDataType.Add(args[i].Type);

                if (args[i].Type == DataType.UserData)
                {
                    found.argsUserDataType.Add(args[i].UserData.Descriptor.Type);
                }
                else
                {
                    found.argsUserDataType.Add(null);
                }
            }
        }

        private bool CheckMatch(
            bool hasObject,
            CallbackArguments args,
            OverloadCacheItem overloadCacheItem
        )
        {
            if (overloadCacheItem.hasObject && !hasObject)
            {
                return false;
            }

            if (args.Count != overloadCacheItem.argsDataType.Count)
            {
                return false;
            }

            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].Type != overloadCacheItem.argsDataType[i])
                {
                    return false;
                }

                if (args[i].Type == DataType.UserData)
                {
                    if (args[i].UserData.Descriptor.Type != overloadCacheItem.argsUserDataType[i])
                    {
                        return false;
                    }
                }
            }

            overloadCacheItem.hitIndexAtLastHit = ++_cacheHits;
            return true;
        }

        /// <summary>
        /// Calculates the score for the overload.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="method">The method.</param>
        /// <param name="isExtMethod">if set to <c>true</c>, is an extension method.</param>
        /// <returns></returns>
        private int CalcScoreForOverload(
            ScriptExecutionContext context,
            CallbackArguments args,
            IOverloadableMemberDescriptor method,
            bool isExtMethod
        )
        {
            int totalScore = ScriptToClrConversions.WEIGHT_EXACT_MATCH;
            int argsBase = args.IsMethodCall ? 1 : 0;
            int argsCnt = argsBase;
            bool varArgsUsed = false;

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (isExtMethod && i == 0)
                {
                    continue;
                }

                if (method.Parameters[i].IsOut)
                {
                    continue;
                }

                Type parameterType = method.Parameters[i].Type;

                if (
                    (parameterType == typeof(Script))
                    || (parameterType == typeof(ScriptExecutionContext))
                    || (parameterType == typeof(CallbackArguments))
                )
                {
                    continue;
                }

                if (i == method.Parameters.Count - 1 && method.VarArgsArrayType != null)
                {
                    int varargCnt = 0;
                    DynValue firstArg = null;
                    int scoreBeforeVargars = totalScore;

                    // update score for varargs
                    while (true)
                    {
                        DynValue arg = args.RawGet(argsCnt, false);
                        if (arg == null)
                        {
                            break;
                        }

                        if (firstArg == null)
                        {
                            firstArg = arg;
                        }

                        argsCnt += 1;

                        varargCnt += 1;

                        int score = CalcScoreForSingleArgument(
                            method.Parameters[i],
                            method.VarArgsElementType,
                            arg,
                            isOptional: false
                        );
                        totalScore = Math.Min(totalScore, score);
                    }

                    // check if exact-match
                    if (varargCnt == 1)
                    {
                        if (firstArg.Type == DataType.UserData && firstArg.UserData.Object != null)
                        {
                            if (
                                Framework.Do.IsAssignableFrom(
                                    method.VarArgsArrayType,
                                    firstArg.UserData.Object.GetType()
                                )
                            )
                            {
                                totalScore = scoreBeforeVargars;
                                continue;
                            }
                        }
                    }

                    // apply varargs penalty to score
                    if (varargCnt == 0)
                    {
                        totalScore = Math.Min(
                            totalScore,
                            ScriptToClrConversions.WEIGHT_VARARGS_EMPTY
                        );
                    }

                    varArgsUsed = true;
                }
                else
                {
                    DynValue arg = args.RawGet(argsCnt, false) ?? DynValue.Void;

                    int score = CalcScoreForSingleArgument(
                        method.Parameters[i],
                        parameterType,
                        arg,
                        method.Parameters[i].HasDefaultValue
                    );

                    totalScore = Math.Min(totalScore, score);

                    argsCnt += 1;
                }
            }

            if (totalScore > 0)
            {
                if ((args.Count - argsBase) <= method.Parameters.Count)
                {
                    totalScore += ScriptToClrConversions.WEIGHT_NO_EXTRA_PARAMS_BONUS;
                    totalScore *= 1000;
                }
                else if (varArgsUsed)
                {
                    totalScore -= ScriptToClrConversions.WEIGHT_VARARGS_MALUS;
                    totalScore *= 1000;
                }
                else
                {
                    totalScore *= 1000;
                    totalScore -=
                        ScriptToClrConversions.WEIGHT_EXTRA_PARAMS_MALUS
                        * ((args.Count - argsBase) - method.Parameters.Count);
                    totalScore = Math.Max(1, totalScore);
                }
            }

#if DEBUG_OVERLOAD_RESOLVER
            System.Diagnostics.Debug.WriteLine(
                string.Format(
                    "[OVERLOAD] : Score {0} for method {1}",
                    totalScore,
                    method.SortDiscriminant
                )
            );
#endif
            return totalScore;
        }

        private static int CalcScoreForSingleArgument(
            ParameterDescriptor desc,
            Type parameterType,
            DynValue arg,
            bool isOptional
        )
        {
            int score = ScriptToClrConversions.DynValueToObjectOfTypeWeight(
                arg,
                parameterType,
                isOptional
            );

            if (parameterType.IsByRef || desc.IsOut || desc.IsRef)
            {
                score = Math.Max(0, score + ScriptToClrConversions.WEIGHT_BYREF_BONUSMALUS);
            }

            return score;
        }

        /// <summary>
        /// Gets a callback function as a delegate
        /// </summary>
        /// <param name="script">The script for which the callback must be generated.</param>
        /// <param name="obj">The object (null for static).</param>
        /// <returns></returns>
        public Func<ScriptExecutionContext, CallbackArguments, DynValue> GetCallback(
            Script script,
            object obj
        )
        {
            return (context, args) => PerformOverloadedCall(script, obj, context, args);
        }

        void IOptimizableDescriptor.Optimize()
        {
            foreach (IOptimizableDescriptor d in _overloads.OfType<IOptimizableDescriptor>())
            {
                d.Optimize();
            }
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
        /// Gets a value indicating whether there is at least one static method in the resolution list
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool IsStatic
        {
            get { return _overloads.Any(o => o.IsStatic); }
        }

        /// <summary>
        /// Gets the types of access supported by this member
        /// </summary>
        public MemberDescriptorAccess MemberAccess
        {
            get { return MemberDescriptorAccess.CanExecute | MemberDescriptorAccess.CanRead; }
        }

        /// <summary>
        /// Gets the value of this member as a <see cref="DynValue" /> to be exposed to scripts.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object owning this member, or null if static.</param>
        /// <returns>
        /// The value of this member as a <see cref="DynValue" />.
        /// </returns>
        public DynValue GetValue(Script script, object obj)
        {
            return DynValue.NewCallback(GetCallbackFunction(script, obj));
        }

        /// <summary>
        /// Sets the value of this member from a <see cref="DynValue" />.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object owning this member, or null if static.</param>
        /// <param name="value">The value to be set.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void SetValue(Script script, object obj, DynValue value)
        {
            this.CheckAccess(MemberDescriptorAccess.CanWrite, obj);
        }

        /// <summary>
        /// Prepares the descriptor for hard-wiring.
        /// The descriptor fills the passed table with all the needed data for hardwire generators to generate the appropriate code.
        /// </summary>
        /// <param name="t">The table to be filled</param>
        public void PrepareForWiring(Table t)
        {
            t.Set("class", DynValue.NewString(GetType().FullName));
            t.Set("name", DynValue.NewString(Name));
            t.Set("decltype", DynValue.NewString(DeclaringType.FullName));
            DynValue mst = DynValue.NewPrimeTable();
            t.Set("overloads", mst);

            int i = 0;

            foreach (IOverloadableMemberDescriptor m in _overloads)
            {
                if (m is IWireableDescriptor sd)
                {
                    DynValue mt = DynValue.NewPrimeTable();
                    mst.Table.Set(++i, mt);
                    sd.PrepareForWiring(mt.Table);
                }
                else
                {
                    mst.Table.Set(
                        ++i,
                        DynValue.NewString(
                            $"unsupported - {m.GetType().FullName} is not serializable"
                        )
                    );
                }
            }
        }
    }
}

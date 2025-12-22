//#define DEBUG_OVERLOAD_RESOLVER

namespace WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors
{
    using System;
    using System.Collections.Generic;
    using BasicDescriptors;
    using Compatibility;
    using Converters;
    using DataTypes;
    using Errors;
    using Execution;

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
        /// <summary>
        /// Comparer that sorts overload descriptors deterministically by their discriminant.
        /// </summary>
        private sealed class OverloadableMemberDescriptorComparer
            : IComparer<IOverloadableMemberDescriptor>
        {
            public static readonly OverloadableMemberDescriptorComparer Instance = new();

            private OverloadableMemberDescriptorComparer() { }

            /// <summary>
            /// Orders overloadable descriptors by their <see cref="IOverloadableMemberDescriptor.SortDiscriminant"/>.
            /// </summary>
            public int Compare(IOverloadableMemberDescriptor x, IOverloadableMemberDescriptor y)
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                return string.Compare(
                    x.SortDiscriminant,
                    y.SortDiscriminant,
                    StringComparison.Ordinal
                );
            }
        }

        private const int CacheSize = 5;

        private class OverloadCacheItem
        {
            public bool hasObject;
            public IOverloadableMemberDescriptor method;
            public List<DataType> argumentDataTypes;
            public List<Type> argumentUserDataTypes;
            public int hitIndexAtLastHit;
        }

        /// <summary>
        /// Lightweight structure for last-call caching optimization.
        /// Stored as a struct to avoid extra heap allocation; copied atomically by assignment.
        /// This enables a very fast check before the full cache lookup.
        /// </summary>
        private struct LastCallCacheEntry
        {
            public IOverloadableMemberDescriptor method;
            public int argCount;
            public bool hasObject;
            public DataType arg0Type;
            public DataType arg1Type;
            public DataType arg2Type;
            public Type arg0UserDataType;
            public Type arg1UserDataType;
            public Type arg2UserDataType;
        }

        private readonly List<IOverloadableMemberDescriptor> _overloads = new();
        private IReadOnlyList<IOverloadableMemberDescriptor> _extOverloads =
            Array.Empty<IOverloadableMemberDescriptor>();
        private bool _unsorted = true;
        private OverloadCacheItem[] _cache = new OverloadCacheItem[CacheSize];
        private int _cacheHits;
        private int _extensionMethodVersion;

        // Last-call cache: stores the most recently resolved method for fast repeated calls.
        // This is a per-instance field (not static) for thread safety.
        private LastCallCacheEntry _lastCall;

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
            IReadOnlyList<IOverloadableMemberDescriptor> extMethods
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
                _overloads.Sort(OverloadableMemberDescriptorComparer.Instance);
                _unsorted = false;
            }

            if (extMethodCacheNotExpired)
            {
                // OPTIMIZATION: Last-call fast path - check if this call matches the most recent one.
                // This avoids iterating through the full cache array for tight loops that call
                // the same method repeatedly with the same argument signature.
                if (CheckLastCallMatch(obj != null, args))
                {
#if DEBUG_OVERLOAD_RESOLVER
                    System.Diagnostics.Debug.WriteLine("[OVERLOAD] : LAST-CALL HIT!");
#endif
                    return _lastCall.method.Execute(script, obj, context, args);
                }

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
            // Update last-call cache for fast repeated calls with same signature
            UpdateLastCallCache(hasObject, args, bestOverload);

            int lowestHits = int.MaxValue;
            OverloadCacheItem found = null;
            for (int i = 0; i < _cache.Length; i++)
            {
                if (_cache[i] == null)
                {
                    found = new OverloadCacheItem()
                    {
                        argumentDataTypes = new List<DataType>(),
                        argumentUserDataTypes = new List<Type>(),
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
                _cache = new OverloadCacheItem[CacheSize];
                found = new OverloadCacheItem()
                {
                    argumentDataTypes = new List<DataType>(),
                    argumentUserDataTypes = new List<Type>(),
                };
                _cache[0] = found;
                _cacheHits = 0;
            }

            found.method = bestOverload;
            found.hitIndexAtLastHit = ++_cacheHits;
            found.argumentDataTypes.Clear();
            found.argumentUserDataTypes.Clear();
            found.hasObject = hasObject;

            for (int i = 0; i < args.Count; i++)
            {
                found.argumentDataTypes.Add(args[i].Type);

                if (args[i].Type == DataType.UserData)
                {
                    found.argumentUserDataTypes.Add(args[i].UserData.Descriptor.Type);
                }
                else
                {
                    found.argumentUserDataTypes.Add(null);
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

            if (args.Count != overloadCacheItem.argumentDataTypes.Count)
            {
                return false;
            }

            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].Type != overloadCacheItem.argumentDataTypes[i])
                {
                    return false;
                }

                if (args[i].Type == DataType.UserData)
                {
                    if (
                        args[i].UserData.Descriptor.Type
                        != overloadCacheItem.argumentUserDataTypes[i]
                    )
                    {
                        return false;
                    }
                }
                else if (overloadCacheItem.argumentUserDataTypes[i] != null)
                {
                    return false;
                }
            }

            overloadCacheItem.hitIndexAtLastHit = ++_cacheHits;
            return true;
        }

        /// <summary>
        /// Fast check for last-call cache hit. This method is optimized for the common case
        /// where the same method is called repeatedly with the same argument signature
        /// (e.g., in a tight loop). It avoids iterating through the full cache array.
        /// </summary>
        /// <remarks>
        /// The check handles up to 3 arguments inline (common case) without allocations.
        /// For calls with more arguments, falls through to the full cache lookup.
        /// </remarks>
        private bool CheckLastCallMatch(bool hasObject, CallbackArguments args)
        {
            // No cached method yet
            if (_lastCall.method == null)
            {
                return false;
            }

            // Object state mismatch (instance vs static call)
            if (_lastCall.hasObject && !hasObject)
            {
                return false;
            }

            // Argument count mismatch
            if (args.Count != _lastCall.argCount)
            {
                return false;
            }

            // For 0-3 arguments, do inline comparison (most common cases)
            // This avoids any loop overhead for typical method calls
            int argCount = args.Count;

            if (argCount > 3)
            {
                // Fall through to full cache for 4+ arguments
                return false;
            }

            // Check argument 0
            if (argCount >= 1)
            {
                if (args[0].Type != _lastCall.arg0Type)
                {
                    return false;
                }

                if (args[0].Type == DataType.UserData)
                {
                    if (args[0].UserData.Descriptor.Type != _lastCall.arg0UserDataType)
                    {
                        return false;
                    }
                }
                else if (_lastCall.arg0UserDataType != null)
                {
                    return false;
                }
            }

            // Check argument 1
            if (argCount >= 2)
            {
                if (args[1].Type != _lastCall.arg1Type)
                {
                    return false;
                }

                if (args[1].Type == DataType.UserData)
                {
                    if (args[1].UserData.Descriptor.Type != _lastCall.arg1UserDataType)
                    {
                        return false;
                    }
                }
                else if (_lastCall.arg1UserDataType != null)
                {
                    return false;
                }
            }

            // Check argument 2
            if (argCount >= 3)
            {
                if (args[2].Type != _lastCall.arg2Type)
                {
                    return false;
                }

                if (args[2].Type == DataType.UserData)
                {
                    if (args[2].UserData.Descriptor.Type != _lastCall.arg2UserDataType)
                    {
                        return false;
                    }
                }
                else if (_lastCall.arg2UserDataType != null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the last-call cache entry with the current call's signature.
        /// </summary>
        private void UpdateLastCallCache(
            bool hasObject,
            CallbackArguments args,
            IOverloadableMemberDescriptor method
        )
        {
            _lastCall.method = method;
            _lastCall.hasObject = hasObject;
            _lastCall.argCount = args.Count;

            // Clear UserData types first
            _lastCall.arg0UserDataType = null;
            _lastCall.arg1UserDataType = null;
            _lastCall.arg2UserDataType = null;

            // Store up to 3 arguments inline
            if (args.Count >= 1)
            {
                _lastCall.arg0Type = args[0].Type;
                if (args[0].Type == DataType.UserData)
                {
                    _lastCall.arg0UserDataType = args[0].UserData.Descriptor.Type;
                }
            }

            if (args.Count >= 2)
            {
                _lastCall.arg1Type = args[1].Type;
                if (args[1].Type == DataType.UserData)
                {
                    _lastCall.arg1UserDataType = args[1].UserData.Descriptor.Type;
                }
            }

            if (args.Count >= 3)
            {
                _lastCall.arg2Type = args[2].Type;
                if (args[2].Type == DataType.UserData)
                {
                    _lastCall.arg2UserDataType = args[2].UserData.Descriptor.Type;
                }
            }
        }

        /// <summary>
        /// Calculates the score for the overload.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="method">The method.</param>
        /// <param name="isExtMethod">if set to <c>true</c>, is an extension method.</param>
        /// <returns></returns>
        private static int CalcScoreForOverload(
            ScriptExecutionContext context,
            CallbackArguments args,
            IOverloadableMemberDescriptor method,
            bool isExtMethod
        )
        {
            int totalScore = ScriptToClrConversions.WeightExactMatch;
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
                    int varArgCount = 0;
                    DynValue firstArg = null;
                    int scoreBeforeVarArgs = totalScore;

                    // update score for varargs
                    while (true)
                    {
                        DynValue arg = args.RawGet(argsCnt, false);
                        if (arg == null)
                        {
                            break;
                        }

                        firstArg ??= arg;

                        argsCnt += 1;

                        varArgCount += 1;

                        int score = CalcScoreForSingleArgument(
                            method.Parameters[i],
                            method.VarArgsElementType,
                            arg,
                            isOptional: false
                        );
                        totalScore = Math.Min(totalScore, score);
                    }

                    // check if exact-match
                    if (varArgCount == 1 && firstArg != null)
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
                                totalScore = scoreBeforeVarArgs;
                                continue;
                            }
                        }
                    }

                    // apply varargs penalty to score
                    if (varArgCount == 0)
                    {
                        totalScore = Math.Min(
                            totalScore,
                            ScriptToClrConversions.WeightVarArgsEmpty
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
                    totalScore += ScriptToClrConversions.WeightNoExtraParamsBonus;
                    totalScore *= 1000;
                }
                else if (varArgsUsed)
                {
                    totalScore -= ScriptToClrConversions.WeightVarArgsMalus;
                    totalScore *= 1000;
                }
                else
                {
                    totalScore *= 1000;
                    totalScore -=
                        ScriptToClrConversions.WeightExtraParamsMalus
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
                score = Math.Max(0, score + ScriptToClrConversions.WeightByRefBonusMalus);
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

        /// <summary>
        /// Optimizes each contained overload, allowing reflection-heavy descriptors to compile delegates up front.
        /// </summary>
        public void Optimize()
        {
            foreach (IOverloadableMemberDescriptor overload in _overloads)
            {
                if (overload is IOptimizableDescriptor descriptor)
                {
                    descriptor.Optimize();
                }
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
            get { return _overloads.Exists(o => o.IsStatic); }
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
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

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

        /// <summary>
        /// Helpers surfaced for tests so they can inspect cache behavior and scoring.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Calls the private overload-scoring routine using the supplied descriptor/method.
            /// </summary>
            public static int CalcScoreForOverload(
                OverloadedMethodMemberDescriptor descriptor,
                ScriptExecutionContext context,
                CallbackArguments args,
                IOverloadableMemberDescriptor method,
                bool isExtensionMethod
            )
            {
                return OverloadedMethodMemberDescriptor.CalcScoreForOverload(
                    context,
                    args,
                    method,
                    isExtensionMethod
                );
            }

            /// <summary>
            /// Resizes the internal overload cache so tests can simulate cache thrash scenarios.
            /// </summary>
            public static void SetCacheSize(OverloadedMethodMemberDescriptor descriptor, int size)
            {
                if (size < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(size));
                }

                descriptor._cache = new OverloadCacheItem[size];
                descriptor._cacheHits = 0;
            }

            /// <summary>
            /// Clears the last-call cache for testing purposes.
            /// </summary>
            public static void ClearLastCallCache(OverloadedMethodMemberDescriptor descriptor)
            {
                descriptor._lastCall = default;
            }

            /// <summary>
            /// Checks if the last-call cache has a cached method.
            /// </summary>
            public static bool HasLastCallCached(OverloadedMethodMemberDescriptor descriptor)
            {
                return descriptor._lastCall.method != null;
            }

            /// <summary>
            /// Gets the argument count stored in the last-call cache.
            /// </summary>
            public static int GetLastCallArgCount(OverloadedMethodMemberDescriptor descriptor)
            {
                return descriptor._lastCall.argCount;
            }
        }
    }
}

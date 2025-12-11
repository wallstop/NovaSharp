namespace WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Converters;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Options;

    /// <summary>
    /// An abstract user data descriptor which accepts members described by <see cref="IMemberDescriptor"/> objects and
    /// correctly dispatches to them.
    /// Metamethods are also by default dispatched to operator overloads and other similar methods - see
    /// <see cref="MetaIndex"/> .
    /// </summary>
    public abstract class DispatchingUserDataDescriptor
        : IUserDataDescriptor,
            IOptimizableDescriptor
    {
        private int _extMethodsVersion;
        private readonly Dictionary<string, IMemberDescriptor> _metaMembers = new(
            StringComparer.OrdinalIgnoreCase
        );
        private readonly Dictionary<string, IMemberDescriptor> _members = new(
            StringComparer.OrdinalIgnoreCase
        );

        /// <summary>
        /// The special name used by CLR for indexer getters
        /// </summary>
        protected const string SpecialNameIndexerGet = "get_Item";

        /// <summary>
        /// The special name used by CLR for indexer setters
        /// </summary>
        protected const string SpecialNameIndexerSet = "set_Item";

        /// <summary>
        /// The special name used by CLR for explicit cast conversions
        /// </summary>
        protected const string SpecialNameCastExplicit = "op_Explicit";

        /// <summary>
        /// The special name used by CLR for implicit cast conversions
        /// </summary>
        protected const string SpecialNameCastImplicit = "op_Implicit";

        /// <summary>
        /// Gets the name of the descriptor (usually, the name of the type described).
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the type this descriptor refers to
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets a human readable friendly name of the descriptor
        /// </summary>
        public string FriendlyName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardUserDataDescriptor" /> class.
        /// </summary>
        /// <param name="type">The type this descriptor refers to.</param>
        /// <param name="friendlyName">A friendly name for the type, or null.</param>
        protected DispatchingUserDataDescriptor(Type type, string friendlyName = null)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            Name = type.FullName;
            FriendlyName = friendlyName ?? type.Name;
        }

        /// <summary>
        /// Adds a member to the meta-members list.
        /// </summary>
        /// <param name="name">The name of the metamethod.</param>
        /// <param name="desc">The desc.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if a name conflict is detected and one of the conflicting members does not support overloads.
        /// </exception>
        public void AddMetaMember(string name, IMemberDescriptor desc)
        {
            if (desc != null)
            {
                AddMemberTo(_metaMembers, name, desc);
            }
        }

        /// <summary>
        /// Adds a DynValue as a member
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddDynValue(string name, DynValue value)
        {
            DynValueMemberDescriptor desc = new(name, value);
            AddMemberTo(_members, name, desc);
        }

        /// <summary>
        /// Adds a property to the member list
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="desc">The descriptor.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if a name conflict is detected and one of the conflicting members does not support overloads.
        /// </exception>
        public void AddMember(string name, IMemberDescriptor desc)
        {
            if (desc != null)
            {
                AddMemberTo(_members, name, desc);
            }
        }

        /// <summary>
        /// Gets the member names.
        /// </summary>
        public IEnumerable<string> MemberNames
        {
            get { return _members.Keys; }
        }

        /// <summary>
        /// Gets the members.
        /// </summary>
        public IEnumerable<KeyValuePair<string, IMemberDescriptor>> Members
        {
            get { return _members; }
        }

        /// <summary>
        /// Finds the member with a given name. If not found, null is returned.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <returns></returns>
        public IMemberDescriptor FindMember(string memberName)
        {
            return _members.GetOrDefault(memberName);
        }

        /// <summary>
        /// Removes the member with a given name. In case of overloaded functions, all overloads are removed.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        public void RemoveMember(string memberName)
        {
            _members.Remove(memberName);
        }

        /// <summary>
        /// Gets the meta member names.
        /// </summary>
        public IEnumerable<string> MetaMemberNames
        {
            get { return _metaMembers.Keys; }
        }

        /// <summary>
        /// Gets the meta members.
        /// </summary>
        public IEnumerable<KeyValuePair<string, IMemberDescriptor>> MetaMembers
        {
            get { return _metaMembers; }
        }

        /// <summary>
        /// Finds the meta member with a given name. If not found, null is returned.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        public IMemberDescriptor FindMetaMember(string memberName)
        {
            return _metaMembers.GetOrDefault(memberName);
        }

        /// <summary>
        /// Removes the meta member with a given name. In case of overloaded functions, all overloads are removed.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        public void RemoveMetaMember(string memberName)
        {
            _metaMembers.Remove(memberName);
        }

        private void AddMemberTo(
            Dictionary<string, IMemberDescriptor> members,
            string name,
            IMemberDescriptor desc
        )
        {
            if (desc is IOverloadableMemberDescriptor overloadable)
            {
                if (members.TryGetValue(name, out IMemberDescriptor existing))
                {
                    if (existing is OverloadedMethodMemberDescriptor overloads)
                    {
                        overloads.AddOverload(overloadable);
                        return;
                    }

                    ThrowMemberConflict(name);
                }

                members.Add(name, new OverloadedMethodMemberDescriptor(name, Type, overloadable));
                return;
            }

            if (!members.TryAdd(name, desc))
            {
                ThrowMemberConflict(name);
            }
        }

        private void ThrowMemberConflict(string name)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Multiple members named {0} are being added to type {1} and one or more of these members do not support overloads.",
                    name,
                    Type.FullName
                )
            );
        }

        /// <summary>
        /// Performs an "index" "get" operation. This tries to resolve minor variations of member names.
        /// </summary>
        /// <param name="script">The script originating the request</param>
        /// <param name="obj">The object (null if a static request is done)</param>
        /// <param name="index">The index.</param>
        /// <param name="isDirectIndexing">If set to true, it's indexed with a name, if false it's indexed through brackets.</param>
        /// <returns></returns>
        public virtual DynValue Index(
            Script script,
            object obj,
            DynValue index,
            bool isDirectIndexing
        )
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (!isDirectIndexing)
            {
                IMemberDescriptor mdesc = _members
                    .GetOrDefault(SpecialNameIndexerGet)
                    .WithAccessOrNull(MemberDescriptorAccess.CanExecute);

                if (mdesc != null)
                {
                    return ExecuteIndexer(mdesc, script, obj, index, null);
                }
            }

            index = index.ToScalar();

            if (index.Type != DataType.String)
            {
                return null;
            }

            List<string> candidates = BuildMemberNameCandidates(
                index.String,
                Script.GlobalOptions.FuzzySymbolMatching
            );

            DynValue v = null;

            foreach (string candidate in candidates)
            {
                v = TryIndex(script, obj, candidate);
                if (v != null)
                {
                    break;
                }
            }

            if (v == null && _extMethodsVersion < UserData.GetExtensionMethodsChangeVersion())
            {
                _extMethodsVersion = UserData.GetExtensionMethodsChangeVersion();

                foreach (string candidate in candidates)
                {
                    v = TryIndexOnExtMethod(script, obj, candidate);
                    if (v != null)
                    {
                        break;
                    }
                }
            }

            return v;
        }

        /// <summary>
        /// Tries to perform an indexing operation by checking newly added extension methods for the given indexName.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="indexName">Member name to be indexed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private DynValue TryIndexOnExtMethod(Script script, object obj, string indexName)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (indexName == null)
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            IReadOnlyList<IOverloadableMemberDescriptor> methods =
                UserData.GetExtensionMethodsByNameAndType(indexName, Type);

            if (methods != null && methods.Count > 0)
            {
                OverloadedMethodMemberDescriptor ext = new(indexName, Type);
                ext.SetExtensionMethodsSnapshot(
                    UserData.GetExtensionMethodsChangeVersion(),
                    methods
                );
                _members.Add(indexName, ext);
                return DynValue.NewCallback(ext.GetCallback(script, obj));
            }

            return null;
        }

        /// <summary>
        /// Determines whether the descriptor contains the specified member (by exact name)
        /// </summary>
        /// <param name="exactName">Name of the member.</param>
        /// <returns></returns>
        public bool HasMember(string exactName)
        {
            return _members.ContainsKey(exactName);
        }

        /// <summary>
        /// Determines whether the descriptor contains the specified member in the meta list (by exact name)
        /// </summary>
        /// <param name="exactName">Name of the meta-member.</param>
        /// <returns></returns>
        public bool HasMetaMember(string exactName)
        {
            return _metaMembers.ContainsKey(exactName);
        }

        /// <summary>
        /// Tries to perform an indexing operation by checking methods and properties for the given indexName
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="indexName">Member name to be indexed.</param>
        /// <returns></returns>
        protected virtual DynValue TryIndex(Script script, object obj, string indexName)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (_members.TryGetValue(indexName, out IMemberDescriptor desc))
            {
                return desc.GetValue(script, obj);
            }

            return null;
        }

        /// <summary>
        /// Performs an "index" "set" operation. This tries to resolve minor variations of member names.
        /// </summary>
        /// <param name="script">The script originating the request</param>
        /// <param name="obj">The object (null if a static request is done)</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value to be set</param>
        /// <param name="isDirectIndexing">If set to true, it's indexed with a name, if false it's indexed through brackets.</param>
        /// <returns></returns>
        public virtual bool SetIndex(
            Script script,
            object obj,
            DynValue index,
            DynValue value,
            bool isDirectIndexing
        )
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!isDirectIndexing)
            {
                IMemberDescriptor mdesc = _members
                    .GetOrDefault(SpecialNameIndexerSet)
                    .WithAccessOrNull(MemberDescriptorAccess.CanExecute);

                if (mdesc != null)
                {
                    ExecuteIndexer(mdesc, script, obj, index, value);
                    return true;
                }
            }

            index = index.ToScalar();

            if (index.Type != DataType.String)
            {
                return false;
            }

            List<string> candidates = BuildMemberNameCandidates(
                index.String,
                Script.GlobalOptions.FuzzySymbolMatching
            );

            foreach (string candidate in candidates)
            {
                if (TrySetIndex(script, obj, candidate, value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to perform an indexing "set" operation by checking methods and properties for the given indexName
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="indexName">Member name to be indexed.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected virtual bool TrySetIndex(
            Script script,
            object obj,
            string indexName,
            DynValue value
        )
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            IMemberDescriptor descr = _members.GetOrDefault(indexName);

            if (descr != null)
            {
                descr.SetValue(script, obj, value);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Propagates optimization hints to all nested member descriptors (meta + regular) so they can cache reflection state.
        /// </summary>
        public virtual void Optimize()
        {
            foreach (IMemberDescriptor descriptor in _metaMembers.Values)
            {
                if (descriptor is IOptimizableDescriptor optimizable)
                {
                    optimizable.Optimize();
                }
            }

            foreach (IMemberDescriptor descriptor in _members.Values)
            {
                if (descriptor is IOptimizableDescriptor optimizable)
                {
                    optimizable.Optimize();
                }
            }
        }

        private static List<string> BuildMemberNameCandidates(
            string name,
            FuzzySymbolMatchingBehavior behavior
        )
        {
            List<string> results = new();
            HashSet<string> seen = new(StringComparer.Ordinal);

            void Add(string candidate)
            {
                if (!string.IsNullOrEmpty(candidate) && seen.Add(candidate))
                {
                    results.Add(candidate);
                }
            }

            Add(name);

            string camel = DescriptorHelpers.Camelify(name);
            string upperFirst = DescriptorHelpers.UpperFirstLetter(name);
            string pascal = DescriptorHelpers.UpperFirstLetter(camel);
            string normalizedUpper = DescriptorHelpers.NormalizeUppercaseRuns(upperFirst);
            string normalizedPascal = DescriptorHelpers.NormalizeUppercaseRuns(pascal);
            string snake = DescriptorHelpers.ToUpperUnderscore(name);

            if (
                (behavior & FuzzySymbolMatchingBehavior.UpperFirstLetter)
                == FuzzySymbolMatchingBehavior.UpperFirstLetter
            )
            {
                Add(upperFirst);
                Add(normalizedUpper);
            }

            if (
                (behavior & FuzzySymbolMatchingBehavior.Camelify)
                == FuzzySymbolMatchingBehavior.Camelify
            )
            {
                Add(camel);
            }

            if (
                (behavior & FuzzySymbolMatchingBehavior.PascalCase)
                == FuzzySymbolMatchingBehavior.PascalCase
            )
            {
                Add(pascal);
                Add(normalizedPascal);
            }

            Add(snake);

            if (!name.StartsWith("On", StringComparison.OrdinalIgnoreCase))
            {
                Add("On" + upperFirst);
                Add("On" + pascal);
                Add("On" + normalizedUpper);
                Add("On" + normalizedPascal);
            }

            return results;
        }

        /// <summary>
        /// Converts the specified name from underscore_case to camelCase.
        /// Just a wrapper over the <see cref="DescriptorHelpers"/> method with the same name,
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        protected static string Camelify(string name)
        {
            return DescriptorHelpers.Camelify(name);
        }

        /// <summary>
        /// Converts the specified name to one with an uppercase first letter (something to Something).
        /// Just a wrapper over the <see cref="DescriptorHelpers"/> method with the same name,
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        protected static string UpperFirstLetter(string name)
        {
            return DescriptorHelpers.UpperFirstLetter(name);
        }

        /// <summary>
        /// Converts this userdata to string
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public virtual string AsString(object obj)
        {
            return (obj != null) ? obj.ToString() : null;
        }

        /// <summary>
        /// Executes the specified indexer method.
        /// </summary>
        /// <param name="mdesc">The method descriptor</param>
        /// <param name="script">The script.</param>
        /// <param name="obj">The object.</param>
        /// <param name="index">The indexer parameter</param>
        /// <param name="value">The dynvalue to set on a setter, or null.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual DynValue ExecuteIndexer(
            IMemberDescriptor mdesc,
            Script script,
            object obj,
            DynValue index,
            DynValue value
        )
        {
            if (mdesc == null)
            {
                throw new ArgumentNullException(nameof(mdesc));
            }

            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            IList<DynValue> values;

            if (index.Type == DataType.Tuple)
            {
                if (value == null)
                {
                    values = index.Tuple;
                }
                else
                {
                    values = new List<DynValue>(index.Tuple);
                    values.Add(value);
                }
            }
            else
            {
                if (value == null)
                {
                    values = new DynValue[] { index };
                }
                else
                {
                    values = new DynValue[] { index, value };
                }
            }

            CallbackArguments args = new(values, false);
            ScriptExecutionContext execCtx = script.CreateDynamicExecutionContext();

            DynValue v = mdesc.GetValue(script, obj);

            if (v.Type != DataType.ClrFunction)
            {
                throw new ScriptRuntimeException(
                    "a clr callback was expected in member {0}, while a {1} was found",
                    mdesc.Name,
                    v.Type
                );
            }

            return v.Callback.ClrCallback(execCtx, args);
        }

        /// <summary>
        /// Gets a "meta" operation on this userdata. If a descriptor does not support this functionality,
        /// it should return "null" (not a nil).
        /// See <see cref="IUserDataDescriptor.MetaIndex" /> for further details.
        ///
        /// If a method exists marked with <see cref="NovaSharpUserDataMetamethodAttribute" /> for the specific
        /// metamethod requested, that method is returned.
        ///
        /// If the above fails, the following dispatching occur:
        ///
        /// __add, __sub, __mul, __div, __mod and __unm are dispatched to C# operator overloads (if they exist)
        /// __eq is dispatched to System.Object.Equals.
        /// __lt and __le are dispatched IComparable.Compare, if the type implements IComparable or IComparable{object}
        /// __len is dispatched to Length and Count properties, if those exist.
        /// __iterator is handled if the object implements IEnumerable or IEnumerator.
        /// __tonumber is dispatched to implicit or explicit conversion operators to standard numeric types.
        /// __tobool is dispatched to an implicit or explicit conversion operator to bool. If that fails, operator true is used.
        ///
        /// <param name="script">The script originating the request</param>
        /// <param name="obj">The object (null if a static request is done)</param>
        /// <param name="metaname">The name of the metamember.</param>
        /// </summary>
        /// <returns></returns>
        public virtual DynValue MetaIndex(Script script, object obj, string metaname)
        {
            if (script == null)
            {
                throw new ArgumentNullException(nameof(script));
            }

            if (metaname == null)
            {
                throw new ArgumentNullException(nameof(metaname));
            }

            IMemberDescriptor desc = _metaMembers.GetOrDefault(metaname);

            if (desc != null)
            {
                return desc.GetValue(script, obj);
            }

            switch (metaname)
            {
                case "__add":
                    return DispatchMetaOnMethod(script, obj, "op_Addition");
                case "__sub":
                    return DispatchMetaOnMethod(script, obj, "op_Subtraction");
                case "__mul":
                    return DispatchMetaOnMethod(script, obj, "op_Multiply");
                case "__div":
                    return DispatchMetaOnMethod(script, obj, "op_Division");
                case "__mod":
                    return DispatchMetaOnMethod(script, obj, "op_Modulus");
                case "__unm":
                    return DispatchMetaOnMethod(script, obj, "op_UnaryNegation");
                case "__eq":
                    return MultiDispatchEqual(script, obj);
                case "__lt":
                    return MultiDispatchLessThan(script, obj);
                case "__le":
                    return MultiDispatchLessThanOrEqual(script, obj);
                case "__len":
                    return TryDispatchLength(script, obj);
                case "__tonumber":
                    return TryDispatchToNumber(script, obj);
                case "__tobool":
                    return TryDispatchToBool(script, obj);
                case "__iterator":
                    return ClrToScriptConversions.EnumerationToDynValue(script, obj);
                default:
                    return null;
            }
        }

        private static int PerformComparison(object obj, object p1, object p2)
        {
            IComparable comp = (IComparable)obj;

            if (comp != null)
            {
                if (ReferenceEquals(obj, p1))
                {
                    return comp.CompareTo(p2);
                }
                else if (ReferenceEquals(obj, p2))
                {
                    return -comp.CompareTo(p1);
                }
            }

            throw new InternalErrorException("unexpected case");
        }

        private static DynValue MultiDispatchLessThanOrEqual(Script script, object obj)
        {
            if (obj is IComparable comp)
            {
                return DynValue.NewCallback(
                    (context, args) =>
                        DynValue.NewBoolean(
                            PerformComparison(obj, args[0].ToObject(), args[1].ToObject()) <= 0
                        )
                );
            }

            return null;
        }

        private static DynValue MultiDispatchLessThan(Script script, object obj)
        {
            if (obj is IComparable comp)
            {
                return DynValue.NewCallback(
                    (context, args) =>
                        DynValue.NewBoolean(
                            PerformComparison(obj, args[0].ToObject(), args[1].ToObject()) < 0
                        )
                );
            }

            return null;
        }

        private DynValue TryDispatchLength(Script script, object obj)
        {
            if (obj == null)
            {
                return null;
            }

            IMemberDescriptor lenprop = _members.GetOrDefault("Length");
            if (lenprop != null && lenprop.CanRead() && !lenprop.CanExecute())
            {
                return lenprop.GetGetterCallbackAsDynValue(script, obj);
            }

            IMemberDescriptor countprop = _members.GetOrDefault("Count");
            if (countprop != null && countprop.CanRead() && !countprop.CanExecute())
            {
                return countprop.GetGetterCallbackAsDynValue(script, obj);
            }

            return null;
        }

        private static DynValue MultiDispatchEqual(Script script, object obj)
        {
            return DynValue.NewCallback(
                (context, args) =>
                    DynValue.NewBoolean(CheckEquality(obj, args[0].ToObject(), args[1].ToObject()))
            );
        }

        private static bool CheckEquality(object obj, object p1, object p2)
        {
            if (obj != null)
            {
                if (ReferenceEquals(obj, p1))
                {
                    return obj.Equals(p2);
                }
                else if (ReferenceEquals(obj, p2))
                {
                    return obj.Equals(p1);
                }
            }

            return Equals(p1, p2);
        }

        private DynValue DispatchMetaOnMethod(Script script, object obj, string methodName)
        {
            IMemberDescriptor desc = _members.GetOrDefault(methodName);

            if (desc != null)
            {
                return desc.GetValue(script, obj);
            }
            else
            {
                return null;
            }
        }

        private DynValue TryDispatchToNumber(Script script, object obj)
        {
            foreach (Type t in NumericConversions.NumericTypesOrdered)
            {
                string name = t.GetConversionMethodName();
                DynValue v = DispatchMetaOnMethod(script, obj, name);
                if (v != null)
                {
                    return v;
                }
            }
            return null;
        }

        private DynValue TryDispatchToBool(Script script, object obj)
        {
            string name = typeof(bool).GetConversionMethodName();
            DynValue v = DispatchMetaOnMethod(script, obj, name);
            if (v != null)
            {
                return v;
            }

            return DispatchMetaOnMethod(script, obj, "op_True");
        }

        /// <summary>
        /// Determines whether the specified object is compatible with the specified type.
        /// Unless a very specific behaviour is needed, the correct implementation is a
        /// simple " return type.IsInstanceOfType(obj); "
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public virtual bool IsTypeCompatible(Type type, object obj)
        {
            return Framework.Do.IsInstanceOfType(type, obj);
        }
    }
}

namespace NovaSharp.Interpreter.Interop.StandardDescriptors
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Standard descriptor for userdata types.
    /// </summary>
    public class StandardUserDataDescriptor : DispatchingUserDataDescriptor, IWireableDescriptor
    {
        /// <summary>
        /// Gets the interop access mode this descriptor uses for members access
        /// </summary>
        public InteropAccessMode AccessMode { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardUserDataDescriptor"/> class.
        /// </summary>
        /// <param name="type">The type this descriptor refers to.</param>
        /// <param name="accessMode">The interop access mode this descriptor uses for members access</param>
        /// <param name="friendlyName">A human readable friendly name of the descriptor.</param>
        public StandardUserDataDescriptor(
            Type type,
            InteropAccessMode accessMode,
            string friendlyName = null
        )
            : base(type, friendlyName)
        {
            if (accessMode == InteropAccessMode.NoReflectionAllowed)
            {
                throw new ArgumentException(
                    "Can't create a StandardUserDataDescriptor under a NoReflectionAllowed access mode"
                );
            }

            if (Script.GlobalOptions.Platform.IsRunningOnAOT())
            {
                accessMode = InteropAccessMode.Reflection;
            }

            if (accessMode == InteropAccessMode.Default)
            {
                accessMode = UserData.DefaultAccessMode;
            }

            AccessMode = accessMode;

            FillMemberList();
        }

        /// <summary>
        /// Fills the member list.
        /// </summary>
        private void FillMemberList()
        {
            HashSet<string> membersToIgnore = new();
            object[] customAttributes = Framework
                .Do.GetCustomAttributes(Type, typeof(NovaSharpHideMemberAttribute), true);
            for (int i = 0; i < customAttributes.Length; i++)
            {
                if (customAttributes[i] is NovaSharpHideMemberAttribute hideMember)
                {
                    membersToIgnore.Add(hideMember.MemberName);
                }
            }

            Type type = Type;

            if (AccessMode == InteropAccessMode.HideMembers)
            {
                return;
            }

            if (!type.IsDelegateType())
            {
                // add declared constructors
                foreach (ConstructorInfo ci in Framework.Do.GetConstructors(type))
                {
                    if (membersToIgnore.Contains("__new"))
                    {
                        continue;
                    }

                    AddMember("__new", MethodMemberDescriptor.TryCreateIfVisible(ci, AccessMode));
                }

                // valuetypes don't reflect their empty ctor.. actually empty ctors are a perversion, we don't care and implement ours
                if (Framework.Do.IsValueType(type) && !membersToIgnore.Contains("__new"))
                {
                    AddMember("__new", new ValueTypeDefaultCtorMemberDescriptor(type));
                }
            }

            // add methods to method list and metamethods
            foreach (MethodInfo mi in Framework.Do.GetMethods(type))
            {
                if (membersToIgnore.Contains(mi.Name))
                {
                    continue;
                }

                MethodMemberDescriptor md = MethodMemberDescriptor.TryCreateIfVisible(
                    mi,
                    AccessMode
                );

                if (md != null)
                {
                    if (!MethodMemberDescriptor.CheckMethodIsCompatible(mi, false))
                    {
                        continue;
                    }

                    // transform explicit/implicit conversions to a friendlier name.
                    string name = mi.Name;
                    if (
                        mi.IsSpecialName
                        && (
                            mi.Name == SpecialNameCastExplicit || mi.Name == SpecialNameCastImplicit
                        )
                    )
                    {
                        name = mi.ReturnType.GetConversionMethodName();
                    }

                    AddMember(name, md);

                    foreach (string metaname in mi.GetMetaNamesFromAttributes())
                    {
                        AddMetaMember(metaname, md);
                    }
                }
            }

            // get properties
            foreach (PropertyInfo pi in Framework.Do.GetProperties(type))
            {
                if (
                    pi.IsSpecialName
                    || pi.GetIndexParameters().Length > 0
                    || membersToIgnore.Contains(pi.Name)
                )
                {
                    continue;
                }

                AddMember(pi.Name, PropertyMemberDescriptor.TryCreateIfVisible(pi, AccessMode));
            }

            // get fields
            foreach (FieldInfo fi in Framework.Do.GetFields(type))
            {
                if (fi.IsSpecialName || membersToIgnore.Contains(fi.Name))
                {
                    continue;
                }

                AddMember(fi.Name, FieldMemberDescriptor.TryCreateIfVisible(fi, AccessMode));
            }

            // get events
            foreach (EventInfo ei in Framework.Do.GetEvents(type))
            {
                if (ei.IsSpecialName || membersToIgnore.Contains(ei.Name))
                {
                    continue;
                }

                AddMember(ei.Name, EventMemberDescriptor.TryCreateIfVisible(ei, AccessMode));
            }

            // get nested types and create statics
            foreach (Type nestedType in Framework.Do.GetNestedTypes(type))
            {
                if (membersToIgnore.Contains(nestedType.Name))
                {
                    continue;
                }

                if (!Framework.Do.IsGenericTypeDefinition(nestedType))
                {
                    if (
                        Framework.Do.IsNestedPublic(nestedType)
                        || Framework
                            .Do.GetCustomAttributes(
                                nestedType,
                                typeof(NovaSharpUserDataAttribute),
                                true
                            )
                            .Length > 0
                    )
                    {
                        IUserDataDescriptor descr = UserData.RegisterType(nestedType, AccessMode);

                        if (descr != null)
                        {
                            AddDynValue(nestedType.Name, UserData.CreateStatic(nestedType));
                        }
                    }
                }
            }

            if (!membersToIgnore.Contains("[this]"))
            {
                if (Type.IsArray)
                {
                    int rank = Type.GetArrayRank();

                    ParameterDescriptor[] getPars = new ParameterDescriptor[rank];
                    ParameterDescriptor[] setPars = new ParameterDescriptor[rank + 1];

                    for (int i = 0; i < rank; i++)
                    {
                        getPars[i] = setPars[i] = new ParameterDescriptor(
                            "idx" + i.ToString(),
                            typeof(int)
                        );
                    }

                    setPars[rank] = new ParameterDescriptor("value", Type.GetElementType());

                    AddMember(
                        SpecialNameIndexerSet,
                        new ArrayMemberDescriptor(SpecialNameIndexerSet, true, setPars)
                    );
                    AddMember(
                        SpecialNameIndexerGet,
                        new ArrayMemberDescriptor(SpecialNameIndexerGet, false, getPars)
                    );
                }
                else if (Type == typeof(Array))
                {
                    AddMember(
                        SpecialNameIndexerSet,
                        new ArrayMemberDescriptor(SpecialNameIndexerSet, true)
                    );
                    AddMember(
                        SpecialNameIndexerGet,
                        new ArrayMemberDescriptor(SpecialNameIndexerGet, false)
                    );
                }
            }
        }

        public void PrepareForWiring(Table t)
        {
            if (
                AccessMode == InteropAccessMode.HideMembers
                || Framework.Do.GetAssembly(Type) == Framework.Do.GetAssembly(GetType())
            )
            {
                t.Set("skip", DynValue.NewBoolean(true));
            }
            else
            {
                t.Set("visibility", DynValue.NewString(Type.GetClrVisibility()));

                t.Set("class", DynValue.NewString(GetType().FullName));
                DynValue tm = DynValue.NewPrimeTable();
                t.Set("members", tm);
                DynValue tmm = DynValue.NewPrimeTable();
                t.Set("metamembers", tmm);

                Serialize(tm.Table, Members);
                Serialize(tmm.Table, MetaMembers);
            }
        }

        private void Serialize(
            Table t,
            IEnumerable<KeyValuePair<string, IMemberDescriptor>> members
        )
        {
            foreach (KeyValuePair<string, IMemberDescriptor> pair in members)
            {
                if (pair.Value is IWireableDescriptor sd)
                {
                    DynValue mt = DynValue.NewPrimeTable();
                    t.Set(pair.Key, mt);
                    sd.PrepareForWiring(mt.Table);
                }
                else
                {
                    t.Set(
                        pair.Key,
                        DynValue.NewString(
                            "unsupported member type : " + pair.Value.GetType().FullName
                        )
                    );
                }
            }
        }
    }
}

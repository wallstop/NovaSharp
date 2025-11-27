namespace NovaSharp.Interpreter.Interop.UserDataRegistries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.ProxyObjects;
    using NovaSharp.Interpreter.Interop.RegistrationPolicies;
    using NovaSharp.Interpreter.Interop.StandardDescriptors;

    /// <summary>
    /// Registry of all type descriptors. Use the UserData static helpers to access these.
    /// </summary>
    internal static class TypeDescriptorRegistry
    {
        private sealed class DescriptorRegistryState
        {
            public DescriptorRegistryState()
            {
                SyncRoot = new object();
                TypeRegistry = new Dictionary<Type, IUserDataDescriptor>();
                TypeRegistryHistory = new Dictionary<Type, IUserDataDescriptor>();
                DefaultAccessModeValue = InteropAccessMode.LazyOptimized;
                RegistrationPolicy = InteropRegistrationPolicy.Default;
            }

            public DescriptorRegistryState(DescriptorRegistryState template)
            {
                SyncRoot = new object();
                TypeRegistry = new Dictionary<Type, IUserDataDescriptor>(template.TypeRegistry);
                TypeRegistryHistory = new Dictionary<Type, IUserDataDescriptor>(
                    template.TypeRegistryHistory
                );
                DefaultAccessModeValue = template.DefaultAccessModeValue;
                RegistrationPolicy = template.RegistrationPolicy;
            }

            /// <summary>
            /// Gets a lock object guarding access to the descriptor dictionaries.
            /// </summary>
            public object SyncRoot { get; }

            /// <summary>
            /// Gets the active registry of descriptors keyed by their CLR type.
            /// </summary>
            public Dictionary<Type, IUserDataDescriptor> TypeRegistry { get; }

            /// <summary>
            /// Gets the history of descriptors that have been registered within the current process.
            /// </summary>
            public Dictionary<Type, IUserDataDescriptor> TypeRegistryHistory { get; }

            /// <summary>
            /// Gets or sets the default access mode applied to newly discovered descriptors.
            /// </summary>
            public InteropAccessMode DefaultAccessModeValue { get; set; }

            /// <summary>
            /// Gets or sets the registration policy used when resolving duplicate descriptors.
            /// </summary>
            public IRegistrationPolicy RegistrationPolicy { get; set; }
        }

        private sealed class RegistryScope : IDisposable
        {
            private readonly RegistryScope _previous;

            public RegistryScope(DescriptorRegistryState state, RegistryScope previous)
            {
                State = state;
                _previous = previous;
            }

            /// <summary>
            /// Gets the snapshot of descriptor state active within this scope.
            /// </summary>
            public DescriptorRegistryState State { get; }

            /// <summary>
            /// Restores the previous registry scope when the scope is disposed.
            /// </summary>
            public void Dispose()
            {
                ScopedState.Value = _previous;
            }
        }

        private static readonly DescriptorRegistryState GlobalState = new();
        private static readonly AsyncLocal<RegistryScope> ScopedState = new();

        private static DescriptorRegistryState CurrentState =>
            ScopedState.Value?.State ?? GlobalState;

        private static object CurrentSyncRoot => CurrentState.SyncRoot;

        private static Dictionary<Type, IUserDataDescriptor> TypeRegistry =>
            CurrentState.TypeRegistry;

        private static Dictionary<Type, IUserDataDescriptor> TypeRegistryHistory =>
            CurrentState.TypeRegistryHistory;

        /// <summary>
        /// Creates an isolation scope so that registrations performed inside it can be reverted easily.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> that restores the previous registry when disposed.</returns>
        internal static IDisposable EnterIsolationScope()
        {
            DescriptorRegistryState clone = new(CurrentState);
            RegistryScope scope = new(clone, ScopedState.Value);
            ScopedState.Value = scope;
            return scope;
        }

        /// <summary>
        /// Registers all types marked with a NovaSharpUserDataAttribute that ar contained in an assembly.
        /// </summary>
        /// <param name="asm">The assembly.</param>
        /// <param name="includeExtensionTypes">if set to <c>true</c> extension types are registered to the appropriate registry.</param>
        internal static void RegisterAssembly(
            Assembly asm = null,
            bool includeExtensionTypes = false
        )
        {
            if (asm == null)
            {
#if NETFX_CORE || DOTNET_CORE
                throw new NotSupportedException(
                    "Assembly.GetCallingAssembly is not supported on target framework."
                );
#else
                asm = Assembly.GetCallingAssembly();
#endif
            }

            if (includeExtensionTypes)
            {
                var extensionTypes =
                    from t in asm.SafeGetTypes()
                    let attributes = Framework.Do.GetCustomAttributes(
                        t,
                        typeof(ExtensionAttribute),
                        true
                    )
                    where attributes != null && attributes.Length > 0
                    select new { Attributes = attributes, DataType = t };

                foreach (var extType in extensionTypes)
                {
                    UserData.RegisterExtensionType(extType.DataType);
                }
            }

            var userDataTypes =
                from t in asm.SafeGetTypes()
                let attributes = Framework.Do.GetCustomAttributes(
                    t,
                    typeof(NovaSharpUserDataAttribute),
                    true
                )
                where attributes != null && attributes.Length > 0
                select new { Attributes = attributes, DataType = t };

            foreach (var userDataType in userDataTypes)
            {
                UserData.RegisterType(
                    userDataType.DataType,
                    userDataType.Attributes.OfType<NovaSharpUserDataAttribute>().First().AccessMode
                );
            }
        }

        /// <summary>
        /// Determines whether the specified type is registered. Note that this should be used only to check if a descriptor
        /// has been registered EXACTLY. For many types a descriptor can still be created, for example through the descriptor
        /// of a base type or implemented interfaces.
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns></returns>
        internal static bool IsTypeRegistered(Type type)
        {
            DescriptorRegistryState state = CurrentState;
            lock (state.SyncRoot)
            {
                return state.TypeRegistry.ContainsKey(type);
            }
        }

        /// <summary>
        /// Unregisters a type.
        /// WARNING: unregistering types at runtime is a dangerous practice and may cause unwanted errors.
        /// Use this only for testing purposes or to re-register the same type in a slightly different way.
        /// Additionally, it's a good practice to discard all previous loaded scripts after calling this method.
        /// </summary>
        /// <param name="t">The The type to be unregistered</param>
        internal static void UnregisterType(Type t)
        {
            DescriptorRegistryState state = CurrentState;
            lock (state.SyncRoot)
            {
                if (state.TypeRegistry.TryGetValue(t, out IUserDataDescriptor descriptor))
                {
                    PerformRegistration(state, t, null, descriptor);
                }
            }
        }

        /// <summary>
        /// Gets or sets the default access mode to be used in the whole application
        /// </summary>
        /// <value>
        /// The default access mode.
        /// </value>
        /// <exception cref="System.ArgumentException">InteropAccessMode is InteropAccessMode.Default</exception>
        internal static InteropAccessMode DefaultAccessMode
        {
            get { return CurrentState.DefaultAccessModeValue; }
            set
            {
                if (value == InteropAccessMode.Default)
                {
                    throw new ArgumentException("InteropAccessMode is InteropAccessMode.Default");
                }

                CurrentState.DefaultAccessModeValue = value;
            }
        }

        /// <summary>
        /// Registers a proxy type.
        /// </summary>
        /// <param name="proxyFactory">The proxy factory.</param>
        /// <param name="accessMode">The access mode.</param>
        /// <param name="friendlyName">Name of the friendly.</param>
        /// <returns></returns>
        internal static IUserDataDescriptor RegisterProxyTypeImpl(
            IProxyFactory proxyFactory,
            InteropAccessMode accessMode,
            string friendlyName
        )
        {
            IUserDataDescriptor proxyDescriptor = RegisterTypeImpl(
                proxyFactory.ProxyType,
                accessMode,
                friendlyName,
                null
            );
            return RegisterTypeImpl(
                proxyFactory.TargetType,
                accessMode,
                friendlyName,
                new ProxyUserDataDescriptor(proxyFactory, proxyDescriptor, friendlyName)
            );
        }

        /// <summary>
        /// Registers a type
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="accessMode">The access mode (used only if a default type descriptor is created).</param>
        /// <param name="friendlyName">Friendly name of the descriptor.</param>
        /// <param name="descriptor">The descriptor, or null to use a default one.</param>
        /// <returns></returns>
        internal static IUserDataDescriptor RegisterTypeImpl(
            Type type,
            InteropAccessMode accessMode,
            string friendlyName,
            IUserDataDescriptor descriptor
        )
        {
            accessMode = ResolveDefaultAccessModeForType(accessMode, type);

            DescriptorRegistryState state = CurrentState;
            lock (state.SyncRoot)
            {
                state.TypeRegistry.TryGetValue(type, out IUserDataDescriptor oldDescriptor);

                if (descriptor == null)
                {
                    if (IsTypeBlacklisted(type))
                    {
                        return null;
                    }

                    if (Framework.Do.GetInterfaces(type).Any(ii => ii == typeof(IUserDataType)))
                    {
                        AutoDescribingUserDataDescriptor audd = new(type, friendlyName);
                        return PerformRegistration(state, type, audd, oldDescriptor);
                    }
                    else if (Framework.Do.IsGenericTypeDefinition(type))
                    {
                        StandardGenericsUserDataDescriptor typeGen = new(type, accessMode);
                        return PerformRegistration(state, type, typeGen, oldDescriptor);
                    }
                    else if (Framework.Do.IsEnum(type))
                    {
                        StandardEnumUserDataDescriptor enumDescr = new(type, friendlyName);
                        return PerformRegistration(state, type, enumDescr, oldDescriptor);
                    }
                    else
                    {
                        StandardUserDataDescriptor udd = new(type, accessMode, friendlyName);

                        if (accessMode == InteropAccessMode.BackgroundOptimized)
                        {
#if NETFX_CORE
                            System.Threading.Tasks.Task.Run(() =>
                                ((IOptimizableDescriptor)udd).Optimize()
                            );
#else
                            ThreadPool.QueueUserWorkItem(o =>
                                ((IOptimizableDescriptor)udd).Optimize()
                            );
#endif
                        }

                        return PerformRegistration(state, type, udd, oldDescriptor);
                    }
                }
                else
                {
                    PerformRegistration(state, type, descriptor, oldDescriptor);
                    return descriptor;
                }
            }
        }

        private static IUserDataDescriptor PerformRegistration(
            DescriptorRegistryState state,
            Type type,
            IUserDataDescriptor newDescriptor,
            IUserDataDescriptor oldDescriptor
        )
        {
            IUserDataDescriptor result = state.RegistrationPolicy.HandleRegistration(
                newDescriptor,
                oldDescriptor
            );

            if (result != oldDescriptor)
            {
                if (result == null)
                {
                    state.TypeRegistry.Remove(type);
                }
                else
                {
                    state.TypeRegistry[type] = result;
                    state.TypeRegistryHistory[type] = result;
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves the default type of the access mode for the given type
        /// </summary>
        /// <param name="accessMode">The access mode.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        internal static InteropAccessMode ResolveDefaultAccessModeForType(
            InteropAccessMode accessMode,
            Type type
        )
        {
            if (accessMode == InteropAccessMode.Default)
            {
                NovaSharpUserDataAttribute attr = Framework
                    .Do.GetCustomAttributes(type, true)
                    .OfType<NovaSharpUserDataAttribute>()
                    .SingleOrDefault();

                if (attr != null)
                {
                    accessMode = attr.AccessMode;
                }
            }

            if (accessMode == InteropAccessMode.Default)
            {
                accessMode = DefaultAccessMode;
            }

            return accessMode;
        }

        /// <summary>
        /// Gets the best possible type descriptor for a specified CLR type.
        /// </summary>
        /// <param name="type">The CLR type for which the descriptor is desired.</param>
        /// <param name="searchInterfaces">if set to <c>true</c> interfaces are used in the search.</param>
        /// <returns></returns>
        internal static IUserDataDescriptor GetDescriptorForType(Type type, bool searchInterfaces)
        {
            DescriptorRegistryState state = CurrentState;
            lock (state.SyncRoot)
            {
                IUserDataDescriptor typeDescriptor = null;

                // if the type has been explicitly registered, return its descriptor as it's complete
                if (state.TypeRegistry.TryGetValue(type, out IUserDataDescriptor descriptorForType))
                {
                    return descriptorForType;
                }

                if (state.RegistrationPolicy.AllowTypeAutoRegistration(type))
                {
                    // no autoreg of delegates
                    if (!Framework.Do.IsAssignableFrom((typeof(Delegate)), type))
                    {
                        return RegisterTypeImpl(type, DefaultAccessMode, type.FullName, null);
                    }
                }

                // search for the base object descriptors
                for (Type t = type; t != null; t = Framework.Do.GetBaseType(t))
                {
                    if (state.TypeRegistry.TryGetValue(t, out IUserDataDescriptor u))
                    {
                        typeDescriptor = u;
                        break;
                    }
                    else if (Framework.Do.IsGenericType(t))
                    {
                        if (state.TypeRegistry.TryGetValue(t.GetGenericTypeDefinition(), out u))
                        {
                            typeDescriptor = u;
                            break;
                        }
                    }
                }

                if (typeDescriptor is IGeneratorUserDataDescriptor descriptor)
                {
                    typeDescriptor = descriptor.Generate(type);
                }

                // we should not search interfaces (for example, it's just for static members..), no need to look further
                if (!searchInterfaces)
                {
                    return typeDescriptor;
                }

                List<IUserDataDescriptor> descriptors = new();

                if (typeDescriptor != null)
                {
                    descriptors.Add(typeDescriptor);
                }

                if (searchInterfaces)
                {
                    foreach (Type interfaceType in Framework.Do.GetInterfaces(type))
                    {
                        if (
                            state.TypeRegistry.TryGetValue(
                                interfaceType,
                                out IUserDataDescriptor interfaceDescriptor
                            )
                        )
                        {
                            if (interfaceDescriptor is IGeneratorUserDataDescriptor dataDescriptor)
                            {
                                interfaceDescriptor = dataDescriptor.Generate(type);
                            }

                            if (interfaceDescriptor != null)
                            {
                                descriptors.Add(interfaceDescriptor);
                            }
                        }
                        else if (Framework.Do.IsGenericType(interfaceType))
                        {
                            if (
                                state.TypeRegistry.TryGetValue(
                                    interfaceType.GetGenericTypeDefinition(),
                                    out interfaceDescriptor
                                )
                            )
                            {
                                if (
                                    interfaceDescriptor
                                    is IGeneratorUserDataDescriptor dataDescriptor
                                )
                                {
                                    interfaceDescriptor = dataDescriptor.Generate(type);
                                }

                                if (interfaceDescriptor != null)
                                {
                                    descriptors.Add(interfaceDescriptor);
                                }
                            }
                        }
                    }
                }

                if (descriptors.Count == 1)
                {
                    return descriptors[0];
                }
                else if (descriptors.Count == 0)
                {
                    return null;
                }
                else
                {
                    return new CompositeUserDataDescriptor(descriptors, type);
                }
            }
        }

        private static bool FrameworkIsAssignableFrom(Type type)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines whether the specified type is blacklisted.
        /// Blacklisted types CANNOT be registered using default descriptors but they can still be registered
        /// with custom descriptors. Forcing registration of blacklisted types in this way can introduce
        /// side effects.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <returns></returns>
        public static bool IsTypeBlacklisted(Type t)
        {
            if (
                Framework.Do.IsValueType(t)
                && Framework.Do.GetInterfaces(t).Contains(typeof(System.Collections.IEnumerator))
            )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the list of registered types.
        /// </summary>
        /// <value>
        /// The registered types.
        /// </value>
        public static IEnumerable<KeyValuePair<Type, IUserDataDescriptor>> RegisteredTypes
        {
            get
            {
                DescriptorRegistryState state = CurrentState;
                lock (state.SyncRoot)
                {
                    return state.TypeRegistry.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the list of registered types, including unregistered types.
        /// </summary>
        /// <value>
        /// The registered types.
        /// </value>
        public static IEnumerable<KeyValuePair<Type, IUserDataDescriptor>> RegisteredTypesHistory
        {
            get
            {
                DescriptorRegistryState state = CurrentState;
                lock (state.SyncRoot)
                {
                    return state.TypeRegistryHistory.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets or sets the registration policy.
        /// </summary>
        internal static IRegistrationPolicy RegistrationPolicy
        {
            get { return CurrentState.RegistrationPolicy; }
            set
            {
                CurrentState.RegistrationPolicy =
                    value ?? throw new ArgumentNullException(nameof(value));
            }
        }
    }
}

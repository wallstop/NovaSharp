namespace WallstopStudios.NovaSharp.Interpreter.Interop.UserDataRegistries
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.BasicDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using WallstopStudios.NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Registry of all extension methods. Use the UserData static helpers to access these.
    /// </summary>
    internal class ExtensionMethodsRegistry
    {
        private sealed class ExtensionRegistryState
        {
            public ExtensionRegistryState()
            {
                SyncRoot = new object();
                Registry = new MultiDictionary<string, IOverloadableMemberDescriptor>(
                    StringComparer.Ordinal
                );
                UnresolvedGenerics = new MultiDictionary<string, UnresolvedGenericMethod>(
                    StringComparer.Ordinal
                );
                ChangeVersion = 0;
            }

            public ExtensionRegistryState(ExtensionRegistryState template)
            {
                SyncRoot = new object();
                Registry = CloneRegistry(template.Registry);
                UnresolvedGenerics = CloneGenericsRegistry(template.UnresolvedGenerics);
                ChangeVersion = template.ChangeVersion;
            }

            public object SyncRoot { get; }

            public MultiDictionary<string, IOverloadableMemberDescriptor> Registry { get; }

            public MultiDictionary<string, UnresolvedGenericMethod> UnresolvedGenerics { get; }

            public int ChangeVersion { get; set; }
        }

        private sealed class RegistryScope : IDisposable
        {
            private readonly RegistryScope _previous;

            public RegistryScope(ExtensionRegistryState state, RegistryScope previous)
            {
                State = state ?? throw new ArgumentNullException(nameof(state));
                _previous = previous;
            }

            public ExtensionRegistryState State { get; }

            public void Dispose()
            {
                ScopedState.Value = _previous;
            }
        }

        private static readonly ExtensionRegistryState GlobalState = new();
        private static readonly AsyncLocal<RegistryScope> ScopedState = new();

        private static ExtensionRegistryState CurrentState =>
            ScopedState.Value?.State ?? GlobalState;

        private class UnresolvedGenericMethod
        {
            /// <summary>
            /// Gets the reflection method info for the unresolved generic extension method.
            /// </summary>
            public MethodInfo Method { get; }

            /// <summary>
            /// Gets the access mode that should be used when the descriptor is materialized.
            /// </summary>
            public InteropAccessMode AccessMode { get; }

            /// <summary>
            /// Tracks the generic types that have already been materialized to avoid duplicates.
            /// </summary>
            public HashSet<Type> AlreadyAddedTypes { get; } = new();

            public UnresolvedGenericMethod(MethodInfo mi, InteropAccessMode mode)
            {
                AccessMode = mode;
                Method = mi;
            }

            public UnresolvedGenericMethod Clone()
            {
                UnresolvedGenericMethod copy = new(Method, AccessMode);
                copy.AlreadyAddedTypes.UnionWith(AlreadyAddedTypes);
                return copy;
            }
        }

        /// <summary>
        /// Registers an extension Type (that is a type containing extension methods)
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="mode">The InteropAccessMode.</param>
        public static void RegisterExtensionType(
            Type type,
            InteropAccessMode mode = InteropAccessMode.Default
        )
        {
            ExtensionRegistryState state = CurrentState;
            lock (state.SyncRoot)
            {
                bool changesDone = false;

                MethodInfo[] methods = Framework.Do.GetMethods(type);

                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo mi = methods[i];

                    if (!mi.IsStatic)
                    {
                        continue;
                    }

                    if (mi.GetCustomAttributes(typeof(ExtensionAttribute), false).Length == 0)
                    {
                        continue;
                    }

                    if (mi.ContainsGenericParameters)
                    {
                        state.UnresolvedGenerics.Add(
                            mi.Name,
                            new UnresolvedGenericMethod(mi, mode)
                        );
                        changesDone = true;
                        continue;
                    }

                    if (!MethodMemberDescriptor.CheckMethodIsCompatible(mi, false))
                    {
                        continue;
                    }

                    MethodMemberDescriptor desc = new(mi, mode);

                    state.Registry.Add(mi.Name, desc);
                    changesDone = true;
                }

                if (changesDone)
                {
                    ++state.ChangeVersion;
                }
            }
        }

        private static object FrameworkGetMethods()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets all the extension methods which can match a given name
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static IEnumerable<IOverloadableMemberDescriptor> GetExtensionMethodsByName(
            string name
        )
        {
            ExtensionRegistryState state = CurrentState;
            lock (state.SyncRoot)
            {
                return new List<IOverloadableMemberDescriptor>(state.Registry.Find(name));
            }
        }

        /// <summary>
        /// Gets a number which gets incremented every time the extension methods registry changes.
        /// Use this to invalidate caches based on extension methods
        /// </summary>
        /// <returns></returns>
        public static int GetExtensionMethodsChangeVersion()
        {
            return CurrentState.ChangeVersion;
        }

        /// <summary>
        /// Gets all the extension methods which can match a given name and extending a given Type
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="extendedType">The extended type.</param>
        /// <returns></returns>
        public static List<IOverloadableMemberDescriptor> GetExtensionMethodsByNameAndType(
            string name,
            Type extendedType
        )
        {
            ExtensionRegistryState state = CurrentState;
            List<UnresolvedGenericMethod> unresolvedGenerics = new();

            lock (state.SyncRoot)
            {
                foreach (UnresolvedGenericMethod method in state.UnresolvedGenerics.Find(name))
                {
                    unresolvedGenerics.Add(method);
                }
            }

            foreach (UnresolvedGenericMethod ugm in unresolvedGenerics)
            {
                ParameterInfo[] args = ugm.Method.GetParameters();
                if (args.Length == 0)
                {
                    continue;
                }

                Type extensionType = args[0].ParameterType;

                Type genericType = GetGenericMatch(extensionType, extendedType);

                if (ugm.AlreadyAddedTypes.Add(genericType))
                {
                    if (genericType != null)
                    {
                        MethodInfo mi = InstantiateMethodInfo(
                            ugm.Method,
                            extensionType,
                            genericType,
                            extendedType
                        );
                        if (mi != null)
                        {
                            if (!MethodMemberDescriptor.CheckMethodIsCompatible(mi, false))
                            {
                                continue;
                            }

                            MethodMemberDescriptor desc = new(mi, ugm.AccessMode);

                            lock (state.SyncRoot)
                            {
                                state.Registry.Add(ugm.Method.Name, desc);
                                ++state.ChangeVersion;
                            }
                        }
                    }
                }
            }

            List<IOverloadableMemberDescriptor> matches = new();

            foreach (IOverloadableMemberDescriptor descriptor in state.Registry.Find(name))
            {
                Type extensionType = descriptor.ExtensionMethodType;
                if (
                    extensionType != null
                    && Framework.Do.IsAssignableFrom(extensionType, extendedType)
                )
                {
                    matches.Add(descriptor);
                }
            }

            return matches;
        }

        private static MethodInfo InstantiateMethodInfo(
            MethodInfo mi,
            Type extensionType,
            Type genericType,
            Type extendedType
        )
        {
            Type[] defs = mi.GetGenericArguments();
            Type[] tdefs = Framework.Do.GetGenericArguments(genericType);

            if (tdefs.Length == defs.Length)
            {
                return mi.MakeGenericMethod(tdefs);
            }

            return null;
        }

        private static Type GetGenericMatch(Type extensionType, Type extendedType)
        {
            if (!extensionType.IsGenericParameter)
            {
                extensionType = extensionType.GetGenericTypeDefinition();

                foreach (Type t in extendedType.GetAllImplementedTypes())
                {
                    if (
                        Framework.Do.IsGenericType(t)
                        && t.GetGenericTypeDefinition() == extensionType
                    )
                    {
                        return t;
                    }
                }
            }

            return null;
        }

        internal static IDisposable EnterIsolationScope()
        {
            ExtensionRegistryState clone = new(CurrentState);
            RegistryScope scope = new(clone, ScopedState.Value);
            ScopedState.Value = scope;
            return scope;
        }

        private static MultiDictionary<string, IOverloadableMemberDescriptor> CloneRegistry(
            MultiDictionary<string, IOverloadableMemberDescriptor> source
        )
        {
            MultiDictionary<string, IOverloadableMemberDescriptor> clone = new(
                StringComparer.Ordinal
            );
            foreach (string key in source.Keys)
            {
                foreach (IOverloadableMemberDescriptor value in source.Find(key))
                {
                    clone.Add(key, value);
                }
            }

            return clone;
        }

        private static MultiDictionary<string, UnresolvedGenericMethod> CloneGenericsRegistry(
            MultiDictionary<string, UnresolvedGenericMethod> source
        )
        {
            MultiDictionary<string, UnresolvedGenericMethod> clone = new(StringComparer.Ordinal);
            foreach (string key in source.Keys)
            {
                foreach (UnresolvedGenericMethod value in source.Find(key))
                {
                    clone.Add(key, value.Clone());
                }
            }

            return clone;
        }
    }
}

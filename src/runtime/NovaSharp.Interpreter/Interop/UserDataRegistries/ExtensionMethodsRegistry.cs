namespace NovaSharp.Interpreter.Interop.UserDataRegistries
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataStructs;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Interop.BasicDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.MemberDescriptors;
    using NovaSharp.Interpreter.Interop.StandardDescriptors.ReflectionMemberDescriptors;

    /// <summary>
    /// Registry of all extension methods. Use the UserData static helpers to access these.
    /// </summary>
    internal class ExtensionMethodsRegistry
    {
        private static readonly object SLock = new();
        private static readonly MultiDictionary<string, IOverloadableMemberDescriptor> SRegistry =
            new();
        private static readonly MultiDictionary<
            string,
            UnresolvedGenericMethod
        > SUnresolvedGenericsRegistry = new();
        private static int ExtensionMethodChangeVersion;

        private class UnresolvedGenericMethod
        {
            public readonly MethodInfo Method;
            public readonly InteropAccessMode AccessMode;
            public readonly HashSet<Type> AlreadyAddedTypes = new();

            public UnresolvedGenericMethod(MethodInfo mi, InteropAccessMode mode)
            {
                AccessMode = mode;
                Method = mi;
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
            lock (SLock)
            {
                bool changesDone = false;

                foreach (MethodInfo mi in Framework.Do.GetMethods(type).Where(mi => mi.IsStatic))
                {
                    if (mi.GetCustomAttributes(typeof(ExtensionAttribute), false).Length == 0)
                    {
                        continue;
                    }

                    if (mi.ContainsGenericParameters)
                    {
                        SUnresolvedGenericsRegistry.Add(
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

                    SRegistry.Add(mi.Name, desc);
                    changesDone = true;
                }

                if (changesDone)
                {
                    ++ExtensionMethodChangeVersion;
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
            lock (SLock)
            {
                return new List<IOverloadableMemberDescriptor>(SRegistry.Find(name));
            }
        }

        /// <summary>
        /// Gets a number which gets incremented every time the extension methods registry changes.
        /// Use this to invalidate caches based on extension methods
        /// </summary>
        /// <returns></returns>
        public static int GetExtensionMethodsChangeVersion()
        {
            return ExtensionMethodChangeVersion;
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
            List<UnresolvedGenericMethod> unresolvedGenerics = null;

            lock (SLock)
            {
                unresolvedGenerics = SUnresolvedGenericsRegistry.Find(name).ToList();
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

                            SRegistry.Add(ugm.Method.Name, desc);
                            ++ExtensionMethodChangeVersion;
                        }
                    }
                }
            }

            return SRegistry
                .Find(name)
                .Where(d =>
                    d.ExtensionMethodType != null
                    && Framework.Do.IsAssignableFrom(d.ExtensionMethodType, extendedType)
                )
                .ToList();
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
    }
}

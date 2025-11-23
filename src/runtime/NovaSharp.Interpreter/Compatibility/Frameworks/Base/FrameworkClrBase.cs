namespace NovaSharp.Interpreter.Compatibility.Frameworks.Base
{
#if !NETFX_CORE || DOTNET_CORE

    using System;
    using System.Reflection;

    /// <summary>
    /// Shared implementation for CLR-based runtimes (classic .NET Framework and .NET Core) that
    /// exposes the full <see cref="Type"/> reflection surface and therefore can rely on rich binding
    /// flags when enumerating members.
    /// </summary>
    internal abstract class FrameworkClrBase : FrameworkReflectionBase
    {
        private readonly BindingFlags _bindingflagsMember =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        private readonly BindingFlags _bindingflagsInnerclass =
            BindingFlags.Public | BindingFlags.NonPublic;

        /// <inheritdoc/>
        public override MethodInfo GetAddMethod(EventInfo ei)
        {
            return ei.GetAddMethod(true);
        }

        /// <inheritdoc/>
        public override ConstructorInfo[] GetConstructors(Type type)
        {
            return GetTypeInfoFromType(type).GetConstructors(_bindingflagsMember);
        }

        /// <inheritdoc/>
        public override EventInfo[] GetEvents(Type type)
        {
            return GetTypeInfoFromType(type).GetEvents(_bindingflagsMember);
        }

        /// <inheritdoc/>
        public override FieldInfo[] GetFields(Type type)
        {
            return GetTypeInfoFromType(type).GetFields(_bindingflagsMember);
        }

        /// <inheritdoc/>
        public override Type[] GetGenericArguments(Type type)
        {
            return GetTypeInfoFromType(type).GetGenericArguments();
        }

        /// <inheritdoc/>
        public override MethodInfo GetGetMethod(PropertyInfo pi)
        {
            return pi.GetGetMethod(true);
        }

        /// <inheritdoc/>
        public override Type[] GetInterfaces(Type t)
        {
            return GetTypeInfoFromType(t).GetInterfaces();
        }

        /// <inheritdoc/>
        public override MethodInfo GetMethod(Type type, string name)
        {
            return GetTypeInfoFromType(type).GetMethod(name);
        }

        /// <inheritdoc/>
        public override MethodInfo[] GetMethods(Type type)
        {
            return GetTypeInfoFromType(type).GetMethods(_bindingflagsMember);
        }

        /// <inheritdoc/>
        public override Type[] GetNestedTypes(Type type)
        {
            return GetTypeInfoFromType(type).GetNestedTypes(_bindingflagsInnerclass);
        }

        /// <inheritdoc/>
        public override PropertyInfo[] GetProperties(Type type)
        {
            return GetTypeInfoFromType(type).GetProperties(_bindingflagsMember);
        }

        /// <inheritdoc/>
        public override PropertyInfo GetProperty(Type type, string name)
        {
            return GetTypeInfoFromType(type).GetProperty(name);
        }

        /// <inheritdoc/>
        public override MethodInfo GetRemoveMethod(EventInfo ei)
        {
            return ei.GetRemoveMethod(true);
        }

        /// <inheritdoc/>
        public override MethodInfo GetSetMethod(PropertyInfo pi)
        {
            return pi.GetSetMethod(true);
        }

        /// <inheritdoc/>
        public override bool IsAssignableFrom(Type current, Type toCompare)
        {
            return GetTypeInfoFromType(current).IsAssignableFrom(toCompare);
        }

        /// <inheritdoc/>
        public override bool IsInstanceOfType(Type t, object o)
        {
            return GetTypeInfoFromType(t).IsInstanceOfType(o);
        }

        /// <inheritdoc/>
        public override MethodInfo GetMethod(Type resourcesType, string name, Type[] types)
        {
            return GetTypeInfoFromType(resourcesType).GetMethod(name, types);
        }

        /// <inheritdoc/>
        public override Type[] GetAssemblyTypes(Assembly assembly)
        {
            return assembly.GetTypes();
        }
    }
}

#endif

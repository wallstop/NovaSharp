#if NETFX_CORE && !DOTNET_CORE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WallstopStudios.NovaSharp.Interpreter.Compatibility.Frameworks.Base;

namespace WallstopStudios.NovaSharp.Interpreter.Compatibility.Frameworks
{
    /// <summary>
    /// Windows Store / WinRT implementation that only exposes the restricted reflection surface and
    /// therefore must translate between runtime-provided extension methods and the
    /// <see cref="FrameworkBase" /> contract.
    /// </summary>
    internal class FrameworkCurrent : FrameworkReflectionBase
    {
        /// <inheritdoc/>
        public override TypeInfo GetTypeInfoFromType(Type t)
        {
            return t.GetTypeInfo();
        }

        private T[] SafeArray<T>(IEnumerable<T> prop)
        {
            return prop != null ? prop.ToArray() : new T[0];
        }

        /// <inheritdoc/>
        public override MethodInfo GetAddMethod(EventInfo ei)
        {
            return ei.AddMethod;
        }

        /// <inheritdoc/>
        public override ConstructorInfo[] GetConstructors(Type type)
        {
            return SafeArray(GetTypeInfoFromType(type).DeclaredConstructors);
        }

        /// <inheritdoc/>
        public override EventInfo[] GetEvents(Type type)
        {
            return SafeArray(type.GetRuntimeEvents());
        }

        /// <inheritdoc/>
        public override FieldInfo[] GetFields(Type type)
        {
            return SafeArray(type.GetRuntimeFields());
        }

        /// <inheritdoc/>
        public override Type[] GetGenericArguments(Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        /// <inheritdoc/>
        public override MethodInfo GetGetMethod(PropertyInfo pi)
        {
            return pi.GetMethod;
        }

        /// <inheritdoc/>
        public override Type GetInterface(Type type, string name)
        {
            return type.GetTypeInfo().ImplementedInterfaces.FirstOrDefault(t => t.Name == name);
        }

        /// <inheritdoc/>
        public override Type[] GetInterfaces(Type t)
        {
            return SafeArray(GetTypeInfoFromType(t).ImplementedInterfaces);
        }

        /// <inheritdoc/>
        public override MethodInfo GetMethod(Type type, string name)
        {
            return type.GetRuntimeMethods().FirstOrDefault(mi => mi.Name == name);
        }

        /// <inheritdoc/>
        public override MethodInfo[] GetMethods(Type type)
        {
            return SafeArray(type.GetRuntimeMethods());
        }

        /// <inheritdoc/>
        public override Type[] GetNestedTypes(Type type)
        {
            return SafeArray(
                GetTypeInfoFromType(type).DeclaredNestedTypes.Select(ti => ti.AsType())
            );
        }

        /// <inheritdoc/>
        public override PropertyInfo[] GetProperties(Type type)
        {
            return SafeArray(type.GetRuntimeProperties());
        }

        /// <inheritdoc/>
        public override PropertyInfo GetProperty(Type type, string name)
        {
            return type.GetRuntimeProperty(name);
        }

        /// <inheritdoc/>
        public override MethodInfo GetRemoveMethod(EventInfo ei)
        {
            return ei.RemoveMethod;
        }

        /// <inheritdoc/>
        public override MethodInfo GetSetMethod(PropertyInfo pi)
        {
            return pi.SetMethod;
        }

        /// <inheritdoc/>
        public override bool IsAssignableFrom(Type current, Type toCompare)
        {
            return current.GetTypeInfo().IsAssignableFrom(toCompare.GetTypeInfo());
        }

        /// <inheritdoc/>
        public override bool IsDbNull(object o)
        {
            return o != null && o.GetType().FullName.StartsWith("System.DBNull");
        }

        /// <inheritdoc/>
        public override bool IsInstanceOfType(Type t, object o)
        {
            if (o == null)
                return false;

            return t.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo());
        }

        /// <inheritdoc/>
        public override bool StringContainsChar(string str, char chr)
        {
            return str.Contains(chr);
        }

        /// <inheritdoc/>
        public override MethodInfo GetMethod(Type resourcesType, string name, Type[] types)
        {
            return resourcesType.GetRuntimeMethod(name, types);
        }

        /// <inheritdoc/>
        public override Type[] GetAssemblyTypes(Assembly assembly)
        {
            return SafeArray(assembly.DefinedTypes.Select(ti => ti.AsType()));
        }
    }
}
#endif

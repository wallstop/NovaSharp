namespace WallstopStudios.NovaSharp.Interpreter.Compatibility.Frameworks.Base
{
    using System;
    using System.Linq;
    using System.Reflection;
    using TTypeInfo = System.Reflection.TypeInfo;

    /// <summary>
    /// Base helper for frameworks that expose the modern <see cref="TypeInfo"/> surface. It provides
    /// shared implementations of the <see cref="FrameworkBase" /> contract by delegating to
    /// <see cref="TypeInfo"/> APIs and leaves only the runtime-specific <see cref="Type"/> to
    /// <see cref="TypeInfo"/> conversion to derived classes.
    /// </summary>
    internal abstract class FrameworkReflectionBase : FrameworkBase
    {
        /// <summary>
        /// Converts the supplied <see cref="Type"/> into its corresponding <see cref="TypeInfo"/>
        /// representation for the active runtime.
        /// </summary>
        /// <param name="t">Type being inspected.</param>
        /// <returns>The runtime <see cref="TypeInfo"/> wrapper.</returns>
        public abstract TTypeInfo GetTypeInfoFromType(Type t);

        /// <inheritdoc/>
        public override Assembly GetAssembly(Type t)
        {
            return GetTypeInfoFromType(t).Assembly;
        }

        /// <inheritdoc/>
        public override Type GetBaseType(Type t)
        {
            return GetTypeInfoFromType(t).BaseType;
        }

        /// <inheritdoc/>
        public override bool IsValueType(Type t)
        {
            return GetTypeInfoFromType(t).IsValueType;
        }

        /// <inheritdoc/>
        public override bool IsInterface(Type t)
        {
            return GetTypeInfoFromType(t).IsInterface;
        }

        /// <inheritdoc/>
        public override bool IsNestedPublic(Type t)
        {
            return GetTypeInfoFromType(t).IsNestedPublic;
        }

        /// <inheritdoc/>
        public override bool IsAbstract(Type t)
        {
            return GetTypeInfoFromType(t).IsAbstract;
        }

        /// <inheritdoc/>
        public override bool IsEnum(Type t)
        {
            return GetTypeInfoFromType(t).IsEnum;
        }

        /// <inheritdoc/>
        public override bool IsGenericTypeDefinition(Type t)
        {
            return GetTypeInfoFromType(t).IsGenericTypeDefinition;
        }

        /// <inheritdoc/>
        public override bool IsGenericType(Type t)
        {
            return GetTypeInfoFromType(t).IsGenericType;
        }

        /// <inheritdoc/>
        public override Attribute[] GetCustomAttributes(Type t, bool inherit)
        {
            return GetTypeInfoFromType(t)
                .GetCustomAttributes(inherit)
                .OfType<Attribute>()
                .ToArray();
        }

        /// <inheritdoc/>
        public override Attribute[] GetCustomAttributes(Type t, Type at, bool inherit)
        {
            return GetTypeInfoFromType(t)
                .GetCustomAttributes(at, inherit)
                .OfType<Attribute>()
                .ToArray();
        }
    }
}

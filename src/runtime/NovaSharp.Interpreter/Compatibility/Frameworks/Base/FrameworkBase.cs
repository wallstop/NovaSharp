namespace NovaSharp.Interpreter.Compatibility.Frameworks.Base
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Provides a reflection abstraction that hides per-framework differences so the interpreter can
    /// inspect assemblies, types, and members without sprinkling conditional compilation throughout
    /// the runtime.
    /// </summary>
    /// <remarks>
    /// Each supported target framework (classic CLR, .NET Core, WinRT, PCL, etc.) implements this
    /// contract by forwarding to the reflection primitives available on that platform.
    /// </remarks>
    public abstract class FrameworkBase
    {
        /// <summary>
        /// Determines whether the provided string contains the specified character using the
        /// platform's ordinal comparison semantics.
        /// </summary>
        /// <param name="str">String to inspect. Can be <see langword="null"/>.</param>
        /// <param name="chr">Character to locate within <paramref name="str"/>.</param>
        /// <returns>
        /// <see langword="true"/> when the character exists in the string; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public abstract bool StringContainsChar(string str, char chr);

        /// <summary>
        /// Checks whether the supplied type represents a value type on the current framework.
        /// </summary>
        /// <param name="t">Type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the type is a value type; otherwise <see langword="false"/>.
        /// </returns>
        public abstract bool IsValueType(Type t);

        /// <summary>
        /// Resolves the assembly that declares the supplied type.
        /// </summary>
        /// <param name="t">Type whose declaring assembly should be returned.</param>
        /// <returns>The assembly that defines <paramref name="t"/>.</returns>
        public abstract Assembly GetAssembly(Type t);

        /// <summary>
        /// Gets the direct base type of the supplied type using the active platform's type model.
        /// </summary>
        /// <param name="t">Type whose base type should be returned.</param>
        /// <returns>The base type for <paramref name="t"/>; <see langword="null"/> when none.</returns>
        public abstract Type GetBaseType(Type t);

        /// <summary>
        /// Indicates whether the supplied type is a constructed generic type.
        /// </summary>
        /// <param name="t">Type to examine.</param>
        /// <returns>
        /// <see langword="true"/> when the type is a constructed generic; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public abstract bool IsGenericType(Type t);

        /// <summary>
        /// Indicates whether the supplied type is an open generic type definition.
        /// </summary>
        /// <param name="t">Type to examine.</param>
        /// <returns>
        /// <see langword="true"/> when the type is a generic type definition; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public abstract bool IsGenericTypeDefinition(Type t);

        /// <summary>
        /// Determines whether the supplied type represents an enumeration.
        /// </summary>
        /// <param name="t">Type to examine.</param>
        /// <returns><see langword="true"/> when the type is an enum.</returns>
        public abstract bool IsEnum(Type t);

        /// <summary>
        /// Determines whether the supplied type is declared as a nested public type.
        /// </summary>
        /// <param name="t">Type to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the type is nested and public; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public abstract bool IsNestedPublic(Type t);

        /// <summary>
        /// Determines whether the supplied type is abstract.
        /// </summary>
        /// <param name="t">Type to inspect.</param>
        /// <returns><see langword="true"/> when the type is abstract.</returns>
        public abstract bool IsAbstract(Type t);

        /// <summary>
        /// Determines whether the supplied type is an interface.
        /// </summary>
        /// <param name="t">Type to inspect.</param>
        /// <returns><see langword="true"/> when the type is an interface.</returns>
        public abstract bool IsInterface(Type t);

        /// <summary>
        /// Retrieves the custom attributes applied to a type, honoring the requested inheritance
        /// behavior.
        /// </summary>
        /// <param name="t">Type whose attributes should be returned.</param>
        /// <param name="inherit">
        /// When <see langword="true"/>, attributes inherited from base types are included.
        /// </param>
        /// <returns>An array of resolved attributes (possibly empty).</returns>
        public abstract Attribute[] GetCustomAttributes(Type t, bool inherit);

        /// <summary>
        /// Retrieves the custom attributes of the specified type that are applied to the provided
        /// type.
        /// </summary>
        /// <param name="t">Type whose attributes should be returned.</param>
        /// <param name="at">Attribute type to filter by.</param>
        /// <param name="inherit">
        /// When <see langword="true"/>, attributes inherited from base types are included.
        /// </param>
        /// <returns>An array of resolved attributes (possibly empty).</returns>
        public abstract Attribute[] GetCustomAttributes(Type t, Type at, bool inherit);

        /// <summary>
        /// Returns the interfaces implemented by the supplied type.
        /// </summary>
        /// <param name="t">Type to inspect.</param>
        /// <returns>An array of implemented interfaces.</returns>
        public abstract Type[] GetInterfaces(Type t);

        /// <summary>
        /// Determines whether the provided object is an instance of the supplied type.
        /// </summary>
        /// <param name="t">Type to compare against.</param>
        /// <param name="o">Object instance being tested.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="o"/> can be assigned to <paramref name="t"/>.
        /// </returns>
        public abstract bool IsInstanceOfType(Type t, object o);

        /// <summary>
        /// Returns the add accessor for the specified event, enabling NovaSharp to attach managed
        /// handlers across platforms.
        /// </summary>
        /// <param name="ei">Event whose add method should be returned.</param>
        /// <returns>The event add accessor.</returns>
        public abstract MethodInfo GetAddMethod(EventInfo ei);

        /// <summary>
        /// Returns the remove accessor for the specified event.
        /// </summary>
        /// <param name="ei">Event whose remove method should be returned.</param>
        /// <returns>The event remove accessor.</returns>
        public abstract MethodInfo GetRemoveMethod(EventInfo ei);

        /// <summary>
        /// Returns the getter accessor for the specified property.
        /// </summary>
        /// <param name="pi">Property to inspect.</param>
        /// <returns>The getter method for <paramref name="pi"/>.</returns>
        public abstract MethodInfo GetGetMethod(PropertyInfo pi);

        /// <summary>
        /// Returns the setter accessor for the specified property.
        /// </summary>
        /// <param name="pi">Property to inspect.</param>
        /// <returns>The setter method for <paramref name="pi"/>.</returns>
        public abstract MethodInfo GetSetMethod(PropertyInfo pi);

        /// <summary>
        /// Resolves an implemented interface by name using the active framework's lookup semantics.
        /// </summary>
        /// <param name="type">Type providing the interface implementation.</param>
        /// <param name="name">Simple interface name to look up.</param>
        /// <returns>The matching interface type or <see langword="null"/>.</returns>
        public abstract Type GetInterface(Type type, string name);

        /// <summary>
        /// Returns all properties declared on the supplied type.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <returns>An array of <see cref="PropertyInfo"/> instances.</returns>
        public abstract PropertyInfo[] GetProperties(Type type);

        /// <summary>
        /// Retrieves the named property from the supplied type using the platform's binding rules.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <param name="name">Property name to look up.</param>
        /// <returns>The matching property or <see langword="null"/>.</returns>
        public abstract PropertyInfo GetProperty(Type type, string name);

        /// <summary>
        /// Returns the nested types declared by the supplied type.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <returns>An array of nested types.</returns>
        public abstract Type[] GetNestedTypes(Type type);

        /// <summary>
        /// Returns the events declared by the supplied type.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <returns>An array of events.</returns>
        public abstract EventInfo[] GetEvents(Type type);

        /// <summary>
        /// Returns the constructors declared by the supplied type, including optionally non-public
        /// ones as dictated by the framework.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <returns>An array of constructors.</returns>
        public abstract ConstructorInfo[] GetConstructors(Type type);

        /// <summary>
        /// Enumerates the types defined in the provided assembly.
        /// </summary>
        /// <param name="assembly">Assembly to inspect.</param>
        /// <returns>An array of defined types.</returns>
        public abstract Type[] GetAssemblyTypes(Assembly assembly);

        /// <summary>
        /// Returns the methods declared by the supplied type.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <returns>An array of methods.</returns>
        public abstract MethodInfo[] GetMethods(Type type);

        /// <summary>
        /// Returns the fields declared by the supplied type.
        /// </summary>
        /// <param name="t">Type to inspect.</param>
        /// <returns>An array of fields.</returns>
        public abstract FieldInfo[] GetFields(Type t);

        /// <summary>
        /// Resolves the named method declared on the supplied type.
        /// </summary>
        /// <param name="type">Type to inspect.</param>
        /// <param name="name">Method name to locate.</param>
        /// <returns>The matching method or <see langword="null"/>.</returns>
        public abstract MethodInfo GetMethod(Type type, string name);

        /// <summary>
        /// Returns the generic type arguments for the supplied constructed type.
        /// </summary>
        /// <param name="t">Constructed generic type.</param>
        /// <returns>An array of generic argument types.</returns>
        public abstract Type[] GetGenericArguments(Type t);

        /// <summary>
        /// Determines whether <paramref name="current"/> can be assigned from
        /// <paramref name="toCompare"/> using the platform's type system rules.
        /// </summary>
        /// <param name="current">Target type.</param>
        /// <param name="toCompare">Candidate type.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="current"/> is assignable from
        /// <paramref name="toCompare"/>.
        /// </returns>
        public abstract bool IsAssignableFrom(Type current, Type toCompare);

        /// <summary>
        /// Determines whether the supplied object represents the platform-specific <c>DBNull</c>
        /// sentinel.
        /// </summary>
        /// <param name="o">Value to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when <paramref name="o"/> is the framework's DBNull instance.
        /// </returns>
        public abstract bool IsDbNull(object o);

        /// <summary>
        /// Resolves the named method with the specified signature from the provided type. This
        /// overload exists because some hosts only expose <see cref="Type.GetMethod(string, Type[])"/>
        /// via extension methods.
        /// </summary>
        /// <param name="resourcesType">Type that declares the method.</param>
        /// <param name="v">Name of the method to locate.</param>
        /// <param name="type">Parameter types that describe the signature.</param>
        /// <returns>The matching method or <see langword="null"/>.</returns>
        public abstract MethodInfo GetMethod(Type resourcesType, string v, Type[] type);
    }
}

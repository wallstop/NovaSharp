namespace NovaSharp.Interpreter.Compatibility.Frameworks
{
#if !NETFX_CORE || DOTNET_CORE

    using System;
    using System.Reflection;

    internal abstract class FrameworkClrBase : FrameworkReflectionBase
    {
        private readonly BindingFlags _bindingflagsMember =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        private readonly BindingFlags _bindingflagsInnerclass =
            BindingFlags.Public | BindingFlags.NonPublic;

        public override MethodInfo GetAddMethod(EventInfo ei)
        {
            return ei.GetAddMethod(true);
        }

        public override ConstructorInfo[] GetConstructors(Type type)
        {
            return GetTypeInfoFromType(type).GetConstructors(_bindingflagsMember);
        }

        public override EventInfo[] GetEvents(Type type)
        {
            return GetTypeInfoFromType(type).GetEvents(_bindingflagsMember);
        }

        public override FieldInfo[] GetFields(Type type)
        {
            return GetTypeInfoFromType(type).GetFields(_bindingflagsMember);
        }

        public override Type[] GetGenericArguments(Type type)
        {
            return GetTypeInfoFromType(type).GetGenericArguments();
        }

        public override MethodInfo GetGetMethod(PropertyInfo pi)
        {
            return pi.GetGetMethod(true);
        }

        public override Type[] GetInterfaces(Type t)
        {
            return GetTypeInfoFromType(t).GetInterfaces();
        }

        public override MethodInfo GetMethod(Type type, string name)
        {
            return GetTypeInfoFromType(type).GetMethod(name);
        }

        public override MethodInfo[] GetMethods(Type type)
        {
            return GetTypeInfoFromType(type).GetMethods(_bindingflagsMember);
        }

        public override Type[] GetNestedTypes(Type type)
        {
            return GetTypeInfoFromType(type).GetNestedTypes(_bindingflagsInnerclass);
        }

        public override PropertyInfo[] GetProperties(Type type)
        {
            return GetTypeInfoFromType(type).GetProperties(_bindingflagsMember);
        }

        public override PropertyInfo GetProperty(Type type, string name)
        {
            return GetTypeInfoFromType(type).GetProperty(name);
        }

        public override MethodInfo GetRemoveMethod(EventInfo ei)
        {
            return ei.GetRemoveMethod(true);
        }

        public override MethodInfo GetSetMethod(PropertyInfo pi)
        {
            return pi.GetSetMethod(true);
        }

        public override bool IsAssignableFrom(Type current, Type toCompare)
        {
            return GetTypeInfoFromType(current).IsAssignableFrom(toCompare);
        }

        public override bool IsInstanceOfType(Type t, object o)
        {
            return GetTypeInfoFromType(t).IsInstanceOfType(o);
        }

        public override MethodInfo GetMethod(Type resourcesType, string name, Type[] types)
        {
            return GetTypeInfoFromType(resourcesType).GetMethod(name, types);
        }

        public override Type[] GetAssemblyTypes(Assembly asm)
        {
            return asm.GetTypes();
        }
    }
}

#endif

namespace NovaSharp.Interpreter.Compatibility.Frameworks
{
#if DOTNET_CORE

    using NovaSharp.Interpreter.Compatibility.Frameworks.Base;
    using System;
    using System.Reflection;

    internal class FrameworkCurrent : FrameworkClrBase
    {
        public override Type GetInterface(Type type, string name)
        {
            return type.GetTypeInfo().GetInterface(name);
        }

        public override TypeInfo GetTypeInfoFromType(Type t)
        {
            return t.GetTypeInfo();
        }

        public override bool IsDbNull(object o)
        {
            return o != null && o.GetType().FullName.StartsWith("System.DBNull");
        }

        public override bool StringContainsChar(string str, char chr)
        {
            return str.Contains(chr);
        }
    }
#endif
}

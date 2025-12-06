namespace WallstopStudios.NovaSharp.Interpreter.Compatibility.Frameworks
{
#if DOTNET_CORE

    using System;
    using System.Reflection;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility.Frameworks.Base;

    /// <summary>
    /// .NET Core implementation that relies on <see cref="FrameworkClrBase"/> for the heavy lifting
    /// and fills in the remaining platform-specific behaviors such as <c>DBNull</c> detection.
    /// </summary>
    internal class FrameworkCurrent : FrameworkClrBase
    {
        /// <inheritdoc/>
        public override Type GetInterface(Type type, string name)
        {
            return type.GetTypeInfo().GetInterface(name);
        }

        /// <inheritdoc/>
        public override TypeInfo GetTypeInfoFromType(Type t)
        {
            return t.GetTypeInfo();
        }

        /// <inheritdoc/>
        public override bool IsDbNull(object o)
        {
            return o != null
                && o.GetType().FullName.StartsWith("System.DBNull", StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool StringContainsChar(string str, char chr)
        {
            return str?.Contains(chr, StringComparison.Ordinal) ?? false;
        }
    }
#endif
}

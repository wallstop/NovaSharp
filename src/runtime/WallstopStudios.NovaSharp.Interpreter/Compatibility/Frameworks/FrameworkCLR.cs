#if !(DOTNET_CORE || NETFX_CORE) && !PCL

using System;
using WallstopStudios.NovaSharp.Interpreter.Compatibility.Frameworks.Base;

namespace WallstopStudios.NovaSharp.Interpreter.Compatibility.Frameworks
{
    /// <summary>
    /// Classic .NET Framework implementation that treats <see cref="Type"/> as its own
    /// <c>TypeInfo</c> representation and relies on legacy reflection APIs.
    /// </summary>
    internal class FrameworkCurrent : FrameworkClrBase
    {
        /// <inheritdoc/>
        public override Type GetTypeInfoFromType(Type t)
        {
            return t;
        }

        /// <inheritdoc/>
        public override bool IsDbNull(object o)
        {
            return o != null && Convert.IsDBNull(o);
        }

        /// <inheritdoc/>
        public override bool StringContainsChar(string str, char chr)
        {
            return str.Contains(chr);
        }

        /// <inheritdoc/>
        public override Type GetInterface(Type type, string name)
        {
            return type.GetInterface(name);
        }
    }
}

#endif

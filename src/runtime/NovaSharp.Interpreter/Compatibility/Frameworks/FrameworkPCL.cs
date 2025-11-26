#if !(DOTNET_CORE || NETFX_CORE) && PCL

using System;
using System.Linq;
using NovaSharp.Interpreter.Compatibility.Frameworks.Base;

namespace NovaSharp.Interpreter.Compatibility.Frameworks
{
    /// <summary>
    /// Portable Class Library implementation that emulates the CLR surface but must manually search
    /// interfaces and string helpers because the legacy APIs are missing in this profile.
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
            return o != null && o.GetType().FullName.StartsWith("System.DBNull", StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override bool StringContainsChar(string str, char chr)
        {
            return str != null
                && str.IndexOf(chr.ToString(), StringComparison.Ordinal) >= 0;
        }

        /// <inheritdoc/>
        public override Type GetInterface(Type type, string name)
        {
            return type.GetInterfaces().FirstOrDefault(t => t.Name == name);
        }
    }
}

#endif

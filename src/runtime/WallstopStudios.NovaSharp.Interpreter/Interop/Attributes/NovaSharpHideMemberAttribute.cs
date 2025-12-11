namespace WallstopStudios.NovaSharp.Interpreter.Interop.Attributes
{
    using System;

    /// <summary>
    /// Lists a userdata member not to be exposed to scripts referencing it by name.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = true,
        AllowMultiple = true
    )]
    public sealed class NovaSharpHideMemberAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the member to be hidden.
        /// </summary>
        public string MemberName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpHideMemberAttribute"/> class.
        /// </summary>
        /// <param name="memberName">Name of the member to hide.</param>
        public NovaSharpHideMemberAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}

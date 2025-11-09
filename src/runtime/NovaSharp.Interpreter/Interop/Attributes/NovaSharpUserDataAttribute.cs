using System;

namespace NovaSharp.Interpreter
{
    /// <summary>
    /// Marks a type of automatic registration as userdata (which happens only if UserData.RegisterAssembly is called).
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct,
        Inherited = false,
        AllowMultiple = false
    )]
    public sealed class NovaSharpUserDataAttribute : Attribute
    {
        /// <summary>
        /// The interop access mode
        /// </summary>
        public InteropAccessMode AccessMode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpUserDataAttribute"/> class.
        /// </summary>
        public NovaSharpUserDataAttribute()
        {
            AccessMode = InteropAccessMode.Default;
        }
    }
}

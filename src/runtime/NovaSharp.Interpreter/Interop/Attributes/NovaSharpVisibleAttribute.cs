using System;

namespace NovaSharp.Interpreter.Interop
{
    /// <summary>
    /// Forces a class member visibility to scripts. Can be used to hide public members or to expose non-public ones.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method
            | AttributeTargets.Property
            | AttributeTargets.Field
            | AttributeTargets.Constructor
            | AttributeTargets.Event,
        Inherited = true,
        AllowMultiple = false
    )]
    public sealed class NovaSharpVisibleAttribute : Attribute
    {
        /// <summary>
        /// Gets a value indicating whether this <see cref="NovaSharpVisibleAttribute"/> is set to "visible".
        /// </summary>
        public bool Visible { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpVisibleAttribute"/> class.
        /// </summary>
        /// <param name="visible">if set to true the member will be exposed to scripts, if false the member will be hidden.</param>
        public NovaSharpVisibleAttribute(bool visible)
        {
            Visible = visible;
        }
    }
}

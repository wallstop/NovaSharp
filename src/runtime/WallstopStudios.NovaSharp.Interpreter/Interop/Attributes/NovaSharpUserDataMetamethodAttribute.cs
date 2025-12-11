namespace WallstopStudios.NovaSharp.Interpreter.Interop.Attributes
{
    using System;

    /// <summary>
    /// Marks a method as the handler of metamethods of a userdata type
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public sealed class NovaSharpUserDataMetamethodAttribute : Attribute
    {
        /// <summary>
        /// The metamethod name (like '__div', '__ipairs', etc.)
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpUserDataMetamethodAttribute"/> class.
        /// </summary>
        /// <param name="name">The metamethod name (like '__div', '__ipairs', etc.)</param>
        public NovaSharpUserDataMetamethodAttribute(string name)
        {
            Name = name;
        }
    }
}

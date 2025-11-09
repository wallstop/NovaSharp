using System;

namespace NovaSharp.Interpreter
{
    /// <summary>
    /// Marks a property as a configruation property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class NovaSharpPropertyAttribute : Attribute
    {
        /// <summary>
        /// The metamethod name (like '__div', '__ipairs', etc.)
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpPropertyAttribute"/> class.
        /// </summary>
        public NovaSharpPropertyAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NovaSharpPropertyAttribute"/> class.
        /// </summary>
        /// <param name="name">The name for this property</param>
        public NovaSharpPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}

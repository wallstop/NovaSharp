namespace WallstopStudios.NovaSharp.Interpreter.Modules
{
    using System;
    using global::NovaSharp;

    /// <summary>
    /// In a module type, mark methods or fields with this attribute to have them exposed as module functions.
    /// Methods must be static and return <see cref="LuaValue"/> with either a
    /// <see cref="CallbackArguments"/> or <see cref="CallbackArgumentsView"/> callback signature.
    /// Built-in module callbacks may be non-public.
    /// Fields must be static or const strings, with an anonymous Lua function inside.
    ///
    /// See <see cref="NovaSharpModuleAttribute"/> for more information about modules.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Field,
        Inherited = false,
        AllowMultiple = false
    )]
    public sealed class NovaSharpModuleMethodAttribute : Attribute
    {
        /// <summary>
        /// Initializes a module method whose Lua-visible names follow the standard variant rules.
        /// </summary>
        public NovaSharpModuleMethodAttribute() { }

        /// <summary>
        /// Initializes a built-in module method with one exact Lua-visible name.
        /// </summary>
        /// <param name="exactName">The sole Lua-visible name.</param>
        internal NovaSharpModuleMethodAttribute(string exactName)
        {
            if (string.IsNullOrEmpty(exactName))
            {
                throw new ArgumentException(
                    "An exact module method name is required.",
                    nameof(exactName)
                );
            }

            Name = exactName;
            UsesExactName = true;
        }

        /// <summary>
        /// Gets or sets the name of the function in the module (defaults to member name)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets whether registration must expose only <see cref="Name"/>.
        /// </summary>
        internal bool UsesExactName { get; }
    }
}

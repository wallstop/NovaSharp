namespace WallstopStudios.NovaSharp.Interpreter.Modules
{
    using System;

    /// <summary>
    /// In a module type, mark methods or fields with this attribute to have them exposed as module functions.
    /// Methods must have the signature "public static DynValue ...(ScriptExecutionContext, CallbackArguments)" or
    /// "public static DynValue ...(ScriptExecutionContext, CallbackArgumentsView)" or
    /// "public static DynValue ...(CallbackArgumentsView)".
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
        /// Gets or sets the name of the function in the module (defaults to member name)
        /// </summary>
        public string Name { get; set; }
    }
}

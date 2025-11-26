namespace NovaSharp.Interpreter.DataTypes
{
    using System;

    /// <summary>
    /// Controls how <see cref="DynValue" /> instances validate and coerce their underlying payload.
    /// </summary>
    [Flags]
    public enum TypeValidationOptions
    {
        /// <summary>
        /// Default behaviour (mutable, not to-be-closed).
        /// </summary>
        None = 0,

        /// <summary>
        /// Allows <c>nil</c> to satisfy the requested validation.
        /// </summary>
        AllowNil = 1,

        /// <summary>
        /// Allows automatic conversions between scalar CLR/Lua types when possible.
        /// </summary>
        AutoConvert = 2,
    }
}

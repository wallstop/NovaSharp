namespace NovaSharp.Interpreter.Interop.BasicDescriptors
{
    using System;

    /// <summary>
    /// Permissions for members access
    /// </summary>
    [Flags]
    public enum MemberDescriptorAccess
    {
        [Obsolete("Prefer explicit MemberDescriptorAccess combinations.", false)]
        None = 0,

        /// <summary>
        /// The member can be read from
        /// </summary>
        CanRead = 1 << 0,

        /// <summary>
        /// The member can be written to
        /// </summary>
        CanWrite = 1 << 1,

        /// <summary>
        /// The can be invoked
        /// </summary>
        CanExecute = 1 << 2,
    }
}

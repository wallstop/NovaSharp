namespace WallstopStudios.NovaSharp.Interpreter.Debugging
{
    using System;

    /// <summary>
    /// Enumeration of capabilities for a debugger
    /// </summary>
    [Flags]
    public enum DebuggerCaps
    {
        [Obsolete("Prefer explicit DebuggerCaps combinations.", false)]
        None = 0,

        /// <summary>
        /// Flag set if the debugger can debug source code
        /// </summary>
        CanDebugSourceCode = 1 << 0,

        /// <summary>
        /// Flag set if the can debug VM bytecode
        /// </summary>
        CanDebugByteCode = 1 << 1,

        /// <summary>
        /// Flag set if the debugger uses breakpoints based on lines instead of tokens
        /// </summary>
        HasLineBasedBreakpoints = 1 << 2,
    }
}

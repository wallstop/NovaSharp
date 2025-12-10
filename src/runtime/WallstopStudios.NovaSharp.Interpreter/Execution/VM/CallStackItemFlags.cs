namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    using System;

    /// <summary>
    /// Flags describing how a call stack frame was entered and how it should behave.
    /// </summary>
    [Flags]
    internal enum CallStackItemFlags
    {
        /// <summary>
        /// Default flag (unused); prefer explicit combinations.
        /// </summary>
        [Obsolete("Prefer explicit CallStackItemFlags combinations.", false)]
        None = 0,

        /// <summary>
        /// Marks the frame as an entry point.
        /// </summary>
        EntryPoint = 1 << 0,

        /// <summary>
        /// Marks the entry frame created when resuming a coroutine (bit 1).
        /// </summary>
        ResumeEntryPointBit = 1 << 1,

        /// <summary>
        /// Marks the frame created when a CLR caller invokes Lua code (bit 2).
        /// </summary>
        CallEntryPointBit = 1 << 2,

        /// <summary>
        /// Indicates that the call was compiled as a tail call.
        /// </summary>
        TailCall = 1 << 4,

        /// <summary>
        /// Indicates that the call originated from <c>object:method()</c> syntax (implicit self).
        /// </summary>
        MethodCall = 1 << 5,
    }

    /// <summary>
    /// Combined flag values for <see cref="CallStackItemFlags"/>.
    /// These are the recommended way to specify combined flags for call stack frames.
    /// </summary>
    internal static class CallStackItemFlagsPresets
    {
        /// <summary>
        /// Marks the entry frame created when resuming a coroutine.
        /// </summary>
        public const CallStackItemFlags ResumeEntryPoint =
            CallStackItemFlags.EntryPoint | CallStackItemFlags.ResumeEntryPointBit;

        /// <summary>
        /// Marks the frame created when a CLR caller invokes Lua code.
        /// </summary>
        public const CallStackItemFlags CallEntryPoint =
            CallStackItemFlags.EntryPoint | CallStackItemFlags.CallEntryPointBit;
    }
}

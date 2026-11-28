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
        /// Marks a CLR call entry that the classic library initiated on behalf of Lua code
        /// (bit 3). Errors from these calls follow Lua callback semantics and stay deferred
        /// with the coroutine until <c>coroutine.close</c>.
        /// </summary>
        LibraryCallEntryBit = 1 << 3,

        /// <summary>
        /// Marks a CLR call entry that the VM created to run a Lua close handler (bit 8).
        /// These entries are not catchable host boundaries for error decoration.
        /// </summary>
        VmCloseCallEntryBit = 1 << 8,

        /// <summary>
        /// Indicates that the call was compiled as a tail call.
        /// </summary>
        TailCall = 1 << 4,

        /// <summary>
        /// Indicates that the call originated from <c>object:method()</c> syntax (implicit self).
        /// </summary>
        MethodCall = 1 << 5,

        /// <summary>
        /// Adjusts the eventual operator metamethod result to one Lua value.
        /// </summary>
        SingleOperatorResult = 1 << 6,

        /// <summary>
        /// Carries a Lua 5.1 operator's non-yieldable boundary across nested calls.
        /// </summary>
        Lua51OperatorYieldBoundary = 1 << 7,
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

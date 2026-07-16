namespace WallstopStudios.NovaSharp.Interpreter.Execution.VM
{
    /// <summary>
    /// Initial and ceiling VM stack sizes. The stacks grow geometrically when deeper code needs more space,
    /// up to the configured ceiling, past which a deterministic Lua stack overflow error is raised.
    /// </summary>
    internal static class VmStackDefaults
    {
        public const int ValueStackInitialCapacity = 512;
        public const int ExecutionStackInitialCapacity = 64;

        /// <summary>
        /// Default value-slot ceiling per coroutine. Mirrors the reference interpreter's
        /// <c>LUAI_MAXSTACK</c> so realistic recursion succeeds while runaway recursion errors deterministically
        /// instead of exhausting host memory.
        /// </summary>
        public const int ValueStackMaxCapacity = 1_000_000;

        /// <summary>
        /// Default call-frame ceiling per coroutine. Bounds runaway recursion depth as a backstop to the
        /// value-stack ceiling.
        /// </summary>
        public const int ExecutionStackMaxCapacity = 1_000_000;
    }
}

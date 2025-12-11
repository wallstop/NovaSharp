namespace WallstopStudios.NovaSharp.Interpreter.Sandboxing
{
    using System;

    /// <summary>
    /// Identifies the type of sandbox limit that was exceeded.
    /// </summary>
    public enum SandboxViolationType
    {
        /// <summary>
        /// Legacy placeholder; prefer an explicit violation type.
        /// </summary>
        [Obsolete("Use a specific SandboxViolationType.", false)]
        Unknown = 0,

        /// <summary>
        /// Script executed more instructions than allowed by <see cref="SandboxOptions.MaxInstructions"/>.
        /// </summary>
        InstructionLimitExceeded = 1,

        /// <summary>
        /// Call stack depth exceeded <see cref="SandboxOptions.MaxCallStackDepth"/>.
        /// </summary>
        RecursionLimitExceeded = 2,

        /// <summary>
        /// Script attempted to access a restricted module or function.
        /// </summary>
        ModuleAccessDenied = 3,

        /// <summary>
        /// Script attempted to use a restricted Lua function (e.g., loadfile, dofile).
        /// </summary>
        FunctionAccessDenied = 4,

        /// <summary>
        /// Memory usage exceeded <see cref="SandboxOptions.MaxMemoryBytes"/>.
        /// </summary>
        MemoryLimitExceeded = 5,

        /// <summary>
        /// Coroutine count exceeded <see cref="SandboxOptions.MaxCoroutines"/>.
        /// </summary>
        CoroutineLimitExceeded = 6,
    }
}

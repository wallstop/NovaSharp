namespace NovaSharp.Interpreter.Sandboxing
{
    /// <summary>
    /// Identifies the type of sandbox limit that was exceeded.
    /// </summary>
    public enum SandboxViolationType
    {
        /// <summary>
        /// Script executed more instructions than allowed by <see cref="SandboxOptions.MaxInstructions"/>.
        /// </summary>
        InstructionLimitExceeded,

        /// <summary>
        /// Call stack depth exceeded <see cref="SandboxOptions.MaxCallStackDepth"/>.
        /// </summary>
        RecursionLimitExceeded,

        /// <summary>
        /// Script attempted to access a restricted module or function.
        /// </summary>
        ModuleAccessDenied,

        /// <summary>
        /// Script attempted to use a restricted Lua function (e.g., loadfile, dofile).
        /// </summary>
        FunctionAccessDenied,
    }
}

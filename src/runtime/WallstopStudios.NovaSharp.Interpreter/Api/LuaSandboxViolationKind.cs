namespace NovaSharp
{
    /// <summary>
    /// Identifies the sandbox rule or limit violated by a Lua script.
    /// </summary>
    public enum LuaSandboxViolationKind
    {
        /// <summary>
        /// The violation type was not available.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The script executed more instructions than the sandbox allows.
        /// </summary>
        InstructionLimitExceeded = 1,

        /// <summary>
        /// The script exceeded the configured Lua call stack depth.
        /// </summary>
        RecursionLimitExceeded = 2,

        /// <summary>
        /// The script attempted to access a restricted module.
        /// </summary>
        ModuleAccessDenied = 3,

        /// <summary>
        /// The script attempted to access a restricted function.
        /// </summary>
        FunctionAccessDenied = 4,

        /// <summary>
        /// The script exceeded the configured memory limit.
        /// </summary>
        MemoryLimitExceeded = 5,

        /// <summary>
        /// The script exceeded the configured coroutine count.
        /// </summary>
        CoroutineLimitExceeded = 6,
    }
}

namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    using System.Runtime.Serialization;
#endif

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Exception raised when Lua code violates configured sandbox limits or restrictions.
    /// </summary>
    public class LuaSandboxException : LuaRuntimeException
    {
        /// <summary>
        /// Initializes a new <see cref="LuaSandboxException"/> without a message.
        /// </summary>
        public LuaSandboxException() { }

        /// <summary>
        /// Initializes a new <see cref="LuaSandboxException"/> with a message.
        /// </summary>
        public LuaSandboxException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="LuaSandboxException"/> with a message and inner exception.
        /// </summary>
        public LuaSandboxException(string message, Exception innerException)
            : base(message, innerException) { }

        internal LuaSandboxException(SandboxViolationException innerException)
            : base(innerException)
        {
            SandboxViolationDetails details =
                innerException == null ? default : innerException.Details;
            ViolationKind = ToFacadeKind(details.Kind);
            ConfiguredLimit = details.LimitValue;
            ActualValue = details.ActualValue;
            DeniedAccessName = details.AccessName;
        }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        /// <summary>
        /// Initializes a new instance from serialized data.
        /// </summary>
        protected LuaSandboxException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif

        /// <summary>
        /// Gets the kind of sandbox violation.
        /// </summary>
        public LuaSandboxViolationKind ViolationKind { get; }

        /// <summary>
        /// Gets the configured limit that was exceeded, or 0 for access-denial violations.
        /// </summary>
        public long ConfiguredLimit { get; }

        /// <summary>
        /// Gets the observed value when the limit was exceeded, or 0 for access-denial violations.
        /// </summary>
        public long ActualValue { get; }

        /// <summary>
        /// Gets the denied module or function name, or null for limit violations.
        /// </summary>
        public string DeniedAccessName { get; }

        /// <summary>
        /// Gets whether this violation came from a configured numeric limit.
        /// </summary>
        public bool IsLimitViolation =>
            ViolationKind == LuaSandboxViolationKind.InstructionLimitExceeded
            || ViolationKind == LuaSandboxViolationKind.RecursionLimitExceeded
            || ViolationKind == LuaSandboxViolationKind.MemoryLimitExceeded
            || ViolationKind == LuaSandboxViolationKind.CoroutineLimitExceeded;

        /// <summary>
        /// Gets whether this violation came from denied module or function access.
        /// </summary>
        public bool IsAccessDenied =>
            ViolationKind == LuaSandboxViolationKind.ModuleAccessDenied
            || ViolationKind == LuaSandboxViolationKind.FunctionAccessDenied;

        private static LuaSandboxViolationKind ToFacadeKind(SandboxViolationType? kind)
        {
            switch (kind)
            {
                case SandboxViolationType.InstructionLimitExceeded:
                    return LuaSandboxViolationKind.InstructionLimitExceeded;
                case SandboxViolationType.RecursionLimitExceeded:
                    return LuaSandboxViolationKind.RecursionLimitExceeded;
                case SandboxViolationType.ModuleAccessDenied:
                    return LuaSandboxViolationKind.ModuleAccessDenied;
                case SandboxViolationType.FunctionAccessDenied:
                    return LuaSandboxViolationKind.FunctionAccessDenied;
                case SandboxViolationType.MemoryLimitExceeded:
                    return LuaSandboxViolationKind.MemoryLimitExceeded;
                case SandboxViolationType.CoroutineLimitExceeded:
                    return LuaSandboxViolationKind.CoroutineLimitExceeded;
                default:
                    return LuaSandboxViolationKind.Unknown;
            }
        }
    }
}

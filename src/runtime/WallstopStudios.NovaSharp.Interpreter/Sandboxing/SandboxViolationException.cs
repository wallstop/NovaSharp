namespace WallstopStudios.NovaSharp.Interpreter.Sandboxing
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    using System.Runtime.Serialization;
#endif

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Exception raised when a sandboxed script violates a configured limit.
    /// </summary>
    public class SandboxViolationException : ScriptRuntimeException
    {
        /// <summary>
        /// Initializes a new <see cref="SandboxViolationException"/> with no message.
        /// </summary>
        public SandboxViolationException()
            : base() { }

        /// <summary>
        /// Initializes a new <see cref="SandboxViolationException"/> with the specified message.
        /// </summary>
        /// <param name="message">Human-readable description of the violation.</param>
        public SandboxViolationException(string message)
            : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="SandboxViolationException"/> with message and inner exception.
        /// </summary>
        /// <param name="message">Human-readable description of the violation.</param>
        /// <param name="innerException">The exception that caused this violation.</param>
        public SandboxViolationException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Initializes a new <see cref="SandboxViolationException"/> with structured violation details.
        /// This is the preferred constructor for programmatic exception creation.
        /// </summary>
        /// <param name="details">The structured violation details.</param>
        public SandboxViolationException(SandboxViolationDetails details)
            : base(details.FormatMessage())
        {
            Details = details;
        }

        /// <summary>
        /// Initializes a new <see cref="SandboxViolationException"/> with violation type and details.
        /// </summary>
        /// <param name="violationType">The type of sandbox limit exceeded.</param>
        /// <param name="limit">The configured limit value.</param>
        /// <param name="current">The current value when the limit was exceeded.</param>
        public SandboxViolationException(
            SandboxViolationType violationType,
            long limit,
            long current
        )
            : base(FormatViolationMessage(violationType, limit, current))
        {
            Details = CreateLimitDetails(violationType, limit, current);
        }

        /// <summary>
        /// Initializes a new <see cref="SandboxViolationException"/> for module/function access denial.
        /// </summary>
        /// <param name="violationType">The type of access denial.</param>
        /// <param name="accessedName">The name of the module or function that was denied.</param>
        public SandboxViolationException(SandboxViolationType violationType, string accessedName)
            : base(FormatAccessDenialMessage(violationType, accessedName))
        {
            Details = CreateAccessDetails(violationType, accessedName);
        }

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
        /// <summary>
        /// Initializes a new instance from serialized data.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Streaming context.</param>
        protected SandboxViolationException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
#endif

        /// <summary>
        /// Gets the structured details of this sandbox violation.
        /// This property provides type-safe access to violation-specific information.
        /// </summary>
        public SandboxViolationDetails Details { get; }

        /// <summary>
        /// Gets the type of sandbox violation that occurred.
        /// </summary>
        public SandboxViolationType ViolationType => Details.Kind;

        /// <summary>
        /// Gets the configured limit that was exceeded, if applicable.
        /// Only valid when <see cref="Details"/>.<see cref="SandboxViolationDetails.IsLimitViolation"/> is <c>true</c>.
        /// </summary>
        public long ConfiguredLimit => Details.LimitValue;

        /// <summary>
        /// Gets the actual value when the limit was exceeded, if applicable.
        /// Only valid when <see cref="Details"/>.<see cref="SandboxViolationDetails.IsLimitViolation"/> is <c>true</c>.
        /// </summary>
        public long ActualValue => Details.ActualValue;

        /// <summary>
        /// Gets the name of the module or function that was denied access, if applicable.
        /// Only valid when <see cref="Details"/>.<see cref="SandboxViolationDetails.IsAccessDenial"/> is <c>true</c>.
        /// </summary>
        public string DeniedAccessName => Details.AccessName;

        private static SandboxViolationDetails CreateLimitDetails(
            SandboxViolationType violationType,
            long limit,
            long current
        )
        {
            switch (violationType)
            {
                case SandboxViolationType.InstructionLimitExceeded:
                    return SandboxViolationDetails.InstructionLimit(limit, current);
                case SandboxViolationType.RecursionLimitExceeded:
                    return SandboxViolationDetails.RecursionLimit((int)limit, (int)current);
                case SandboxViolationType.MemoryLimitExceeded:
                    return SandboxViolationDetails.MemoryLimit(limit, current);
                case SandboxViolationType.CoroutineLimitExceeded:
                    return SandboxViolationDetails.CoroutineLimit((int)limit, (int)current);
                default:
                    // Fallback for unknown types - use instruction limit format
                    return SandboxViolationDetails.InstructionLimit(limit, current);
            }
        }

        private static SandboxViolationDetails CreateAccessDetails(
            SandboxViolationType violationType,
            string accessedName
        )
        {
            switch (violationType)
            {
                case SandboxViolationType.ModuleAccessDenied:
                    return SandboxViolationDetails.ModuleAccessDenied(accessedName);
                case SandboxViolationType.FunctionAccessDenied:
                    return SandboxViolationDetails.FunctionAccessDenied(accessedName);
                default:
                    // Fallback for unknown types - use module access format
                    return SandboxViolationDetails.ModuleAccessDenied(accessedName);
            }
        }

        private static string FormatViolationMessage(
            SandboxViolationType violationType,
            long limit,
            long current
        )
        {
            switch (violationType)
            {
                case SandboxViolationType.InstructionLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: instruction limit exceeded (limit: {0}, executed: {1})",
                        limit,
                        current
                    );
                case SandboxViolationType.RecursionLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: recursion limit exceeded (limit: {0}, depth: {1})",
                        limit,
                        current
                    );
                case SandboxViolationType.MemoryLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: memory limit exceeded (limit: {0} bytes, used: {1} bytes)",
                        limit,
                        current
                    );
                case SandboxViolationType.CoroutineLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: coroutine limit exceeded (limit: {0}, count: {1})",
                        limit,
                        current
                    );
                default:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: {0}",
                        violationType
                    );
            }
        }

        private static string FormatAccessDenialMessage(
            SandboxViolationType violationType,
            string accessedName
        )
        {
            switch (violationType)
            {
                case SandboxViolationType.ModuleAccessDenied:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: access to module '{0}' is denied",
                        accessedName
                    );
                case SandboxViolationType.FunctionAccessDenied:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: access to function '{0}' is denied",
                        accessedName
                    );
                default:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: access to '{0}' is denied",
                        accessedName
                    );
            }
        }
    }
}

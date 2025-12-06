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
            ViolationType = violationType;
            ConfiguredLimit = limit;
            ActualValue = current;
        }

        /// <summary>
        /// Initializes a new <see cref="SandboxViolationException"/> for module/function access denial.
        /// </summary>
        /// <param name="violationType">The type of access denial.</param>
        /// <param name="accessedName">The name of the module or function that was denied.</param>
        public SandboxViolationException(SandboxViolationType violationType, string accessedName)
            : base(FormatAccessDenialMessage(violationType, accessedName))
        {
            ViolationType = violationType;
            DeniedAccessName = accessedName;
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
        /// Gets the type of sandbox violation that occurred.
        /// </summary>
        public SandboxViolationType ViolationType { get; }

        /// <summary>
        /// Gets the configured limit that was exceeded, if applicable.
        /// </summary>
        public long ConfiguredLimit { get; }

        /// <summary>
        /// Gets the actual value when the limit was exceeded, if applicable.
        /// </summary>
        public long ActualValue { get; }

        /// <summary>
        /// Gets the name of the module or function that was denied access, if applicable.
        /// </summary>
        public string DeniedAccessName { get; }

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

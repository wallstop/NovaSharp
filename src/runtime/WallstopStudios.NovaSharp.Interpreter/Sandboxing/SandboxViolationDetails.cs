namespace WallstopStudios.NovaSharp.Interpreter.Sandboxing
{
    using System;

#if !(PCL || ((!UNITY_EDITOR) && (ENABLE_DOTNET)) || NETFX_CORE)
    [Serializable]
#endif
    /// <summary>
    /// Provides detailed, structured information about a sandbox violation.
    /// Use the <see cref="Kind"/> property to determine which detail properties are valid.
    /// </summary>
    public readonly struct SandboxViolationDetails : IEquatable<SandboxViolationDetails>
    {
        /// <summary>
        /// Creates details for an instruction limit violation.
        /// </summary>
        /// <param name="limit">The configured instruction limit.</param>
        /// <param name="executed">The number of instructions executed when the limit was exceeded.</param>
        /// <returns>A new <see cref="SandboxViolationDetails"/> for instruction limit violations.</returns>
        public static SandboxViolationDetails InstructionLimit(long limit, long executed)
        {
            return new SandboxViolationDetails(
                SandboxViolationType.InstructionLimitExceeded,
                limit,
                executed,
                null
            );
        }

        /// <summary>
        /// Creates details for a recursion limit violation.
        /// </summary>
        /// <param name="limit">The configured call stack depth limit.</param>
        /// <param name="depth">The actual call stack depth when the limit was exceeded.</param>
        /// <returns>A new <see cref="SandboxViolationDetails"/> for recursion limit violations.</returns>
        public static SandboxViolationDetails RecursionLimit(int limit, int depth)
        {
            return new SandboxViolationDetails(
                SandboxViolationType.RecursionLimitExceeded,
                limit,
                depth,
                null
            );
        }

        /// <summary>
        /// Creates details for a memory limit violation.
        /// </summary>
        /// <param name="limit">The configured memory limit in bytes.</param>
        /// <param name="used">The actual memory usage in bytes when the limit was exceeded.</param>
        /// <returns>A new <see cref="SandboxViolationDetails"/> for memory limit violations.</returns>
        public static SandboxViolationDetails MemoryLimit(long limit, long used)
        {
            return new SandboxViolationDetails(
                SandboxViolationType.MemoryLimitExceeded,
                limit,
                used,
                null
            );
        }

        /// <summary>
        /// Creates details for a coroutine limit violation.
        /// </summary>
        /// <param name="limit">The configured maximum coroutine count.</param>
        /// <param name="count">The actual coroutine count when the limit was exceeded.</param>
        /// <returns>A new <see cref="SandboxViolationDetails"/> for coroutine limit violations.</returns>
        public static SandboxViolationDetails CoroutineLimit(int limit, int count)
        {
            return new SandboxViolationDetails(
                SandboxViolationType.CoroutineLimitExceeded,
                limit,
                count,
                null
            );
        }

        /// <summary>
        /// Creates details for a module access denial.
        /// </summary>
        /// <param name="moduleName">The name of the module that was denied.</param>
        /// <returns>A new <see cref="SandboxViolationDetails"/> for module access denials.</returns>
        public static SandboxViolationDetails ModuleAccessDenied(string moduleName)
        {
            return new SandboxViolationDetails(
                SandboxViolationType.ModuleAccessDenied,
                0,
                0,
                moduleName
            );
        }

        /// <summary>
        /// Creates details for a function access denial.
        /// </summary>
        /// <param name="functionName">The name of the function that was denied.</param>
        /// <returns>A new <see cref="SandboxViolationDetails"/> for function access denials.</returns>
        public static SandboxViolationDetails FunctionAccessDenied(string functionName)
        {
            return new SandboxViolationDetails(
                SandboxViolationType.FunctionAccessDenied,
                0,
                0,
                functionName
            );
        }

        private SandboxViolationDetails(
            SandboxViolationType kind,
            long limitValue,
            long actualValue,
            string accessName
        )
        {
            Kind = kind;
            LimitValue = limitValue;
            ActualValue = actualValue;
            AccessName = accessName;
        }

        /// <summary>
        /// Gets the kind of sandbox violation.
        /// </summary>
        public SandboxViolationType Kind { get; }

        /// <summary>
        /// Gets the configured limit value. Valid when <see cref="Kind"/> is
        /// <see cref="SandboxViolationType.InstructionLimitExceeded"/>,
        /// <see cref="SandboxViolationType.RecursionLimitExceeded"/>, or
        /// <see cref="SandboxViolationType.MemoryLimitExceeded"/>.
        /// </summary>
        public long LimitValue { get; }

        /// <summary>
        /// Gets the actual value when the limit was exceeded. Valid when <see cref="Kind"/> is
        /// <see cref="SandboxViolationType.InstructionLimitExceeded"/>,
        /// <see cref="SandboxViolationType.RecursionLimitExceeded"/>, or
        /// <see cref="SandboxViolationType.MemoryLimitExceeded"/>.
        /// </summary>
        public long ActualValue { get; }

        /// <summary>
        /// Gets the name of the denied module or function. Valid when <see cref="Kind"/> is
        /// <see cref="SandboxViolationType.ModuleAccessDenied"/> or
        /// <see cref="SandboxViolationType.FunctionAccessDenied"/>.
        /// </summary>
        public string AccessName { get; }

        /// <summary>
        /// Gets a value indicating whether this is a limit-based violation
        /// (instruction, recursion, memory, or coroutine limit).
        /// </summary>
        public bool IsLimitViolation =>
            Kind == SandboxViolationType.InstructionLimitExceeded
            || Kind == SandboxViolationType.RecursionLimitExceeded
            || Kind == SandboxViolationType.MemoryLimitExceeded
            || Kind == SandboxViolationType.CoroutineLimitExceeded;

        /// <summary>
        /// Gets a value indicating whether this is an access-denial violation
        /// (module or function access denied).
        /// </summary>
        public bool IsAccessDenial =>
            Kind == SandboxViolationType.ModuleAccessDenied
            || Kind == SandboxViolationType.FunctionAccessDenied;

        /// <summary>
        /// Formats a human-readable message describing this violation.
        /// </summary>
        /// <returns>A formatted message string.</returns>
        public string FormatMessage()
        {
            switch (Kind)
            {
                case SandboxViolationType.InstructionLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: instruction limit exceeded (limit: {0}, executed: {1})",
                        LimitValue,
                        ActualValue
                    );
                case SandboxViolationType.RecursionLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: recursion limit exceeded (limit: {0}, depth: {1})",
                        LimitValue,
                        ActualValue
                    );
                case SandboxViolationType.MemoryLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: memory limit exceeded (limit: {0} bytes, used: {1} bytes)",
                        LimitValue,
                        ActualValue
                    );
                case SandboxViolationType.CoroutineLimitExceeded:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: coroutine limit exceeded (limit: {0}, count: {1})",
                        LimitValue,
                        ActualValue
                    );
                case SandboxViolationType.ModuleAccessDenied:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: access to module '{0}' is denied",
                        AccessName ?? "(unknown)"
                    );
                case SandboxViolationType.FunctionAccessDenied:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: access to function '{0}' is denied",
                        AccessName ?? "(unknown)"
                    );
                default:
                    return string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Sandbox violation: {0}",
                        Kind
                    );
            }
        }

        /// <inheritdoc/>
        public override string ToString() => FormatMessage();

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SandboxViolationDetails other && Equals(other);
        }

        /// <inheritdoc/>
        public bool Equals(SandboxViolationDetails other)
        {
            return Kind == other.Kind
                && LimitValue == other.LimitValue
                && ActualValue == other.ActualValue
                && string.Equals(AccessName, other.AccessName, StringComparison.Ordinal);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Kind.GetHashCode();
                hash = (hash * 31) + LimitValue.GetHashCode();
                hash = (hash * 31) + ActualValue.GetHashCode();
                hash =
                    (hash * 31)
                    + (AccessName != null ? AccessName.GetHashCode(StringComparison.Ordinal) : 0);
                return hash;
            }
        }

        /// <summary>
        /// Determines whether two <see cref="SandboxViolationDetails"/> instances are equal.
        /// </summary>
        public static bool operator ==(SandboxViolationDetails left, SandboxViolationDetails right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="SandboxViolationDetails"/> instances are not equal.
        /// </summary>
        public static bool operator !=(SandboxViolationDetails left, SandboxViolationDetails right)
        {
            return !left.Equals(right);
        }
    }
}

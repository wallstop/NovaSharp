namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;
    using Cysharp.Text;

    /// <summary>
    /// Represents the result of a mod operation (load, unload, reload).
    /// </summary>
    public sealed class ModOperationResult
    {
        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the final state of the mod after the operation.
        /// </summary>
        public ModLoadState State { get; }

        /// <summary>
        /// Gets the exception that caused the operation to fail, if any.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Gets a human-readable message describing the result.
        /// </summary>
        public string Message { get; }

        private ModOperationResult(
            bool success,
            ModLoadState state,
            string message,
            Exception error
        )
        {
            Success = success;
            State = state;
            Message = message ?? string.Empty;
            Error = error;
        }

        /// <summary>
        /// Creates a successful operation result.
        /// </summary>
        /// <param name="state">The final mod state.</param>
        /// <param name="message">Optional message describing the result.</param>
        /// <returns>A successful <see cref="ModOperationResult"/>.</returns>
        public static ModOperationResult Succeeded(ModLoadState state, string message = null)
        {
            return new ModOperationResult(true, state, message, null);
        }

        /// <summary>
        /// Creates a failed operation result.
        /// </summary>
        /// <param name="state">The final mod state.</param>
        /// <param name="error">The exception that caused the failure.</param>
        /// <param name="message">Optional message describing the failure.</param>
        /// <returns>A failed <see cref="ModOperationResult"/>.</returns>
        public static ModOperationResult Failed(
            ModLoadState state,
            Exception error,
            string message = null
        )
        {
            return new ModOperationResult(
                false,
                state,
                message ?? error?.Message ?? "Operation failed",
                error
            );
        }

        /// <summary>
        /// Creates a failed operation result from an error message.
        /// </summary>
        /// <param name="state">The final mod state.</param>
        /// <param name="message">The error message.</param>
        /// <returns>A failed <see cref="ModOperationResult"/>.</returns>
        public static ModOperationResult Failed(ModLoadState state, string message)
        {
            return new ModOperationResult(false, state, message, null);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
            if (Success)
            {
                sb.Append("Success (State=");
                sb.Append(ModLoadStateStrings.GetName(State));
                sb.Append(')');
                if (!string.IsNullOrEmpty(Message))
                {
                    sb.Append(": ");
                    sb.Append(Message);
                }
                return sb.ToString();
            }

            sb.Append("Failed (State=");
            sb.Append(ModLoadStateStrings.GetName(State));
            sb.Append("): ");
            sb.Append(Message);
            if (Error != null)
            {
                sb.Append(" [");
                sb.Append(Error.GetType().Name);
                sb.Append(']');
            }
            return sb.ToString();
        }
    }
}

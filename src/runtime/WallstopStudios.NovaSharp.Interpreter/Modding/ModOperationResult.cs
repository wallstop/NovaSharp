namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;

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
            if (Success)
            {
                return string.IsNullOrEmpty(Message)
                    ? $"Success (State={State})"
                    : $"Success (State={State}): {Message}";
            }

            return Error != null
                ? $"Failed (State={State}): {Message} [{Error.GetType().Name}]"
                : $"Failed (State={State}): {Message}";
        }
    }
}

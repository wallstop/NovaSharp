namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;

    /// <summary>
    /// Event arguments for mod lifecycle events.
    /// </summary>
    public class ModEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the mod container that raised the event.
        /// </summary>
        public IModContainer ModContainer { get; }

        /// <summary>
        /// Gets the state of the mod at the time of the event.
        /// </summary>
        public ModLoadState State { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModEventArgs"/> class.
        /// </summary>
        /// <param name="modContainer">The mod container.</param>
        /// <param name="state">The current mod state.</param>
        public ModEventArgs(IModContainer modContainer, ModLoadState state)
        {
            ModContainer = modContainer ?? throw new ArgumentNullException(nameof(modContainer));
            State = state;
        }
    }

    /// <summary>
    /// Event arguments for mod error events.
    /// </summary>
    public sealed class ModErrorEventArgs : ModEventArgs
    {
        /// <summary>
        /// Gets the exception that caused the error.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// Gets a description of the operation that failed.
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModErrorEventArgs"/> class.
        /// </summary>
        /// <param name="modContainer">The mod container.</param>
        /// <param name="state">The current mod state.</param>
        /// <param name="error">The exception that caused the error.</param>
        /// <param name="operation">The operation that failed.</param>
        public ModErrorEventArgs(
            IModContainer modContainer,
            ModLoadState state,
            Exception error,
            string operation
        )
            : base(modContainer, state)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error));
            Operation = operation ?? string.Empty;
        }
    }
}

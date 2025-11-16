namespace NovaSharp.Interpreter.DataTypes
{
    using System;

    /// <summary>
    /// State of coroutines
    /// </summary>
    public enum CoroutineState
    {
        /// <summary>
        /// Legacy placeholder; prefer an explicit state.
        /// </summary>
        [Obsolete("Use a concrete CoroutineState.", false)]
        Unknown = 0,

        /// <summary>
        /// This is the main coroutine
        /// </summary>
        Main = 1,

        /// <summary>
        /// Coroutine has not started yet
        /// </summary>
        NotStarted = 2,

        /// <summary>
        /// Coroutine is suspended
        /// </summary>
        Suspended = 3,

        /// <summary>
        /// Coroutine has been forcefully suspended (i.e. auto-yielded)
        /// </summary>
        ForceSuspended = 4,

        /// <summary>
        /// Coroutine is running
        /// </summary>
        Running = 5,

        /// <summary>
        /// Coroutine has terminated
        /// </summary>
        Dead = 6,
    }
}

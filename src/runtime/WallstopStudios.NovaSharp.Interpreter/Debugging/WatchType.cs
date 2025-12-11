namespace WallstopStudios.NovaSharp.Interpreter.Debugging
{
    using System;

    /// <summary>
    /// Enumeration of the possible watch types
    /// </summary>
    public enum WatchType
    {
        /// <summary>
        /// A real variable watch
        /// </summary>
        [Obsolete("Use a specific WatchType.", false)]
        Unknown = 0,
        Watches = 1,

        /// <summary>
        /// The status of the v-stack
        /// </summary>
        VStack = 2,

        /// <summary>
        /// The call stack
        /// </summary>
        CallStack = 3,

        /// <summary>
        /// The list of coroutines
        /// </summary>
        Coroutines = 4,

        /// <summary>
        /// Topmost local variables
        /// </summary>
        Locals = 5,

        /// <summary>
        /// The list of currently active coroutines
        /// </summary>
        Threads = 6,

        /// <summary>
        /// The maximum value of this enum
        /// </summary>
        MaxValue = 7,
    }
}

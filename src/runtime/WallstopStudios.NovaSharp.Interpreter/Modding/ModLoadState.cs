namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;

    /// <summary>
    /// Represents the lifecycle state of a mod container.
    /// </summary>
    public enum ModLoadState
    {
        /// <summary>
        /// Default/invalid state - should not be used.
        /// </summary>
        [Obsolete("Use a specific ModLoadState.", false)]
        Unknown = 0,

        /// <summary>
        /// The mod has been registered but not yet loaded.
        /// </summary>
        Unloaded = 1,

        /// <summary>
        /// The mod is currently being loaded (script execution in progress).
        /// </summary>
        Loading = 2,

        /// <summary>
        /// The mod has been successfully loaded and is active.
        /// </summary>
        Loaded = 3,

        /// <summary>
        /// The mod is currently being unloaded.
        /// </summary>
        Unloading = 4,

        /// <summary>
        /// The mod is currently being reloaded.
        /// </summary>
        Reloading = 5,

        /// <summary>
        /// The mod failed to load due to an error.
        /// </summary>
        Faulted = 6,
    }
}

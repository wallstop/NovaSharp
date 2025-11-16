namespace NovaSharp.Interpreter.Platforms
{
    using System;

    /// <summary>
    /// Enumeration of standard file handles
    /// </summary>
    public enum StandardFileType
    {
        /// <summary>
        /// Legacy placeholder; prefer explicit file handles.
        /// </summary>
        [Obsolete("Use a concrete StandardFileType value.", false)]
        Unknown = 0,

        /// <summary>
        /// Standard Input
        /// </summary>
        StdIn = 1,

        /// <summary>
        /// Standard Output
        /// </summary>
        StdOut = 2,

        /// <summary>
        /// Standard Error Output
        /// </summary>
        StdErr = 3,
    }
}

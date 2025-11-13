namespace NovaSharp.Interpreter.Diagnostics
{
    using System;

    /// <summary>
    /// Enumeration of the possible performance counters
    /// </summary>
    public enum PerformanceCounter
    {
        /// <summary>
        /// Measures the time spent parsing the source creating the AST
        /// </summary>
        [Obsolete("Use a specific PerformanceCounter.", false)]
        Unknown = 0,
        AstCreation = 1,

        /// <summary>
        /// Measures the time spent converting ASTs in bytecode
        /// </summary>
        Compilation = 2,

        /// <summary>
        /// Measures the time spent in executing scripts
        /// </summary>
        Execution = 3,

        /// <summary>
        /// Measures the on the fly creation/compilation of functions in userdata descriptors
        /// </summary>
        AdaptersCompilation = 4,

        /// <summary>
        /// Sentinel value to get the enum size
        /// </summary>
        LastValue = 5,
    }
}

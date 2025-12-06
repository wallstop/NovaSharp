namespace WallstopStudios.NovaSharp.Cli
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter;

    /// <summary>
    /// Captures the state shared between CLI commands and the REPL loop.
    /// </summary>
    internal class ShellContext
    {
        private bool _exitRequested;

        /// <summary>
        /// Gets the script instance currently owned by the REPL.
        /// </summary>
        public Script Script { get; }

        /// <summary>
        /// Gets a value indicating whether a command requested interpreter exit.
        /// </summary>
        public bool IsExitRequested
        {
            get { return _exitRequested; }
        }

        /// <summary>
        /// Gets the exit code that should be used when terminating the process.
        /// </summary>
        public int ExitCode { get; private set; }

        /// <summary>
        /// Initializes a new shell context for the provided <paramref name="script"/>.
        /// </summary>
        /// <param name="script">Active script instance.</param>
        public ShellContext(Script script)
        {
            Script = script ?? throw new ArgumentNullException(nameof(script));
        }

        /// <summary>
        /// Requests termination of the REPL loop using the provided exit code.
        /// </summary>
        /// <param name="exitCode">Process exit code.</param>
        public void RequestExit(int exitCode = 0)
        {
            _exitRequested = true;
            ExitCode = exitCode;
        }
    }
}

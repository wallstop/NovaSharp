namespace NovaSharp
{
    using System;
    using NovaSharp.Interpreter;

    public class ShellContext
    {
        private bool _exitRequested;

        public Script Script { get; }

        public bool IsExitRequested
        {
            get { return _exitRequested; }
        }

        public int ExitCode { get; private set; }

        public ShellContext(Script script)
        {
            Script = script ?? throw new ArgumentNullException(nameof(script));
        }

        public void RequestExit(int exitCode = 0)
        {
            _exitRequested = true;
            ExitCode = exitCode;
        }
    }
}

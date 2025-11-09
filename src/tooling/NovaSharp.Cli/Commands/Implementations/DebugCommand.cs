namespace NovaSharp.Commands.Implementations
{
    using System;
    using System.Diagnostics;
    using RemoteDebugger;

    internal sealed class DebugCommand : ICommand
    {
        private RemoteDebuggerService _debugger;

        public string Name
        {
            get { return "debug"; }
        }

        public void DisplayShortHelp()
        {
            Console.WriteLine("debug - Starts the interactive debugger");
        }

        public void DisplayLongHelp()
        {
            Console.WriteLine(
                "debug - Starts the interactive debugger. Requires a web browser with Flash installed."
            );
        }

        public void Execute(ShellContext context, string arguments)
        {
            if (_debugger == null)
            {
                _debugger = new RemoteDebuggerService();
                _debugger.Attach(context.Script, "NovaSharp REPL interpreter", false);
                Process.Start(_debugger.HttpUrlStringLocalHost);
            }
        }
    }
}

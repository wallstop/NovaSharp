namespace NovaSharp.Commands.Implementations
{
    using System;
    using System.Diagnostics;
    using RemoteDebugger;

    internal sealed class DebugCommand : ICommand
    {
        internal static Func<IRemoteDebuggerBridge> DebuggerFactory { get; set; } =
            () => new RemoteDebuggerServiceBridge();

        internal static Action<string> BrowserLauncher { get; set; } =
            url =>
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }

                Process.Start(url);
            };

        private IRemoteDebuggerBridge _debugger;

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
                _debugger = DebuggerFactory();
                _debugger.Attach(context.Script, "NovaSharp REPL interpreter", false);
                string url = _debugger.HttpUrlStringLocalHost;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    BrowserLauncher(url);
                }
            }
        }
    }
}

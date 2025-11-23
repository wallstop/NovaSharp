namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using NovaSharp.Interpreter.Compatibility;
    using RemoteDebugger;

    internal sealed class DebugCommand : ICommand
    {
        internal static Func<IRemoteDebuggerBridge> DebuggerFactory { get; set; } =
            () => new RemoteDebuggerServiceBridge();

        internal static IBrowserLauncher BrowserLauncher { get; set; } =
            ProcessBrowserLauncher.Instance;

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
                LuaCompatibilityProfile profile = context?.Script?.CompatibilityProfile;
                if (profile != null)
                {
                    Console.WriteLine(
                        $"[compatibility] Debugger session running under {profile.GetFeatureSummary()}"
                    );
                }

                _debugger = DebuggerFactory();
                _debugger.Attach(context.Script, "NovaSharp REPL interpreter", false);
                string url = _debugger.HttpUrlStringLocalHost;
                if (
                    !string.IsNullOrWhiteSpace(url)
                    && Uri.TryCreate(url, UriKind.Absolute, out Uri parsed)
                )
                {
                    BrowserLauncher?.Launch(parsed);
                }
            }
        }
    }
}

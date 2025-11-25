namespace NovaSharp.Cli.Commands.Implementations
{
    using System;
    using NovaSharp.Cli;
    using NovaSharp.Interpreter.Compatibility;
    using RemoteDebugger;

    /// <summary>
    /// CLI command that attaches the NovaSharp remote debugger to the current REPL script.
    /// </summary>
    internal sealed class DebugCommand : ICommand
    {
        /// <summary>
        /// Factory used to create debugger bridge instances; overridden in tests.
        /// </summary>
        internal static Func<IRemoteDebuggerBridge> DebuggerFactory { get; set; } =
            () => new RemoteDebuggerServiceBridge();

        /// <summary>
        /// Browser launcher invoked after the debugger is attached; overridable for tests/hosts.
        /// </summary>
        internal static IBrowserLauncher BrowserLauncher { get; set; } =
            ProcessBrowserLauncher.Instance;

        private IRemoteDebuggerBridge _debugger;

        /// <inheritdoc />
        public string Name
        {
            get { return "debug"; }
        }

        /// <inheritdoc />
        public void DisplayShortHelp()
        {
            Console.WriteLine(CliMessages.DebugCommandShortHelp);
        }

        /// <inheritdoc />
        public void DisplayLongHelp()
        {
            Console.WriteLine(CliMessages.DebugCommandLongHelp);
        }

        /// <inheritdoc />
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
                Uri url = _debugger.HttpUrlStringLocalHost;
                if (url != null)
                {
                    BrowserLauncher?.Launch(url);
                }
            }
        }
    }
}

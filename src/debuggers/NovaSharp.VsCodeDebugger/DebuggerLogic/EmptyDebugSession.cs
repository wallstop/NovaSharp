namespace NovaSharp.VsCodeDebugger.DebuggerLogic
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    using System;
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using SDK;

    internal sealed class EmptyDebugSession : DebugSession
    {
        private readonly NovaSharpVsCodeDebugServer _server;

        internal EmptyDebugSession(NovaSharpVsCodeDebugServer server)
            : base(true, false)
        {
            _server = server;
        }

        public override void Initialize(Response response, Table args)
        {
#if DOTNET_CORE
            SendText(
                "Connected to NovaSharp {0} [{1}]",
                Script.VERSION,
                Script.GlobalOptions.Platform.GetPlatformName()
            );
#else
            SendText(
                "Connected to NovaSharp {0} [{1}] on process {2} (PID {3})",
                Script.VERSION,
                Script.GlobalOptions.Platform.GetPlatformName(),
                System.Diagnostics.Process.GetCurrentProcess().ProcessName,
                System.Diagnostics.Process.GetCurrentProcess().Id
            );
#endif

            SendText(
                "No script is set as default for debugging; use the debug console to select the script to debug.\n"
            );

            SendList();

            SendResponse(
                response,
                new Capabilities()
                {
                    // This debug adapter does not need the configurationDoneRequest.
                    SupportsConfigurationDoneRequest = false,

                    // This debug adapter does not support function breakpoints.
                    SupportsFunctionBreakpoints = false,

                    // This debug adapter doesn't support conditional breakpoints.
                    SupportsConditionalBreakpoints = false,

                    // This debug adapter does not support a side effect free evaluate request for data hovers.
                    SupportsEvaluateForHovers = false,

                    // This debug adapter does not support exception breakpoint filters
                    ExceptionBreakpointFilters = Array.Empty<object>(),
                }
            );

            // Debugger is ready to accept breakpoints immediately
            SendEvent(new InitializedEvent());
        }

        private void SendList()
        {
            int currId = _server.CurrentId ?? -1000;

            SendText("==========================================================");

            foreach (KeyValuePair<int, string> pair in _server.GetAttachedDebuggersByIdAndName())
            {
                string isdef = (pair.Key == currId) ? " (default)" : "";
                SendText("{0} : {1}{2}", pair.Key.ToString().PadLeft(9), pair.Value, isdef);
            }
            SendText("");
            SendText("Type the number of the script to debug, or '!' to refresh");
        }

        public override void Attach(Response response, Table arguments)
        {
            SendResponse(response);
        }

        public override void Continue(Response response, Table arguments)
        {
            SendList();
            SendResponse(response);
        }

        public override void Disconnect(Response response, Table arguments)
        {
            SendResponse(response);
        }

        private static string GetString(Table args, string property, string dflt = null)
        {
            string s = (string)args[property];
            if (s == null)
            {
                return dflt;
            }
            s = s.Trim();
            if (s.Length == 0)
            {
                return dflt;
            }
            return s;
        }

        public override void Evaluate(Response response, Table args)
        {
            string expression = GetString(args, "expression");
            string context = GetString(args, "context") ?? "hover";

            if (context == "repl")
            {
                ExecuteRepl(expression);
            }

            SendResponse(response);
        }

        private void ExecuteRepl(string cmd)
        {
            if (int.TryParse(cmd, out int id))
            {
                _server.CurrentId = id;

                SendText("Re-attach the debugger to debug the selected script.");

                Unbind();
            }
            else
            {
                SendList();
            }
        }

        public override void Launch(Response response, Table arguments)
        {
            SendResponse(response);
        }

        public override void Next(Response response, Table arguments)
        {
            SendList();
            SendResponse(response);
        }

        public override void Pause(Response response, Table arguments)
        {
            SendList();
            SendResponse(response);
        }

        public override void Scopes(Response response, Table arguments)
        {
            SendResponse(response);
        }

        public override void SetBreakpoints(Response response, Table args)
        {
            SendResponse(response);
        }

        public override void StackTrace(Response response, Table args)
        {
            SendResponse(response);
        }

        public override void StepIn(Response response, Table arguments)
        {
            SendList();
            SendResponse(response);
        }

        public override void StepOut(Response response, Table arguments)
        {
            SendList();
            SendResponse(response);
        }

        public override void Threads(Response response, Table arguments)
        {
            List<Thread> threads = new() { new Thread(0, "Main Thread") };
            SendResponse(response, new ThreadsResponseBody(threads));
        }

        public override void Variables(Response response, Table arguments)
        {
            SendResponse(response);
        }

        private void SendText(string msg, params object[] args)
        {
            msg = FormatString(msg, args);
            SendEvent(new OutputEvent("console", msg + "\n"));
        }

        public void Unbind()
        {
            SendText("Bye.");
            SendEvent(new TerminatedEvent());
        }

        private static string FormatString(string format, object[] args)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            if (args == null || args.Length == 0)
            {
                return format;
            }

            return string.Format(format, args);
        }
    }
}
#endif

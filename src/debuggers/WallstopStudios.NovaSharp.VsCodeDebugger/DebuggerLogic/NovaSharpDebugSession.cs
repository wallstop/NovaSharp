namespace WallstopStudios.NovaSharp.VsCodeDebugger.DebuggerLogic
{
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Debugging;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;
    using SDK;

    /// <summary>
    /// Full-featured VS Code debug adapter session that proxies runtime state via <see cref="AsyncDebugger"/>.
    /// </summary>
    internal sealed class NovaSharpDebugSession : DebugSession, IAsyncDebuggerClient
    {
        private readonly AsyncDebugger _debug;
        private readonly NovaSharpVsCodeDebugServer _server;
        private readonly List<DynValue> _variables = new();
        private bool _notifyExecutionEnd;

        private const int ScopeLocals = 65536;
        private const int ScopeSelf = 65537;

        /// <summary>
        /// Initializes a new VS Code session bound to the specified debugger instance.
        /// </summary>
        internal NovaSharpDebugSession(NovaSharpVsCodeDebugServer server, AsyncDebugger debugger)
            : base(true, false)
        {
            _server = server;
            _debug = debugger;
        }

        /// <summary>
        /// Sends handshake messages, reports platform information, and binds this session to the async debugger.
        /// </summary>
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
                "Debugging script '{0}'; use the debug console to debug another script.",
                _debug.Name
            );

            SendText("Type '!help' in the Debug Console for available Commands.");

            LuaCompatibilityProfile profile = _debug.Script?.CompatibilityProfile;
            if (profile != null)
            {
                SendText(
                    "[compatibility] Debugger session running under {0}",
                    profile.GetFeatureSummary()
                );
            }

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

            _debug.Client = this;
        }

        /// <summary>
        /// Acknowledges attach requests (session is already wired by the server).
        /// </summary>
        public override void Attach(Response response, Table Arguments)
        {
            SendResponse(response);
        }

        /// <summary>
        /// Resumes script execution when VS Code issues a Continue request.
        /// </summary>
        public override void ContinueExecution(Response response, Table Arguments)
        {
            _debug.QueueAction(
                new DebuggerAction(_debug.Script?.TimeProvider)
                {
                    Action = DebuggerAction.ActionType.Run,
                }
            );
            SendResponse(response);
        }

        /// <summary>
        /// Detaches the session from the async debugger without tearing down the server.
        /// </summary>
        public override void Disconnect(Response response, Table Arguments)
        {
            _debug.Client = null;
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

        /// <summary>
        /// Handles watch/hover/repl evaluations issued by VS Code.
        /// </summary>
        public override void Evaluate(Response response, Table args)
        {
            string expression = GetString(args, "expression");
            int frameId = GetInt(args, "frameId", 0);
            string context = GetString(args, "context") ?? "hover";

            if (frameId != 0 && context != "repl")
            {
                SendText(
                    "Warning : Evaluation of variables/watches is always done with the top-level scope."
                );
            }

            if (
                context == "repl"
                && !string.IsNullOrEmpty(expression)
                && expression.StartsWith('!')
            )
            {
                ExecuteRepl(expression.Substring(1));
                SendResponse(response);
                return;
            }

            DynValue v = _debug.Evaluate(expression) ?? DynValue.Nil;
            _variables.Add(v);

            SendResponse(
                response,
                new EvaluateResponseBody(v.ToDebugPrintString(), _variables.Count - 1)
                {
                    Type = v.Type.ToLuaDebuggerString(),
                }
            );
        }

        private void ExecuteRepl(string cmd)
        {
            ReadOnlySpan<char> commandSpan = cmd.AsSpan().TrimWhitespace();
            string commandText = commandSpan.Length == cmd.Length ? cmd : new string(commandSpan);
            bool showHelp = false;

            if (commandSpan.Equals("help".AsSpan(), StringComparison.Ordinal))
            {
                showHelp = true;
            }
            else if (commandSpan.StartsWith("geterror".AsSpan(), StringComparison.Ordinal))
            {
                SendText("Current error regex : {0}", _debug.ErrorRegex.ToString());
            }
            else if (commandSpan.StartsWith("seterror".AsSpan(), StringComparison.Ordinal))
            {
                ReadOnlySpan<char> regexSpan = commandSpan
                    .Slice("seterror".Length)
                    .TrimWhitespace();
                string regex = regexSpan.Length == 0 ? string.Empty : new string(regexSpan);

                try
                {
                    Regex rx = new(regex);
                    _debug.ErrorRegex = rx;
                    SendText("Current error regex : {0}", _debug.ErrorRegex.ToString());
                }
                catch (ArgumentException ex)
                {
                    SendText("Error setting regex: {0}", ex.Message);
                }
            }
            else if (commandSpan.StartsWith("execendnotify".AsSpan(), StringComparison.Ordinal))
            {
                ReadOnlySpan<char> valueSpan = commandSpan
                    .Slice("execendnotify".Length)
                    .TrimWhitespace();

                if (!valueSpan.IsEmpty)
                {
                    if (valueSpan.Equals("off".AsSpan(), StringComparison.Ordinal))
                    {
                        _notifyExecutionEnd = false;
                    }
                    else if (valueSpan.Equals("on".AsSpan(), StringComparison.Ordinal))
                    {
                        _notifyExecutionEnd = true;
                    }
                    else
                    {
                        SendText("Error : expected 'on' or 'off'");
                    }
                }

                SendText(
                    "Notifications of execution end are : {0}",
                    _notifyExecutionEnd ? "enabled" : "disabled"
                );
            }
            else if (commandSpan.Equals("list".AsSpan(), StringComparison.Ordinal))
            {
                int currId = _server.CurrentId ?? -1000;

                foreach (
                    KeyValuePair<int, string> pair in _server.GetAttachedDebuggersByIdAndName()
                )
                {
                    string isthis = (pair.Key == _debug.Id) ? " (this)" : string.Empty;
                    string isdef = (pair.Key == currId) ? " (default)" : string.Empty;

                    SendText(
                        "{0} : {1}{2}{3}",
                        pair.Key.ToString(CultureInfo.InvariantCulture).PadLeft(9),
                        pair.Value,
                        isdef,
                        isthis
                    );
                }
            }
            else if (
                commandSpan.StartsWith("select".AsSpan(), StringComparison.Ordinal)
                || commandSpan.StartsWith("switch".AsSpan(), StringComparison.Ordinal)
            )
            {
                bool isSwitch = commandSpan.StartsWith("switch".AsSpan(), StringComparison.Ordinal);
                ReadOnlySpan<char> idSpan = commandSpan.Slice("switch".Length).TrimWhitespace();
                string idText = idSpan.Length == 0 ? string.Empty : new string(idSpan);

                try
                {
                    int id = int.Parse(idText, CultureInfo.InvariantCulture);
                    _server.CurrentId = id;

                    if (isSwitch)
                    {
                        Unbind();
                    }
                    else
                    {
                        SendText(
                            "Next time you'll attach the debugger, it will be atteched to script #{0}",
                            id
                        );
                    }
                }
                catch (FormatException ex)
                {
                    SendText("Error selecting debugger: {0}", ex.Message);
                }
                catch (OverflowException ex)
                {
                    SendText("Error selecting debugger: {0}", ex.Message);
                }
            }
            else
            {
                SendText("Syntax error : {0}\n", commandText);
                showHelp = true;
            }

            if (showHelp)
            {
                SendText("Available Commands : ");
                SendText("    !help - gets this help");
                SendText("    !list - lists the other scripts which can be debugged");
                SendText("    !select <id> - select another script for future sessions");
                SendText(
                    "    !switch <id> - switch to another script (same as select + disconnect)"
                );
                SendText("    !seterror <regex> - sets the regex which tells which errors to trap");
                SendText(
                    "    !geterror - gets the current value of the regex which tells which errors to trap"
                );
                SendText(
                    "    !execendnotify [on|off] - sets the notification of end of execution on or off (default = off)"
                );
                SendText("    ... or type an expression to evaluate it on the fly.");
            }
        }

        /// <summary>
        /// Launch is a no-op because scripts are already running; VS Code expects an acknowledgement.
        /// </summary>
        public override void Launch(Response response, Table Arguments)
        {
            SendResponse(response);
        }

        /// <summary>
        /// Performs a step-over action in response to a Next request.
        /// </summary>
        public override void StepOver(Response response, Table Arguments)
        {
            _debug.QueueAction(
                new DebuggerAction(_debug.Script?.TimeProvider)
                {
                    Action = DebuggerAction.ActionType.StepOver,
                }
            );
            SendResponse(response);
        }

        private static StoppedEvent CreateStoppedEvent(string reason, string text = null)
        {
            return new StoppedEvent(0, reason, text);
        }

        /// <summary>
        /// Requests a pause so the runtime stops on the next statement.
        /// </summary>
        public override void Pause(Response response, Table Arguments)
        {
            _debug.PauseRequested = true;
            SendResponse(response);
            SendText("Pause pending -- will pause at first script statement.");
        }

        /// <summary>
        /// Returns the logical scopes (locals/self) for the paused stack frame.
        /// </summary>
        public override void Scopes(Response response, Table Arguments)
        {
            List<Scope> scopes = new();

            scopes.Add(new Scope("Locals", ScopeLocals));
            scopes.Add(new Scope("Self", ScopeSelf));

            SendResponse(response, new ScopesResponseBody(scopes));
        }

        /// <summary>
        /// Applies breakpoints for the specified source file.
        /// </summary>
        public override void SetBreakpoints(Response response, Table args)
        {
            string path = null;

            if (args["source"] is Table argsSource)
            {
                string p = argsSource["path"].ToString();
                if (p != null && p.Trim().Length > 0)
                {
                    path = p;
                }
            }

            if (path == null)
            {
                SendErrorResponse(
                    response,
                    3010,
                    "setBreakpoints: property 'source' is empty or malformed",
                    null,
                    false,
                    true
                );
                return;
            }

            path = ConvertClientPathToDebugger(path);

            SourceCode src = _debug.FindSourceByName(path);

            if (src == null)
            {
                // we only support breakpoints in files mono can handle
                SendResponse(response, new SetBreakpointsResponseBody());
                return;
            }

            Table clientLines = args.Get("lines").Table;

            HashSet<int> lin = new(
                clientLines
                    .Values.Select(jt => ConvertClientLineToDebugger(jt.ToObject<int>()))
                    .ToArray()
            );

            HashSet<int> lin2 = _debug.DebugService.ResetBreakpoints(src, lin);

            List<Breakpoint> breakpoints = new();
            foreach (int l in lin)
            {
                breakpoints.Add(new Breakpoint(lin2.Contains(l), l));
            }

            response.SetBody(new SetBreakpointsResponseBody(breakpoints));
            SendResponse(response);
        }

        /// <summary>
        /// Converts the runtime call stack into VS Code stack frames.
        /// </summary>
        public override void StackTrace(Response response, Table args)
        {
            int maxLevels = GetInt(args, "levels", 10);
            //int threadReference = getInt(args, "threadId", 0);

            List<StackFrame> stackFrames = new();

            List<WatchItem> stack = _debug.GetWatches(WatchType.CallStack);

            WatchItem coroutine = _debug.GetWatches(WatchType.Threads).LastOrDefault();

            int level = 0;
            int max = Math.Min(maxLevels - 3, stack.Count);

            while (level < max)
            {
                WatchItem frame = stack[level];

                string name = frame.Name;
                SourceRef sourceRef = frame.Location ?? _defaultSourceRef;
                int sourceIdx = sourceRef.SourceIdx;
                string path = sourceRef.IsClrLocation
                    ? "(native)"
                    : (_debug.GetSourceFile(sourceIdx) ?? "???");
                string sourceName = Path.GetFileName(path);

                Source source = new(sourceName, path); // ConvertDebuggerPathToClient(path));

                stackFrames.Add(
                    new StackFrame(
                        level,
                        name,
                        source,
                        ConvertDebuggerLineToClient(sourceRef.FromLine),
                        sourceRef.FromChar,
                        ConvertDebuggerLineToClient(sourceRef.ToLine),
                        sourceRef.ToChar
                    )
                );

                level++;
            }

            if (stack.Count > maxLevels - 3)
            {
                stackFrames.Add(new StackFrame(level++, "(...)", null, 0));
            }

            if (coroutine != null)
            {
                stackFrames.Add(new StackFrame(level++, "(" + coroutine.Name + ")", null, 0));
            }
            else
            {
                stackFrames.Add(new StackFrame(level++, "(main coroutine)", null, 0));
            }

            stackFrames.Add(new StackFrame(level++, "(native)", null, 0));

            SendResponse(response, new StackTraceResponseBody(stackFrames));
        }

        private readonly SourceRef _defaultSourceRef = new(-1, 0, 0, 0, 0, false);

        private static int GetInt(Table args, string propName, int defaultValue)
        {
            DynValue jo = args.Get(propName);

            if (jo.Type != DataType.Number)
            {
                return defaultValue;
            }
            else
            {
                return jo.ToObject<int>();
            }
        }

        /// <summary>
        /// Steps into the next function call.
        /// </summary>
        public override void StepIn(Response response, Table Arguments)
        {
            _debug.QueueAction(
                new DebuggerAction(_debug.Script?.TimeProvider)
                {
                    Action = DebuggerAction.ActionType.StepIn,
                }
            );
            SendResponse(response);
        }

        /// <summary>
        /// Steps out of the current function.
        /// </summary>
        public override void StepOut(Response response, Table Arguments)
        {
            _debug.QueueAction(
                new DebuggerAction(_debug.Script?.TimeProvider)
                {
                    Action = DebuggerAction.ActionType.StepOut,
                }
            );
            SendResponse(response);
        }

        /// <summary>
        /// Reports the logical thread list (single main thread today).
        /// </summary>
        public override void Threads(Response response, Table Arguments)
        {
            List<Thread> threads = new() { new Thread(0, "Main Thread") };
            SendResponse(response, new ThreadsResponseBody(threads));
        }

        /// <summary>
        /// Returns locals/self/expanded variable content for the identifiers requested by VS Code.
        /// </summary>
        public override void Variables(Response response, Table Arguments)
        {
            int index = GetInt(Arguments, "variablesReference", -1);

            List<Variable> variables = new();

            if (index == ScopeSelf)
            {
                DynValue v = _debug.Evaluate("self");
                VariableInspector.InspectVariable(v, variables);
            }
            else if (index == ScopeLocals)
            {
                foreach (WatchItem w in _debug.GetWatches(WatchType.Locals))
                {
                    variables.Add(
                        new Variable(w.Name, (w.Value ?? DynValue.Void).ToDebugPrintString())
                    );
                }
            }
            else if (index < 0 || index >= _variables.Count)
            {
                variables.Add(new Variable("<error>", null));
            }
            else
            {
                VariableInspector.InspectVariable(_variables[index], variables);
            }

            SendResponse(response, new VariablesResponseBody(variables));
        }

        /// <inheritdoc />
        void IAsyncDebuggerClient.SendStopEvent()
        {
            SendEvent(CreateStoppedEvent("step"));
        }

        /// <inheritdoc />
        void IAsyncDebuggerClient.OnWatchesUpdated(WatchType watchType)
        {
            if (watchType == WatchType.CallStack)
            {
                _variables.Clear();
            }
        }

        /// <inheritdoc />
        void IAsyncDebuggerClient.OnSourceCodeChanged(int sourceId)
        {
            if (_debug.IsSourceOverride(sourceId))
            {
                SendText(
                    "Loaded source '{0}' -> '{1}'",
                    _debug.GetSource(sourceId).Name,
                    _debug.GetSourceFile(sourceId)
                );
            }
            else
            {
                SendText("Loaded source '{0}'", _debug.GetSource(sourceId).Name);
            }
        }

        /// <inheritdoc cref="IAsyncDebuggerClient.OnExecutionEnded" />
        public void OnExecutionEnded()
        {
            if (_notifyExecutionEnd)
            {
                SendText("Execution ended.");
            }
        }

        private void SendText(string msg, params object[] args)
        {
            msg = FormatString(msg, args);
            // SendEvent(new OutputEvent("console", DateTime.Now.ToString("u") + ": " + msg + "\n"));
            SendEvent(new OutputEvent("console", msg + "\n"));
        }

        /// <inheritdoc />
        public void OnException(ScriptRuntimeException ex)
        {
            SendText("runtime error : {0}", ex.DecoratedMessage);
        }

        /// <inheritdoc />
        public void Unbind()
        {
            SendText("Debug session has been closed by the hosting process.");
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

            return string.Format(CultureInfo.InvariantCulture, format, args);
        }
    }
}
#endif

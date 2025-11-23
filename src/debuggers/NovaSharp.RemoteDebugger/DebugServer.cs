namespace NovaSharp.RemoteDebugger
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Network;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using Threading;

    /// <summary>
    /// Implements the TCP-based remote debugger endpoint used by the Flash/HTML clients to
    /// inspect scripts, manage breakpoints, and stream watch values.
    /// </summary>
    public class DebugServer : IDebugger, IDisposable
    {
        private readonly List<DynamicExpression> _watches = new();
        private readonly HashSet<string> _watchesChanging = new();
        private readonly Utf8TcpServer _server;
        private readonly Script _script;
        private readonly string _appName;
        private readonly object _lock = new();
        private readonly BlockingQueue<DebuggerAction> _queuedActions = new();
        private readonly SourceRef _lastSentSourceRef = null;
        private bool _inGetActionLoop = false;
        private bool _hostBusySent = false;
        private bool _requestPause = false;
        private readonly string[] _cachedWatches = new string[(int)WatchType.MaxValue];
        private bool _freeRunAfterAttach;
        private Regex _errorRegEx = new(@"\A.*\Z");
        private static readonly IReadOnlyDictionary<WatchType, string> WatchElementNames =
            new Dictionary<WatchType, string>()
            {
                { WatchType.CallStack, "callstack" },
                { WatchType.Watches, "watches" },
            };
        private static readonly ConcurrentDictionary<WatchType, string> WatchElementNameCache =
            new();

        public DebugServer(
            string appName,
            Script script,
            int port,
            Utf8TcpServerOptions options,
            bool freeRunAfterAttach
        )
        {
            _appName = appName;

            _server = new Utf8TcpServer(port, 1 << 20, '\0', options);
            _server.Start();
            _server.OnDataReceived += OnServerDataReceived;
            _script = script;
            _freeRunAfterAttach = freeRunAfterAttach;
        }

        public string AppName
        {
            get { return _appName; }
        }
        public int Port
        {
            get { return _server.PortNumber; }
        }

        /// <summary>
        /// Returns a short textual description of the server state reported on the jump page.
        /// </summary>
        /// <returns><c>"Busy"</c>, <c>"Waiting debugger"</c>, or <c>"Unknown"</c> depending on the server state.</returns>
        public string GetState()
        {
            if (_hostBusySent)
            {
                return "Busy";
            }
            else if (_inGetActionLoop)
            {
                return "Waiting debugger";
            }
            else
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the number of currently connected remote debugger clients.
        /// </summary>
        /// <returns>Active connection count.</returns>
        public int ConnectedClients()
        {
            return _server.GetConnectedClients();
        }

        /// <summary>
        /// Pushes a source file (and all of its lines) to the attached debugger UI.
        /// </summary>
        /// <param name="sourceCode">Source code descriptor obtained from the script runtime.</param>
        public void SetSourceCode(SourceCode sourceCode)
        {
            Send(xw =>
            {
                using (xw.Element("source-code"))
                {
                    xw.Attribute("id", sourceCode.SourceId).Attribute("name", sourceCode.Name);

                    foreach (string line in sourceCode.Lines)
                    {
                        xw.ElementCData("l", EpurateNewLines(line));
                    }
                }
            });
        }

        private string EpurateNewLines(string line)
        {
            return line.Replace('\n', ' ').Replace('\r', ' ');
        }

        private void Send(Action<XmlWriter> a)
        {
            XmlWriterSettings xs = new()
            {
                CheckCharacters = true,
                CloseOutput = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                Encoding = Encoding.UTF8,
                Indent = false,
            };

            StringBuilder sb = new();
            XmlWriter xw = XmlWriter.Create(sb, xs);

            a(xw);

            xw.Close();

            string xml = sb.ToString();
            _server.BroadcastMessage(xml);
        }

        private DebuggerAction CreateAction()
        {
            return new DebuggerAction(_script?.TimeProvider);
        }

        private DebuggerAction CreateAction(DebuggerAction.ActionType actionType)
        {
            DebuggerAction action = CreateAction();
            action.Action = actionType;
            return action;
        }

        private void SendWelcome()
        {
            Send(xw =>
            {
                using (xw.Element("welcome"))
                {
                    xw.Attribute("app", _appName)
                        .Attribute(
                            "NovaSharpver",
                            Assembly.GetAssembly(typeof(Script)).GetName().Version.ToString()
                        );
                }
            });

            SendOption("error_rx", _errorRegEx.ToString());
        }

        /// <summary>
        /// Sends updated watch data for the requested watch type when the values have changed.
        /// </summary>
        /// <param name="watchType">The watch channel being updated (call stack or watches).</param>
        /// <param name="items">Resolved watch values.</param>
        public void Update(WatchType watchType, IEnumerable<WatchItem> items)
        {
            if (watchType != WatchType.CallStack && watchType != WatchType.Watches)
            {
                return;
            }

            int watchIdx = (int)watchType;

            string watchHash = string.Join("|", items.Select(l => l.ToString()).ToArray());

            if (_cachedWatches[watchIdx] == null || _cachedWatches[watchIdx] != watchHash)
            {
                _cachedWatches[watchIdx] = watchHash;

                Send(xw =>
                {
                    using (xw.Element(GetWatchElementName(watchType)))
                    {
                        foreach (WatchItem wi in items)
                        {
                            using (xw.Element("item"))
                            {
                                if (wi.Name == null)
                                {
                                    if (watchType == WatchType.CallStack)
                                    {
                                        xw.Attribute(
                                            "name",
                                            ((wi.RetAddress < 0) ? "<chunk-root>" : "<??unknown??>")
                                        );
                                    }
                                    else
                                    {
                                        xw.Attribute("name", "(null name ??)");
                                    }
                                }
                                else
                                {
                                    xw.Attribute("name", wi.Name);
                                }

                                if (wi.Value != null)
                                {
                                    xw.Attribute("value", wi.Value.ToString());
                                    xw.Attribute(
                                        "type",
                                        wi.IsError ? "error" : wi.Value.Type.ToLuaDebuggerString()
                                    );
                                }

                                xw.Attribute("address", wi.Address.ToString("X8"));
                                xw.Attribute("baseptr", wi.BasePtr.ToString("X8"));
                                xw.Attribute("lvalue", wi.LValue);
                                xw.Attribute("retaddress", wi.RetAddress.ToString("X8"));
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Placeholder for legacy bytecode streaming support. Retained for protocol compatibility.
        /// </summary>
        /// <param name="byteCode">Interpreter bytecode lines.</param>
        public void SetByteCode(string[] byteCode)
        {
            // Skip sending bytecode updates for now.
            //Send(xw =>
            //	{
            //		using (xw.Element("bytecode"))
            //		{
            //			foreach (string line in byteCode)
            //				xw.Element("l", line);
            //		}
            //	});
        }

        /// <summary>
        /// Enqueues a debugger action received from the client.
        /// </summary>
        /// <param name="action">Action requested by the remote UI.</param>
        public void QueueAction(DebuggerAction action)
        {
            _queuedActions.Enqueue(action);
        }

        /// <summary>
        /// Blocks until the next debugger action should be executed by the runtime.
        /// </summary>
        /// <param name="ip">Instruction pointer associated with the pause.</param>
        /// <param name="sourceref">Source location for the paused instruction.</param>
        /// <returns>The next action to execute.</returns>
        public DebuggerAction GetAction(int ip, SourceRef sourceref)
        {
            try
            {
                if (_freeRunAfterAttach)
                {
                    _freeRunAfterAttach = false;
                    return CreateAction(DebuggerAction.ActionType.Run);
                }

                _inGetActionLoop = true;
                _requestPause = false;

                if (_hostBusySent)
                {
                    _hostBusySent = false;
                    SendMessage("Host ready!");
                }

                if (sourceref != _lastSentSourceRef)
                {
                    Send(xw =>
                    {
                        SendSourceRef(xw, sourceref);
                    });
                }

                while (true)
                {
                    DebuggerAction da = _queuedActions.Dequeue();

                    if (
                        da.Action == DebuggerAction.ActionType.Refresh
                        || da.Action == DebuggerAction.ActionType.HardRefresh
                    )
                    {
                        lock (_lock)
                        {
                            HashSet<string> existing = new();

                            // remove all not present anymore
                            _watches.RemoveAll(de => !_watchesChanging.Contains(de.ExpressionCode));

                            // add all missing
                            existing.UnionWith(_watches.Select(de => de.ExpressionCode));

                            _watches.AddRange(
                                _watchesChanging
                                    .Where(code => !existing.Contains(code))
                                    .Select(code => CreateDynExpr(code))
                            );
                        }

                        return da;
                    }

                    if (
                        da.Action == DebuggerAction.ActionType.ToggleBreakpoint
                        || da.Action == DebuggerAction.ActionType.SetBreakpoint
                        || da.Action == DebuggerAction.ActionType.ClearBreakpoint
                    )
                    {
                        return da;
                    }

                    if (da.Age < TimeSpan.FromMilliseconds(100))
                    {
                        return da;
                    }
                }
            }
            finally
            {
                _inGetActionLoop = false;
            }
        }

        private DynamicExpression CreateDynExpr(string code)
        {
            try
            {
                return _script.CreateDynamicExpression(code);
            }
            catch (Exception ex)
            {
                SendMessage($"Error setting watch {code} :\n{ex.Message}");
                return _script.CreateConstantDynamicExpression(
                    code,
                    DynValue.NewString(ex.Message)
                );
            }
        }

        private void SendSourceRef(XmlWriter xw, SourceRef sourceref)
        {
            using (xw.Element("source-loc"))
            {
                xw.Attribute("srcid", sourceref.SourceIdx)
                    .Attribute("cf", sourceref.FromChar)
                    .Attribute("ct", sourceref.ToChar)
                    .Attribute("lf", sourceref.FromLine)
                    .Attribute("lt", sourceref.ToLine);
            }
        }

        private void OnServerDataReceived(object sender, Utf8TcpPeerEventArgs e)
        {
            XmlDocument xdoc = new();
            xdoc.LoadXml(e.Message);

            if (xdoc.DocumentElement.Name == "policy-file-request")
            {
                Send(xw =>
                {
                    using (xw.Element("cross-domain-policy"))
                    {
                        using (xw.Element("allow-access-from"))
                        {
                            xw.Attribute("domain", "*");
                            xw.Attribute("to-ports", _server.PortNumber);
                        }
                    }
                });
                return;
            }

            if (xdoc.DocumentElement.Name == "Command")
            {
                string cmdAttribute = xdoc.DocumentElement.GetAttribute("cmd");
                string cmd = InvariantString.ToLowerInvariantIfNeeded(cmdAttribute);
                string arg = xdoc.DocumentElement.GetAttribute("arg");

                switch (cmd)
                {
                    case "handshake":
                        SendWelcome();

                        for (int i = 0; i < _script.SourceCodeCount; i++)
                        {
                            SetSourceCode(_script.GetSourceCode(i));
                        }

                        break;
                    case "refresh":
                        lock (_lock)
                        {
                            for (int i = 0; i < (int)WatchType.MaxValue; i++)
                            {
                                _cachedWatches[i] = null;
                            }
                        }
                        QueueRefresh();
                        break;
                    case "stepin":
                        QueueAction(CreateAction(DebuggerAction.ActionType.StepIn));
                        break;
                    case "run":
                        QueueAction(CreateAction(DebuggerAction.ActionType.Run));
                        break;
                    case "stepover":
                        QueueAction(CreateAction(DebuggerAction.ActionType.StepOver));
                        break;
                    case "stepout":
                        QueueAction(CreateAction(DebuggerAction.ActionType.StepOut));
                        break;
                    case "pause":
                        _requestPause = true;
                        break;
                    case "error_rx":
                        _errorRegEx = new Regex(arg.Trim());
                        SendOption("error_rx", _errorRegEx.ToString());
                        break;
                    case "addwatch":
                        lock (_lock)
                        {
                            _watchesChanging.UnionWith(
                                arg.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(s => s.Trim())
                            );
                        }

                        QueueRefresh();
                        break;
                    case "delwatch":
                        lock (_lock)
                        {
                            string[] args = arg.Split(
                                new char[] { ',' },
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            foreach (string a in args)
                            {
                                _watchesChanging.Remove(a);
                            }
                        }
                        QueueRefresh();
                        break;
                    case "breakpoint":
                        DebuggerAction.ActionType action = DebuggerAction
                            .ActionType
                            .ToggleBreakpoint;

                        if (arg == "set")
                        {
                            action = DebuggerAction.ActionType.SetBreakpoint;
                        }
                        else if (arg == "clear")
                        {
                            action = DebuggerAction.ActionType.ClearBreakpoint;
                        }

                        DebuggerAction breakpointAction = CreateAction(action);
                        breakpointAction.SourceId = int.Parse(
                            xdoc.DocumentElement.GetAttribute("src")
                        );
                        breakpointAction.SourceLine = int.Parse(
                            xdoc.DocumentElement.GetAttribute("line")
                        );
                        breakpointAction.SourceCol = int.Parse(
                            xdoc.DocumentElement.GetAttribute("col")
                        );
                        QueueAction(breakpointAction);
                        break;
                }
            }
        }

        private void QueueRefresh()
        {
            if (!_inGetActionLoop)
            {
                SendMessage("Host busy, wait for it to become ready...");
                _hostBusySent = true;
            }

            QueueAction(CreateAction(DebuggerAction.ActionType.HardRefresh));
        }

        private void SendOption(string optionName, string optionVal)
        {
            Send(xw =>
            {
                using (xw.Element(optionName))
                {
                    xw.Attribute("arg", optionVal);
                }
            });
        }

        private void SendMessage(string text)
        {
            Send(xw =>
            {
                xw.ElementCData("message", text);
            });
        }

        /// <summary>
        /// Gets the watch expressions currently tracked by the debugger.
        /// </summary>
        /// <returns>The list of dynamic expressions representing watch slots.</returns>
        public List<DynamicExpression> GetWatchItems()
        {
            return _watches;
        }

        /// <summary>
        /// Indicates whether a pause has been requested by the remote debugger or due to an error.
        /// </summary>
        /// <returns><c>true</c> when the runtime should pause execution.</returns>
        public bool IsPauseRequested()
        {
            return _requestPause;
        }

        /// <summary>
        /// Notifies connected clients that script execution has finished.
        /// </summary>
        public void SignalExecutionEnded()
        {
            Send(xw => xw.Element("execution-completed", ""));
        }

        /// <summary>
        /// Sends the full breakpoint list to the remote debugger.
        /// </summary>
        /// <param name="refs">Breakpoints to publish.</param>
        public void RefreshBreakpoints(IEnumerable<SourceRef> refs)
        {
            Send(xw =>
            {
                using (xw.Element("breakpoints"))
                {
                    foreach (SourceRef rf in refs)
                    {
                        SendSourceRef(xw, rf);
                    }
                }
            });
        }

        /// <summary>
        /// Broadcasts details about a runtime exception and optionally requests a pause.
        /// </summary>
        /// <param name="ex">Exception thrown by the running script.</param>
        /// <returns><c>true</c> if the debugger should pause execution after logging the error.</returns>
        public bool SignalRuntimeException(ScriptRuntimeException ex)
        {
            SendMessage($"Error: {ex.DecoratedMessage}");
            _requestPause = _errorRegEx.IsMatch(ex.Message);
            return IsPauseRequested();
        }

        /// <summary>
        /// Releases the underlying TCP server.
        /// </summary>
        public void Dispose()
        {
            _server.Dispose();
        }

        /// <summary>
        /// Part of the <see cref="IDebugger"/> contract. The remote debugger does not require the service instance.
        /// </summary>
        /// <param name="debugService">Debug service provided by the runtime.</param>
        public void SetDebugService(DebugService debugService) { }

        /// <summary>
        /// Reports the debugger capabilities supported by the remote debugger endpoint.
        /// </summary>
        /// <returns>The remote debugger capability flag set.</returns>
        public DebuggerCaps GetDebuggerCaps()
        {
            return DebuggerCaps.CanDebugSourceCode;
        }

        private static string GetWatchElementName(WatchType watchType)
        {
            if (WatchElementNames.TryGetValue(watchType, out string known))
            {
                return known;
            }

            return WatchElementNameCache.GetOrAdd(
                watchType,
                static wt => InvariantString.ToLowerInvariantIfNeeded(wt.ToString())
            );
        }
    }
}

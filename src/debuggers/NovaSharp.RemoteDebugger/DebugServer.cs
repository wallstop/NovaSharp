using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.Debugging;
using NovaSharp.RemoteDebugger.Network;
using NovaSharp.RemoteDebugger.Threading;

namespace NovaSharp.RemoteDebugger
{
    public class DebugServer : IDebugger, IDisposable
    {
        readonly List<DynamicExpression> _watches = new();
        readonly HashSet<string> _watchesChanging = new();
        readonly Utf8TcpServer _server;
        readonly Script _script;
        readonly string _appName;
        readonly object _lock = new();
        readonly BlockingQueue<DebuggerAction> _queuedActions = new();
        readonly SourceRef _lastSentSourceRef = null;
        bool _inGetActionLoop = false;
        bool _hostBusySent = false;
        private bool _requestPause = false;
        readonly string[] _cachedWatches = new string[(int)WatchType.MaxValue];
        bool _freeRunAfterAttach;
        Regex _errorRegEx = new(@"\A.*\Z");

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
            _server.DataReceived += _Server_DataReceived;
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

        public int ConnectedClients()
        {
            return _server.GetConnectedClients();
        }

        #region Writes

        public void SetSourceCode(SourceCode sourceCode)
        {
            Send(xw =>
            {
                using (xw.Element("source-code"))
                {
                    xw.Attribute("id", sourceCode.SourceID).Attribute("name", sourceCode.Name);

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
                    using (xw.Element(watchType.ToString().ToLowerInvariant()))
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

        #endregion

        public void QueueAction(DebuggerAction action)
        {
            _queuedActions.Enqueue(action);
        }

        public DebuggerAction GetAction(int ip, SourceRef sourceref)
        {
            try
            {
                if (_freeRunAfterAttach)
                {
                    _freeRunAfterAttach = false;
                    return new DebuggerAction() { Action = DebuggerAction.ActionType.Run };
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
                SendMessage(string.Format("Error setting watch {0} :\n{1}", code, ex.Message));
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

        void _Server_DataReceived(object sender, Utf8TcpPeerEventArgs e)
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
                string cmd = xdoc.DocumentElement.GetAttribute("cmd").ToLowerInvariant();
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
                    case "stepin":
                        QueueAction(
                            new DebuggerAction() { Action = DebuggerAction.ActionType.StepIn }
                        );
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
                    case "run":
                        QueueAction(
                            new DebuggerAction() { Action = DebuggerAction.ActionType.Run }
                        );
                        break;
                    case "stepover":
                        QueueAction(
                            new DebuggerAction() { Action = DebuggerAction.ActionType.StepOver }
                        );
                        break;
                    case "stepout":
                        QueueAction(
                            new DebuggerAction() { Action = DebuggerAction.ActionType.StepOut }
                        );
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

                        QueueAction(
                            new DebuggerAction()
                            {
                                Action = action,
                                SourceID = int.Parse(xdoc.DocumentElement.GetAttribute("src")),
                                SourceLine = int.Parse(xdoc.DocumentElement.GetAttribute("line")),
                                SourceCol = int.Parse(xdoc.DocumentElement.GetAttribute("col")),
                            }
                        );
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

            QueueAction(new DebuggerAction() { Action = DebuggerAction.ActionType.HardRefresh });
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

        public List<DynamicExpression> GetWatchItems()
        {
            return _watches;
        }

        public bool IsPauseRequested()
        {
            return _requestPause;
        }

        public void SignalExecutionEnded()
        {
            Send(xw => xw.Element("execution-completed", ""));
        }

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

        public bool SignalRuntimeException(ScriptRuntimeException ex)
        {
            SendMessage(string.Format("Error: {0}", ex.DecoratedMessage));
            _requestPause = _errorRegEx.IsMatch(ex.Message);
            return IsPauseRequested();
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        public void SetDebugService(DebugService debugService) { }

        public DebuggerCaps GetDebuggerCaps()
        {
            return DebuggerCaps.CanDebugSourceCode;
        }
    }
}

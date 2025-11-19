namespace NovaSharp.RemoteDebugger
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Network;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public class RemoteDebuggerService : IDisposable
    {
        private readonly RemoteDebuggerOptions _options;
        private readonly DebugWebHost _httpServer;
        private readonly string _jumpPage;
        private readonly int _rpcPortMax;
        private readonly List<DebugServer> _debugServers = new();

        private readonly object _lock = new();

        public RemoteDebuggerService()
            : this(RemoteDebuggerOptions.Default) { }

        public RemoteDebuggerService(RemoteDebuggerOptions options)
        {
            _options = options;

            if (options.HttpPort.HasValue)
            {
                Utf8TcpServerOptions httpopts =
                    options.NetworkOptions & (~Utf8TcpServerOptions.SingleClientOnly);
                _httpServer = new DebugWebHost(options.HttpPort.Value, httpopts);

                if (options.SingleScriptMode)
                {
                    _httpServer.RegisterResource(
                        "/",
                        HttpResource.CreateText(
                            HttpResourceType.Html,
                            string.Format(
                                "<html><body><iframe height='100%' width='100%' src='Debugger?port={0}'>Please follow <a href='{0}'>link</a>.</iframe></body></html>",
                                options.RpcPortBase
                            )
                        )
                    );
                }
                else
                {
                    _jumpPage = _httpServer.GetJumpPageText();

                    _httpServer.RegisterResource("/", HttpResource.CreateCallback(GetJumpPageData));
                }

                _httpServer.Start();
            }

            _rpcPortMax = options.RpcPortBase;
        }

        private HttpResource GetJumpPageData(Dictionary<string, string> arg)
        {
            lock (_lock)
            {
                return HttpResource.CreateText(
                    HttpResourceType.Html,
                    string.Format(_jumpPage, GetJumpHtmlFragment())
                );
            }
        }

        public void Attach(Script s, string scriptName, bool freeRunAfterAttach = false)
        {
            lock (_lock)
            {
                DebugServer d = new(
                    scriptName,
                    s,
                    _rpcPortMax,
                    _options.NetworkOptions,
                    freeRunAfterAttach
                );
                s.AttachDebugger(d);
                _debugServers.Add(d);
            }
        }

        public string GetJumpHtmlFragment()
        {
            StringBuilder sb = new();
            lock (_lock)
            {
                foreach (DebugServer d in _debugServers)
                {
                    sb.AppendFormat(
                        "<tr><td><a href=\"Debugger?port={0}\">{1}</a></td><td>{2}</td><td>{3}</td><td>{0}</td></tr>\n",
                        d.Port,
                        d.AppName,
                        d.GetState(),
                        d.ConnectedClients()
                    );
                }
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            _httpServer.Dispose();
            _debugServers.ForEach(s => s.Dispose());
        }

        public string HttpUrlStringLocalHost
        {
            get
            {
                if (_httpServer != null)
                {
                    return $"http://127.0.0.1:{_options.HttpPort.Value}/";
                }
                return null;
            }
        }
    }
}

namespace NovaSharp.RemoteDebugger
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Network;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modding;
    using NovaSharp.Interpreter.Modules;

    public class RemoteDebuggerService : IDisposable
    {
        private readonly RemoteDebuggerOptions _options;
        private readonly DebugWebHost _httpServer;
        private readonly string _jumpPage;
        private int _nextRpcPort;
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

            _nextRpcPort = options.RpcPortBase;
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
                int rpcPort = AcquireNextRpcPort();

                DebugServer d = new(
                    scriptName,
                    s,
                    rpcPort,
                    _options.NetworkOptions,
                    freeRunAfterAttach
                );
                s.AttachDebugger(d);
                _debugServers.Add(d);
            }
        }

        private int AcquireNextRpcPort()
        {
            int candidate = _nextRpcPort;

            while (IsPortInUse(candidate))
            {
                candidate = IncrementPort(candidate);
            }

            _nextRpcPort = IncrementPort(candidate);
            return candidate;
        }

        private bool IsPortInUse(int port)
        {
            foreach (DebugServer server in _debugServers)
            {
                if (server.Port == port)
                {
                    return true;
                }
            }

            return false;
        }

        private static int IncrementPort(int value)
        {
            if (value == int.MaxValue)
            {
                throw new InvalidOperationException(
                    "Remote debugger has exhausted the available RPC ports."
                );
            }

            return value + 1;
        }

        /// <summary>
        /// Creates a <see cref="Script"/> from the specified mod directory, applies any <c>mod.json</c>
        /// compatibility profile, attaches it to the debugger, and returns the created script so callers
        /// can execute entry points before or after attaching.
        /// </summary>
        /// <param name="modDirectory">Directory containing the mod scripts and optional <c>mod.json</c>.</param>
        /// <param name="scriptName">
        /// Optional name to display in debugger UIs. Defaults to the directory name when not specified.
        /// </param>
        /// <param name="modules">Module mask used when instantiating the script.</param>
        /// <param name="baseOptions">
        /// Optional options snapshot used as the baseline before manifest compatibility is applied.
        /// </param>
        /// <param name="infoSink">Receives informational messages emitted by manifest processing.</param>
        /// <param name="warningSink">Receives warnings emitted by manifest processing.</param>
        /// <param name="freeRunAfterAttach">
        /// When true, the debugger allows the script to continue running immediately after attachment.
        /// </param>
        public Script AttachFromDirectory(
            string modDirectory,
            string scriptName = null,
            CoreModules modules = CoreModules.PresetComplete,
            ScriptOptions baseOptions = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null,
            bool freeRunAfterAttach = false
        )
        {
            if (string.IsNullOrWhiteSpace(modDirectory))
            {
                throw new ArgumentException(
                    "Value cannot be null or whitespace.",
                    nameof(modDirectory)
                );
            }

            Script script = ModManifestCompatibility.CreateScriptFromDirectory(
                modDirectory,
                modules,
                baseOptions,
                infoSink,
                warningSink
            );

            string resolvedName = scriptName ?? GetDirectoryName(modDirectory) ?? "Script";
            LuaCompatibilityProfile profile = script.CompatibilityProfile;
            if (profile != null)
            {
                infoSink?.Invoke(
                    $"Script '{resolvedName}' running under {profile.GetFeatureSummary()}."
                );
            }

            Attach(script, resolvedName, freeRunAfterAttach);
            return script;
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
            _httpServer?.Dispose();
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

        private static string GetDirectoryName(string directory)
        {
            try
            {
                string fullPath = Path.GetFullPath(directory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return Path.GetFileName(fullPath);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

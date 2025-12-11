namespace WallstopStudios.NovaSharp.RemoteDebugger
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Network;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modding;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Coordinates debugger servers for attached scripts and, optionally, an HTTP host that serves
    /// the debugger UI and jump page.
    /// </summary>
    public class RemoteDebuggerService : IDisposable
    {
        private readonly RemoteDebuggerOptions _options;
        private readonly DebugWebHost _httpServer;
        private readonly string _jumpPage;
        private int _nextRpcPort;
        private readonly List<DebugServer> _debugServers = new();

        private readonly object _lock = new();
        private bool _disposed;

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
                                CultureInfo.InvariantCulture,
                                "<html><body><iframe height='100%' width='100%' src='Debugger?port={0}'>Please follow <a href='{0}'>link</a>.</iframe></body></html>",
                                options.RpcPortBase
                            )
                        )
                    );
                }
                else
                {
                    _jumpPage = DebugWebHost.GetJumpPageText();

                    _httpServer.RegisterResource("/", HttpResource.CreateCallback(GetJumpPageData));
                }

                _httpServer.Start();
            }

            _nextRpcPort = options.RpcPortBase;
        }

        private HttpResource GetJumpPageData(Dictionary<string, string> arg)
        {
            ThrowIfDisposed();

            lock (_lock)
            {
                return HttpResource.CreateText(
                    HttpResourceType.Html,
                    string.Format(CultureInfo.InvariantCulture, _jumpPage, GetJumpHtmlFragment())
                );
            }
        }

        /// <summary>
        /// Attaches a running script to the remote debugger service and starts listening for UI connections.
        /// </summary>
        /// <param name="s">Script instance to debug.</param>
        /// <param name="scriptName">Display name shown inside debugger UIs.</param>
        /// <param name="freeRunAfterAttach">
        /// When <c>true</c>, the script automatically resumes execution after the debugger acknowledges the attach.
        /// </param>
        public void Attach(Script s, string scriptName, bool freeRunAfterAttach = false)
        {
            ThrowIfDisposed();

            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

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
            CoreModules modules = default,
            ScriptOptions baseOptions = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null,
            bool freeRunAfterAttach = false
        )
        {
            ThrowIfDisposed();

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

        /// <summary>
        /// Builds the HTML table rows rendered by the jump page so users can pick a script to debug.
        /// </summary>
        /// <returns>HTML fragment listing each attached debugger server.</returns>
        public string GetJumpHtmlFragment()
        {
            ThrowIfDisposed();

            StringBuilder sb = new();
            lock (_lock)
            {
                foreach (DebugServer d in _debugServers)
                {
                    sb.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "<tr><td><a href=\"Debugger?port={0}\">{1}</a></td><td>{2}</td><td>{3}</td><td>{0}</td></tr>\n",
                        d.Port,
                        d.AppName,
                        d.State,
                        d.ConnectedClients
                    );
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Stops the HTTP server (if any) and disposes all running debugger servers.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _httpServer?.Dispose();

                lock (_lock)
                {
                    foreach (DebugServer server in _debugServers)
                    {
                        server.Dispose();
                    }

                    _debugServers.Clear();
                }
            }

            _disposed = true;
        }

        public Uri HttpUrlStringLocalHost
        {
            get
            {
                ThrowIfDisposed();

                if (_httpServer != null)
                {
                    return new UriBuilder("http", "127.0.0.1", _options.HttpPort.Value, "/").Uri;
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
            catch (PathTooLongException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RemoteDebuggerService));
            }
        }
    }
}

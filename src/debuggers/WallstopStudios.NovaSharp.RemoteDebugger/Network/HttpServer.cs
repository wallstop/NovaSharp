namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// This is a very very (very!) simplified and light http server. It exists to run on platforms where
    /// more standard methods offered by .NET BCL are not available and/or if privileges cannot be
    /// excalated. This just uses a TcpListener and a Socket.
    /// This supports only GET method and basic (or no) authentication.
    /// </summary>
    public class HttpServer : IDisposable
    {
        private readonly Utf8TcpServer _server;
        private readonly Dictionary<string, List<string>> _httpData = new();
        private readonly Dictionary<string, HttpResource> _resources = new();
        private readonly object _lock = new();
        private bool _disposed;

        private const string ErrorTemplate =
            "<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML 2.0//EN\"><html><head><title>{0}</title></head><body><h1>{0}</h1>{1}<hr><address>NovaSharp Remote Debugger / {2}</address></body></html><!-- This padding is added to bring the error message over 512 bytes to avoid some browsers custom errors. This padding is added to bring the error message over 512 bytes to avoid some browsers custom errors. This padding is added to bring the error message over 512 bytes to avoid some browsers custom errors. This padding is added to bring the error message over 512 bytes to avoid some browsers custom errors. This padding is added to bring the error message over 512 bytes to avoid some browsers custom errors. -->";
        private static readonly char[] SpaceTabSeparators = { ' ', '\t' };
        private static readonly char[] ColonSeparator = { ':' };
        private static readonly char[] QuestionSeparator = { '?' };
        private static readonly char[] AmpersandSeparator = { '&' };
        private static readonly char[] EqualsSeparator = { '=' };

        private static readonly string Version = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version.ToString();

        private readonly string _error401 = string.Format(
            CultureInfo.InvariantCulture,
            ErrorTemplate,
            "401 Unauthorized",
            "Please login.",
            Version
        );

        private readonly string _error404 = string.Format(
            CultureInfo.InvariantCulture,
            ErrorTemplate,
            "404 Not Found",
            "The specified resource cannot be found.",
            Version
        );

        private readonly string _error500 = string.Format(
            CultureInfo.InvariantCulture,
            ErrorTemplate,
            "500 Internal Server Error",
            "An internal server error occurred.",
            Version
        );

        public HttpServer(int port, Utf8TcpServerOptions options)
        {
            _server = new Utf8TcpServer(port, 100 << 10, '\n', options);
            _server.OnDataReceived += OnDataReceivedAny;
            _server.OnClientDisconnected += OnClientDisconnected;
        }

        /// <summary>
        /// Optional callback invoked to validate HTTP basic-auth credentials.
        /// </summary>
        /// <remarks>
        /// The first parameter is the provided username and the second parameter is the password.
        /// Return <c>true</c> to allow the request to proceed; <c>false</c> returns a 401 response.
        /// </remarks>
        public Func<string, string, bool> Authenticator { get; set; }

        /// <summary>
        /// Starts listening for HTTP requests on the configured TCP port.
        /// </summary>
        public void Start()
        {
            _server.Start();
        }

        private void OnDataReceivedAny(object sender, Utf8TcpPeerEventArgs e)
        {
            lock (_lock)
            {
                string msg = e
                    .Message.Replace("\n", string.Empty, StringComparison.Ordinal)
                    .Replace("\r", string.Empty, StringComparison.Ordinal);

                if (!_httpData.TryGetValue(e.Peer.Id, out List<string> httpdata))
                {
                    httpdata = new List<string>();
                    _httpData.Add(e.Peer.Id, httpdata);
                }

                if (msg.Length == 0)
                {
                    ExecHttpRequest(e.Peer, httpdata);
                    e.Peer.Disconnect();
                }
                else
                {
                    httpdata.Add(msg);
                }
            }
        }

        private static void SendHttp(
            Utf8TcpPeer peer,
            string responseCode,
            string contentType,
            string data,
            params string[] extraHeaders
        )
        {
            SendHttp(peer, responseCode, contentType, Encoding.UTF8.GetBytes(data), extraHeaders);
        }

        private static void SendHttp(
            Utf8TcpPeer peer,
            string responseCode,
            string contentType,
            ReadOnlyMemory<byte> data,
            params string[] extraHeaders
        )
        {
            peer.Send("HTTP/1.0 {0}", responseCode);
            peer.Send("Server: NovaSharp-remote-debugger/{0}", Version);
            peer.Send("Content-Type: {0}", contentType);
            peer.Send("Content-Length: {0}", data.Length);
            peer.Send("Connection: close");
            peer.Send("Cache-Control: max-age=0, no-cache");

            foreach (string h in extraHeaders)
            {
                peer.Send(h);
            }

            peer.Send("");
            peer.SendBinary(data.Span);
        }

        private void ExecHttpRequest(Utf8TcpPeer peer, List<string> httpdata)
        {
            try
            {
                if (Authenticator != null)
                {
                    string authstr = httpdata.FirstOrDefault(s =>
                        s.StartsWith("Authorization:", StringComparison.Ordinal)
                    );
                    bool authorized = false;

                    if (authstr != null)
                    {
                        ParseAuthenticationString(authstr, out string user, out string password);
                        authorized = Authenticator(user, password);
                    }

                    if (!authorized)
                    {
                        SendHttp(
                            peer,
                            "401 Not Authorized",
                            "text/html",
                            _error401,
                            "WWW-Authenticate: Basic realm=\"NovaSharp-remote-debugger\""
                        );
                        return;
                    }
                }

                HttpResource res = GetResourceFromPath(httpdata[0]);

                if (res == null)
                {
                    SendHttp(peer, "404 Not Found", "text/html", _error404);
                }
                else
                {
                    SendHttp(peer, "200 OK", res.ContentTypeString, res.Data);
                }
            }
            catch (FormatException ex)
            {
                HandleHttpException(peer, ex);
            }
            catch (DecoderFallbackException ex)
            {
                HandleHttpException(peer, ex);
            }
            catch (SocketException ex)
            {
                HandleHttpException(peer, ex);
            }
            catch (ObjectDisposedException ex)
            {
                HandleHttpException(peer, ex);
            }
            catch (InvalidOperationException ex)
            {
                HandleHttpException(peer, ex);
            }
            catch (IOException ex)
            {
                HandleHttpException(peer, ex);
            }
        }

        private static void ParseAuthenticationString(
            string authstr,
            out string user,
            out string password
        )
        {
            // example: Authorization: Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==
            user = null;
            password = null;

            string[] parts = authstr.Split(
                SpaceTabSeparators,
                StringSplitOptions.RemoveEmptyEntries
            );

            if (parts.Length < 3)
            {
                return;
            }

            if (parts[1] != "Basic")
            {
                return;
            }

            byte[] credentialBytes = Convert.FromBase64String(parts[2]);
            string credentialString = Encoding.UTF8.GetString(credentialBytes);
            string[] credentials = credentialString.Split(ColonSeparator, 2);

            if (credentials.Length != 2)
            {
                return;
            }

            user = credentials[0];
            password = credentials[1];
        }

        private HttpResource GetResourceFromPath(string path)
        {
            string[] parts = path.Split(SpaceTabSeparators, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                return null;
            }

            if (parts[0] != "GET")
            {
                return null;
            }

            string uri = parts[1];

            if (!uri.Contains('?', StringComparison.Ordinal))
            {
                return GetResourceFromUri(uri, null);
            }
            else
            {
                string[] macroparts = uri.Split(QuestionSeparator, 2);
                uri = macroparts[0];
                string[] tuples = macroparts[1]
                    .Split(AmpersandSeparator, StringSplitOptions.RemoveEmptyEntries);

                Dictionary<string, string> args = new();
                foreach (string t in tuples)
                {
                    ParseArgument(t, args);
                }

                return GetResourceFromUri(uri, args);
            }
        }

        private static void ParseArgument(string t, Dictionary<string, string> args)
        {
            string[] parts = t.Split(EqualsSeparator, 2);

            if (parts.Length == 2)
            {
                args.Add(parts[0], parts[1]);
            }
            else
            {
                args.Add(t, null);
            }
        }

        private HttpResource GetResourceFromUri(string uri, Dictionary<string, string> args)
        {
            if (uri != "/")
            {
                uri = uri.TrimEnd('/');
            }

            if (_resources.TryGetValue(uri, out HttpResource ret))
            {
                if (ret.Type == HttpResourceType.Callback)
                {
                    if (args == null)
                    {
                        args = new Dictionary<string, string>();
                    }

                    args.Add("?", uri);
                    return ret.Callback(args);
                }
                else
                {
                    return ret;
                }
            }

            return null;
        }

        private void HandleHttpException(Utf8TcpPeer peer, Exception ex)
        {
            _server.Logger(ex.Message);

            try
            {
                SendHttp(peer, "500 Internal Server Error", "text/html", _error500);
            }
            catch (SocketException socketEx)
            {
                _server.Logger(socketEx.Message);
            }
            catch (ObjectDisposedException disposedEx)
            {
                _server.Logger(disposedEx.Message);
            }
            catch (InvalidOperationException invalidOpEx)
            {
                _server.Logger(invalidOpEx.Message);
            }
            catch (IOException ioEx)
            {
                _server.Logger(ioEx.Message);
            }
        }

        private void OnClientDisconnected(object sender, Utf8TcpPeerEventArgs e)
        {
            lock (_lock)
            {
                if (_httpData.ContainsKey(e.Peer.Id))
                {
                    _httpData.Remove(e.Peer.Id);
                }
            }
        }

        /// <summary>
        /// Registers the resource.
        /// </summary>
        /// <param name="path">The path, including a starting '/'.</param>
        /// <param name="resource">The resource.</param>
        public void RegisterResource(string path, HttpResource resource)
        {
            _resources.Add(path, resource);
        }

        /// <summary>
        /// Releases the underlying TCP server and disconnects any connected clients.
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
                _server.OnDataReceived -= OnDataReceivedAny;
                _server.OnClientDisconnected -= OnClientDisconnected;
                _server.Dispose();
            }

            _disposed = true;
        }
    }
}

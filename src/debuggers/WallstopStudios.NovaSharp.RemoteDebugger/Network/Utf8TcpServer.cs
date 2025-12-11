namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Lightweight TCP server that frames UTF-8 payloads using a single character terminator and
    /// broadcasts debugger messages to each connected peer.
    /// </summary>
    public class Utf8TcpServer : IDisposable, IDebuggerTransport
    {
        private readonly int _portNumber = 1912;
        private readonly IPAddress _ipAddress;
        private TcpListener _listener;
        private Action<string> _logger;
        private readonly List<Utf8TcpPeer> _peerList = new();
        private readonly object _peerListLock = new();
        private bool _disposed;

        /// <summary>
        /// Gets the character used to delimit packets within the stream.
        /// </summary>
        public char PacketSeparator { get; private set; }

        /// <summary>
        /// Gets the server configuration flags that control binding, single-client mode, etc.
        /// </summary>
        public Utf8TcpServerOptions Options { get; private set; }

        /// <summary>
        /// Raised when a new client connects to the server.
        /// </summary>
        public event EventHandler<Utf8TcpPeerEventArgs> OnClientConnected;

        /// <summary>
        /// Raised when a connected client sends a complete packet.
        /// </summary>
        public event EventHandler<Utf8TcpPeerEventArgs> OnDataReceived;

        /// <summary>
        /// Raised when a client disconnects or is forcibly removed.
        /// </summary>
        public event EventHandler<Utf8TcpPeerEventArgs> OnClientDisconnected;

        /// <summary>
        /// Gets the TCP port currently used by the listener.
        /// </summary>
        public int PortNumber
        {
            get { return _portNumber; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8TcpServer"/> class.
        /// </summary>
        /// <param name="port">Port to listen on.</param>
        /// <param name="bufferSize">Receive buffer size allocated per client.</param>
        /// <param name="packetSeparator">Character used to terminate packets.</param>
        /// <param name="options">Network configuration flags.</param>
        public Utf8TcpServer(
            int port,
            int bufferSize,
            char packetSeparator,
            Utf8TcpServerOptions options
        )
        {
            _ipAddress =
                ((options & Utf8TcpServerOptions.LocalHostOnly) != 0)
                    ? IPAddress.Loopback
                    : IPAddress.Any;
            _portNumber = port;
            _logger = s => System.Diagnostics.Debug.WriteLine(s);
            PacketSeparator = packetSeparator;
            BufferSize = bufferSize;
            Options = options;
        }

        /// <summary>
        /// Gets or sets the sink used to log socket-level diagnostics.
        /// </summary>
        public Action<string> Logger
        {
            get { return _logger; }
            set { _logger = value ?? (s => Console.WriteLine(s)); }
        }

        /// <summary>
        /// Starts listening for incoming TCP connections.
        /// </summary>
        public void Start()
        {
            _listener = new TcpListener(_ipAddress, _portNumber);
            _listener.Start();
            _listener.BeginAcceptSocket(OnAcceptSocket, null);
        }

        /// <summary>
        /// Gets the maximum size of a packet accepted from any peer.
        /// </summary>
        public int BufferSize { get; private set; }

        private void OnAcceptSocket(IAsyncResult ar)
        {
            try
            {
                TcpListener listener = _listener;
                if (listener == null)
                {
                    return;
                }

                Socket s = listener.EndAcceptSocket(ar);
                AddNewClient(s);
                listener.BeginAcceptSocket(OnAcceptSocket, null);
            }
            catch (SocketException ex)
            {
                Logger("OnAcceptSocket : " + ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                Logger("OnAcceptSocket : " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                // Listener was stopped or never started; swallow so test hosts can dispose gracefully.
                Logger("OnAcceptSocket : " + ex.Message);
            }
        }

        /// <summary>
        /// Gets the number of active client connections.
        /// </summary>
        public int ConnectedClientCount
        {
            get
            {
                lock (_peerListLock)
                {
                    return _peerList.Count;
                }
            }
        }

        private void AddNewClient(Socket socket)
        {
            if ((Options & Utf8TcpServerOptions.SingleClientOnly) != 0)
            {
                lock (_peerListLock)
                {
                    foreach (Utf8TcpPeer pp in _peerList)
                    {
                        pp.Disconnect();
                    }
                }
            }

            Utf8TcpPeer peer = new(this, socket);

            lock (_peerListLock)
            {
                _peerList.Add(peer);
                peer.OnConnectionClosed += OnPeerDisconnected;
                peer.OnDataReceived += OnPeerDataReceived;
            }

            if (OnClientConnected != null)
            {
                Utf8TcpPeerEventArgs args = new(peer);
                OnClientConnected(this, args);
            }

            peer.Start();
        }

        private void OnPeerDataReceived(object sender, Utf8TcpPeerEventArgs e)
        {
            OnDataReceived?.Invoke(this, e);
        }

        private void OnPeerDisconnected(object sender, Utf8TcpPeerEventArgs e)
        {
            OnClientDisconnected?.Invoke(this, e);

            lock (_peerListLock)
            {
                _peerList.Remove(e.Peer);
                e.Peer.OnConnectionClosed -= OnPeerDisconnected;
                e.Peer.OnDataReceived -= OnPeerDataReceived;
            }
        }

        /// <summary>
        /// Sends the supplied message to every connected peer (after adding the terminator).
        /// </summary>
        /// <param name="message">Payload to broadcast.</param>
        public void BroadcastMessage(string message)
        {
            List<Utf8TcpPeer> peers;

            lock (_peerListLock)
            {
                peers = _peerList.ToList();
            }

            message = CompleteMessage(message);

            if (message == null)
            {
                return;
            }

            foreach (Utf8TcpPeer peer in peers)
            {
                peer.SendTerminated(message);
            }
        }

        /// <summary>
        /// Ensures that the supplied payload ends with the configured terminator so the client can
        /// treat it as a complete packet.
        /// </summary>
        /// <param name="message">Message to inspect.</param>
        /// <returns>The original message or the same message with the separator appended.</returns>
        public string CompleteMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return PacketSeparator.ToString();
            }

            if (message[^1] != PacketSeparator)
            {
                message = message + PacketSeparator;
            }

            return message;
        }

        /// <summary>
        /// Stops accepting new clients and closes the underlying listener.
        /// </summary>
        public void Stop()
        {
            _listener?.Stop();
            _listener = null;
        }

        /// <summary>
        /// Disposes the server and releases any active sockets.
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
                Stop();

                List<Utf8TcpPeer> peers;
                lock (_peerListLock)
                {
                    peers = _peerList.ToList();
                    _peerList.Clear();
                }

                foreach (Utf8TcpPeer peer in peers)
                {
                    peer.OnConnectionClosed -= OnPeerDisconnected;
                    peer.OnDataReceived -= OnPeerDataReceived;
                    peer.Disconnect();
                }
            }

            _disposed = true;
        }
    }
}

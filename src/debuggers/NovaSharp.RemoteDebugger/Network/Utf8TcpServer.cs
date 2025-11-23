namespace NovaSharp.RemoteDebugger.Network
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    public class Utf8TcpServer : IDisposable
    {
        private readonly int _portNumber = 1912;
        private readonly IPAddress _ipAddress;
        private TcpListener _listener = null;
        private Action<string> _logger;
        private readonly List<Utf8TcpPeer> _peerList = new();
        private readonly object _peerListLock = new();
        public char PacketSeparator { get; private set; }

        public Utf8TcpServerOptions Options { get; private set; }

        public event EventHandler<Utf8TcpPeerEventArgs> OnClientConnected;
        public event EventHandler<Utf8TcpPeerEventArgs> OnDataReceived;
        public event EventHandler<Utf8TcpPeerEventArgs> OnClientDisconnected;

        public int PortNumber
        {
            get { return _portNumber; }
        }

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

        public Action<string> Logger
        {
            get { return _logger; }
            set { _logger = value ?? (s => Console.WriteLine(s)); }
        }

        public void Start()
        {
            _listener = new TcpListener(_ipAddress, _portNumber);
            _listener.Start();
            _listener.BeginAcceptSocket(OnAcceptSocket, null);
        }

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

        public int GetConnectedClients()
        {
            lock (_peerListLock)
            {
                return _peerList.Count;
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
            try
            {
                OnClientDisconnected?.Invoke(this, e);

                lock (_peerListLock)
                {
                    _peerList.Remove(e.Peer);
                    e.Peer.OnConnectionClosed -= OnPeerDisconnected;
                    e.Peer.OnDataReceived -= OnPeerDataReceived;
                }
            }
            catch { }
        }

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
                try
                {
                    peer.SendTerminated(message);
                }
                catch { }
            }
        }

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

        public void Stop()
        {
            _listener?.Stop();
            _listener = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    using System;
    using System.Globalization;
    using System.Net.Sockets;
    using System.Text;

    /// <summary>
    /// Represents a single TCP client connected to the remote debugger server using UTF-8 packets
    /// terminated by <see cref="Utf8TcpServer.PacketSeparator"/>.
    /// </summary>
    public class Utf8TcpPeer
    {
        private readonly Socket _socket;
        private readonly Utf8TcpServer _server;
        private int _prevSize;
        private readonly byte[] _recvBuffer;

        /// <summary>
        /// Gets the unique identifier assigned to this peer connection.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Raised when the client disconnects or the server closes the socket.
        /// </summary>
        public event EventHandler<Utf8TcpPeerEventArgs> OnConnectionClosed;

        /// <summary>
        /// Raised when a complete packet (terminated by the configured separator) is received.
        /// </summary>
        public event EventHandler<Utf8TcpPeerEventArgs> OnDataReceived;

        internal Utf8TcpPeer(Utf8TcpServer server, Socket socket)
        {
            _socket = socket;
            _server = server;
            _recvBuffer = new byte[_server.BufferSize];
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Begins asynchronously reading packets from the connected socket.
        /// </summary>
        internal void Start()
        {
            _socket.BeginReceive(
                _recvBuffer,
                0,
                _recvBuffer.Length,
                SocketFlags.None,
                DataReceivedInternal,
                null
            );
        }

        private void DataReceivedInternal(IAsyncResult ar)
        {
            try
            {
                bool dataReceived = false;
                int size = _socket.EndReceive(ar);

                if (size == 0)
                {
                    CloseConnection("zero byte received");
                    return;
                }

                int ptr0 = _prevSize;
                _prevSize += size;

                do
                {
                    dataReceived = false;

                    char term = _server.PacketSeparator;

                    for (int i = ptr0; i < _prevSize; i++)
                    {
                        if (_recvBuffer[i] == term)
                        {
                            dataReceived = true;
                            string message = Encoding.UTF8.GetString(_recvBuffer, 0, i);

                            for (int j = i + 1; j < _prevSize; j++)
                            {
                                _recvBuffer[j - (i + 1)] = _recvBuffer[j];
                            }

                            ptr0 = 0;
                            _prevSize = _prevSize - i - 1;

                            OnDataReceived?.Invoke(this, new Utf8TcpPeerEventArgs(this, message));

                            break;
                        }
                    }
                } while (dataReceived);

                if (_socket.Connected)
                {
                    _socket.BeginReceive(
                        _recvBuffer,
                        _prevSize,
                        _recvBuffer.Length - _prevSize,
                        SocketFlags.None,
                        DataReceivedInternal,
                        null
                    );
                }
            }
            catch (SocketException ex)
            {
                _server.Logger(ex.Message);
                CloseConnection(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                _server.Logger(ex.Message);
                CloseConnection(ex.Message);
            }
        }

        private void CloseConnection(string reason)
        {
            OnConnectionClosed?.Invoke(this, new Utf8TcpPeerEventArgs(this, reason));

            try
            {
                _socket.Close();
            }
            catch (SocketException ex)
            {
                _server.Logger(ex.Message);
            }
            catch (ObjectDisposedException)
            {
                // Socket already closed; nothing to do.
            }
        }

        /// <summary>
        /// Sends a message, ensuring the server appends the packet terminator.
        /// </summary>
        /// <param name="message">The textual payload to transmit.</param>
        public void Send(string message)
        {
            SendTerminated(_server.CompleteMessage(message));
        }

        /// <summary>
        /// Sends a formatted message with the server-provided terminator appended.
        /// </summary>
        /// <param name="message">Composite format string.</param>
        /// <param name="args">Arguments used to format <paramref name="message"/>.</param>
        public void Send(string message, params object[] args)
        {
            SendTerminated(_server.CompleteMessage(FormatString(message, args)));
        }

        /// <summary>
        /// Sends a message that already contains the packet terminator.
        /// </summary>
        /// <param name="message">The fully formatted payload to transmit.</param>
        public void SendTerminated(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            SendBinary(bytes);
        }

        /// <summary>
        /// Closes the underlying socket and triggers <see cref="OnConnectionClosed"/>.
        /// </summary>
        public void Disconnect()
        {
            _socket.Close();
        }

        /// <summary>
        /// Sends raw binary data to the peer.
        /// </summary>
        /// <param name="bytes">Payload to transmit.</param>
        public void SendBinary(ReadOnlySpan<byte> bytes)
        {
            try
            {
                _socket.Send(bytes);
            }
            catch (SocketException ex)
            {
                _server.Logger(ex.Message);
                CloseConnection(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                _server.Logger(ex.Message);
                CloseConnection(ex.Message);
            }
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

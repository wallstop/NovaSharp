namespace NovaSharp.RemoteDebugger.Network
{
    using System.Net.Sockets;
    using System.Text;

    public class Utf8TcpPeer
    {
        private readonly Socket _socket;
        private readonly Utf8TcpServer _server;
        private int _prevSize = 0;
        private readonly byte[] _recvBuffer;

        public string Id { get; private set; }

        public event EventHandler<Utf8TcpPeerEventArgs> OnConnectionClosed;
        public event EventHandler<Utf8TcpPeerEventArgs> OnDataReceived;

        internal Utf8TcpPeer(Utf8TcpServer server, Socket socket)
        {
            _socket = socket;
            _server = server;
            _recvBuffer = new byte[_server.BufferSize];
            Id = Guid.NewGuid().ToString();
        }

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
            catch
            {
                // Swallow
            }
        }

        public void Send(string message)
        {
            SendTerminated(_server.CompleteMessage(message));
        }

        public void Send(string message, params object[] args)
        {
            SendTerminated(_server.CompleteMessage(string.Format(message, args)));
        }

        public void SendTerminated(string message)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            SendBinary(bytes);
        }

        public void Disconnect()
        {
            _socket.Close();
        }

        public void SendBinary(byte[] bytes)
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
    }
}

namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    using System;

    /// <summary>
    /// Abstraction over the debugger transport so tests can substitute in-memory implementations.
    /// </summary>
    internal interface IDebuggerTransport : IDisposable
    {
        public event EventHandler<Utf8TcpPeerEventArgs> OnDataReceived;

        /// <summary>
        /// Gets the TCP port that the transport is currently bound to.
        /// </summary>
        public int PortNumber { get; }

        /// <summary>
        /// Gets the number of debugger clients currently connected to this transport.
        /// </summary>
        public int ConnectedClientCount { get; }

        /// <summary>
        /// Starts listening for debugger connections using the configured transport implementation.
        /// </summary>
        public void Start();

        /// <summary>
        /// Broadcasts a UTF-8 payload to every connected debugger client.
        /// </summary>
        /// <param name="message">Payload to send to connected peers.</param>
        public void BroadcastMessage(string message);
    }
}

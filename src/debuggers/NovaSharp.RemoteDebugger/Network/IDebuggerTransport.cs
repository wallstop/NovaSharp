namespace NovaSharp.RemoteDebugger.Network
{
    using System;

    /// <summary>
    /// Abstraction over the debugger transport so tests can substitute in-memory implementations.
    /// </summary>
    internal interface IDebuggerTransport : IDisposable
    {
        public event EventHandler<Utf8TcpPeerEventArgs> OnDataReceived;

        public int PortNumber { get; }

        public int ConnectedClientCount { get; }

        public void Start();

        public void BroadcastMessage(string message);
    }
}

namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    /// <summary>
    /// Event payload describing a peer notification and optional text message.
    /// </summary>
    public class Utf8TcpPeerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Utf8TcpPeerEventArgs"/> class.
        /// </summary>
        /// <param name="peer">Peer associated with the event.</param>
        /// <param name="message">Optional descriptive message supplied by the server.</param>
        public Utf8TcpPeerEventArgs(Utf8TcpPeer peer, string message = null)
        {
            Peer = peer;
            Message = message;
        }

        /// <summary>
        /// Gets the peer associated with the event.
        /// </summary>
        public Utf8TcpPeer Peer { get; private set; }

        /// <summary>
        /// Gets the payload associated with the event, when provided.
        /// </summary>
        public string Message { get; private set; }
    }
}

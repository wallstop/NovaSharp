namespace WallstopStudios.NovaSharp.RemoteDebugger.Network
{
    /// <summary>
    /// Flags that control how the remote debugger TCP server accepts and manages connections.
    /// </summary>
    [Flags]
    public enum Utf8TcpServerOptions
    {
        /// <summary>
        /// Connections can come only from localhost
        /// </summary>
        LocalHostOnly = 1,

        /// <summary>
        /// As a client connects, every other non-connected client is disconnected
        /// </summary>
        SingleClientOnly = 2,

        /// <summary>
        /// No options enabled.
        /// </summary>
        None = 0,
    }
}

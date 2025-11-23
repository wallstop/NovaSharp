namespace NovaSharp.RemoteDebugger
{
    using Network;

    /// <summary>
    /// Configuration surface for <see cref="RemoteDebuggerService"/> instances.
    /// </summary>
    public struct RemoteDebuggerOptions
    {
        /// <summary>
        /// Gets or sets the TCP server flags applied to each debug endpoint.
        /// </summary>
        public Utf8TcpServerOptions NetworkOptions;

        /// <summary>
        /// When true, every HTTP request redirects to the single script instance.
        /// </summary>
        public bool SingleScriptMode;

        /// <summary>
        /// Gets or sets the HTTP port used to serve the debugger UI (when enabled).
        /// </summary>
        public int? HttpPort;

        /// <summary>
        /// Gets or sets the first port used when allocating RPC listeners for attached scripts.
        /// </summary>
        public int RpcPortBase;

        /// <summary>
        /// Gets the default configuration used when no options are supplied.
        /// </summary>
        public static RemoteDebuggerOptions Default
        {
            get
            {
                return new RemoteDebuggerOptions()
                {
                    NetworkOptions =
                        Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly,
                    SingleScriptMode = false,
                    HttpPort = 2705,
                    RpcPortBase = 2006,
                };
            }
        }
    }
}

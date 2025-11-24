namespace NovaSharp.RemoteDebugger
{
    using System;
    using Network;

    /// <summary>
    /// Configuration surface for <see cref="RemoteDebuggerService"/> instances.
    /// </summary>
    public struct RemoteDebuggerOptions : IEquatable<RemoteDebuggerOptions>
    {
        /// <summary>
        /// Gets or sets the TCP server flags applied to each debug endpoint.
        /// </summary>
        public Utf8TcpServerOptions NetworkOptions { get; set; }

        /// <summary>
        /// When true, every HTTP request redirects to the single script instance.
        /// </summary>
        public bool SingleScriptMode { get; set; }

        /// <summary>
        /// Gets or sets the HTTP port used to serve the debugger UI (when enabled).
        /// </summary>
        public int? HttpPort { get; set; }

        /// <summary>
        /// Gets or sets the first port used when allocating RPC listeners for attached scripts.
        /// </summary>
        public int RpcPortBase { get; set; }

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

        /// <inheritdoc />
        public bool Equals(RemoteDebuggerOptions other)
        {
            return NetworkOptions == other.NetworkOptions
                && SingleScriptMode == other.SingleScriptMode
                && HttpPort == other.HttpPort
                && RpcPortBase == other.RpcPortBase;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is RemoteDebuggerOptions other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(NetworkOptions, SingleScriptMode, HttpPort, RpcPortBase);
        }

        public static bool operator ==(RemoteDebuggerOptions left, RemoteDebuggerOptions right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RemoteDebuggerOptions left, RemoteDebuggerOptions right)
        {
            return !left.Equals(right);
        }
    }
}

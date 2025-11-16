namespace NovaSharp.RemoteDebugger
{
    using Network;

    public struct RemoteDebuggerOptions
    {
        public Utf8TcpServerOptions networkOptions;

        public bool singleScriptMode;

        public int? httpPort;
        public int rpcPortBase;

        public static RemoteDebuggerOptions Default
        {
            get
            {
                return new RemoteDebuggerOptions()
                {
                    networkOptions =
                        Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly,
                    singleScriptMode = false,
                    httpPort = 2705,
                    rpcPortBase = 2006,
                };
            }
        }
    }
}

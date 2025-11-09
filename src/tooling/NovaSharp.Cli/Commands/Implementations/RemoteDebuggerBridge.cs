namespace NovaSharp.Commands.Implementations
{
    using RemoteDebugger;

    internal interface IRemoteDebuggerBridge
    {
        void Attach(Interpreter.Script script, string scriptName, bool freeRunAfterAttach);

        string HttpUrlStringLocalHost { get; }
    }

    internal sealed class RemoteDebuggerServiceBridge : IRemoteDebuggerBridge
    {
        private readonly RemoteDebuggerService _service;

        public RemoteDebuggerServiceBridge()
            : this(new RemoteDebuggerService()) { }

        internal RemoteDebuggerServiceBridge(RemoteDebuggerService service)
        {
            _service = service;
        }

        public void Attach(Interpreter.Script script, string scriptName, bool freeRunAfterAttach)
        {
            _service.Attach(script, scriptName, freeRunAfterAttach);
        }

        public string HttpUrlStringLocalHost
        {
            get { return _service.HttpUrlStringLocalHost; }
        }
    }
}

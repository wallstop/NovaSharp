namespace NovaSharp.Cli.Commands.Implementations
{
    using NovaSharp.Interpreter;
    using RemoteDebugger;

    public interface IRemoteDebuggerBridge
    {
        public void Attach(Script script, string scriptName, bool freeRunAfterAttach);

        public string HttpUrlStringLocalHost { get; }
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

        public void Attach(Script script, string scriptName, bool freeRunAfterAttach)
        {
            _service.Attach(script, scriptName, freeRunAfterAttach);
        }

        public string HttpUrlStringLocalHost
        {
            get { return _service.HttpUrlStringLocalHost; }
        }
    }
}

namespace WallstopStudios.NovaSharp.Cli.Commands.Implementations
{
    using System;
    using RemoteDebugger;
    using WallstopStudios.NovaSharp.Interpreter;

    /// <summary>
    /// Abstraction that lets CLI commands attach the remote debugger without referencing concrete services.
    /// </summary>
    internal interface IRemoteDebuggerBridge
    {
        /// <summary>
        /// Attaches the debugger to the specified script.
        /// </summary>
        /// <param name="script">Script instance to debug.</param>
        /// <param name="scriptName">Friendly name displayed in the debugger UI.</param>
        /// <param name="freeRunAfterAttach">Whether execution should resume immediately after attaching.</param>
        public void Attach(Script script, string scriptName, bool freeRunAfterAttach);

        /// <summary>
        /// Gets the HTTP URL that launches the debugger UI on localhost.
        /// </summary>
        public Uri HttpUrlStringLocalHost { get; }
    }

    /// <summary>
    /// Concrete debugger bridge backed by <see cref="RemoteDebuggerService"/>.
    /// </summary>
    internal sealed class RemoteDebuggerServiceBridge : IRemoteDebuggerBridge
    {
        private readonly RemoteDebuggerService _service;

        /// <summary>
        /// Initializes a new bridge using a fresh <see cref="RemoteDebuggerService"/>.
        /// </summary>
        public RemoteDebuggerServiceBridge()
            : this(new RemoteDebuggerService()) { }

        internal RemoteDebuggerServiceBridge(RemoteDebuggerService service)
        {
            _service = service;
        }

        /// <inheritdoc />
        public void Attach(Script script, string scriptName, bool freeRunAfterAttach)
        {
            _service.Attach(script, scriptName, freeRunAfterAttach);
        }

        /// <inheritdoc />
        public Uri HttpUrlStringLocalHost
        {
            get { return _service.HttpUrlStringLocalHost; }
        }
    }
}

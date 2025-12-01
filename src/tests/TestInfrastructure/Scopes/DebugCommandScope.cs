namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.RemoteDebugger;

    /// <summary>
    /// Provides a disposable wrapper for overriding <see cref="DebugCommand"/> hooks.
    /// </summary>
    internal sealed class DebugCommandScope : IDisposable
    {
        private readonly Func<IRemoteDebuggerBridge> _previousFactory;
        private readonly IBrowserLauncher _previousLauncher;
        private bool _disposed;

        private DebugCommandScope(
            Func<IRemoteDebuggerBridge> debuggerFactory,
            IBrowserLauncher browserLauncher
        )
        {
            _previousFactory = DebugCommand.DebuggerFactory;
            _previousLauncher = DebugCommand.BrowserLauncher;

            if (debuggerFactory != null)
            {
                DebugCommand.DebuggerFactory = debuggerFactory;
            }

            if (browserLauncher != null)
            {
                DebugCommand.BrowserLauncher = browserLauncher;
            }
        }

        public static DebugCommandScope Override(
            Func<IRemoteDebuggerBridge> debuggerFactory,
            IBrowserLauncher browserLauncher
        )
        {
            if (debuggerFactory == null && browserLauncher == null)
            {
                throw new ArgumentException(
                    "At least one debug command override must be provided.",
                    nameof(debuggerFactory)
                );
            }

            return new DebugCommandScope(debuggerFactory, browserLauncher);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DebugCommand.DebuggerFactory = _previousFactory;
            DebugCommand.BrowserLauncher = _previousLauncher;
            _disposed = true;
        }
    }
}

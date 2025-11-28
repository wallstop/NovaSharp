namespace NovaSharp.Interpreter.Tests.TestUtilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using NovaSharp.Interpreter;
    using NovaSharp.RemoteDebugger;
    using NovaSharp.RemoteDebugger.Network;

    internal static class RemoteDebuggerTestFactory
    {
        public static Script BuildScript(
            string code,
            string chunkName,
            ScriptOptions options = null
        )
        {
            Script script = options is null ? new Script() : new Script(options);
            script.Options.DebugPrint = _ => { };
            script.LoadString(code, null, chunkName);
            return script;
        }

        public static bool ContainsOrdinal(string source, string value)
        {
            return source != null && source.Contains(value, StringComparison.Ordinal);
        }
    }

    internal sealed class RemoteDebuggerHarness : IDisposable
    {
        private readonly InMemoryDebuggerTransport _transport = new();
        private bool _disposed;

        public RemoteDebuggerHarness(Script script, bool freeRunAfterAttach)
        {
            Server = new DebugServer("NovaSharp.Tests", script, freeRunAfterAttach, _transport);
        }

        public DebugServer Server { get; }

        public RemoteDebuggerTestClient CreateClient()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(RemoteDebuggerHarness));
            return new RemoteDebuggerTestClient(_transport);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Server.Dispose();
            _transport.Dispose();
        }
    }

    internal sealed class RemoteDebuggerTestClient : IDisposable
    {
        private readonly InMemoryDebuggerTransport.ClientConnection _connection;
        private bool _disposed;

        public RemoteDebuggerTestClient(InMemoryDebuggerTransport transport)
        {
            _connection = transport.ConnectClient();
        }

        public void SendCommand(string xml)
        {
            EnsureNotDisposed();
            _connection.Send(xml);
        }

        public List<string> ReadUntil(Func<List<string>, bool> predicate, TimeSpan timeout)
        {
            EnsureNotDisposed();
            List<string> messages = new();
            TestWaitHelpers.SpinUntilOrThrow(
                () =>
                {
                    messages.AddRange(ReadAvailable());
                    return messages.Count > 0 && predicate(messages);
                },
                timeout,
                "Timed out waiting for debugger messages."
            );
            return messages;
        }

        public void Drain(TimeSpan timeout)
        {
            EnsureNotDisposed();
            Stopwatch total = Stopwatch.StartNew();
            Stopwatch quietWindow = Stopwatch.StartNew();

            while (total.Elapsed < timeout)
            {
                if (ReadAvailable().Count > 0)
                {
                    quietWindow.Restart();
                    continue;
                }

                if (quietWindow.Elapsed >= TimeSpan.FromMilliseconds(25))
                {
                    return;
                }

                Thread.Sleep(1);
            }
        }

        public List<string> ReadAll(TimeSpan timeout)
        {
            EnsureNotDisposed();
            List<string> messages = new();
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                List<string> chunk = ReadAvailable();
                if (chunk.Count == 0)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    messages.AddRange(chunk);
                }
            }

            return messages;
        }

        private List<string> ReadAvailable()
        {
            return _connection.ReadAvailable();
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(RemoteDebuggerTestClient));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _connection.Dispose();
        }
    }

    internal sealed class InMemoryDebuggerTransport : IDebuggerTransport
    {
        private readonly List<ClientConnection> _clients = new();
        private readonly object _gate = new();
        private bool _disposed;

        public event EventHandler<Utf8TcpPeerEventArgs> OnDataReceived;

        public int PortNumber => 0;

        public int ConnectedClientCount
        {
            get
            {
                lock (_gate)
                {
                    return _clients.Count;
                }
            }
        }

        public void Start() { }

        public ClientConnection ConnectClient()
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, nameof(InMemoryDebuggerTransport));

                ClientConnection connection = new(this);
                _clients.Add(connection);
                return connection;
            }
        }

        public void BroadcastMessage(string message)
        {
            ClientConnection[] snapshot;
            lock (_gate)
            {
                snapshot = _clients.ToArray();
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Enqueue(message);
            }
        }

        internal void SendFromClient(ClientConnection connection, string payload)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(InMemoryDebuggerTransport));
            OnDataReceived?.Invoke(this, new Utf8TcpPeerEventArgs(null, payload));
        }

        internal void Disconnect(ClientConnection connection)
        {
            lock (_gate)
            {
                _clients.Remove(connection);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            ClientConnection[] snapshot;
            lock (_gate)
            {
                snapshot = _clients.ToArray();
                _clients.Clear();
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Dispose();
            }
        }

        public sealed class ClientConnection : IDisposable
        {
            private readonly InMemoryDebuggerTransport _owner;
            private readonly Queue<string> _messages = new();
            private readonly object _gate = new();
            private bool _disposed;

            internal ClientConnection(InMemoryDebuggerTransport owner)
            {
                _owner = owner;
            }

            public void Send(string payload)
            {
                ObjectDisposedException.ThrowIf(_disposed, nameof(ClientConnection));
                _owner.SendFromClient(this, payload);
            }

            public void Enqueue(string message)
            {
                lock (_gate)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    _messages.Enqueue(message);
                }
            }

            public List<string> ReadAvailable()
            {
                List<string> results = new();
                lock (_gate)
                {
                    while (_messages.Count > 0)
                    {
                        results.Add(_messages.Dequeue());
                    }
                }

                return results;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _owner.Disconnect(this);
            }
        }
    }
}

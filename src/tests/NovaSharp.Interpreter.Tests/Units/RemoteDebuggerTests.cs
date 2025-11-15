namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.RemoteDebugger;
    using NovaSharp.RemoteDebugger.Network;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RemoteDebuggerTests
    {
        private static readonly Utf8TcpServerOptions ServerOptions =
            Utf8TcpServerOptions.LocalHostOnly | Utf8TcpServerOptions.SingleClientOnly;

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

        [Test]
        public void HandshakeBroadcastsWelcomeAndSourceCode()
        {
            Script script = BuildScript("return 42", "handshake.lua");
            using DebugServer server = CreateServer(script, freeRunAfterAttach: false, out int port);
            using RemoteDebuggerTestClient client = new(port);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");

            IList<string> messages = client.ReadUntil(
                payloads =>
                    payloads.Any(m => m.Contains("<welcome"))
                    && payloads.Any(m => m.Contains("source-code")),
                DefaultTimeout
            );

            Assert.Multiple(() =>
            {
                Assert.That(messages.Any(m => m.Contains("<welcome")), Is.True);
                Assert.That(messages.Any(m => m.Contains("<source-code")), Is.True);
            });
        }

        [Test]
        public void RunAndBreakpointCommandsBecomeQueuedActions()
        {
            Script script = BuildScript("local x = 1 return x", "queue.lua");
            using DebugServer server = CreateServer(script, freeRunAfterAttach: false, out int port);
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"run\" arg=\"\" />");
            DebuggerAction run = server.GetAction(0, sourceRef);
            Assert.That(run.Action, Is.EqualTo(DebuggerAction.ActionType.Run));

            client.SendCommand("<Command cmd=\"breakpoint\" arg=\"set\" src=\"0\" line=\"1\" col=\"0\" />");
            DebuggerAction breakpoint = server.GetAction(0, sourceRef);

            Assert.Multiple(() =>
            {
                Assert.That(breakpoint.Action, Is.EqualTo(DebuggerAction.ActionType.SetBreakpoint));
                Assert.That(breakpoint.SourceId, Is.EqualTo(0));
                Assert.That(breakpoint.SourceLine, Is.EqualTo(1));
            });
        }

        [Test]
        public void AddWatchQueuesHardRefreshAndCreatesDynamicExpression()
        {
            Script script = BuildScript("local value = 10 return value", "watch.lua");
            using DebugServer server = CreateServer(script, freeRunAfterAttach: false, out int port);
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"value\" />");

            DebuggerAction refresh = server.GetAction(0, sourceRef);
            Assert.That(refresh.Action, Is.EqualTo(DebuggerAction.ActionType.HardRefresh));

            IReadOnlyCollection<string> watchCodes = server
                .GetWatchItems()
                .Select(w => w.ExpressionCode)
                .ToArray();
            Assert.That(watchCodes, Does.Contain("value"));
        }

        [Test]
        public void ErrorRegexCommandControlsPauseRequests()
        {
            Script script = BuildScript("return 0", "error.lua");
            using DebugServer server = CreateServer(script, freeRunAfterAttach: false, out int port);
            using RemoteDebuggerTestClient client = new(port);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"error_rx\" arg=\"timeout\" />");
            IList<string> messages = client.ReadUntil(
                payloads => payloads.Any(m => m.Contains("<error_rx")),
                DefaultTimeout
            );
            Assert.That(messages.Last(), Does.Contain("timeout"));

            bool shouldPause = server.SignalRuntimeException(new ScriptRuntimeException("timeout occurred"));
            Assert.That(shouldPause, Is.True);

            bool otherPause = server.SignalRuntimeException(new ScriptRuntimeException("other failure"));
            Assert.That(otherPause, Is.False);
        }

        private static DebugServer CreateServer(Script script, bool freeRunAfterAttach, out int port)
        {
            port = GetFreeTcpPort();
            return new DebugServer("NovaSharp.Tests", script, port, ServerOptions, freeRunAfterAttach);
        }

        private static Script BuildScript(string code, string chunkName)
        {
            Script script = new();
            script.Options.DebugPrint = _ => { };
            script.LoadString(code, null, chunkName);
            return script;
        }

        private static int GetFreeTcpPort()
        {
            TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private sealed class RemoteDebuggerTestClient : IDisposable
        {
            private readonly TcpClient _client;
            private readonly NetworkStream _stream;
            private readonly StringBuilder _buffer = new();

            internal RemoteDebuggerTestClient(int port)
            {
                _client = new TcpClient(AddressFamily.InterNetwork);
                _client.Connect(IPAddress.Loopback, port);
                _stream = _client.GetStream();
            }

            internal void SendCommand(string xml)
            {
                byte[] payload = Encoding.UTF8.GetBytes(xml + '\0');
                _stream.Write(payload, 0, payload.Length);
                _stream.Flush();
            }

            internal IList<string> ReadUntil(Func<IList<string>, bool> predicate, TimeSpan timeout)
            {
                List<string> messages = new();
                DateTime deadline = DateTime.UtcNow + timeout;

                while (DateTime.UtcNow < deadline)
                {
                    messages.AddRange(ReadAvailable(TimeSpan.FromMilliseconds(25)));
                    if (messages.Count > 0 && predicate(messages))
                    {
                        return messages;
                    }

                    Thread.Sleep(5);
                }

                throw new TimeoutException("Timed out waiting for debugger messages.");
            }

            internal void Drain(TimeSpan timeout)
            {
                DateTime deadline = DateTime.UtcNow + timeout;
                while (DateTime.UtcNow < deadline)
                {
                    IList<string> chunk = ReadAvailable(TimeSpan.FromMilliseconds(25));
                    if (chunk.Count == 0)
                    {
                        Thread.Sleep(5);
                    }
                }
            }

            private IList<string> ReadAvailable(TimeSpan timeout)
            {
                List<string> messages = new();
                DateTime deadline = DateTime.UtcNow + timeout;
                byte[] buffer = new byte[1024];

                while (DateTime.UtcNow < deadline)
                {
                    if (!_stream.DataAvailable)
                    {
                        Thread.Sleep(2);
                        continue;
                    }

                    int read = _stream.Read(buffer, 0, buffer.Length);
                    for (int i = 0; i < read; i++)
                    {
                        if (buffer[i] == 0)
                        {
                            messages.Add(_buffer.ToString());
                            _buffer.Clear();
                        }
                        else
                        {
                            _buffer.Append((char)buffer[i]);
                        }
                    }

                    if (messages.Count > 0)
                    {
                        break;
                    }
                }

                return messages;
            }

            public void Dispose()
            {
                _stream.Dispose();
                _client.Dispose();
            }
        }
    }
}

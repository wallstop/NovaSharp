namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tests.TestUtilities;
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
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");

            List<string> messages = client.ReadUntil(
                payloads =>
                    payloads.Any(m => ContainsOrdinal(m, "<welcome"))
                    && payloads.Any(m => ContainsOrdinal(m, "source-code")),
                DefaultTimeout
            );

            Assert.Multiple(() =>
            {
                Assert.That(messages.Any(m => ContainsOrdinal(m, "<welcome")), Is.True);
                Assert.That(messages.Any(m => ContainsOrdinal(m, "<source-code")), Is.True);
            });
        }

        [Test]
        public void RunAndBreakpointCommandsBecomeQueuedActions()
        {
            Script script = BuildScript("local x = 1 return x", "queue.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"run\" arg=\"\" />");
            DebuggerAction run = server.GetAction(0, sourceRef);
            Assert.That(run.Action, Is.EqualTo(DebuggerAction.ActionType.Run));

            client.SendCommand(
                "<Command cmd=\"breakpoint\" arg=\"set\" src=\"0\" line=\"1\" col=\"0\" />"
            );
            DebuggerAction breakpoint = server.GetAction(0, sourceRef);

            Assert.Multiple(() =>
            {
                Assert.That(breakpoint.Action, Is.EqualTo(DebuggerAction.ActionType.SetBreakpoint));
                Assert.That(breakpoint.SourceId, Is.EqualTo(0));
                Assert.That(breakpoint.SourceLine, Is.EqualTo(1));
            });
        }

        [Test]
        public void BreakpointCommandSupportsClearAndToggle()
        {
            Script script = BuildScript("return 0", "breakpoints.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand(
                "<Command cmd=\"breakpoint\" arg=\"clear\" src=\"5\" line=\"6\" col=\"7\" />"
            );
            DebuggerAction clear = server.GetAction(0, sourceRef);

            Assert.Multiple(() =>
            {
                Assert.That(clear.Action, Is.EqualTo(DebuggerAction.ActionType.ClearBreakpoint));
                Assert.That(clear.SourceId, Is.EqualTo(5));
                Assert.That(clear.SourceLine, Is.EqualTo(6));
                Assert.That(clear.SourceCol, Is.EqualTo(7));
            });

            client.SendCommand(
                "<Command cmd=\"breakpoint\" arg=\"\" src=\"9\" line=\"3\" col=\"2\" />"
            );
            DebuggerAction toggle = server.GetAction(0, sourceRef);

            Assert.Multiple(() =>
            {
                Assert.That(toggle.Action, Is.EqualTo(DebuggerAction.ActionType.ToggleBreakpoint));
                Assert.That(toggle.SourceId, Is.EqualTo(9));
                Assert.That(toggle.SourceLine, Is.EqualTo(3));
                Assert.That(toggle.SourceCol, Is.EqualTo(2));
            });
        }

        [Test]
        public void AddWatchQueuesHardRefreshAndCreatesDynamicExpression()
        {
            Script script = BuildScript("local value = 10 return value", "watch.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
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
        public void InvalidWatchExpressionFallsBackToConstant()
        {
            Script script = BuildScript("return 0", "invalid-watch.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"function(()\" />");
            DebuggerAction refresh = server.GetAction(0, sourceRef);
            Assert.That(refresh.Action, Is.EqualTo(DebuggerAction.ActionType.HardRefresh));

            List<string> messages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "Error setting watch")),
                DefaultTimeout
            );
            Assert.That(messages.Last(), Does.Contain("function(()"));

            DynamicExpression watch = server.GetWatchItems().Single();
            DynValue value = watch.Evaluate();
            Assert.Multiple(() =>
            {
                Assert.That(watch.ExpressionCode, Is.EqualTo("function(()"));
                Assert.That(watch.IsConstant(), Is.True);
                Assert.That(value.Type, Is.EqualTo(DataType.String));
                Assert.That(value.String, Does.Contain("unexpected symbol near"));
            });
        }

        [Test]
        public void ErrorRegexCommandControlsPauseRequests()
        {
            Script script = BuildScript("return 0", "error.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"error_rx\" arg=\"timeout\" />");
            List<string> messages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "<error_rx")),
                DefaultTimeout
            );
            Assert.That(messages.Last(), Does.Contain("timeout"));

            bool shouldPause = server.SignalRuntimeException(
                new ScriptRuntimeException("timeout occurred")
            );
            Assert.That(shouldPause, Is.True);

            bool otherPause = server.SignalRuntimeException(
                new ScriptRuntimeException("other failure")
            );
            Assert.That(otherPause, Is.False);
        }

        [Test]
        public void AddWatchQueuesRefreshAndTransitionsHostState()
        {
            Script script = BuildScript("local value = 1 return value", "state.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"value\" />");

            List<string> busyMessages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "Host busy")),
                DefaultTimeout
            );
            Assert.That(busyMessages.Any(m => ContainsOrdinal(m, "Host busy")), Is.True);
            Assert.That(server.State, Is.EqualTo("Busy"));

            DebuggerAction refresh = server.GetAction(0, sourceRef);
            Assert.That(refresh.Action, Is.EqualTo(DebuggerAction.ActionType.HardRefresh));

            List<string> readyMessages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "Host ready")),
                DefaultTimeout
            );
            Assert.That(readyMessages.Any(m => ContainsOrdinal(m, "Host ready")), Is.True);
            Assert.That(server.State, Is.EqualTo("Unknown"));
        }

        [Test]
        public void AddWatchDuringActiveGetActionDoesNotSendHostBusy()
        {
            Script script = BuildScript("return 0", "busy-loop.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            Task<DebuggerAction> actionTask = Task.Run(() => server.GetAction(0, sourceRef));
            TestWaitHelpers.SpinUntilOrThrow(
                () => server.State == "Waiting debugger",
                DefaultTimeout,
                "Debugger never entered GetAction loop."
            );

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"live\" />");
            Assert.That(actionTask.Wait(DefaultTimeout), Is.True, "GetAction never completed.");

            List<string> messages = client.ReadAll(TimeSpan.FromMilliseconds(150));
            Assert.That(messages.Any(m => ContainsOrdinal(m, "Host busy")), Is.False);

            Assert.Multiple(() =>
            {
                Assert.That(
                    actionTask.Result.Action,
                    Is.EqualTo(DebuggerAction.ActionType.HardRefresh)
                );
                Assert.That(
                    server.GetWatchItems().Select(w => w.ExpressionCode),
                    Does.Contain("live")
                );
            });
        }

        [Test]
        public void UpdateCallStackSendsFormattedItemsOncePerChange()
        {
            Script script = BuildScript("return 1", "callstack.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            WatchItem[] frames =
            {
                new WatchItem
                {
                    Address = 0x10,
                    BasePtr = 0x20,
                    RetAddress = -1,
                    Name = null,
                    Value = DynValue.NewString("entry"),
                    LValue = SymbolRef.DefaultEnv,
                },
                new WatchItem
                {
                    Address = 0x30,
                    BasePtr = 0x40,
                    RetAddress = 0x50,
                    Name = "foo",
                    Value = DynValue.NewNumber(42),
                    LValue = SymbolRef.Global("foo", SymbolRef.DefaultEnv),
                    IsError = true,
                },
            };

            server.Update(WatchType.CallStack, frames);
            List<string> callStackMessages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "<callstack")),
                DefaultTimeout
            );

            string payload = callStackMessages.First(m => ContainsOrdinal(m, "<callstack"));
            Assert.Multiple(() =>
            {
                Assert.That(payload, Does.Contain("&lt;chunk-root&gt;"));
                Assert.That(payload, Does.Contain("foo"));
                Assert.That(payload, Does.Contain("value=\"42\""));
            });

            // Repeat with identical payload to cover the cache-hit exit path.
            server.Update(WatchType.CallStack, frames);
            // And ensure non-callstack watch types short-circuit.
            server.Update(WatchType.VStack, frames);
        }

        [Test]
        public void RefreshCommandClearsCachedWatches()
        {
            Script script = BuildScript("return 0", "refresh.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            WatchItem[] watches =
            {
                new WatchItem
                {
                    Name = "value",
                    Value = DynValue.NewNumber(5),
                    LValue = SymbolRef.Global("value", SymbolRef.DefaultEnv),
                },
            };

            server.Update(WatchType.Watches, watches);
            client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "<watches")),
                DefaultTimeout
            );

            server.Update(WatchType.Watches, watches);
            client.Drain(TimeSpan.FromMilliseconds(50));

            client.SendCommand("<Command cmd=\"refresh\" arg=\"\" />");
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);
            DebuggerAction refreshAction = server.GetAction(0, sourceRef);
            Assert.That(refreshAction.Action, Is.EqualTo(DebuggerAction.ActionType.HardRefresh));

            server.Update(WatchType.Watches, watches);
            List<string> refreshed = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "<watches")),
                DefaultTimeout
            );
            Assert.That(refreshed.Last(), Does.Contain("value"));
        }

        [Test]
        public void DeleteWatchCommandQueuesHardRefresh()
        {
            Script script = BuildScript("return 0", "delwatch.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"foo,bar\" />");
            server.GetAction(0, sourceRef);
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"delwatch\" arg=\"foo, bar\" />");
            DebuggerAction refresh = server.GetAction(0, sourceRef);

            Assert.That(refresh.Action, Is.EqualTo(DebuggerAction.ActionType.HardRefresh));
        }

        [Test]
        public void PauseCommandRequestsDebuggerPause()
        {
            Script script = BuildScript("return 0", "pause.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"pause\" arg=\"\" />");
            TestWaitHelpers.SpinUntilOrThrow(
                () => server.IsPauseRequested(),
                DefaultTimeout,
                "Pause command was not processed."
            );
        }

        [Test]
        public void StepCommandsQueueActions()
        {
            Script script = BuildScript("return 0", "steps.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"stepin\" arg=\"\" />");
            Assert.That(
                server.GetAction(0, sourceRef).Action,
                Is.EqualTo(DebuggerAction.ActionType.StepIn)
            );

            client.SendCommand("<Command cmd=\"stepover\" arg=\"\" />");
            Assert.That(
                server.GetAction(0, sourceRef).Action,
                Is.EqualTo(DebuggerAction.ActionType.StepOver)
            );

            client.SendCommand("<Command cmd=\"stepout\" arg=\"\" />");
            Assert.That(
                server.GetAction(0, sourceRef).Action,
                Is.EqualTo(DebuggerAction.ActionType.StepOut)
            );
        }

        [Test]
        public void ExpiredActionsAreSkippedBeforeReturningNextEntry()
        {
            FakeTimeProvider timeProvider = new();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = timeProvider,
            };
            Script script = BuildScript("return 0", "aging.lua", options);
            using DebugServer server = CreateServer(script, freeRunAfterAttach: false, out _);
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            DebuggerAction stale = new(script.TimeProvider)
            {
                Action = DebuggerAction.ActionType.Run,
            };
            server.QueueAction(stale);

            timeProvider.Advance(TimeSpan.FromMilliseconds(250));

            DebuggerAction fresh = new(script.TimeProvider)
            {
                Action = DebuggerAction.ActionType.StepIn,
            };
            server.QueueAction(fresh);

            DebuggerAction action = server.GetAction(0, sourceRef);
            Assert.That(action.Action, Is.EqualTo(DebuggerAction.ActionType.StepIn));
        }

        [Test]
        public void FreeRunAfterAttachReturnsRunAction()
        {
            Script script = BuildScript("return 0", "autorun.lua");
            using DebugServer server = CreateServer(script, freeRunAfterAttach: true, out int port);
            using RemoteDebuggerTestClient client = new(port);
            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);
            DebuggerAction action = server.GetAction(0, sourceRef);
            Assert.That(action.Action, Is.EqualTo(DebuggerAction.ActionType.Run));
        }

        [Test]
        public void PolicyFileRequestRespondsWithCrossDomainPayload()
        {
            Script script = BuildScript("return 0", "policy.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);

            client.SendCommand("<policy-file-request/>");
            List<string> policy = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "cross-domain-policy")),
                DefaultTimeout
            );

            Assert.That(policy.Last(), Does.Contain("allow-access-from"));
        }

        [Test]
        public void ConnectedClientsReflectActiveSessions()
        {
            Script script = BuildScript("return 0", "clients.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);

            TestWaitHelpers.SpinUntilOrThrow(
                () => server.ConnectedClients == 1,
                DefaultTimeout,
                "Tcp client never registered with the server."
            );
        }

        [Test]
        public void DebuggerCapsAdvertiseSourceCodeSupport()
        {
            Script script = BuildScript("return 0", "caps.lua");
            using DebugServer server = CreateServer(script, freeRunAfterAttach: false, out _);
            Assert.That(server.GetDebuggerCaps(), Is.EqualTo(DebuggerCaps.CanDebugSourceCode));
        }

        [Test]
        public void SignalExecutionEndedBroadcastsCompletion()
        {
            Script script = BuildScript("return 0", "end.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            TestWaitHelpers.SpinUntilOrThrow(
                () => server.ConnectedClients == 1,
                DefaultTimeout,
                "Tcp client never registered with the server."
            );

            server.SignalExecutionEnded();
            List<string> payloads = client.ReadUntil(
                msgs => msgs.Any(m => ContainsOrdinal(m, "execution-completed")),
                DefaultTimeout
            );
            Assert.That(payloads.Last(), Does.Contain("execution-completed"));
        }

        [Test]
        public void RefreshBreakpointsBroadcastsLocations()
        {
            Script script = BuildScript("return 0", "breakpoints.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);
            TestWaitHelpers.SpinUntilOrThrow(
                () => server.ConnectedClients == 1,
                DefaultTimeout,
                "Tcp client never registered with the server."
            );

            SourceRef breakpoint = new(0, 1, 2, 3, 4, false);
            server.RefreshBreakpoints(new[] { breakpoint });

            List<string> payloads = client.ReadUntil(
                msgs => msgs.Any(m => ContainsOrdinal(m, "<breakpoints")),
                DefaultTimeout
            );

            Assert.Multiple(() =>
            {
                Assert.That(payloads.Last(), Does.Contain("srcid=\"0\""));
                Assert.That(payloads.Last(), Does.Contain("lf=\"3\""));
                Assert.That(payloads.Last(), Does.Contain("lt=\"4\""));
            });
        }

        [Test]
        public void HandshakeRespondsToPolicyRequestsBeforeCommands()
        {
            Script script = BuildScript("return 0", "state.lua");
            using DebugServer server = CreateServer(
                script,
                freeRunAfterAttach: false,
                out int port
            );
            using RemoteDebuggerTestClient client = new(port);

            client.SendCommand("<policy-file-request/>");
            client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "cross-domain-policy")),
                DefaultTimeout
            );

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            List<string> messages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "<welcome")),
                DefaultTimeout
            );

            Assert.That(messages.Any(m => ContainsOrdinal(m, "<welcome")), Is.True);
        }

        private static DebugServer CreateServer(
            Script script,
            bool freeRunAfterAttach,
            out int port
        )
        {
            port = GetFreeTcpPort();
            return new DebugServer(
                "NovaSharp.Tests",
                script,
                port,
                ServerOptions,
                freeRunAfterAttach
            );
        }

        private static Script BuildScript(
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

        private static int GetFreeTcpPort()
        {
            using TcpListener listener = new(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static bool ContainsOrdinal(string source, string value)
        {
            return source != null && source.Contains(value, StringComparison.Ordinal);
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

            internal List<string> ReadUntil(Func<List<string>, bool> predicate, TimeSpan timeout)
            {
                List<string> messages = new();
                TestWaitHelpers.SpinUntilOrThrow(
                    () =>
                    {
                        messages.AddRange(ReadAvailable(TimeSpan.FromMilliseconds(25)));
                        return messages.Count > 0 && predicate(messages);
                    },
                    timeout,
                    "Timed out waiting for debugger messages."
                );
                return messages;
            }

            internal void Drain(TimeSpan timeout)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed < timeout)
                {
                    List<string> chunk = ReadAvailable(TimeSpan.FromMilliseconds(25));
                    if (chunk.Count == 0)
                    {
                        Thread.Sleep(5);
                    }
                }
            }

            internal List<string> ReadAll(TimeSpan timeout)
            {
                List<string> messages = new();
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed < timeout)
                {
                    List<string> chunk = ReadAvailable(TimeSpan.FromMilliseconds(25));
                    if (chunk.Count > 0)
                    {
                        messages.AddRange(chunk);
                    }

                    Thread.Sleep(5);
                }

                return messages;
            }

            private List<string> ReadAvailable(TimeSpan timeout)
            {
                List<string> messages = new();
                Stopwatch stopwatch = Stopwatch.StartNew();
                byte[] buffer = new byte[1024];

                while (stopwatch.Elapsed < timeout)
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

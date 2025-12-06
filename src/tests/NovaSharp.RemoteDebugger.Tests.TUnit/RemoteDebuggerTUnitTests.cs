namespace NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NovaSharp.RemoteDebugger;
    using static NovaSharp.Interpreter.Tests.TestUtilities.RemoteDebuggerTestFactory;

    public sealed class RemoteDebuggerTUnitTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

        [global::TUnit.Core.Test]
        public async Task HandshakeStreamsWelcomeAndSourceCode()
        {
            Script script = BuildScript("return 42", "tunit-handshake.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            using RemoteDebuggerTestClient client = harness.CreateClient();

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");

            TimeSpan timeout = TimeSpan.FromSeconds(2);
            List<string> messages = client.ReadUntil(
                payloads =>
                    payloads.Any(m => ContainsOrdinal(m, "<welcome"))
                    && payloads.Any(m => ContainsOrdinal(m, "source-code")),
                timeout
            );

            await Assert
                .That(messages.Any(m => ContainsOrdinal(m, "<welcome")))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(messages.Any(m => ContainsOrdinal(m, "source-code")))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunAndBreakpointCommandsBecomeQueuedActions()
        {
            Script script = BuildScript("local x = 1 return x", "queue.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"run\" arg=\"\" />");
            DebuggerAction run = server.GetAction(0, sourceRef);
            await Assert
                .That(run.Action)
                .IsEqualTo(DebuggerAction.ActionType.Run)
                .ConfigureAwait(false);

            client.SendCommand(
                "<Command cmd=\"breakpoint\" arg=\"set\" src=\"0\" line=\"1\" col=\"0\" />"
            );
            DebuggerAction breakpoint = server.GetAction(0, sourceRef);
            await Assert
                .That(breakpoint.Action)
                .IsEqualTo(DebuggerAction.ActionType.SetBreakpoint)
                .ConfigureAwait(false);
            await Assert.That(breakpoint.SourceId).IsEqualTo(0).ConfigureAwait(false);
            await Assert.That(breakpoint.SourceLine).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task BreakpointCommandSupportsClearAndToggle()
        {
            Script script = BuildScript("return 0", "breakpoints.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand(
                "<Command cmd=\"breakpoint\" arg=\"clear\" src=\"5\" line=\"6\" col=\"7\" />"
            );
            DebuggerAction clear = server.GetAction(0, sourceRef);
            await Assert
                .That(clear.Action)
                .IsEqualTo(DebuggerAction.ActionType.ClearBreakpoint)
                .ConfigureAwait(false);
            await Assert.That(clear.SourceId).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(clear.SourceLine).IsEqualTo(6).ConfigureAwait(false);
            await Assert.That(clear.SourceCol).IsEqualTo(7).ConfigureAwait(false);

            client.SendCommand(
                "<Command cmd=\"breakpoint\" arg=\"\" src=\"9\" line=\"3\" col=\"2\" />"
            );
            DebuggerAction toggle = server.GetAction(0, sourceRef);
            await Assert
                .That(toggle.Action)
                .IsEqualTo(DebuggerAction.ActionType.ToggleBreakpoint)
                .ConfigureAwait(false);
            await Assert.That(toggle.SourceId).IsEqualTo(9).ConfigureAwait(false);
            await Assert.That(toggle.SourceLine).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(toggle.SourceCol).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AddWatchQueuesHardRefreshAndCreatesDynamicExpression()
        {
            Script script = BuildScript("local value = 10 return value", "tunit-addwatch.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"value\" />");

            DebuggerAction refresh = server.GetAction(0, sourceRef);
            await Assert
                .That(refresh.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);

            List<string> watchCodes = server.GetWatchItems().Select(w => w.ExpressionCode).ToList();
            await Assert.That(watchCodes).Contains("value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task WatchesEvaluateExpressionsAgainstScriptState()
        {
            Script script = BuildScript("return watched", "tunit-watch-eval.lua");
            script.Globals.Set("watched", DynValue.NewNumber(10));
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"watched\" />");
            DebuggerAction refresh = server.GetAction(0, sourceRef);
            await Assert
                .That(refresh.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);

            DynamicExpression watch = server.GetWatchItems().Single();
            DynValue firstValue = watch.Evaluate();
            await Assert.That(watch.IsConstant()).IsFalse().ConfigureAwait(false);
            await Assert.That(firstValue.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(firstValue.Number).IsEqualTo(10).ConfigureAwait(false);

            script.Globals.Set("watched", DynValue.NewNumber(42));
            DynValue updatedValue = watch.Evaluate();
            await Assert.That(updatedValue.Number).IsEqualTo(42).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InvalidWatchExpressionFallsBackToConstant()
        {
            Script script = BuildScript("return 0", "invalid-watch.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"function(()\" />");
            DebuggerAction refresh = server.GetAction(0, sourceRef);
            await Assert
                .That(refresh.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);

            List<string> messages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "Error setting watch")),
                DefaultTimeout
            );
            await Assert.That(messages.Last()).Contains("function(()").ConfigureAwait(false);

            DynamicExpression watch = server.GetWatchItems().Single();
            DynValue value = watch.Evaluate();
            await Assert.That(watch.ExpressionCode).IsEqualTo("function(()").ConfigureAwait(false);
            await Assert.That(watch.IsConstant()).IsTrue().ConfigureAwait(false);
            await Assert.That(value.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ErrorRegexCommandControlsPauseRequests()
        {
            Script script = BuildScript("return 0", "error.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"error_rx\" arg=\"timeout\" />");
            List<string> messages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "<error_rx")),
                DefaultTimeout
            );
            await Assert.That(messages.Last()).Contains("timeout").ConfigureAwait(false);

            bool shouldPause = server.SignalRuntimeException(
                new ScriptRuntimeException("timeout occurred")
            );
            await Assert.That(shouldPause).IsTrue().ConfigureAwait(false);

            bool otherPause = server.SignalRuntimeException(
                new ScriptRuntimeException("other failure")
            );
            await Assert.That(otherPause).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HostBusyTransitionsBackToReadyAfterProcessingAction()
        {
            Script script = BuildScript("return 0", "tunit-host-busy.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"foo\" />");
            List<string> busyMessages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "Host busy")),
                DefaultTimeout
            );

            await Assert
                .That(busyMessages.Any(m => ContainsOrdinal(m, "Host busy")))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(server.State).IsEqualTo("Busy").ConfigureAwait(false);

            DebuggerAction refresh = server.GetAction(0, sourceRef);
            await Assert
                .That(refresh.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);

            List<string> readyMessages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "Host ready")),
                DefaultTimeout
            );

            await Assert
                .That(readyMessages.Any(m => ContainsOrdinal(m, "Host ready")))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert.That(server.State).IsEqualTo("Unknown").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PauseCommandQueuesRunActionAndClearsPauseRequest()
        {
            Script script = BuildScript("return 0", "tunit-pause-queue.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"pause\" arg=\"\" />");
            TestWaitHelpers.SpinUntilOrThrow(
                server.IsPauseRequested,
                DefaultTimeout,
                "Pause request was not registered."
            );

            client.SendCommand("<Command cmd=\"run\" arg=\"\" />");

            DebuggerAction action = server.GetAction(0, sourceRef);
            await Assert
                .That(action.Action)
                .IsEqualTo(DebuggerAction.ActionType.Run)
                .ConfigureAwait(false);
            await Assert.That(server.IsPauseRequested()).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task QueuedRefreshesIncludeLatestWatchChanges()
        {
            Script script = BuildScript("return 0", "tunit-queued-refresh.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"alpha\" />");
            client.SendCommand("<Command cmd=\"addwatch\" arg=\"beta\" />");

            DebuggerAction first = server.GetAction(0, sourceRef);
            DebuggerAction second = server.GetAction(0, sourceRef);

            await Assert
                .That(first.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);
            await Assert
                .That(second.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);

            string[] watchExpressions = server
                .GetWatchItems()
                .Select(w => w.ExpressionCode)
                .ToArray();

            await Assert.That(watchExpressions.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(watchExpressions).Contains("alpha").ConfigureAwait(false);
            await Assert.That(watchExpressions).Contains("beta").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DeleteWatchCommandQueuesHardRefresh()
        {
            Script script = BuildScript("return 0", "tunit-delete-watch.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"addwatch\" arg=\"foo,bar\" />");
            server.GetAction(0, sourceRef);
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"delwatch\" arg=\"foo, bar\" />");
            DebuggerAction refresh = server.GetAction(0, sourceRef);

            await Assert
                .That(refresh.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AddWatchDuringActiveGetActionDoesNotSendHostBusy()
        {
            Script script = BuildScript("return 0", "busy-loop.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
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
            bool completed = true;
            try
            {
                await actionTask.WaitAsync(DefaultTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                completed = false;
            }
            await Assert.That(completed).IsTrue().ConfigureAwait(false);

            List<string> messages = client.ReadAll(TimeSpan.FromMilliseconds(150));
            await Assert
                .That(messages.Any(m => ContainsOrdinal(m, "Host busy")))
                .IsFalse()
                .ConfigureAwait(false);
            DebuggerAction result = await actionTask.ConfigureAwait(false);
            await Assert
                .That(result.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);
            await Assert
                .That(server.GetWatchItems().Select(w => w.ExpressionCode))
                .Contains("live")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task UpdateCallStackSendsFormattedItemsOncePerChange()
        {
            Script script = BuildScript("return 1", "callstack.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
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
            await Assert.That(payload).Contains("&lt;chunk-root&gt;").ConfigureAwait(false);
            await Assert.That(payload).Contains("foo").ConfigureAwait(false);
            await Assert.That(payload).Contains("value=\"42\"").ConfigureAwait(false);

            server.Update(WatchType.CallStack, frames);
            server.Update(WatchType.VStack, frames);
        }

        [global::TUnit.Core.Test]
        public async Task RefreshCommandClearsCachedWatches()
        {
            Script script = BuildScript("return 0", "refresh.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
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
            await Assert
                .That(refreshAction.Action)
                .IsEqualTo(DebuggerAction.ActionType.HardRefresh)
                .ConfigureAwait(false);

            server.Update(WatchType.Watches, watches);
            List<string> refreshed = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "<watches")),
                DefaultTimeout
            );
            await Assert.That(refreshed.Last()).Contains("value").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PauseCommandRequestsDebuggerPause()
        {
            Script script = BuildScript("return 0", "pause.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"pause\" arg=\"\" />");
            TestWaitHelpers.SpinUntilOrThrow(
                () => server.IsPauseRequested(),
                DefaultTimeout,
                "Pause command was not processed."
            );
            await Assert.That(server.IsPauseRequested()).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task StepCommandsQueueActions()
        {
            Script script = BuildScript("return 0", "tunit-steps.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            client.SendCommand("<Command cmd=\"stepin\" arg=\"\" />");
            await Assert
                .That(server.GetAction(0, sourceRef).Action)
                .IsEqualTo(DebuggerAction.ActionType.StepIn)
                .ConfigureAwait(false);

            client.SendCommand("<Command cmd=\"stepover\" arg=\"\" />");
            await Assert
                .That(server.GetAction(0, sourceRef).Action)
                .IsEqualTo(DebuggerAction.ActionType.StepOver)
                .ConfigureAwait(false);

            client.SendCommand("<Command cmd=\"stepout\" arg=\"\" />");
            await Assert
                .That(server.GetAction(0, sourceRef).Action)
                .IsEqualTo(DebuggerAction.ActionType.StepOut)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExpiredActionsAreSkippedBeforeReturningNextEntry()
        {
            FakeTimeProvider timeProvider = new();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                TimeProvider = timeProvider,
            };
            Script script = BuildScript("return 0", "aging.lua", options);
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
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
            await Assert
                .That(action.Action)
                .IsEqualTo(DebuggerAction.ActionType.StepIn)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SignalRuntimeExceptionBroadcastsDecoratedMessage()
        {
            Script script = BuildScript("return 0", "tunit-decorated-error.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();

            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            ScriptRuntimeException exception = CreateDecoratedException(
                "failure",
                "decorated:failure"
            );

            bool shouldPause = server.SignalRuntimeException(exception);
            await Assert.That(shouldPause).IsTrue().ConfigureAwait(false);

            List<string> messages = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "decorated:failure")),
                DefaultTimeout
            );

            await Assert
                .That(messages.Any(m => ContainsOrdinal(m, "decorated:failure")))
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RefreshBreakpointsBroadcastsLocations()
        {
            Script script = BuildScript("return 0", "tunit-refresh-breakpoints.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();

            TestWaitHelpers.SpinUntilOrThrow(
                () => server.ConnectedClients == 1,
                DefaultTimeout,
                "Debugger client never registered with the server."
            );

            SourceRef breakpoint = new(0, 1, 2, 3, 4, false);
            server.RefreshBreakpoints(new[] { breakpoint });

            List<string> payloads = client.ReadUntil(
                msgs => msgs.Any(m => ContainsOrdinal(m, "<breakpoints")),
                DefaultTimeout
            );

            string last = payloads.Last();
            await Assert.That(last).Contains("srcid=\"0\"").ConfigureAwait(false);
            await Assert.That(last).Contains("lf=\"3\"").ConfigureAwait(false);
            await Assert.That(last).Contains("lt=\"4\"").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task FreeRunAfterAttachReturnsRunAction()
        {
            Script script = BuildScript("return 0", "autorun.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: true);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            client.SendCommand("<Command cmd=\"handshake\" arg=\"\" />");
            client.Drain(DefaultTimeout);

            SourceRef sourceRef = new(0, 0, 0, 1, 1, isStepStop: false);
            DebuggerAction action = server.GetAction(0, sourceRef);
            await Assert
                .That(action.Action)
                .IsEqualTo(DebuggerAction.ActionType.Run)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PolicyFileRequestRespondsWithCrossDomainPayload()
        {
            Script script = BuildScript("return 0", "policy.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            using RemoteDebuggerTestClient client = harness.CreateClient();

            client.SendCommand("<policy-file-request/>");
            List<string> policy = client.ReadUntil(
                payloads => payloads.Any(m => ContainsOrdinal(m, "cross-domain-policy")),
                DefaultTimeout
            );

            await Assert.That(policy.Last()).Contains("allow-access-from").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ConnectedClientsReflectActiveSessions()
        {
            Script script = BuildScript("return 0", "clients.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();

            TestWaitHelpers.SpinUntilOrThrow(
                () => server.ConnectedClients == 1,
                DefaultTimeout,
                "Debugger client never registered with the server."
            );
            await Assert.That(server.ConnectedClients).IsEqualTo(1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DebuggerCapsAdvertiseSourceCodeSupport()
        {
            Script script = BuildScript("return 0", "caps.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            await Assert
                .That(server.GetDebuggerCaps())
                .IsEqualTo(DebuggerCaps.CanDebugSourceCode)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SignalExecutionEndedBroadcastsCompletion()
        {
            Script script = BuildScript("return 0", "end.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();
            TestWaitHelpers.SpinUntilOrThrow(
                () => server.ConnectedClients == 1,
                DefaultTimeout,
                "Debugger client never registered with the server."
            );

            server.SignalExecutionEnded();
            List<string> payloads = client.ReadUntil(
                msgs => msgs.Any(m => ContainsOrdinal(m, "execution-completed")),
                DefaultTimeout
            );
            await Assert
                .That(payloads.Last())
                .Contains("execution-completed")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task HandshakeRespondsToPolicyRequestsBeforeCommands()
        {
            Script script = BuildScript("return 0", "state.lua");
            using RemoteDebuggerHarness harness = new(script, freeRunAfterAttach: false);
            DebugServer server = harness.Server;
            using RemoteDebuggerTestClient client = harness.CreateClient();

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

            await Assert
                .That(messages.Any(m => ContainsOrdinal(m, "<welcome")))
                .IsTrue()
                .ConfigureAwait(false);
        }

        private static ScriptRuntimeException CreateDecoratedException(
            string message,
            string decorated
        )
        {
            ScriptRuntimeException exception = new(message);
            PropertyInfo property = typeof(InterpreterException).GetProperty(
                "DecoratedMessage",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            property?.SetValue(exception, decorated);
            return exception;
        }
    }
}

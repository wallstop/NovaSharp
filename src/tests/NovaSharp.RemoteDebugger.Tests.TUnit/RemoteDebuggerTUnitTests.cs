namespace NovaSharp.RemoteDebugger.Tests.TUnit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
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

            await Assert.That(messages.Any(m => ContainsOrdinal(m, "<welcome"))).IsTrue();
            await Assert.That(messages.Any(m => ContainsOrdinal(m, "source-code"))).IsTrue();
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
            await Assert.That(refresh.Action).IsEqualTo(DebuggerAction.ActionType.HardRefresh);

            List<string> watchCodes = server.GetWatchItems().Select(w => w.ExpressionCode).ToList();
            await Assert.That(watchCodes).Contains("value");
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
            await Assert.That(refresh.Action).IsEqualTo(DebuggerAction.ActionType.HardRefresh);

            DynamicExpression watch = server.GetWatchItems().Single();
            DynValue firstValue = watch.Evaluate();
            await Assert.That(watch.IsConstant()).IsFalse();
            await Assert.That(firstValue.Type).IsEqualTo(DataType.Number);
            await Assert.That(firstValue.Number).IsEqualTo(10);

            script.Globals.Set("watched", DynValue.NewNumber(42));
            DynValue updatedValue = watch.Evaluate();
            await Assert.That(updatedValue.Number).IsEqualTo(42);
        }
    }
}

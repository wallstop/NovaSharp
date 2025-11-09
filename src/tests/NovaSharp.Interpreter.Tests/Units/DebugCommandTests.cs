namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using Commands.Implementations;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebugCommandTests
    {
        private Func<IRemoteDebuggerBridge> _originalFactory = null!;
        private Action<string> _originalLauncher = null!;

        [SetUp]
        public void SetUp()
        {
            _originalFactory = DebugCommand.DebuggerFactory;
            _originalLauncher = DebugCommand.BrowserLauncher;
        }

        [TearDown]
        public void TearDown()
        {
            DebugCommand.DebuggerFactory = _originalFactory;
            DebugCommand.BrowserLauncher = _originalLauncher;
        }

        [Test]
        public void ExecuteStartsDebuggerAndOpensBrowserOnce()
        {
            FakeDebuggerBridge bridge = new() { Url = "http://debugger" };
            int launchCount = 0;

            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = _ => launchCount++;

            DebugCommand command = new();
            ShellContext context = new(new Script());

            command.Execute(context, string.Empty);
            command.Execute(context, string.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(bridge.AttachCount, Is.EqualTo(1));
                Assert.That(launchCount, Is.EqualTo(1));
                Assert.That(bridge.LastScript, Is.SameAs(context.Script));
                Assert.That(bridge.LastScriptName, Is.EqualTo("NovaSharp REPL interpreter"));
                Assert.That(bridge.LastFreeRun, Is.False);
            });
        }

        [Test]
        public void ExecuteSkipsBrowserLaunchWhenUrlIsEmpty()
        {
            FakeDebuggerBridge bridge = new() { Url = string.Empty };
            bool launched = false;

            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = _ => launched = true;

            DebugCommand command = new();
            command.Execute(new ShellContext(new Script()), string.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(bridge.AttachCount, Is.EqualTo(1));
                Assert.That(launched, Is.False);
            });
        }

        private sealed class FakeDebuggerBridge : IRemoteDebuggerBridge
        {
            public string Url { get; set; } = string.Empty;

            public int AttachCount { get; private set; }

            public Script LastScript { get; private set; } = null!;

            public string LastScriptName { get; private set; } = string.Empty;

            public bool LastFreeRun { get; private set; }

            public void Attach(Script script, string scriptName, bool freeRunAfterAttach)
            {
                AttachCount++;
                LastScript = script;
                LastScriptName = scriptName;
                LastFreeRun = freeRunAfterAttach;
            }

            public string HttpUrlStringLocalHost
            {
                get { return Url; }
            }
        }
    }
}

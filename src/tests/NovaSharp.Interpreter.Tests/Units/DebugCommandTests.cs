namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.RemoteDebugger;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DebugCommandTests
    {
        private Func<IRemoteDebuggerBridge> _originalFactory = null!;
        private IBrowserLauncher _originalLauncher = null!;

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
            FakeDebuggerBridge bridge = new() { Url = new Uri("http://debugger/") };
            TestBrowserLauncher launcher = new();

            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = launcher;

            DebugCommand command = new();
            ShellContext context = new(new Script());

            TextWriter originalOut = Console.Out;
            Console.SetOut(TextWriter.Null);
            try
            {
                command.Execute(context, string.Empty);
                command.Execute(context, string.Empty);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Multiple(() =>
            {
                Assert.That(bridge.AttachCount, Is.EqualTo(1));
                Assert.That(launcher.LaunchCount, Is.EqualTo(1));
                Assert.That(launcher.LastUrl, Is.Not.Null);
                Assert.That(launcher.LastUrl.AbsoluteUri, Is.EqualTo("http://debugger/"));
                Assert.That(bridge.LastScript, Is.SameAs(context.Script));
                Assert.That(bridge.LastScriptName, Is.EqualTo("NovaSharp REPL interpreter"));
                Assert.That(bridge.LastFreeRun, Is.False);
            });
        }

        [Test]
        public void ExecuteSkipsBrowserLaunchWhenUrlIsEmpty()
        {
            FakeDebuggerBridge bridge = new() { Url = null };
            TestBrowserLauncher launcher = new();

            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = launcher;

            DebugCommand command = new();
            TextWriter originalOut = Console.Out;
            Console.SetOut(TextWriter.Null);
            try
            {
                command.Execute(
                    new ShellContext(CreateScript(LuaCompatibilityVersion.Lua54)),
                    string.Empty
                );
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Multiple(() =>
            {
                Assert.That(bridge.AttachCount, Is.EqualTo(1));
                Assert.That(launcher.LaunchCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void ExecuteLogsCompatibilityProfile()
        {
            FakeDebuggerBridge bridge = new() { Url = null };
            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = new TestBrowserLauncher();

            DebugCommand command = new();
            ShellContext context = new(CreateScript(LuaCompatibilityVersion.Lua52));
            StringWriter writer = new();
            TextWriter originalOut = Console.Out;
            try
            {
                Console.SetOut(writer);
                command.Execute(context, string.Empty);
            }
            finally
            {
                Console.SetOut(originalOut);
                writer.Dispose();
            }

            string expectedSummary = context.Script.CompatibilityProfile.GetFeatureSummary();
            Assert.That(
                writer.ToString(),
                Does.Contain($"[compatibility] Debugger session running under {expectedSummary}")
            );
        }

        private sealed class FakeDebuggerBridge : IRemoteDebuggerBridge
        {
            public Uri Url { get; set; }

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

            public Uri HttpUrlStringLocalHost
            {
                get { return Url; }
            }
        }

        private sealed class TestBrowserLauncher : IBrowserLauncher
        {
            public int LaunchCount { get; private set; }

            public Uri LastUrl { get; private set; }

            public void Launch(Uri url)
            {
                LaunchCount++;
                LastUrl = url;
            }
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { CompatibilityVersion = version };

            return new Script(options);
        }
    }
}

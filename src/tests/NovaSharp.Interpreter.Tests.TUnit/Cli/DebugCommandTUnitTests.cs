namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.RemoteDebugger;

    [PlatformDetectorIsolation]
    public sealed class DebugCommandTUnitTests : IDisposable
    {
        private Func<IRemoteDebuggerBridge> _originalFactory;
        private IBrowserLauncher _originalLauncher;

        public DebugCommandTUnitTests()
        {
            _originalFactory = DebugCommand.DebuggerFactory;
            _originalLauncher = DebugCommand.BrowserLauncher;
        }

        public void Dispose()
        {
            DebugCommand.DebuggerFactory = _originalFactory;
            DebugCommand.BrowserLauncher = _originalLauncher;
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteStartsDebuggerAndOpensBrowserOnce()
        {
            FakeDebuggerBridge bridge = new() { Url = new Uri("http://debugger/") };
            TestBrowserLauncher launcher = new();
            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = launcher;

            DebugCommand command = new();
            ShellContext context = new(new Script());

            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: false);
                    command.Execute(context, string.Empty);
                    command.Execute(context, string.Empty);

                    await Assert.That(bridge.AttachCount).IsEqualTo(1).ConfigureAwait(false);
                    await Assert.That(launcher.LaunchCount).IsEqualTo(1).ConfigureAwait(false);
                    await Assert
                        .That(launcher.LastUrl?.AbsoluteUri)
                        .IsEqualTo("http://debugger/")
                        .ConfigureAwait(false);
                    await Assert
                        .That(ReferenceEquals(bridge.LastScript, context.Script))
                        .IsTrue()
                        .ConfigureAwait(false);
                    await Assert
                        .That(bridge.LastScriptName)
                        .IsEqualTo("NovaSharp REPL interpreter")
                        .ConfigureAwait(false);
                    await Assert.That(bridge.LastFreeRun).IsFalse().ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteSkipsBrowserLaunchWhenUrlIsEmpty()
        {
            FakeDebuggerBridge bridge = new() { Url = null };
            TestBrowserLauncher launcher = new();
            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = launcher;

            DebugCommand command = new();

            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: false);
                    command.Execute(
                        new ShellContext(CreateScript(LuaCompatibilityVersion.Lua54)),
                        string.Empty
                    );

                    await Assert.That(bridge.AttachCount).IsEqualTo(1).ConfigureAwait(false);
                    await Assert.That(launcher.LaunchCount).IsEqualTo(0).ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteLogsCompatibilityProfile()
        {
            FakeDebuggerBridge bridge = new() { Url = null };
            DebugCommand.DebuggerFactory = () => bridge;
            DebugCommand.BrowserLauncher = new TestBrowserLauncher();

            DebugCommand command = new();
            ShellContext context = new(CreateScript(LuaCompatibilityVersion.Lua52));

            await ConsoleCaptureCoordinator
                .RunAsync(async () =>
                {
                    using ConsoleCaptureScope consoleScope = new(captureError: false);
                    command.Execute(context, string.Empty);

                    string expectedSummary =
                        context.Script.CompatibilityProfile.GetFeatureSummary();
                    await Assert
                        .That(consoleScope.Writer.ToString())
                        .Contains(
                            $"[compatibility] Debugger session running under {expectedSummary}"
                        )
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { CompatibilityVersion = version };
            return new Script(options);
        }

        private sealed class FakeDebuggerBridge : IRemoteDebuggerBridge
        {
            public Uri Url { get; set; }

            public int AttachCount { get; private set; }

            public Script LastScript { get; private set; }

            public string LastScriptName { get; private set; } = string.Empty;

            public bool LastFreeRun { get; private set; }

            public void Attach(Script script, string scriptName, bool freeRunAfterAttach)
            {
                AttachCount++;
                LastScript = script;
                LastScriptName = scriptName;
                LastFreeRun = freeRunAfterAttach;
            }

            public Uri HttpUrlStringLocalHost => Url;
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
    }
}

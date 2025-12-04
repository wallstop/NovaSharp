namespace NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.RemoteDebugger;
    using NovaSharp.Tests.TestInfrastructure.Scopes;
    using static NovaSharp.Interpreter.Tests.TUnit.Cli.CliTestHelpers;

    public sealed class DebugCommandTUnitTests
    {
        private static readonly SemaphoreSlim DebugCommandGate = new(1, 1);

        [global::TUnit.Core.Test]
        public async Task ExecuteAttachesDebuggerAndLaunchesBrowser()
        {
            SemaphoreSlimLease gateLease = await SemaphoreSlimScope
                .WaitAsync(DebugCommandGate)
                .ConfigureAwait(false);
            await using ConfiguredAsyncDisposable gateLeaseScope = gateLease.ConfigureAwait(false);

            Script script = CreateScript();
            ShellContext context = CreateShellContext(script);
            DebugCommand command = new();

            StubDebuggerBridge bridge = new();
            StubBrowserLauncher launcher = new();

            using (
                DebugCommandScope debugCommandScope = DebugCommandScope.Override(
                    () => bridge,
                    launcher
                )
            )
            {
                command.Execute(context, arguments: string.Empty);

                await Assert.That(bridge.AttachCount).IsEqualTo(1).ConfigureAwait(false);
                await Assert
                    .That(bridge.AttachedScript)
                    .IsSameReferenceAs(script)
                    .ConfigureAwait(false);
                await Assert
                    .That(bridge.AttachedName)
                    .IsEqualTo("NovaSharp REPL interpreter")
                    .ConfigureAwait(false);
                await Assert.That(bridge.FreeRunAfterAttach).IsFalse().ConfigureAwait(false);

                await Assert.That(launcher.LaunchCount).IsEqualTo(1).ConfigureAwait(false);
                await Assert
                    .That(launcher.LastUrl)
                    .IsEqualTo(bridge.HttpUrlStringLocalHost)
                    .ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task ExecuteDoesNotReattachAfterFirstInvocation()
        {
            SemaphoreSlimLease gateLease = await SemaphoreSlimScope
                .WaitAsync(DebugCommandGate)
                .ConfigureAwait(false);
            await using ConfiguredAsyncDisposable gateLeaseScope = gateLease.ConfigureAwait(false);

            Script script = CreateScript();
            ShellContext context = CreateShellContext(script);
            DebugCommand command = new();

            StubDebuggerBridge bridge = new();
            StubBrowserLauncher launcher = new();

            using (
                DebugCommandScope debugCommandScope = DebugCommandScope.Override(
                    () => bridge,
                    launcher
                )
            )
            {
                command.Execute(context, arguments: string.Empty);
                command.Execute(context, arguments: string.Empty);

                await Assert.That(bridge.AttachCount).IsEqualTo(1).ConfigureAwait(false);
                await Assert.That(launcher.LaunchCount).IsEqualTo(1).ConfigureAwait(false);
            }
        }

        private sealed class StubDebuggerBridge : IRemoteDebuggerBridge
        {
            private readonly Uri _httpUrl = new("http://127.0.0.1:41000/");

            public int AttachCount { get; private set; }

            public Script AttachedScript { get; private set; }

            public string AttachedName { get; private set; }

            public bool FreeRunAfterAttach { get; private set; }

            public void Attach(Script script, string scriptName, bool freeRunAfterAttach)
            {
                AttachCount++;
                AttachedScript = script;
                AttachedName = scriptName;
                FreeRunAfterAttach = freeRunAfterAttach;
            }

            public Uri HttpUrlStringLocalHost
            {
                get { return _httpUrl; }
            }
        }

        private sealed class StubBrowserLauncher : IBrowserLauncher
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

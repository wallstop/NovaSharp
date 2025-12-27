namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Cli
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Cli;
    using WallstopStudios.NovaSharp.Cli.Commands.Implementations;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.RemoteDebugger;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;
    using static NovaSharp.Interpreter.Tests.TUnit.Cli.CliTestHelpers;

    public sealed class DebugCommandTUnitTests
    {
        private static readonly SemaphoreSlim DebugCommandGate = new(1, 1);

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ExecuteAttachesDebuggerAndLaunchesBrowser(LuaCompatibilityVersion version)
        {
            SemaphoreSlimLease gateLease = await SemaphoreSlimScope
                .WaitAsync(DebugCommandGate)
                .ConfigureAwait(false);
            await using ConfiguredAsyncDisposable gateLeaseScope = gateLease.ConfigureAwait(false);

            Script script = CreateScript(version);
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
        [AllLuaVersions]
        public async Task ExecuteDoesNotReattachAfterFirstInvocation(
            LuaCompatibilityVersion version
        )
        {
            SemaphoreSlimLease gateLease = await SemaphoreSlimScope
                .WaitAsync(DebugCommandGate)
                .ConfigureAwait(false);
            await using ConfiguredAsyncDisposable gateLeaseScope = gateLease.ConfigureAwait(false);

            Script script = CreateScript(version);
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
